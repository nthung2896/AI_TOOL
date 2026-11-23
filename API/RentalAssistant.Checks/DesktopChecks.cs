using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hinet.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

public static class DesktopChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var dir = Path.Combine(Path.GetTempPath(), "AI_TOOL_checks", Guid.NewGuid().ToString("N"));
        var room = new RentalRoom { Name = "Phòng A", Address = "123 Đường A", MonthlyRent = 3000000, Details = "Có máy lạnh", Available = true };
        var thread = new RentalThread { CustomerName = "Khách thử", Url = "https://www.facebook.com/messages/t/123", Status = "Watching" };
        using (var store = new RentalLocalStore(dir))
        {
            var options = Options.Create(store.Options);
            var ai = new RentalAi(new Factory(new AiHandler()), options);
            await using var browser = new RentalFacebook(options, new RentalMedia(Path.Combine(dir, "images")));
            using var engine = new RentalDesktopEngine(store, ai, browser, NullLogger<RentalDesktopEngine>.Instance);
            check(store.Options.AiMode == "template" && store.Options.ManagementKey.Length >= 32, "desktop starts with free templates and generated key");
            store.Update(d => { d.Rooms.Add(room); d.Threads.Add(thread); return true; });
            var draft = await engine.Draft(room.Id, "Bịa giá 1 đồng", null, default);
            check(draft.Content.Contains("3.000.000") && draft.Content.Contains(room.Address) && !draft.Content.Contains("1 đồng"), "offline draft uses real room data without AI/network");
            check(RentalTemplates.Answer(new() { room }, "Khách: Hẹn xem | Phòng A | An | sai-so | 01/01/2000 10:00").Appointment is null, "offline booking rejects invalid phone and past date");
            var copy = store.Read(); copy.Rooms.Clear();
            check(store.Read().Rooms.Count == 1, "local snapshots cannot mutate stored records");
            var future = DateTimeOffset.UtcNow.AddDays(2).ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm");
            var proposal = await engine.PrepareReply(thread, new("m1", $"Hẹn xem | Phòng A | Nguyễn An | 0912345678 | {future}", ""), default);
            check(proposal.Booking is null && store.Read().Appointments.Count == 0 && !string.IsNullOrEmpty(store.Read().Threads[0].PendingAppointment), "offline booking requires explicit customer confirmation");
            thread = store.Read().Threads[0];
            var confirmed = await engine.PrepareReply(thread, new("m2", "xác nhận", ""), default);
            check(confirmed.Booking?.Phone == "0912345678" && store.Read().Appointments.Count == 1, "confirmed offline booking persists customer and visit");
            bool duplicate = false;
            try { await engine.PrepareReply(store.Read().Threads[0], new("m2", "xác nhận", ""), default); }
            catch (InvalidOperationException) { duplicate = true; }
            check(duplicate && store.Read().Appointments.Count == 1, "duplicate inbound ID cannot create another booking");
            store.Update(d => { d.State.Paused = false; d.Deliveries.Add(new() { Status = "Sending" }); return true; });
        }
        using (var reopened = new RentalLocalStore(dir))
        {
            check(reopened.Read().Rooms.Count == 1 && reopened.Read().Appointments.Count == 1, "customer data survives app restart");
            check(reopened.Read().State.Paused && reopened.Read().Deliveries[0].Status == "NeedsReview" && !reopened.Read().Threads[0].AutoReply, "restart pauses work and quarantines interrupted sends");
            check(File.Exists(Path.Combine(dir, "data.json.backup")), "previous data snapshot kept as backup");
            bool locked = false;
            try { using var second = new RentalLocalStore(dir); } catch (IOException) { locked = true; }
            check(locked, "second instance cannot access same local data");
        }
    }

    public static async Task Smoke(string baseUrl, string directory, Action<bool, string> check)
    {
        var settings = JsonSerializer.Deserialize<RentalOptions>(File.ReadAllText(Path.Combine(directory, "settings.json")), RentalLocalStore.Json)!;
        using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var noKey = await http.GetAsync("/api/rental-assistant/overview");
        check(noKey.StatusCode == HttpStatusCode.Unauthorized, "running desktop API rejects missing key");
        http.DefaultRequestHeaders.Add("X-Rental-Key", settings.ManagementKey);
        using var staticPage = await http.GetAsync("/rental-desktop/");
        check(staticPage.IsSuccessStatusCode && (await staticPage.Content.ReadAsStringAsync()).Contains("app.js"), "standalone UI served without npm");
        var overview = await http.GetFromJsonAsync<JsonElement>("/api/rental-assistant/overview");
        check(overview.GetProperty("storage").GetString() == "LocalJson", "running app uses durable local storage without SQL");
        var update = new RentalDesktop.DesktopSettings(false, 12345678, "private-telegram-test", false, "template", "https://api.openai.com/v1/", "", "private-ai-test", false, false, false, 300, 60, null, null, null, "", "", null, null);
        using var saved = await http.PutAsJsonAsync("/api/rental-assistant/settings", update);
        check(saved.IsSuccessStatusCode, "settings can be saved from UI API");
        var sanitized = await http.GetStringAsync("/api/rental-assistant/settings");
        check(!sanitized.Contains("private-telegram-test") && !sanitized.Contains("private-ai-test") && sanitized.Contains("telegramTokenSaved"), "settings API never returns saved secret values");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } });
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        await page.GotoAsync(baseUrl + "/rental-desktop/#key=" + settings.ManagementKey);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Tổng quan", Exact = true }).WaitForAsync();
        check(!page.Url.Contains(settings.ManagementKey), "launch key removed from browser address");
        await page.Locator("nav [data-action=tab][data-id=rooms]").ClickAsync();
        var name = "Phòng kiểm tra " + Guid.NewGuid().ToString("N")[..5];
        await page.GetByLabel("Tên phòng", new() { Exact = true }).FillAsync(name);
        await page.GetByLabel("Địa chỉ", new() { Exact = true }).FillAsync("123 Đường Kiểm Tra, Quận 7");
        await page.GetByLabel("Giá thuê / tháng (VNĐ)", new() { Exact = true }).FillAsync("3500000");
        await page.GetByLabel("Tiện ích, điện nước, cọc, lưu ý và giờ xem phòng").FillAsync("Có máy lạnh, giờ xem 18:00–20:00.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Lưu phòng", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true }).WaitForAsync();
        check(true, "browser creates room through form and renders saved record");
        await page.Locator("nav [data-action=tab][data-id=posts]").ClickAsync();
        await page.Locator("select[name=roomId]").SelectOptionAsync(new SelectOptionValue { Label = name });
        await page.GetByRole(AriaRole.Button, new() { Name = "Tạo bản nháp", Exact = true }).ClickAsync();
        var postCard = page.Locator("article.ra-card").Filter(new() { Has = page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true }) });
        await postCard.WaitForAsync();
        check((await postCard.Locator(".ra-pre").InnerTextAsync()).Contains("3.500.000"), "browser generates offline draft with real price");
        await postCard.GetByRole(AriaRole.Button, new() { Name = "Sửa / dán bài ChatGPT", Exact = true }).ClickAsync();
        await page.Locator(".ra-dialog").WaitForAsync();
        await page.GetByLabel("Nội dung", new() { Exact = true }).FillAsync("Bài từ ChatGPT Plus — phòng trọ 3,5 triệu, nhắn tin để xem phòng.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Lưu bản nháp", Exact = true }).ClickAsync();
        await page.GetByText("Bài từ ChatGPT Plus — phòng trọ 3,5 triệu, nhắn tin để xem phòng.", new() { Exact = true }).WaitForAsync();
        check(true, "browser can paste and save content written using ChatGPT Plus");
        await page.Locator("nav [data-action=tab][data-id=preview]").ClickAsync();
        await page.Locator("form[data-form=preview]").GetByRole(AriaRole.Button, new() { Name = "Thử trả lời", Exact = true }).ClickAsync();
        await page.Locator(".ra-chat-answer").WaitForAsync();
        check((await page.Locator(".ra-chat-answer").InnerTextAsync()).Contains("3.500.000"), "offline chat preview works without AI token");
        await page.Locator("nav [data-action=tab][data-id=overview]").ClickAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "desktop.png"), FullPage = true });
        await page.SetViewportSizeAsync(390, 844);
        check(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth"), "mobile layout has no horizontal overflow");
        check(errors.Count == 0, "desktop browser has no JavaScript errors");
        var latest = await http.GetFromJsonAsync<JsonElement>("/api/rental-assistant/overview");
        var roomId = latest.GetProperty("rooms").EnumerateArray().First(r => r.GetProperty("name").GetString() == name).GetProperty("id").GetGuid();
        var postId = latest.GetProperty("posts")[0].GetProperty("id").GetGuid();
        using var groupSaved = await http.PutAsJsonAsync("/api/rental-assistant/groups/" + Guid.NewGuid(), new RentalGroup { Name = "Nhóm kiểm tra", Url = "https://www.facebook.com/groups/" + DateTime.UtcNow.Ticks, Enabled = true });
        check(groupSaved.IsSuccessStatusCode, "chosen group saved without contacting Facebook");
        using var approved = await http.PostAsync($"/api/rental-assistant/posts/{postId}/approve", null);
        using var approvedAgain = await http.PostAsync($"/api/rental-assistant/posts/{postId}/approve", null);
        latest = await http.GetFromJsonAsync<JsonElement>("/api/rental-assistant/overview");
        var expectedGroups = latest.GetProperty("groups").EnumerateArray().Count(g => g.GetProperty("enabled").GetBoolean());
        check(approved.IsSuccessStatusCode && approvedAgain.IsSuccessStatusCode && latest.GetProperty("deliveries").EnumerateArray().Count(d => d.GetProperty("postId").GetGuid() == postId) == expectedGroups, "approval is idempotent and does not send while paused");
        using var appointment = await http.PostAsJsonAsync("/api/rental-assistant/appointments", new RentalAppointment { RoomId = roomId, CustomerName = "Khách kiểm tra", Phone = "0912345678", VisitAt = DateTimeOffset.UtcNow.AddDays(1) });
        check(appointment.IsSuccessStatusCode, "manual appointment works without Facebook or AI");
    }
}
