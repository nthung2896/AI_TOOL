using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hinet.Api;

public class RentalWorker(
    IDbContextFactory<RentalDb> factory, IOptions<RentalOptions> options,
    RentalAi ai, RentalFacebook facebook, ILogger<RentalWorker> log) : BackgroundService
{
    private readonly RentalOptions o = options.Value;
    // A plain client avoids HttpClientFactory logging the Telegram token embedded in request URLs.
    private readonly HttpClient telegram = new() { Timeout = TimeSpan.FromSeconds(60) };
    private bool inboxInitialized;
    private DateTimeOffset lastInbox;
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!o.Enabled) return;
        if (string.IsNullOrWhiteSpace(o.ManagementKey) || o.ManagementKey.Length < 32 || o.TelegramOwnerId <= 0 || string.IsNullOrWhiteSpace(o.TelegramToken))
        { log.LogError("RentalAssistant requires ManagementKey (32+ characters), TelegramOwnerId and TelegramToken."); return; }
        // A session-owned SQL lock prevents two processes from posting/replying simultaneously.
        await using var lease = await factory.CreateDbContextAsync(ct);
        try
        {
            await lease.Database.OpenConnectionAsync(ct);
            using var command = lease.Database.GetDbConnection().CreateCommand();
            command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=N'AI_TOOL.RentalWorker', @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=0; SELECT @result;";
            var result = Convert.ToInt32(await command.ExecuteScalarAsync(ct));
            if (result < 0) { log.LogError("Another rental worker owns this database. This instance will not run."); return; }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { log.LogError("Cannot acquire rental worker database lease ({Type}). Initialize the database and restart.", ex.GetType().Name); return; }
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var heartbeat = lease.Database.GetDbConnection().CreateCommand();
                heartbeat.CommandText = "SELECT APPLOCK_MODE('public', 'AI_TOOL.RentalWorker', 'Session')";
                if (await heartbeat.ExecuteScalarAsync(ct) as string != "Exclusive")
                { log.LogError("Rental worker lease lost. Restart is required."); return; }
                await using var db = await factory.CreateDbContextAsync(ct);
                var state = await db.State.SingleAsync(ct);
                // Any operation interrupted by a crash needs human review, never automatic resend.
                await db.Deliveries.Where(x => x.Status == "Sending").ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "NeedsReview"), ct);
                await db.Threads.Where(x => x.Status == "Replying").ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "NeedsReview").SetProperty(x => x.AutoReply, false), ct);
                await PollTelegram(db, state, ct);
                db.ChangeTracker.Clear();
                state = await db.State.SingleAsync(ct);
                if (!state.Paused && o.BrowserEnabled)
                {
                    await Deliver(db, state, ct);
                    if (DateTimeOffset.UtcNow - lastInbox >= TimeSpan.FromSeconds(Math.Max(30, o.InboxIntervalSeconds)))
                    {
                        lastInbox = DateTimeOffset.UtcNow;
                        await Inbox(db, ct);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { log.LogError("Rental worker failed ({Type}). Check database, credentials and selectors.", ex.GetType().Name); }
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
    }

    private async Task<JsonElement> Telegram(string method, object payload, CancellationToken ct)
    {
        using var response = await telegram.PostAsJsonAsync(
            $"https://api.telegram.org/bot{o.TelegramToken}/{method}", payload, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Telegram {method}: HTTP {(int)response.StatusCode}");
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (!result.RootElement.GetProperty("ok").GetBoolean()) throw new InvalidOperationException("Telegram từ chối yêu cầu.");
        return result.RootElement.GetProperty("result").Clone();
    }
    public override void Dispose() { telegram.Dispose(); base.Dispose(); }
    private async Task Notify(string text, CancellationToken ct) =>
        _ = await Telegram("sendMessage", new { chat_id = o.TelegramOwnerId, text = text[..Math.Min(4000, text.Length)] }, ct);

    private async Task PollTelegram(RentalDb db, RentalState state, CancellationToken ct)
    {
        var updates = await Telegram("getUpdates", new { offset = state.TelegramOffset, timeout = 5, allowed_updates = new[] { "message" } }, ct);
        foreach (var update in updates.EnumerateArray())
        {
            var updateId = update.GetProperty("update_id").GetInt64();
            if (update.TryGetProperty("message", out var message) && message.TryGetProperty("text", out var text) &&
                message.GetProperty("chat").GetProperty("type").GetString() == "private" &&
                message.GetProperty("chat").GetProperty("id").GetInt64() == o.TelegramOwnerId &&
                message.GetProperty("from").GetProperty("id").GetInt64() == o.TelegramOwnerId)
            {
                try { await Command(db, state, text.GetString()!, updateId, ct); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Do not expose provider errors containing URLs, tokens, or chat contents.
                    log.LogWarning("Rental command failed ({Type})", ex.GetType().Name);
                    db.ChangeTracker.Clear();
                    state = await db.State.SingleAsync(ct);
                    await Notify("Không thực hiện được lệnh. Kiểm tra cấu hình, ID phòng/bài, phiên Facebook và nhật ký trạng thái trên web.", ct);
                }
            }
            state.TelegramOffset = updateId + 1;
            await db.SaveChangesAsync(ct);
        }
    }
    private async Task Command(RentalDb db, RentalState state, string text, long updateId, CancellationToken ct)
    {
        var parts = text.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLowerInvariant();
        switch (command)
        {
            case "/start": case "/help":
                await Notify("/phong — xem phòng và ID\n/dang ID_PHONG yêu cầu — AI soạn bài\n/duyet ID_BAI — xếp hàng đăng các nhóm đã bật\n/timnhom từ khóa — tìm tối đa 20 nhóm để chọn trên web\n/facebook — mở cửa sổ đăng nhập\n/chay — bật đăng và trả lời\n/dung — tạm dừng\n/lich — lịch xem sắp tới\n/trangthai — kiểm tra\nBạn có thể gõ yêu cầu bình thường nếu chỉ có một phòng còn trống.", ct);
                break;
            case "/facebook": await facebook.OpenLogin(); await Notify("Đã mở Chromium trên máy chạy API. Bạn đăng nhập Facebook tại đó, không gửi mật khẩu qua Telegram.", ct); break;
            case "/dung": state.Paused = true; await db.SaveChangesAsync(ct); await Notify("Đã tạm dừng đăng bài và trả lời tự động.", ct); break;
            case "/chay": state.Paused = false; await db.SaveChangesAsync(ct); await Notify("Đã bật worker. Chỉ đăng nhóm đã chọn và trả lời hội thoại được bật.", ct); break;
            case "/phong":
                var rooms = await db.Rooms.Where(x => x.Available).ToListAsync(ct);
                await Notify(rooms.Count == 0 ? "Chưa có phòng. Thêm phòng trên web trước." : string.Join("\n\n", rooms.Select(r => $"{r.Name} — {r.MonthlyRent:N0}đ\n{r.Id}")), ct); break;
            case "/trangthai": await Notify($"Tạm dừng: {state.Paused}\nFacebook: {facebook.Status}\nĐang chờ đăng: {await db.Deliveries.CountAsync(x => x.Status == "Queued", ct)}", ct); break;
            case "/lich":
                var appointments = await db.Appointments.Where(x => x.VisitAt >= DateTimeOffset.UtcNow).OrderBy(x => x.VisitAt).Take(20).ToListAsync(ct);
                await Notify(appointments.Count == 0 ? "Chưa có lịch xem sắp tới." : string.Join("\n", appointments.Select(a => $"{a.VisitAt.ToOffset(TimeSpan.FromHours(7)):dd/MM HH:mm} — {a.CustomerName} — {a.Phone}")), ct); break;
            case "/timnhom":
                var query = text[(text.IndexOf(' ') + 1)..].Trim();
                if (query == "/timnhom" || query.Length < 3) { await Notify("Dùng /timnhom phòng trọ quận 7", ct); break; }
                var found = await facebook.SearchGroups(query);
                foreach (var group in found) if (!await db.Groups.AnyAsync(x => x.Url == group.Url, ct)) db.Groups.Add(group);
                await db.SaveChangesAsync(ct);
                await Notify($"Tìm thấy {found.Count} nhóm trong kết quả hiện tại. Vào web kiểm tra quy định và bật nhóm muốn đăng. Không tự tham gia nhóm.", ct); break;
            case "/duyet":
                if (parts.Length < 2 || !Guid.TryParse(parts[1], out var postId)) { await Notify("Dùng /duyet ID_BAI", ct); break; }
                await RentalActions.Approve(db, postId, ct); await Notify("Đã xếp hàng đăng vào các nhóm được bật. /chay để worker xử lý.", ct); break;
            default:
                if (command.StartsWith('/') && command != "/dang") { await Notify("Lệnh chưa hỗ trợ. Gõ /help.", ct); break; }
                if (await db.Posts.AnyAsync(x => x.TelegramUpdateId == updateId, ct)) break;
                RentalRoom? room;
                if (command == "/dang" && parts.Length >= 2 && Guid.TryParse(parts[1], out var roomId)) room = await db.Rooms.SingleOrDefaultAsync(x => x.Id == roomId && x.Available, ct);
                else { var available = await db.Rooms.Where(x => x.Available).Take(2).ToListAsync(ct); room = available.Count == 1 ? available[0] : null; }
                if (room is null) { await Notify("Hãy chọn phòng: /phong rồi /dang ID_PHONG yêu cầu viết bài.", ct); break; }
                var post = new RentalPost { RoomId = room.Id, TelegramUpdateId = updateId, Images = room.Listing.Images.ToList(), Content = await ai.Draft(room, text, ct) };
                db.Posts.Add(post); await db.SaveChangesAsync(ct);
                await Notify($"BẢN NHÁP — {room.Name}\n\n{post.Content}\n\nĐăng các nhóm đã bật: /duyet {post.Id}", ct); break;
        }
    }

    private async Task Deliver(RentalDb db, RentalState state, CancellationToken ct)
    {
        if (state.LastPostAt.HasValue && DateTimeOffset.UtcNow - state.LastPostAt < TimeSpan.FromSeconds(Math.Max(60, o.PostIntervalSeconds))) return;
        var delivery = await db.Deliveries.Where(x => x.Status == "Queued").OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (delivery is null) return;
        var post = await db.Posts.SingleAsync(x => x.Id == delivery.PostId, ct);
        var group = await db.Groups.SingleAsync(x => x.Id == delivery.GroupId, ct);
        if (!group.Enabled || !await db.Rooms.AnyAsync(x => x.Id == post.RoomId && x.Available, ct))
        { delivery.Status = "Skipped"; await db.SaveChangesAsync(ct); return; }
        delivery.Status = "Sending"; state.LastPostAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        try
        {
            await facebook.Publish(group.Url, post.Content, post.Images);
            delivery.Status = "Submitted";
            delivery.Detail = "Đã bấm gửi. Cần kiểm tra bài trên nhóm; có thể đang chờ quản trị viên duyệt.";
        }
        catch (Exception ex)
        {
            delivery.Status = "NeedsReview"; delivery.Detail = ex is RentalPublishException ? ex.Message : "Cần kiểm tra phiên Facebook, selector và kết quả gửi trước khi thử lại.";
            state.Paused = true;
        }
        await db.SaveChangesAsync(ct);
        await Notify($"Đăng nhóm {group.Name}: {delivery.Status}\n{delivery.Detail}", ct);
    }

    private async Task Inbox(RentalDb db, CancellationToken ct)
    {
        var discovered = await facebook.DiscoverThreads();
        foreach (var thread in discovered)
        {
            if (await db.Threads.AnyAsync(x => x.Url == thread.Url, ct)) continue;
            thread.Status = inboxInitialized ? "NewIncoming" : "New";
            db.Threads.Add(thread);
        }
        await db.SaveChangesAsync(ct);
        inboxInitialized = true;
        var rooms = await db.Rooms.Where(x => x.Available).ToListAsync(ct);
        foreach (var thread in await db.Threads.Where(x => x.AutoReply).OrderBy(x => x.LastCheckedAt).Take(3).ToListAsync(ct))
        {
            if (await db.State.AsNoTracking().Select(x => x.Paused).SingleAsync(ct)) break;
            try
            {
                thread.LastCheckedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                var message = await facebook.Read(thread.Url);
                if (message is null || message.Id == thread.LastMessageId) continue;
                if (thread.Status == "New")
                { thread.LastMessageId = message.Id; thread.Status = "Watching"; await db.SaveChangesAsync(ct); continue; }
                var booking = string.IsNullOrWhiteSpace(thread.PendingAppointment) ? null : JsonSerializer.Deserialize<BookingCandidate>(thread.PendingAppointment, Json);
                var confirmation = message.Text.Trim().TrimEnd('.', '!').ToLowerInvariant();
                string reply;
                RentalAppointment? appointment = null;
                if (booking is not null && new[] { "xác nhận", "đồng ý", "ok", "xac nhan", "dong y" }.Contains(confirmation))
                {
                    if (booking.VisitAt <= DateTimeOffset.UtcNow || !rooms.Any(r => r.Id == booking.RoomId))
                    { thread.PendingAppointment = ""; reply = "Giờ hẹn hoặc phòng đã thay đổi. Bạn chọn lại phòng và ngày giờ xem giúp mình nhé."; }
                    else
                    {
                        appointment = new RentalAppointment { ThreadId = thread.Id, RoomId = booking.RoomId, CustomerName = booking.CustomerName, Phone = booking.Phone, VisitAt = booking.VisitAt, SourceMessageId = message.Id };
                        db.Appointments.Add(appointment);
                        thread.PendingAppointment = "";
                        reply = $"Đã ghi nhận lịch xem phòng lúc {booking.VisitAt.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy} cho {booking.CustomerName}. Chủ nhà sẽ liên hệ nếu cần thay đổi.";
                    }
                }
                else
                {
                    thread.PendingAppointment = "";
                    var answer = await ai.Answer(rooms, message.Conversation, ct);
                    reply = answer.Reply;
                    var proposed = answer.Appointment;
                    if (proposed is not null && rooms.Any(r => r.Id == proposed.RoomId) &&
                        proposed.VisitAt > DateTimeOffset.UtcNow && proposed.VisitAt < DateTimeOffset.UtcNow.AddDays(90) &&
                        !string.IsNullOrWhiteSpace(proposed.CustomerName) && Regex.IsMatch(proposed.Phone ?? "", @"^(\+84|0)\d{9}$"))
                    {
                        thread.PendingAppointment = JsonSerializer.Serialize(proposed);
                        var room = rooms.Single(r => r.Id == proposed.RoomId);
                        reply = $"Bạn xác nhận lịch xem {room.Name}, {room.Address}, lúc {proposed.VisitAt.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM/yyyy}, tên {proposed.CustomerName}, điện thoại {proposed.Phone}? Trả lời chính xác ‘xác nhận’ để lưu lịch, hoặc gửi thông tin cần sửa.";
                    }
                }
                // Persist before sending: a crash must not cause repeated messages or duplicate bookings.
                thread.Status = "Replying"; thread.LastMessageId = message.Id; thread.LastReply = reply;
                await db.SaveChangesAsync(ct);
                await facebook.Reply(thread.Url, message.Id, reply);
                thread.Status = "Watching"; thread.LastError = "";
                await db.SaveChangesAsync(ct);
                if (appointment is not null) await Notify($"LỊCH XEM PHÒNG\n{appointment.CustomerName} — {appointment.Phone}\n{appointment.VisitAt.ToOffset(TimeSpan.FromHours(7)):dd/MM/yyyy HH:mm}\n{thread.Url}", ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                thread.AutoReply = false; thread.Status = "NeedsReview";
                thread.LastError = "Không thể xác minh/gửi tin nhắn. Kiểm tra trình duyệt trước khi bật lại.";
                await db.SaveChangesAsync(ct);
                await Notify($"Đã dừng tự trả lời {thread.CustomerName}. Cần kiểm tra trên web và Messenger.", ct);
            }
        }
    }
}

public static class RentalActions
{
    public static async Task Approve(RentalDb db, Guid id, CancellationToken ct)
    {
        var post = await db.Posts.SingleAsync(x => x.Id == id, ct);
        if (post.Status != "Draft") return;
        var groups = await db.Groups.Where(x => x.Enabled).ToListAsync(ct);
        if (groups.Count == 0) throw new InvalidOperationException("Chưa chọn nhóm đăng.");
        foreach (var group in groups) db.Deliveries.Add(new RentalDelivery { PostId = id, GroupId = group.Id });
        post.Status = "Approved";
        await db.SaveChangesAsync(ct);
    }
}
