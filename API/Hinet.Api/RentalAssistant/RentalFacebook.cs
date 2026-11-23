using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Hinet.Api;

public record InboxMessage(string Id, string Text, string Conversation);
public sealed class RentalPublishException(string step, Exception inner)
    : Exception($"Đăng Facebook bị dừng ở bước: {step}. Kiểm tra trình duyệt trước khi thử lại.", inner);

// Only the single worker accesses the browser. Never use the user's normal browser profile.
public sealed class RentalFacebook(IOptions<RentalOptions> options) : IAsyncDisposable
{
    private IPlaywright? playwright;
    private IBrowserContext? context;
    private IPage? page;
    public string Status { get; private set; } = "NotStarted";

    private async Task<IPage> Page()
    {
        if (!options.Value.BrowserEnabled) throw new InvalidOperationException("BrowserEnabled chưa bật.");
        if (page is not null && !page.IsClosed) return page;
        playwright ??= await Playwright.CreateAsync();
        context = await playwright.Chromium.LaunchPersistentContextAsync(
            Path.GetFullPath(options.Value.ProfilePath), new() { Headless = false, Locale = "vi-VN" });
        context.SetDefaultTimeout(15000);
        page = await context.NewPageAsync();
        await page.GotoAsync("https://www.facebook.com/", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Status = "LoginRequired";
        return page;
    }
    public async Task OpenLogin() => _ = await Page();

    public static bool ValidUrl(string url, bool group)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Port != 443 || !string.IsNullOrEmpty(uri.UserInfo)) return false;
        if (uri.Host is not ("www.facebook.com" or "facebook.com")) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(uri.AbsolutePath,
            group ? @"^/groups/[a-zA-Z0-9.]+/?$" : @"^/messages/t/[a-zA-Z0-9.]+/?$") && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
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
            if (actual.Host is not ("www.facebook.com" or "facebook.com") || actual.AbsolutePath.TrimEnd('/') != expected.AbsolutePath.TrimEnd('/'))
                throw new InvalidOperationException("Facebook đã chuyển sang trang khác; dừng thao tác.");
        }
        Status = "Ready";
        return p;
    }
    public async Task Publish(string url, string content)
    {
        if (!ValidUrl(url, true)) throw new InvalidOperationException("URL nhóm không hợp lệ.");
        var step = "mở trang nhóm";
        try
        {
            var p = await Navigate(url);
            step = "mở khung tạo bài";
            await p.Locator(options.Value.ComposeButton).First.ClickAsync();
            step = "tìm ô nhập nội dung";
            var editor = p.Locator(options.Value.PostEditor).Filter(new() { Visible = true });
            // The composer mounts asynchronously after the opening click.
            await editor.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            if (await editor.CountAsync() != 1) throw new InvalidOperationException("Không xác định duy nhất ô soạn bài.");
            step = "điền nội dung";
            await editor.FillAsync(content);
            step = "bấm nút Đăng/Post";
            await p.Locator(options.Value.PublishButton).ClickAsync();
        }
        catch (Exception ex) { throw new RentalPublishException(step, ex); }
        // The caller records Submitted, never claims this is published/approved by the group.
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
        try { await p.Locator("a[href*='/groups/']").First.WaitForAsync(new() { Timeout = 10000 }); }
        catch (TimeoutException) { return new(); }
        var hrefs = await p.Locator("a[href*='/groups/']").EvaluateAllAsync<string[]>("nodes => nodes.map(n => n.href)");
        return hrefs.Select(h => h.Split('?')[0]).Where(h => ValidUrl(h, true)).Select(CanonicalUrl).Distinct()
            .Take(20).Select(h => new RentalGroup { Url = h, Name = h.Split('/').Last(s => s.Length > 0), Enabled = false }).ToList();
    }
    public async Task<List<RentalThread>> DiscoverThreads()
    {
        var p = await Navigate("https://www.facebook.com/messages/");
        try { await p.Locator("a[href*='/messages/t/']").First.WaitForAsync(new() { Timeout = 10000 }); }
        catch (TimeoutException) { return new(); }
        var links = await p.Locator("a[href*='/messages/t/']").EvaluateAllAsync<string[]>(
            "nodes => nodes.map(n => JSON.stringify({url:n.href.split('?')[0],name:n.innerText}))");
        return links.Select(x => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(x)!)
            .Where(x => ValidUrl(x["url"], false)).DistinctBy(x => CanonicalUrl(x["url"])).Take(30)
            .Select(x => new RentalThread { Url = CanonicalUrl(x["url"]), CustomerName = x["name"][..Math.Min(x["name"].Length, 150)], AutoReply = options.Value.AutoReplyNewConversations }).ToList();
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
