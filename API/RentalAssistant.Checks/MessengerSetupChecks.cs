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
