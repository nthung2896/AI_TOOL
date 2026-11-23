using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hinet.Api;

public class RentalDesktopEngine(RentalLocalStore store, RentalAi ai, RentalFacebook facebook, ILogger<RentalDesktopEngine> logger) : BackgroundService
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public string LastError { get; private set; } = "";
    public DateTimeOffset? LastCycleAt { get; private set; }
    private readonly HttpClient telegram = new() { Timeout = TimeSpan.FromSeconds(30) };
    private bool initializedInbox;
    private DateTimeOffset lastInbox;
    private RentalOptions Options => store.Options;

    public async Task<T> Exclusive<T>(Func<Task<T>> action, CancellationToken ct)
    {
        await Gate.WaitAsync(ct);
        try { return await action(); }
        finally { Gate.Release(); }
    }
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Exclusive(async () =>
                {
                    if (Options.Enabled)
                    {
                        if (Options.TelegramOwnerId > 0 && !string.IsNullOrWhiteSpace(Options.TelegramToken)) await Poll(ct);
                        if (!store.Read().State.Paused && Options.BrowserEnabled)
                        {
                            await Deliver(ct);
                            if (DateTimeOffset.UtcNow - lastInbox > TimeSpan.FromSeconds(Math.Max(30, Options.InboxIntervalSeconds)))
                            { lastInbox = DateTimeOffset.UtcNow; await Inbox(ct); }
                        }
                    }
                    LastCycleAt = DateTimeOffset.UtcNow;
                    return true;
                }, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                LastError = "Không kết nối được dịch vụ. Kiểm tra Telegram, phiên Facebook và cấu hình nhận diện tin nhắn.";
                logger.LogWarning("Desktop worker cycle failed ({Type})", ex.GetType().Name);
            }
            try { await Task.Delay(2000, ct); } catch (OperationCanceledException) { break; }
        }
    }
    private async Task<JsonElement> Telegram(string method, object body, CancellationToken ct)
    {
        using var response = await telegram.PostAsJsonAsync($"https://api.telegram.org/bot{Options.TelegramToken}/{method}", body, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Telegram HTTP {(int)response.StatusCode}");
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (!result.RootElement.GetProperty("ok").GetBoolean()) throw new InvalidOperationException("Telegram từ chối thao tác.");
        return result.RootElement.GetProperty("result").Clone();
    }
    public async Task<string> TestTelegram(CancellationToken ct)
    {
        var me = await Telegram("getMe", new { }, ct);
        return me.GetProperty("username").GetString() ?? "Bot";
    }
    private async Task Notify(string text, CancellationToken ct)
    {
        if (Options.TelegramOwnerId <= 0 || string.IsNullOrWhiteSpace(Options.TelegramToken)) return;
        try { await Telegram("sendMessage", new { chat_id = Options.TelegramOwnerId, text = text[..Math.Min(4000, text.Length)] }, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { LastError = "Không gửi được thông báo Telegram. Kết quả vẫn được lưu trên web."; }
    }
    private async Task Poll(CancellationToken ct)
    {
        var updates = await Telegram("getUpdates", new { offset = store.Read().State.TelegramOffset, timeout = 1, allowed_updates = new[] { "message" } }, ct);
        foreach (var update in updates.EnumerateArray())
        {
            var id = update.GetProperty("update_id").GetInt64();
            if (update.TryGetProperty("message", out var message) && message.TryGetProperty("text", out var text) &&
                message.GetProperty("chat").GetProperty("type").GetString() == "private" &&
                message.GetProperty("chat").GetProperty("id").GetInt64() == Options.TelegramOwnerId &&
                message.GetProperty("from").GetProperty("id").GetInt64() == Options.TelegramOwnerId)
            {
                try { await Notify(await Command(text.GetString() ?? "", id, ct), ct); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                { await Notify("Không thực hiện được lệnh. Kiểm tra dữ liệu và kết nối trên web.", ct); }
            }
            store.Update(data => { data.State.TelegramOffset = id + 1; return true; });
        }
    }
    public async Task<string> Command(string text, long? updateId, CancellationToken ct)
    {
        var parts = text.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "Gõ /help để xem hướng dẫn.";
        var data = store.Read();
        switch (parts[0].ToLowerInvariant())
        {
            case "/start": case "/help":
                return "/phong — xem phòng và ID\n/dang ID_PHONG yêu cầu — soạn bài\n/duyet ID_BAI — xếp hàng đăng\n/timnhom từ khóa — tìm nhóm\n/facebook — mở đăng nhập\n/chay — bật xử lý\n/dung — dừng\n/lich — lịch xem\n/trangthai — trạng thái\nNếu chỉ có một phòng, bạn có thể gõ yêu cầu bình thường.";
            case "/phong": return data.Rooms.Count == 0 ? "Thêm phòng trên web trước." : string.Join("\n\n", data.Rooms.Select(r => $"{r.Name} — {r.MonthlyRent:N0}đ\n{r.Id}"));
            case "/dung": store.Update(d => { d.State.Paused = true; return true; }); return "Đã tạm dừng.";
            case "/chay": store.Update(d => { d.State.Paused = false; return true; }); LastError = ""; return "Đã bật. Chỉ xử lý nhóm và hội thoại được chọn.";
            case "/facebook": await facebook.OpenLogin(); return "Đăng nhập Facebook trong Chromium trên máy chạy ứng dụng.";
            case "/trangthai": return $"Tạm dừng: {data.State.Paused}\nFacebook: {facebook.Status}\nChế độ nội dung: {Options.AiMode}\nChờ đăng: {data.Deliveries.Count(d => d.Status == "Queued")}";
            case "/lich":
                var visits = data.Appointments.Where(a => a.VisitAt > DateTimeOffset.UtcNow && a.Status != "Cancelled" && a.Status != "Completed").OrderBy(a => a.VisitAt).Take(20).ToList();
                return visits.Count == 0 ? "Chưa có lịch xem sắp tới." : string.Join("\n", visits.Select(a => $"{a.VisitAt.ToOffset(TimeSpan.FromHours(7)):dd/MM HH:mm} — {a.CustomerName} — {a.Phone}"));
            case "/timnhom":
                var query = text[(text.IndexOf(' ') + 1)..].Trim();
                if (query.Length < 3 || query == "/timnhom") return "Dùng /timnhom phòng trọ quận 7";
                var found = await facebook.SearchGroups(query);
                store.Update(d => { foreach (var g in found) if (!d.Groups.Any(x => x.Url == g.Url)) d.Groups.Add(g); return true; });
                return $"Tìm thấy {found.Count} nhóm đang hiển thị. Chọn nhóm được phép đăng trên web.";
            case "/duyet":
                if (parts.Length < 2 || !Guid.TryParse(parts[1], out var postId)) return "Dùng /duyet ID_BAI";
                Approve(postId); return "Đã xếp hàng. /chay để bắt đầu.";
            default:
                if (parts[0].StartsWith('/') && parts[0] != "/dang") return "Lệnh chưa hỗ trợ. Gõ /help.";
                var existing = updateId.HasValue ? data.Posts.FirstOrDefault(p => p.TelegramUpdateId == updateId) : null;
                if (existing is not null) return $"Bản nháp đã tạo: {existing.Content}\n/duyet {existing.Id}";
                RentalRoom? room = null;
                if (parts[0] == "/dang" && parts.Length >= 2 && Guid.TryParse(parts[1], out var roomId)) room = data.Rooms.SingleOrDefault(r => r.Id == roomId && r.Available);
                else { var rooms = data.Rooms.Where(r => r.Available).ToList(); if (rooms.Count == 1) room = rooms[0]; }
                if (room is null) return "Dùng /phong để chọn phòng, sau đó /dang ID_PHONG yêu cầu.";
                var post = await Draft(room.Id, text, updateId, ct);
                return $"BẢN NHÁP ({Options.AiMode})\n\n{post.Content}\n\n/duyet {post.Id}";
        }
    }
    public async Task<RentalPost> Draft(Guid roomId, string instruction, long? updateId, CancellationToken ct)
    {
        var room = store.Read().Rooms.SingleOrDefault(r => r.Id == roomId && r.Available) ?? throw new InvalidOperationException("Chọn phòng còn trống.");
        var post = new RentalPost { RoomId = room.Id, Images = room.Listing.Images.ToList(), Content = await ai.Draft(room, instruction, ct), TelegramUpdateId = updateId };
        return store.Update(d => { d.Posts.Insert(0, post); return post; });
    }
    public void Approve(Guid id) => store.Update(d =>
    {
        var post = d.Posts.SingleOrDefault(p => p.Id == id) ?? throw new InvalidOperationException("Không tìm thấy bài.");
        if (post.Status != "Draft") return true;
        var groups = d.Groups.Where(g => g.Enabled).ToList();
        if (groups.Count == 0) throw new InvalidOperationException("Chọn ít nhất một nhóm trước khi duyệt.");
        foreach (var g in groups) if (!d.Deliveries.Any(x => x.PostId == id && x.GroupId == g.Id)) d.Deliveries.Add(new() { PostId = id, GroupId = g.Id });
        post.Status = "Approved"; return true;
    });

    private async Task Deliver(CancellationToken ct)
    {
        var data = store.Read();
        if (data.State.LastPostAt is { } at && DateTimeOffset.UtcNow - at < TimeSpan.FromSeconds(Math.Max(60, Options.PostIntervalSeconds))) return;
        var delivery = data.Deliveries.FirstOrDefault(d => d.Status == "Queued");
        if (delivery is null) return;
        var post = data.Posts.Single(p => p.Id == delivery.PostId);
        var group = data.Groups.Single(g => g.Id == delivery.GroupId);
        if (!group.Enabled || !data.Rooms.Any(r => r.Id == post.RoomId && r.Available))
        { store.Update(d => { d.Deliveries.Single(x => x.Id == delivery.Id).Status = "Skipped"; return true; }); return; }
        store.Update(d => { d.Deliveries.Single(x => x.Id == delivery.Id).Status = "Sending"; d.State.LastPostAt = DateTimeOffset.UtcNow; return true; });
        try
        {
            await facebook.Publish(group.Url, post.Content, post.Images);
            store.Update(d => { var row = d.Deliveries.Single(x => x.Id == delivery.Id); row.Status = "Submitted"; row.Detail = "Đã bấm gửi. Kiểm tra bài có được nhóm duyệt không."; return true; });
            await Notify($"Đã gửi bài vào {group.Name}. Kiểm tra trạng thái duyệt trên nhóm.", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            store.Update(d => { var row = d.Deliveries.Single(x => x.Id == delivery.Id); row.Status = "NeedsReview"; row.Detail = "Kiểm tra trình duyệt và kết quả gửi trước khi thử lại."; d.State.Paused = true; return true; });
            LastError = "Đăng nhóm chưa xác minh được. Đã tạm dừng để bạn kiểm tra.";
            await Notify(LastError, ct);
        }
    }
    private async Task Inbox(CancellationToken ct)
    {
        // Leave inbox alone until identification is configured; posting can work independently.
        if (string.IsNullOrWhiteSpace(Options.IncomingMessages) || string.IsNullOrWhiteSpace(Options.OutgoingMessages)) return;
        var found = await facebook.DiscoverThreads();
        store.Update(d => { foreach (var t in found) if (!d.Threads.Any(x => x.Url == t.Url)) { t.Status = initializedInbox ? "NewIncoming" : "New"; d.Threads.Add(t); } return true; });
        initializedInbox = true;
        foreach (var thread in store.Read().Threads.Where(t => t.AutoReply).OrderBy(t => t.LastCheckedAt).Take(3))
        {
            if (store.Read().State.Paused) break;
            store.Update(d => { d.Threads.Single(x => x.Id == thread.Id).LastCheckedAt = DateTimeOffset.UtcNow; return true; });
            try
            {
                var message = await facebook.Read(thread.Url);
                if (message is null || message.Id == thread.LastMessageId) continue;
                if (thread.Status == "New")
                { store.Update(d => { var t = d.Threads.Single(x => x.Id == thread.Id); t.LastMessageId = message.Id; t.Status = "Watching"; return true; }); continue; }
                var (reply, booking) = await PrepareReply(thread, message, ct);
                await facebook.Reply(thread.Url, message.Id, reply);
                store.Update(d => { var t = d.Threads.Single(x => x.Id == thread.Id); t.Status = "Watching"; t.LastError = ""; return true; });
                if (booking is not null) await Notify($"LỊCH XEM: {booking.CustomerName} — {booking.Phone}\n{booking.VisitAt.ToOffset(TimeSpan.FromHours(7)):dd/MM HH:mm}\n{thread.Url}", ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                store.Update(d => { var t = d.Threads.Single(x => x.Id == thread.Id); t.AutoReply = false; t.Status = "NeedsReview"; t.LastError = "Kiểm tra phiên Facebook và nhận diện tin nhắn trước khi bật lại."; return true; });
                await Notify($"Đã dừng trả lời {thread.CustomerName}; cần kiểm tra trên web.", ct);
            }
        }
    }
    public async Task<(string Reply, RentalAppointment? Booking)> PrepareReply(RentalThread thread, InboxMessage message, CancellationToken ct)
    {
        var rooms = store.Read().Rooms.Where(r => r.Available).ToList();
        var pending = string.IsNullOrEmpty(thread.PendingAppointment) ? null : JsonSerializer.Deserialize<BookingCandidate>(thread.PendingAppointment, RentalLocalStore.Json);
        RentalAppointment? booking = null;
        BookingCandidate? candidate = null;
        string reply;
        if (pending is not null && new[] { "xac nhan", "dong y", "ok" }.Contains(RentalTemplates.Normalize(message.Text.Trim().TrimEnd('.', '!'))))
        {
            if (pending.VisitAt <= DateTimeOffset.UtcNow || !rooms.Any(r => r.Id == pending.RoomId)) reply = "Phòng hoặc giờ hẹn đã thay đổi. Bạn chọn lại giúp mình nhé.";
            else
            {
                booking = new() { ThreadId = thread.Id, RoomId = pending.RoomId, CustomerName = pending.CustomerName, Phone = pending.Phone, VisitAt = pending.VisitAt, SourceMessageId = message.Id };
                reply = $"Đã ghi nhận lịch xem lúc {pending.VisitAt.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy} cho {pending.CustomerName}, {pending.Phone}. Chủ nhà sẽ liên hệ nếu có thay đổi.";
            }
        }
        else
        {
            var answer = await ai.Answer(rooms, message.Conversation + "\nKhách: " + message.Text, ct);
            reply = answer.Reply;
            var proposed = answer.Appointment;
            if (proposed is not null && rooms.Any(r => r.Id == proposed.RoomId) && proposed.VisitAt > DateTimeOffset.UtcNow &&
                proposed.VisitAt < DateTimeOffset.UtcNow.AddDays(90) && !string.IsNullOrWhiteSpace(proposed.CustomerName) && Regex.IsMatch(proposed.Phone ?? "", @"^(\+84|0)\d{9}$"))
            {
                candidate = proposed;
                reply = $"Xác nhận lịch xem {rooms.Single(r => r.Id == proposed.RoomId).Name} lúc {proposed.VisitAt.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy}, tên {proposed.CustomerName}, điện thoại {proposed.Phone}? Trả lời ‘xác nhận’ để lưu hoặc gửi lại thông tin cần sửa.";
            }
        }
        store.Update(d =>
        {
            var t = d.Threads.Single(x => x.Id == thread.Id);
            if (t.LastMessageId == message.Id) throw new InvalidOperationException("Tin nhắn đã được xử lý.");
            t.LastMessageId = message.Id; t.LastReply = reply; t.Status = "Replying";
            t.PendingAppointment = candidate is null ? "" : JsonSerializer.Serialize(candidate, RentalLocalStore.Json);
            if (booking is not null && !d.Appointments.Any(a => a.ThreadId == t.Id && a.SourceMessageId == message.Id)) d.Appointments.Insert(0, booking);
            return true;
        });
        return (reply, booking);
    }
    public override void Dispose() { telegram.Dispose(); Gate.Dispose(); base.Dispose(); }
}
