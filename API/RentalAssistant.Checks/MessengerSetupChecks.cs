using Hinet.Api;
using Microsoft.Playwright;
using System.Text.Json;

public static class MessengerSetupChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        var root = Path.GetFullPath("API/Hinet.Api/wwwroot/rental-desktop/messenger-setup.js");
        var script = await File.ReadAllTextAsync(root);
        var html = "<div class='chat' style='margin-top:160px'><div class='incoming' data-message-id='m1'>Tin khách một</div><div class='outgoing' data-message-id='m2'>Tin bạn một</div><div class='incoming' data-message-id='m3'>Tin khách hai</div><div class='outgoing' data-message-id='m4'>Tin bạn hai</div></div>";
        await page.SetContentAsync(html);
        await page.EvaluateAsync(script);
        await page.GetByText("Tin khách hai", new() { Exact = true }).ClickAsync();
        await page.GetByText("Tin bạn hai", new() { Exact = true }).ClickAsync();
        var json = await page.EvaluateAsync<string>("JSON.stringify(window.__rentalMessengerSetup.result)");
        var result = JsonSerializer.Deserialize<MessengerSetupResult>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        check(result.Error == "" && result.IncomingId == "m3" && result.OutgoingId == "m4", "guided setup captures the two explicitly labeled message samples");
        check(await page.Locator(result.IncomingMessages).CountAsync() == 2 && await page.Locator(result.OutgoingMessages).CountAsync() == 2, "guided selectors match message direction rather than only the clicked message");
        await page.SetContentAsync(html);
        await RentalFacebook.VerifyMessengerSelectors(page, result);
        check(true, "setup selectors and exact sample IDs survive reloading the conversation");
        await page.EvaluateAsync("document.querySelector('.chat').insertAdjacentHTML('beforeend','<div class=\"incoming\" data-message-id=\"m5\">Tin mới</div>')");
        var messages = await RentalFacebook.ReadIncomingFromPage(page, result.IncomingMessages, result.OutgoingMessages, result.MessageIdAttribute);
        check(messages.Last().Id == "m5", "new incoming messages are recognized after setup");
        var invalid = result with { OutgoingMessages = result.IncomingMessages };
        bool rejected = false;
        try { await RentalFacebook.VerifyMessengerSelectors(page, invalid); } catch (InvalidOperationException) { rejected = true; }
        check(rejected, "overlapping directions cannot pass setup verification");
        var nestedHtml = "<main style='margin-top:160px'><div data-message-id='n1'><div class='bubble left'>Khách mẫu</div></div><div data-message-id='n2'><div class='bubble right'>Bạn mẫu</div></div><div data-message-id='n3'><div class='bubble left'>Khách tiếp</div></div><div data-message-id='n4'><div class='bubble right'>Bạn tiếp</div></div></main>";
        await page.SetContentAsync(nestedHtml);
        await page.EvaluateAsync(script);
        await page.GetByText("Khách mẫu", new() { Exact = true }).ClickAsync();
        await page.GetByText("Bạn mẫu", new() { Exact = true }).ClickAsync();
        var nested = JsonSerializer.Deserialize<MessengerSetupResult>(await page.EvaluateAsync<string>("JSON.stringify(window.__rentalMessengerSetup.result)"), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        check(nested.Error == "" && await page.Locator(nested.IncomingMessages).CountAsync() == 2 && await page.Locator(nested.OutgoingMessages).CountAsync() == 2, "direction below the message ID recognizes other messages in both directions");
        await page.SetContentAsync(nestedHtml);
        await RentalFacebook.VerifyMessengerSelectors(page, nested);
        var nestedMessages = await RentalFacebook.ReadIncomingFromPage(page, nested.IncomingMessages, nested.OutgoingMessages, nested.MessageIdAttribute);
        check(nestedMessages.Select(m => m.Id).SequenceEqual(new[] { "n1", "n3" }), "nested direction survives reload and excludes sent messages");
        await page.SetContentAsync("<main style='margin-top:160px'><div data-message-id='s1'><div style='align-items: flex-start'>Khách layout</div></div><div data-message-id='s2'><div style='align-items: flex-end'>Bạn layout</div></div></main>");
        await page.EvaluateAsync(script);
        await page.GetByText("Khách layout", new() { Exact = true }).ClickAsync();
        await page.GetByText("Bạn layout", new() { Exact = true }).ClickAsync();
        var layout = JsonSerializer.Deserialize<MessengerSetupResult>(await page.EvaluateAsync<string>("JSON.stringify(window.__rentalMessengerSetup.result)"), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        check(layout.Error == "", "inline direction layout can distinguish otherwise identical wrappers");
        await RentalFacebook.VerifyMessengerSelectors(page, layout);
        await page.SetContentAsync("<main style='margin-top:160px'><div class='in' data-message-id='p1'><span data-testid='received'>Khách chọn</span></div><div class='out' data-message-id='p2'><span data-testid='sent'>Bạn chọn</span></div><div class='in' data-message-id='p3'><span data-testid='received'>Ảnh nhận</span><span data-testid='sent'>Trích dẫn</span></div></main>");
        await page.EvaluateAsync(script);
        await page.GetByText("Khách chọn", new() { Exact = true }).ClickAsync();
        await page.GetByText("Bạn chọn", new() { Exact = true }).ClickAsync();
        var paired = JsonSerializer.Deserialize<MessengerSetupResult>(await page.EvaluateAsync<string>("JSON.stringify(window.__rentalMessengerSetup.result)"), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        check(paired.Error == "", "setup tries another candidate pair when preferred selectors overlap on a third message");
        await RentalFacebook.VerifyMessengerSelectors(page, paired);
        check(await page.Locator(paired.IncomingMessages).CountAsync() == 2 && await page.Locator(paired.OutgoingMessages).CountAsync() == 1, "candidate pair keeps received attachments separate from sent messages");
        await page.SetContentAsync("<div style='margin-top:160px'>Tin không có ID ổn định</div>");
        await page.EvaluateAsync(script);
        await page.GetByText("Tin không có ID ổn định", new() { Exact = true }).ClickAsync();
        check(await page.EvaluateAsync<bool>("window.__rentalMessengerSetup.result === null"), "setup refuses to manufacture message IDs from content");
        var directory = Path.Combine(Path.GetTempPath(), "AI_TOOL_setup_checks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "messenger-selectors.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new MessengerSelectors(result.IncomingMessages, result.OutgoingMessages, result.MessageIdAttribute)));
        var options = new RentalOptions { ManagementKey = "preserve-this-key" };
        RentalMessengerSetup.Load(path, options);
        check(options.IncomingMessages == result.IncomingMessages && options.MessageIdAttribute == "data-message-id" && options.ManagementKey == "preserve-this-key", "saved selectors reload without changing unrelated configuration");
        File.Delete(path); Directory.Delete(directory);
    }
}
