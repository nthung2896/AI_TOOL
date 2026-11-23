using Hinet.Api;
using Microsoft.Playwright;

public static class PhotoUploadChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "AI_TOOL_photo_checks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9Zl1sAAAAASUVORK5CYII=");
        var paths = new[] { Path.Combine(directory, "cover.png"), Path.Combine(directory, "room.png") };
        foreach (var path in paths) await File.WriteAllBytesAsync(path, png);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.RouteAsync("https://fixture.test/**", route => route.FulfillAsync(new() { Status = 200, ContentType = "image/png", BodyBytes = png }));
        foreach (var scenario in new[] { (Outside: false, Network: false), (Outside: true, Network: false), (Outside: true, Network: true) })
        {
            var input = "<input id='upload' type='file' accept='image/*,video/*' multiple hidden>";
            await page.SetContentAsync("<div role='dialog'><div contenteditable='true' role='textbox'></div>" +
                (scenario.Outside ? "" : input) +
                "<button aria-label='Photo/video' onclick='document.getElementById(\"upload\").click()'>Photo/video</button>" +
                "<div id='photos'></div><button id='post' onclick='window.sent=true'>Post</button></div>" + (scenario.Outside ? input : ""));
            await page.EvaluateAsync("""
                network => {
                    window.sent = false;
                    document.querySelector('#upload').onchange = event => {
                        [...event.target.files].forEach((file, index) => {
                            const img = new Image();
                            img.src = network ? 'https://fixture.test/photo-' + index : URL.createObjectURL(file);
                            document.querySelector('#photos').append(img);
                        });
                    };
                }
                """, scenario.Network);
            // Reproduce the real configured selector, including its dialog prefix.
            var editor = page.Locator("[role=dialog] [contenteditable=true][role=textbox]");
            await editor.FillAsync("Bài đăng thử có ảnh");
            await RentalFacebook.AttachPhotos(page, editor, paths);
            check(await page.Locator("#upload").EvaluateAsync<int>("input => input.files.length") == 2,
                $"photo attachment selects both files (outside={scenario.Outside}, networkPreview={scenario.Network})");
            check(await page.Locator("#photos img").CountAsync() == 2 && !await page.EvaluateAsync<bool>("window.sent"), "attachment verifies previews without submitting a real post");
        }
        foreach (var path in paths) File.Delete(path);
        Directory.Delete(directory);
    }
}
