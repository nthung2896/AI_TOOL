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
        await page.SetContentAsync("<article id='target'><button aria-label='All reactions: 24'>24</button><a>5 comments</a><a>2 shares</a><button hidden aria-label='999 comments'>999</button></article><article><a>999 shares</a></article>");
        var metrics = await RentalFacebook.ReadVisibleMetrics(page.Locator("#target"));
        check(metrics == new RentalMetrics(24, 5, 2), "read visible counters from the selected post without unrelated or hidden metrics");
        await page.SetContentAsync("<div class='incoming' data-mid='m1'>Khách hỏi phòng</div><div class='outgoing' data-mid='m2'>Chủ nhà trả lời</div><div class='incoming' data-mid='m3'>Khách muốn xem phòng</div>");
        var incoming = await RentalFacebook.ReadIncomingFromPage(page, ".incoming", ".outgoing", "data-mid");
        check(incoming.Count == 2 && incoming[0].Text == "Khách hỏi phòng" && incoming[1].Id == "m3", "notification reader includes customer messages even when the owner has already replied and excludes outgoing messages");
        await page.SetContentAsync("<div role='article'>Room advert fixture<a href='https://www.facebook.com/groups/fixture/posts/111'>Old post</a></div>");
        await page.EvaluateAsync("setTimeout(() => { const a=document.createElement('div'); a.setAttribute('role','article'); a.innerHTML='Room advert fixture <a href=\"https://www.facebook.com/groups/fixture/?multi_permalinks=222\">New post</a>'; document.body.append(a); }, 250)");
        var found = await RentalFacebook.WaitForPostLink(page, "https://www.facebook.com/groups/fixture", "Room advert fixture", new HashSet<string>{"https://www.facebook.com/groups/fixture/posts/111"}, null, TimeSpan.FromSeconds(5));
        check(found == "https://www.facebook.com/groups/fixture/posts/222", "delayed feed insertion discovers the new permalink while excluding old identical adverts");
        await page.SetContentAsync("<div role='article'>Room advert fixture<time datetime='2026-11-23T10:00:00Z'></time><a href='https://www.facebook.com/groups/fixture/posts/444'>Newer</a></div><div role='article'>Room advert fixture<time datetime='2026-11-22T10:00:00Z'></time><a href='https://www.facebook.com/groups/fixture/posts/555'>Older</a></div>");
        found = await RentalFacebook.FindPostLinkOnPage(page, "https://www.facebook.com/groups/fixture", "Room advert fixture", near: DateTimeOffset.Parse("2026-11-23T10:02:00Z"));
        check(found == "https://www.facebook.com/groups/fixture/posts/444", "automatic recovery uses the submission time to distinguish repeated adverts");
        await page.RouteAsync("https://www.facebook.com/api/graphql/", route => route.FulfillAsync(new() { Status = 200, ContentType = "application/json", Headers = new Dictionary<string,string>{{"Access-Control-Allow-Origin","*"}}, Body = "for (;;);{\"data\":{\"story_create\":{\"story\":{\"url\":\"https://www.facebook.com/groups/fixture/posts/333\"}}}}" }));
        await page.SetContentAsync("<button onclick=\"fetch('https://www.facebook.com/api/graphql/',{method:'POST',headers:{'Content-Type':'application/x-www-form-urlencoded'},body:'fb_api_req_friendly_name=ComposerStoryCreateMutation'})\">Post fixture</button>");
        found = await RentalFacebook.SubmitAndCapturePostLink(page, page.GetByRole(AriaRole.Button), "https://www.facebook.com/groups/fixture", "Room advert fixture", new HashSet<string>(), timeout: TimeSpan.FromSeconds(5));
        check(found == "https://www.facebook.com/groups/fixture/posts/333", "submission captures the permalink from an intercepted creation response before the feed renders");
        foreach (var path in paths) File.Delete(path);
        Directory.Delete(directory);
    }
}
