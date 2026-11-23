using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Hinet.Api;

public record InboxMessage(string Id, string Text, string Conversation);
public sealed class RentalPublishException(string step, Exception inner)
    : Exception($"Đăng Facebook bị dừng ở bước: {step}. Kiểm tra trình duyệt trước khi thử lại.", inner);

// Only the single worker accesses the browser. Never use the user's normal browser profile.
public sealed class RentalFacebook(IOptions<RentalOptions> options, RentalMedia media) : IAsyncDisposable
{
    private IPlaywright? playwright;
    private IBrowserContext? context;
    private IPage? page;
    public string Status { get; private set; } = "NotStarted";
    private readonly SemaphoreSlim operationGate = new(1);
    private DateTimeOffset remoteUntil;
    public bool RemoteLoginActive => remoteUntil > DateTimeOffset.UtcNow;
    private sealed class Release(SemaphoreSlim semaphore) : IDisposable { public void Dispose() => semaphore.Release(); }
    public async Task<IDisposable> Acquire(CancellationToken ct = default) { await operationGate.WaitAsync(ct); return new Release(operationGate); }
    public async Task BeginRemoteLogin()
    {
        using var lease = await Acquire();
        remoteUntil = DateTimeOffset.UtcNow.AddMinutes(10);
        var p = await Page();
        await p.SetViewportSizeAsync(1100, 760);
        await p.GotoAsync("https://www.facebook.com/", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
    }
    public async Task<byte[]> RemoteScreen()
    {
        using var lease = await Acquire();
        if (!RemoteLoginActive || page is null) throw new ArgumentException("Phiên đăng nhập đã kết thúc.");
        return await page.ScreenshotAsync(new() { Type = ScreenshotType.Jpeg, Quality = 75 });
    }
    public async Task RemoteInput(string kind, int x, int y, string text)
    {
        using var lease = await Acquire();
        if (!RemoteLoginActive || page is null) throw new ArgumentException();
        if (kind == "click" && x >= 0 && x < 1100 && y >= 0 && y < 760) await page.Mouse.ClickAsync(x, y);
        else if (kind == "text") await page.Keyboard.InsertTextAsync(text);
        else if (kind == "key" && new[] { "Tab", "Enter", "Backspace", "Control+A", "Escape", "ArrowDown", "ArrowUp" }.Contains(text)) await page.Keyboard.PressAsync(text);
        else if (kind == "scroll") await page.Mouse.WheelAsync(0, Math.Clamp(y, -650, 650));
        else throw new ArgumentException();
    }
    public void EndRemoteLogin() => remoteUntil = DateTimeOffset.MinValue;

    private async Task<IPage> Page()
    {
        if (!options.Value.BrowserEnabled) throw new InvalidOperationException("BrowserEnabled chưa bật.");
        if (page is not null && !page.IsClosed) return page;
        playwright ??= await Playwright.CreateAsync();
        context = await playwright.Chromium.LaunchPersistentContextAsync(
            Path.GetFullPath(options.Value.ProfilePath), new() { Headless = options.Value.Headless, Locale = "vi-VN", ServiceWorkers = ServiceWorkerPolicy.Block });
        if (options.Value.Headless)
            await context.RouteAsync("**/*", async route =>
            {
                var allowed = Uri.TryCreate(route.Request.Url, UriKind.Absolute, out var uri) && uri.Scheme == "https" &&
                    new[] { "facebook.com", "facebook.net", "fbcdn.net", "fbsbx.com", "google.com", "gstatic.com" }.Any(domain => uri.Host == domain || uri.Host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));
                if (allowed) await route.ContinueAsync(); else await route.AbortAsync();
            });
        context.SetDefaultTimeout(15000);
        page = await context.NewPageAsync();
        await page.GotoAsync("https://www.facebook.com/", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Status = "LoginRequired";
        return page;
    }
    public async Task OpenLogin() => _ = await Page();
    public async Task BeginMessengerSetup(string url, string script)
    {
        var p = await Navigate(url);
        await p.BringToFrontAsync();
        await p.EvaluateAsync(script);
        Status = "SettingUpMessenger";
    }
    public async Task<MessengerSetupResult?> PollMessengerSetup()
    {
        if (page is null || page.IsClosed) throw new InvalidOperationException("Cửa sổ thiết lập đã đóng.");
        if (!await page.EvaluateAsync<bool>("!!window.__rentalMessengerSetup")) throw new InvalidOperationException("Trang thiết lập đã thay đổi.");
        var json = await page.EvaluateAsync<string?>("window.__rentalMessengerSetup?.result ? JSON.stringify(window.__rentalMessengerSetup.result) : null");
        return json is null ? null : System.Text.Json.JsonSerializer.Deserialize<MessengerSetupResult>(json, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
    }
    public async Task EndMessengerSetup()
    {
        if (page is not null && !page.IsClosed) await page.EvaluateAsync("window.__rentalMessengerSetup?.cleanup?.()");
        Status = "Ready";
    }
    public async Task VerifyMessengerSetup(string url, MessengerSetupResult result)
    {
        await EndMessengerSetup();
        var p = await Navigate(url);
        await VerifyMessengerSelectors(p, result);
    }
    public static async Task VerifyMessengerSelectors(IPage p, MessengerSetupResult result)
    {
        await p.Locator(result.IncomingMessages).First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        await p.Locator(result.OutgoingMessages).First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        var incoming = await ReadIncomingFromPage(p, result.IncomingMessages, result.OutgoingMessages, result.MessageIdAttribute);
        var outgoingIds = await p.Locator(result.OutgoingMessages).EvaluateAllAsync<string[]>("(nodes, attr) => nodes.map(n => n.getAttribute(attr))", result.MessageIdAttribute);
        if (!incoming.Any(m => m.Id == result.IncomingId) || !outgoingIds.Contains(result.OutgoingId) || outgoingIds.Contains(result.IncomingId) || outgoingIds.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Không xác minh được hai tin mẫu sau khi tải lại hội thoại.");
    }

    public static bool ValidUrl(string url, bool group)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Port != 443 || !string.IsNullOrEmpty(uri.UserInfo)) return false;
        if (uri.Host is not ("www.facebook.com" or "facebook.com")) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(uri.AbsolutePath,
            group ? @"^/groups/[a-zA-Z0-9.]+/?$" : @"^/messages/(?:e2ee/)?t/[a-zA-Z0-9.]+/?$") && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
    }
    public static string CanonicalUrl(string url) => "https://www.facebook.com" + new Uri(url).AbsolutePath.TrimEnd('/');
    private async Task<IPage> Navigate(string url)
    {
        var p = await Page();
        await p.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        if (p.Url.Contains("/login") || p.Url.Contains("/checkpoint") || p.Url.Contains("/two_step_verification"))
        {
            Status = "LoginRequired";
            throw new InvalidOperationException("Cần đăng nhập/xác minh Facebook trong cửa sổ Chromium.");
        }
        if (ValidUrl(url, false) || ValidUrl(url, true))
        {
            var expected = new Uri(url);
            var actual = new Uri(p.Url);
            var sameThread = ValidUrl(url, false) && ValidUrl(p.Url, false) && actual.AbsolutePath.TrimEnd('/').Split('/').Last() == expected.AbsolutePath.TrimEnd('/').Split('/').Last();
            if (actual.Host is not ("www.facebook.com" or "facebook.com") || (actual.AbsolutePath.TrimEnd('/') != expected.AbsolutePath.TrimEnd('/') && !sameThread))
                throw new InvalidOperationException("Facebook đã chuyển sang trang khác; dừng thao tác.");
        }
        Status = "Ready";
        return p;
    }
    public async Task<string?> Publish(string url, string content, IReadOnlyList<string>? images = null)
    {
        if (!ValidUrl(url, true)) throw new InvalidOperationException("URL nhóm không hợp lệ.");
        var step = "mở trang nhóm";
        try
        {
            var p = await Navigate(url);
            var priorLinks = (await p.Locator("a[href*='/groups/'], a[href*='story_fbid']")
                .EvaluateAllAsync<string[]>("nodes => nodes.map(n => n.href)"))
                .Where(RentalEngagement.ValidPostUrl).Select(RentalEngagement.Canonical).ToHashSet();
            step = "mở khung tạo bài";
            await p.Locator(options.Value.ComposeButton).First.ClickAsync();
            step = "tìm ô nhập nội dung";
            var editor = p.Locator(options.Value.PostEditor).Filter(new() { Visible = true });
            // The composer mounts asynchronously after the opening click.
            await editor.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            if (await editor.CountAsync() != 1) throw new InvalidOperationException("Không xác định duy nhất ô soạn bài.");
            step = "điền nội dung";
            await editor.FillAsync(content);
            if (images is { Count: > 0 })
            {
                step = "tìm khung tải ảnh";
                Status = "UploadingPhotos";
                media.ValidateImages(images);
                await AttachPhotos(p, editor, images.Select(media.Resolve).ToArray(), value => step = value);
            }
            step = "bấm nút Đăng/Post";
            Status = "SubmittingPost";
            var result = await SubmitAndCapturePostLink(p, p.Locator(options.Value.PublishButton), url, content, priorLinks, value => Status = value);
            Status = result is null ? "PostLinkPending" : "Ready";
            return result;
        }
        catch (Exception ex) { Status = "PublishNeedsReview"; throw new RentalPublishException(step, ex); }
        // The caller records Submitted, never claims this is published/approved by the group.
    }
    public static async Task<string?> SubmitAndCapturePostLink(IPage p, ILocator button, string groupUrl, string content, ISet<string> priorLinks, Action<string>? state = null, TimeSpan? timeout = null)
    {
        var captured = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        async void CaptureCreatedStory(object? sender, IResponse response)
        {
            try
            {
                var host = new Uri(response.Url).Host;
                if (!response.Ok || (host != "facebook.com" && !host.EndsWith(".facebook.com", StringComparison.OrdinalIgnoreCase)) ||
                    !RentalPostLinks.IsCreationRequest(response.Request.PostData)) return;
                var link = RentalPostLinks.FromCreationResponse(await response.TextAsync(), groupUrl);
                if (link is not null) captured.TrySetResult(link);
            }
            catch (Exception) { /* Navigation can discard a response; DOM discovery remains available. */ }
        }
        p.Response += CaptureCreatedStory;
        try
        {
            await button.ClickAsync();
            state?.Invoke("FindingPostLink");
            return await WaitForPostLink(p, groupUrl, content, priorLinks, captured.Task, timeout ?? TimeSpan.FromSeconds(35));
        }
        finally { p.Response -= CaptureCreatedStory; }
    }
    public static async Task<string?> FindPostLinkOnPage(IPage p, string groupUrl, string content, ISet<string>? excluded = null, DateTimeOffset? near = null)
    {
        var normalized = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ").Trim();
        if (normalized.Length < 5) return null;
        var articles = p.Locator("[role=article]").Filter(new() { Visible = true });
        var candidates = new HashSet<string>();
        for (var i = 0; i < Math.Min(await articles.CountAsync(), 30); i++)
        {
            var article = articles.Nth(i);
            var body = System.Text.RegularExpressions.Regex.Replace(await article.InnerTextAsync(new() { Timeout = 2000 }), @"\s+", " ");
            if (!body.Contains(normalized[..Math.Min(normalized.Length, 90)], StringComparison.OrdinalIgnoreCase)) continue;
            // A recovery scan must match the submission time too: repeated adverts must not be mixed up.
            if (near.HasValue)
            {
                var times = await article.Locator("time[datetime], [data-utime]").EvaluateAllAsync<string[]>("nodes => nodes.map(n => n.getAttribute('datetime') || n.getAttribute('data-utime'))");
                var timeMatches = times.Any(value => long.TryParse(value, out var seconds)
                    ? seconds is > 0 and < 253402300800 && Math.Abs((DateTimeOffset.FromUnixTimeSeconds(seconds) - near.Value).TotalMinutes) <= 15
                    : DateTimeOffset.TryParse(value, out var date) && Math.Abs((date - near.Value).TotalMinutes) <= 15);
                if (!timeMatches) continue;
            }
            // Timestamp hrefs can be populated only on hover.
            var timestamp = article.Locator("a:has(time), a:has(abbr), a[href*='multi_permalinks']").First;
            if (await timestamp.CountAsync() > 0)
                try { await timestamp.HoverAsync(new() { Timeout = 1500 }); } catch (Microsoft.Playwright.PlaywrightException) { } catch (TimeoutException) { }
            var links = await article.Locator("a[href]").EvaluateAllAsync<string[]>("nodes => nodes.map(n => n.href)");
            foreach (var href in links)
            {
                var link = RentalPostLinks.Normalize(href, groupUrl);
                if (link is not null && RentalEngagement.BelongsToGroup(link, groupUrl) && (excluded is null || !excluded.Contains(link))) candidates.Add(link);
            }
        }
        return candidates.Count == 1 ? candidates.Single() : null;
    }
    public static async Task<string?> WaitForPostLink(IPage p, string groupUrl, string content, ISet<string> excluded, Task<string>? responseLink, TimeSpan timeout)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (timer.Elapsed < timeout)
        {
            if (responseLink?.IsCompletedSuccessfully == true) return await responseLink;
            try
            {
                var url = await FindPostLinkOnPage(p, groupUrl, content, excluded);
                if (url is not null) return url;
            }
            catch (Microsoft.Playwright.PlaywrightException) { }
            catch (TimeoutException) { }
            await Task.Delay(500);
        }
        return responseLink?.IsCompletedSuccessfully == true ? await responseLink : null;
    }
    public async Task<string?> RecoverPostLink(string groupUrl, string content, DateTimeOffset submittedAt)
    {
        if (!ValidUrl(groupUrl, true)) return null;
        Status = "FindingPostLink";
        try
        {
            var p = await Navigate(CanonicalUrl(groupUrl) + "/search/?q=" + Uri.EscapeDataString(content[..Math.Min(content.Length, 120)]));
            try { await p.Locator("[role=article]").First.WaitForAsync(new() { Timeout = 10000 }); }
            catch (TimeoutException) { return null; }
            return await FindPostLinkOnPage(p, groupUrl, content, near: submittedAt);
        }
        finally { if (Status != "LoginRequired") Status = "Ready"; }
    }
    public async Task<RentalMetrics> ReadMetrics(string url)
    {
        if (!RentalEngagement.ValidPostUrl(url)) throw new ArgumentException("Cần link trực tiếp đến bài viết trong nhóm Facebook.");
        var p = await Navigate(RentalEngagement.Canonical(url));
        var actual = RentalPostLinks.Normalize(p.Url);
        var expectedId = new Uri(RentalEngagement.Canonical(url)).AbsolutePath.Split('/').Last();
        if (actual is null || new Uri(actual).AbsolutePath.Split('/').Last() != expectedId)
            throw new InvalidOperationException("Facebook chưa mở đúng bài viết cần xác minh.");
        var articles = p.Locator("[role=article]").Filter(new() { Visible = true });
        await articles.First.WaitForAsync(new() { Timeout = 15000 });
        var matching = articles.Filter(new() { Has = p.Locator("a[href*='" + new Uri(url).AbsolutePath.TrimEnd('/').Split('/').Last() + "']") });
        var article = await matching.CountAsync() == 1 ? matching : null;
        if (article is null) throw new InvalidOperationException("Không xác định duy nhất bài viết để đọc tương tác.");
        var body = await article.InnerTextAsync();
        if (System.Text.RegularExpressions.Regex.IsMatch(body, "pending approval|awaiting approval|chờ phê duyệt|đang chờ duyệt", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Bài đang chờ nhóm duyệt.");
        return await ReadVisibleMetrics(article);
    }
    public static async Task<RentalMetrics> ReadVisibleMetrics(ILocator article)
    {
        var labels = await article.Locator("[aria-label], a, [role=button]").EvaluateAllAsync<string[]>("nodes => nodes.filter(n => n.getClientRects().length).flatMap(n => [n.getAttribute('aria-label') || '', n.innerText || '']).filter(t => t && t.length < 150)");
        return RentalEngagement.Parse(labels);
    }
    public static async Task AttachPhotos(IPage p, ILocator editor, string[] paths, Action<string>? progress = null)
    {
        // Resolve the editor's actual ancestor: Has(PostEditor) would scope an already
        // dialog-prefixed selector inside another dialog and fail to find the composer.
        var dialog = editor.Locator("xpath=ancestor::*[@role='dialog'][1]");
        if (await dialog.CountAsync() != 1) throw new InvalidOperationException("Không xác định khung đăng ảnh.");
        var before = await dialog.Locator("img").EvaluateAllAsync<string[]>("nodes => nodes.map(n => n.currentSrc || n.src)");
        var input = dialog.Locator("input[type=file][accept*='image' i]");
        progress?.Invoke("mở bộ chọn ảnh");
        if (await input.CountAsync() > 0)
            await input.First.SetInputFilesAsync(paths, new() { Timeout = 15000 });
        else
        {
            // Facebook may mount its file input outside the dialog. Capture the chooser
            // from the photo button rather than assuming the input's DOM location.
            var photo = dialog.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex(@"Ảnh\s*\x2f\s*video|Photo\s*\x2f\s*video|Add photos", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }).First;
            var chooser = await p.RunAndWaitForFileChooserAsync(() => photo.ClickAsync(new() { Timeout = 15000 }), new() { Timeout = 15000 });
            await chooser.SetFilesAsync(paths, new() { Timeout = 15000 });
        }
        progress?.Invoke($"chờ tải xong {paths.Length} ảnh");
        await p.WaitForFunctionAsync("""
            ({dialog, before, count}) => {
                if (!dialog || !dialog.isConnected) return false;
                const photos = [...dialog.querySelectorAll('img')].filter(img => {
                    const src = img.currentSrc || img.src;
                    return src && !before.includes(src) && img.complete && img.naturalWidth > 0 && img.getClientRects().length > 0;
                });
                const uploading = [...dialog.querySelectorAll('[role=progressbar]')].some(el => el.getClientRects().length > 0);
                return photos.length >= count && !uploading;
            }
            """, new { dialog = await dialog.ElementHandleAsync(), before, count = paths.Length }, new() { Timeout = 60000 });
    }
    public async Task<InboxMessage?> Read(string url)
    {
        var o = options.Value;
        if (!ValidUrl(url, false)) throw new InvalidOperationException("URL hội thoại không hợp lệ.");
        if (string.IsNullOrWhiteSpace(o.IncomingMessages) || string.IsNullOrWhiteSpace(o.OutgoingMessages))
            throw new InvalidOperationException("Cần cấu hình selector tin đến/tin đi riêng biệt trước khi bật trả lời.");
        var p = await Navigate(url);
        var incoming = p.Locator(o.IncomingMessages);
        try { await incoming.First.WaitForAsync(new() { Timeout = 10000 }); }
        catch (TimeoutException) { return null; }
        if (await incoming.EvaluateAllAsync<bool>("(nodes, outgoing) => nodes.some(n => n.matches(outgoing))", o.OutgoingMessages))
            throw new InvalidOperationException("Selector tin đến/tin đi bị trùng. Dừng để tránh trả lời tin của chính mình.");
        var last = incoming.Last;
        var id = await last.GetAttributeAsync(o.MessageIdAttribute);
        if (string.IsNullOrWhiteSpace(id) || id.Length > 250) throw new InvalidOperationException("Tin nhắn thiếu ID ổn định; dừng để tránh trả lời trùng.");
        var outgoing = p.Locator(o.OutgoingMessages);
        if (await outgoing.CountAsync() > 0)
        {
            var sent = await outgoing.Last.ElementHandleAsync();
            if (await last.EvaluateAsync<bool>("(node, sent) => !!(node.compareDocumentPosition(sent) & Node.DOCUMENT_POSITION_FOLLOWING)", sent)) return null;
        }
        var history = await p.Locator($"{o.IncomingMessages}, {o.OutgoingMessages}").EvaluateAllAsync<string[]>(
            "(nodes, incoming) => nodes.map(n => (n.matches(incoming) ? 'Khách: ' : 'Chủ nhà: ') + n.innerText)", o.IncomingMessages);
        var transcript = string.Join("\n", history.TakeLast(20));
        return new(id, await last.InnerTextAsync(), transcript[..Math.Min(transcript.Length, 12000)]);
    }
    public async Task<List<InboxMessage>> ReadIncomingMessages(string url)
    {
        if (!ValidUrl(url, false)) throw new InvalidOperationException("URL hội thoại không hợp lệ.");
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.IncomingMessages) || string.IsNullOrWhiteSpace(o.OutgoingMessages))
            throw new InvalidOperationException("Chưa cấu hình nhận diện tin đến/tin đi Messenger.");
        return await ReadIncomingFromPage(await Navigate(url), o.IncomingMessages, o.OutgoingMessages, o.MessageIdAttribute);
    }
    public static async Task<List<InboxMessage>> ReadIncomingFromPage(IPage p, string incomingSelector, string outgoingSelector, string idAttribute)
    {
        var incoming = p.Locator(incomingSelector);
        try { await incoming.First.WaitForAsync(new() { Timeout = 10000 }); }
        catch (TimeoutException) { return new(); }
        if (await incoming.EvaluateAllAsync<bool>("(nodes, outgoing) => nodes.some(n => n.matches(outgoing))", outgoingSelector))
            throw new InvalidOperationException("Selector tin đến và tin đi bị trùng.");
        var values = await incoming.EvaluateAllAsync<string[]>("(nodes, attribute) => nodes.slice(-50).map(n => JSON.stringify({id:n.getAttribute(attribute),text:n.innerText || ''}))", idAttribute);
        var result = new List<InboxMessage>();
        foreach (var value in values)
        {
            using var json = System.Text.Json.JsonDocument.Parse(value);
            var id = json.RootElement.GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(id) || id.Length > 250) throw new InvalidOperationException("Tin nhắn thiếu ID ổn định.");
            result.Add(new(id, json.RootElement.GetProperty("text").GetString() ?? "", ""));
        }
        return result.DistinctBy(m => m.Id).ToList();
    }
    public async Task Reply(string url, string expectedId, string reply)
    {
        // Re-read before sending: do not send a stale answer if a new message arrived.
        var latest = await Read(url);
        if (latest?.Id != expectedId) throw new InvalidOperationException("Tin nhắn đã thay đổi; cần kiểm tra lại.");
        var editor = page!.Locator(options.Value.MessageEditor);
        if (await editor.CountAsync() != 1) throw new InvalidOperationException("Không xác định duy nhất ô nhập Messenger.");
        await editor.FillAsync(reply);
        await editor.PressAsync("Enter");
    }
    public async Task<List<RentalGroup>> SearchGroups(string query)
    {
        var p = await Navigate("https://www.facebook.com/search/groups/?q=" + Uri.EscapeDataString(query));
        return await ReadGroupResults(p);
    }
    public static async Task<List<RentalGroup>> ReadGroupResults(IPage p)
    {
        var results = p.Locator("[role=main] a[href*='/groups/']");
        try { await results.First.WaitForAsync(new() { Timeout = 10000 }); }
        catch (TimeoutException) { return new(); }
        var links = await results.EvaluateAllAsync<string[]>("nodes => nodes.filter(n => n.getClientRects().length).map(n => JSON.stringify({url:n.href.split('?')[0],name:(n.innerText || n.getAttribute('aria-label') || '').trim(),source:n.innerText?.trim() ? 'text' : 'label'}))");
        return links.Select(v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(v)!)
            .Where(v => ValidUrl(v["url"], true)).GroupBy(v => CanonicalUrl(v["url"]))
            .Take(20).Select(g => { var name = g.OrderBy(v => v["source"] == "text" ? 0 : 1).Select(v => RentalGroupNames.Clean(v["name"])).FirstOrDefault(RentalGroupNames.Useful) ?? g.Key.Split('/').Last(); return new RentalGroup { Url = g.Key, Name = name[..Math.Min(name.Length,150)], Enabled = false }; }).ToList();
    }
    public async Task<List<RentalThread>> DiscoverThreads()
    {
        var p = await Navigate("https://www.facebook.com/messages/");
        const string threadLinks = "a[href*='/messages/t/'], a[href*='/messages/e2ee/t/']";
        try { await p.Locator(threadLinks).First.WaitForAsync(new() { Timeout = 10000 }); }
        catch (TimeoutException) { return new(); }
        var links = await p.Locator(threadLinks).EvaluateAllAsync<string[]>(
            "nodes => nodes.map(n => JSON.stringify({url:n.href.split('?')[0],name:n.innerText,unread:[n,...n.querySelectorAll('[aria-label]')].some(e=> /unread message|tin nhắn chưa đọc/i.test(e.getAttribute('aria-label') || '')) ? 'yes' : 'no'}))");
        return links.Select(x => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(x)!)
            .Where(x => ValidUrl(x["url"], false)).DistinctBy(x => CanonicalUrl(x["url"])).Take(30)
            .Select(x => new RentalThread { Url = CanonicalUrl(x["url"]), CustomerName = x["name"][..Math.Min(x["name"].Length, 150)], AutoReply = options.Value.AutoReplyNewConversations, HasUnreadMessages = x["unread"] == "yes" }).ToList();
    }
    public async Task<object> CheckSelectors(string url)
    {
        if (!ValidUrl(url, false)) throw new ArgumentException("Nhập link hội thoại Facebook hợp lệ.");
        var p = await Navigate(url);
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.IncomingMessages) || string.IsNullOrWhiteSpace(o.OutgoingMessages))
            return new { ready = false, message = "Chưa chọn selector tin đến/tin đi. Nhập CSS selector và lưu trước." };
        var inbound = p.Locator(o.IncomingMessages);
        try { await inbound.First.WaitForAsync(new() { Timeout = 10000 }); } catch (TimeoutException) { }
        var incomingCount = await inbound.CountAsync();
        var outgoingCount = await p.Locator(o.OutgoingMessages).CountAsync();
        var editorCount = await p.Locator(o.MessageEditor).CountAsync();
        var id = incomingCount > 0 ? await inbound.Last.GetAttributeAsync(o.MessageIdAttribute) : null;
        var overlap = await p.Locator(o.IncomingMessages).EvaluateAllAsync<bool>("(nodes, outgoing) => nodes.some(n => n.matches(outgoing))", o.OutgoingMessages);
        return new { ready = incomingCount > 0 && editorCount == 1 && !string.IsNullOrWhiteSpace(id) && !overlap,
            incomingCount, outgoingCount, editorCount, stableMessageIdFound = !string.IsNullOrWhiteSpace(id), overlap,
            message = "Chỉ kiểm tra nhận diện, không gửi tin. Đối chiếu tin đến/tin đi thực tế trước khi bật." };
    }
    public async ValueTask DisposeAsync()
    {
        if (context is not null) await context.CloseAsync();
        playwright?.Dispose();
    }
}
