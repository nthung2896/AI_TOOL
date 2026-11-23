using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Hinet.Api;

public static class RentalDesktop
{
    public static async Task RunAsync(string[] args)
    {
        string? Argument(string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        var dataPath = Argument("--rental-data") ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "rental-desktop");
        var port = int.TryParse(Argument("--rental-port"), out var customPort) ? customPort : 5112;
        if (port < 1024 || port > 65535) throw new ArgumentException("Port phải từ 1024 đến 65535.");
        using var store = new RentalLocalStore(dataPath);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>(), WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot") });
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(o => o.SingleLine = true);
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        var url = $"http://127.0.0.1:{port}";
        builder.Configuration["Kestrel:Endpoints:Http:Url"] = url;
        builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = RentalMedia.MaxRequestBytes);
        builder.Logging.AddFilter("System.Net.Http.HttpClient.RentalExternal", LogLevel.None);
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(new RentalMedia(Path.Combine(store.DirectoryPath, "images")));
        builder.Services.AddSingleton<IOptions<RentalOptions>>(Options.Create(store.Options));
        builder.Services.AddHttpClient("RentalExternal", c => c.Timeout = TimeSpan.FromSeconds(90));
        builder.Services.AddSingleton<RentalAi>();
        builder.Services.AddSingleton<RentalFacebook>();
        builder.Services.AddSingleton<RentalDesktopEngine>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<RentalDesktopEngine>());
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
            if (context.Request.Host.Host is not ("127.0.0.1" or "localhost")) { context.Response.StatusCode = 403; return; }
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var key = context.Request.Headers["X-Rental-Key"].ToString();
                if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(key)), SHA256.HashData(Encoding.UTF8.GetBytes(store.Options.ManagementKey))))
                { context.Response.StatusCode = 401; return; }
                var origin = context.Request.Headers.Origin.ToString();
                if (origin.Length > 0 && origin != url && origin != $"http://localhost:{port}") { context.Response.StatusCode = 403; return; }
            }
            try { await next(); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or ValidationException)
            { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { error = ex.Message }); }
            catch (Exception)
            { context.Response.StatusCode = 500; await context.Response.WriteAsJsonAsync(new { error = "Thao tác chưa hoàn tất. Kiểm tra kết nối và cấu hình; dữ liệu đã lưu vẫn được giữ." }); }
        });
        app.UseStaticFiles();
        app.MapGet("/", () => Results.Redirect("/rental-desktop/"));
        app.MapGet("/rental-desktop/", () => Results.File(Path.Combine(AppContext.BaseDirectory, "wwwroot", "rental-desktop", "index.html"), "text/html; charset=utf-8"));
        app.MapGet("/rental-media/{id}", (string id, RentalMedia media) =>
        {
            try { return Results.File(Path.GetFullPath(media.Resolve(id)), RentalMedia.ContentType(id)); }
            catch (ArgumentException) { return Results.NotFound(); }
        });
        var api = app.MapGroup("/api/rental-assistant");
        api.MapPost("/images", async (HttpRequest request, RentalMedia media, CancellationToken ct) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest("Chọn ảnh để tải lên.");
            return Results.Ok(await media.Upload((await request.ReadFormAsync(ct)).Files, ct));
        });
        api.MapGet("/overview", (RentalDesktopEngine engine, RentalFacebook browser) =>
        {
            var data = store.Read();
            return Results.Ok(new { data.State, data.Rooms, data.Groups, data.Threads, posts = data.Posts.Take(100), deliveries = data.Deliveries.TakeLast(200), appointments = data.Appointments.OrderByDescending(a => a.VisitAt).Take(200),
                enabled = store.Options.Enabled, browserEnabled = store.Options.BrowserEnabled, browser = browser.Status, aiMode = store.Options.AiMode,
                aiConfigured = store.Options.AiMode == "template" || (!string.IsNullOrWhiteSpace(store.Options.AiModel) && (store.Options.AiMode == "local" || !string.IsNullOrWhiteSpace(store.Options.AiApiKey))),
                telegramConfigured = store.Options.TelegramOwnerId > 0 && !string.IsNullOrWhiteSpace(store.Options.TelegramToken),
                selectorsConfigured = !string.IsNullOrWhiteSpace(store.Options.IncomingMessages) && !string.IsNullOrWhiteSpace(store.Options.OutgoingMessages),
                storage = "LocalJson", engine.LastError, engine.LastCycleAt });
        });
        api.MapGet("/settings", () => new
        {
            store.Options.Enabled, store.Options.TelegramOwnerId, telegramTokenSaved = !string.IsNullOrWhiteSpace(store.Options.TelegramToken),
            store.Options.AiMode, store.Options.AiBaseUrl, store.Options.AiModel, aiKeySaved = !string.IsNullOrWhiteSpace(store.Options.AiApiKey),
            store.Options.BrowserEnabled, store.Options.AutoReplyNewConversations, store.Options.PostIntervalSeconds, store.Options.InboxIntervalSeconds,
            store.Options.ComposeButton, store.Options.PostEditor, store.Options.PublishButton, store.Options.IncomingMessages,
            store.Options.OutgoingMessages, store.Options.MessageIdAttribute, store.Options.MessageEditor
        });
        api.MapPut("/settings", async (DesktopSettings input, RentalDesktopEngine engine, CancellationToken ct) => await engine.Exclusive(async () =>
        {
            if (input.AiMode is not ("template" or "api" or "local")) throw new ArgumentException("Chọn chế độ nội dung hợp lệ.");
            if (input.TelegramOwnerId < 0) throw new ArgumentException("Telegram ID không hợp lệ.");
            if (!Uri.TryCreate(input.AiBaseUrl, UriKind.Absolute, out var endpoint) || !input.AiBaseUrl.EndsWith('/') ||
                (endpoint.Scheme != "https" && !(input.AiMode == "local" && endpoint.Scheme == "http" && endpoint.IsLoopback))) throw new ArgumentException("Địa chỉ AI cần HTTPS và kết thúc bằng /. AI local cho phép HTTP loopback.");
            if (input.PostIntervalSeconds < 60 || input.InboxIntervalSeconds < 30) throw new ArgumentException("Khoảng đăng tối thiểu 60 giây, kiểm tra inbox tối thiểu 30 giây.");
            store.Update(d => { d.State.Paused = true; return true; });
            store.Configure(o =>
            {
                o.Enabled = input.Enabled; o.TelegramOwnerId = input.TelegramOwnerId;
                if (input.TelegramToken?.Trim().Length > 0) o.TelegramToken = input.TelegramToken.Trim();
                if (input.ClearTelegramToken) o.TelegramToken = "";
                o.AiMode = input.AiMode; o.AiBaseUrl = input.AiBaseUrl; o.AiModel = input.AiModel ?? "";
                if (input.AiApiKey?.Trim().Length > 0) o.AiApiKey = input.AiApiKey.Trim();
                if (input.ClearAiApiKey) o.AiApiKey = "";
                o.BrowserEnabled = input.BrowserEnabled; o.AutoReplyNewConversations = input.AutoReplyNewConversations;
                o.PostIntervalSeconds = input.PostIntervalSeconds; o.InboxIntervalSeconds = input.InboxIntervalSeconds;
                o.ComposeButton = input.ComposeButton ?? o.ComposeButton; o.PostEditor = input.PostEditor ?? o.PostEditor; o.PublishButton = input.PublishButton ?? o.PublishButton;
                o.IncomingMessages = input.IncomingMessages ?? ""; o.OutgoingMessages = input.OutgoingMessages ?? "";
                o.MessageIdAttribute = input.MessageIdAttribute ?? "data-message-id"; o.MessageEditor = input.MessageEditor ?? o.MessageEditor;
            });
            await Task.CompletedTask;
            return Results.Ok(new { message = "Đã lưu. Trợ lý đang tạm dừng; bật lại khi sẵn sàng." });
        }, ct));
        api.MapPut("/state", (PauseInput input) => { store.Update(d => { d.State.Paused = input.Paused; return true; }); return Results.Ok(); });
        api.MapPut("/rooms/{id:guid}", (Guid id, RentalRoom input) =>
        {
            Validate(input); app.Services.GetRequiredService<RentalMedia>().ValidateImages(input.Listing.Images); input.Id = id;
            return store.Update(d => { d.Rooms.RemoveAll(r => r.Id == id); d.Rooms.Add(input); return input; });
        });
        api.MapPut("/groups/{id:guid}", (Guid id, RentalGroup input) =>
        {
            Validate(input);
            if (!RentalFacebook.ValidUrl(input.Url, true)) throw new ArgumentException("Dùng link https://www.facebook.com/groups/ID, không có tham số.");
            input.Id = id; input.Url = RentalFacebook.CanonicalUrl(input.Url);
            return store.Update(d => { if (d.Groups.Any(g => g.Id != id && g.Url == input.Url)) throw new ArgumentException("Nhóm đã có."); d.Groups.RemoveAll(g => g.Id == id); d.Groups.Add(input); return input; });
        });
        api.MapPut("/threads/{id:guid}", (Guid id, RentalThread input) =>
        {
            Validate(input);
            if (!RentalFacebook.ValidUrl(input.Url, false)) throw new ArgumentException("Dùng link https://www.facebook.com/messages/t/ID, không có tham số.");
            input.Url = RentalFacebook.CanonicalUrl(input.Url);
            return store.Update(d =>
            {
                if (d.Threads.Any(t => t.Id != id && t.Url == input.Url)) throw new ArgumentException("Hội thoại đã có.");
                var thread = d.Threads.SingleOrDefault(t => t.Id == id);
                if (thread is null) { thread = new() { Id = id }; d.Threads.Add(thread); }
                if (thread.Url != input.Url) { thread.LastMessageId = ""; thread.Status = "New"; thread.PendingAppointment = ""; }
                thread.Url = input.Url; thread.CustomerName = input.CustomerName; thread.AutoReply = input.AutoReply;
                if (thread.Status == "NeedsReview") thread.Status = "Watching";
                return thread;
            });
        });
        api.MapPost("/posts", async (DraftInput input, RentalDesktopEngine engine, CancellationToken ct) => await engine.Exclusive(() => engine.Draft(input.RoomId, input.Instruction ?? "", null, ct), ct));
        api.MapPut("/posts/{id:guid}", (Guid id, ContentInput input) => store.Update(d =>
        {
            if (string.IsNullOrWhiteSpace(input.Content) || input.Content.Length > 10000) throw new ArgumentException("Nội dung từ 1 đến 10000 ký tự.");
            var post = d.Posts.SingleOrDefault(p => p.Id == id) ?? throw new ArgumentException("Không tìm thấy bài.");
            if (post.Status != "Draft") throw new ArgumentException("Chỉ sửa bản nháp."); post.Content = input.Content; return post;
        }));
        api.MapPost("/posts/{id:guid}/approve", (Guid id, RentalDesktopEngine engine) => { engine.Approve(id); return Results.Ok(); });
        api.MapPost("/deliveries/{id:guid}/cancel", (Guid id) => store.Update(d =>
        { var delivery = d.Deliveries.Single(x => x.Id == id); if (delivery.Status != "Queued") throw new ArgumentException("Chỉ hủy lượt chờ."); delivery.Status = "Cancelled"; return delivery; }));
        api.MapPost("/browser/open", async (RentalDesktopEngine engine, RentalFacebook browser, CancellationToken ct) => await engine.Exclusive(async () => { await browser.OpenLogin(); return Results.Ok(new { message = "Đăng nhập trong cửa sổ Chromium vừa mở." }); }, ct));
        api.MapPost("/browser/check", async (UrlInput input, RentalDesktopEngine engine, RentalFacebook browser, CancellationToken ct) => await engine.Exclusive(async () => Results.Ok(await browser.CheckSelectors(input.Url)), ct));
        api.MapPost("/groups/search", async (QueryInput input, RentalDesktopEngine engine, RentalFacebook browser, CancellationToken ct) => await engine.Exclusive(async () =>
        {
            if (string.IsNullOrWhiteSpace(input.Query) || input.Query.Length > 200) throw new ArgumentException("Nhập từ khóa tìm nhóm.");
            var groups = await browser.SearchGroups(input.Query);
            store.Update(d => { foreach (var g in groups) if (!d.Groups.Any(x => x.Url == g.Url)) d.Groups.Add(g); return true; });
            return Results.Ok(new { count = groups.Count });
        }, ct));
        api.MapPost("/telegram/test", async (RentalDesktopEngine engine, CancellationToken ct) => await engine.Exclusive(async () => Results.Ok(new { username = await engine.TestTelegram(ct) }), ct));
        api.MapPost("/command", async (CommandInput input, RentalDesktopEngine engine, CancellationToken ct) => await engine.Exclusive(async () => Results.Ok(new { reply = await engine.Command(input.Text ?? "", null, ct) }), ct));
        api.MapPost("/chat-preview", async (CommandInput input, RentalAi ai, CancellationToken ct) =>
            Results.Ok(await ai.Answer(store.Read().Rooms, "Khách: " + (input.Text ?? ""), ct)));
        api.MapPost("/appointments", (RentalAppointment input) =>
        {
            if (!store.Read().Rooms.Any(r => r.Id == input.RoomId && r.Available) || string.IsNullOrWhiteSpace(input.CustomerName) ||
                !System.Text.RegularExpressions.Regex.IsMatch(input.Phone ?? "", @"^(\+84|0)\d{9}$") || input.VisitAt <= DateTimeOffset.UtcNow) throw new ArgumentException("Kiểm tra phòng, tên khách, điện thoại và giờ hẹn tương lai.");
            input.Id = Guid.NewGuid(); input.Status = "OwnerCreated";
            return store.Update(d => { d.Appointments.Insert(0, input); return input; });
        });
        api.MapPut("/appointments/{id:guid}/status", (Guid id, StatusInput input) => store.Update(d =>
        { if (input.Status is not ("Completed" or "Cancelled")) throw new ArgumentException("Trạng thái không hợp lệ."); var a = d.Appointments.Single(x => x.Id == id); a.Status = input.Status; return a; }));
        api.MapGet("/backup", () => Results.File(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(store.Read(), RentalLocalStore.Json), "application/json", $"AI_TOOL-{DateTime.Now:yyyyMMdd-HHmm}.json"));
        await app.StartAsync();
        Console.WriteLine($"AI TOOL: {url}/rental-desktop/ | Data: {store.DirectoryPath}");
        if (args.Contains("--open"))
        {
            try { Process.Start(new ProcessStartInfo(url + "/rental-desktop/#key=" + store.Options.ManagementKey) { UseShellExecute = true }); }
            catch { Console.WriteLine("Open the address above. Your management key is in the local settings.json file."); }
        }
        await app.WaitForShutdownAsync();
        await app.DisposeAsync();
    }
    private static void Validate(object model)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), errors, true)) throw new ValidationException(string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    public record PauseInput(bool Paused);
    public record DraftInput(Guid RoomId, string? Instruction);
    public record ContentInput(string Content);
    public record UrlInput(string Url);
    public record QueryInput(string Query);
    public record CommandInput(string? Text);
    public record StatusInput(string Status);
    public record DesktopSettings(bool Enabled, long TelegramOwnerId, string? TelegramToken, bool ClearTelegramToken, string AiMode,
        string AiBaseUrl, string? AiModel, string? AiApiKey, bool ClearAiApiKey, bool BrowserEnabled, bool AutoReplyNewConversations,
        int PostIntervalSeconds, int InboxIntervalSeconds, string? ComposeButton, string? PostEditor, string? PublishButton,
        string? IncomingMessages, string? OutgoingMessages, string? MessageIdAttribute, string? MessageEditor);
}
