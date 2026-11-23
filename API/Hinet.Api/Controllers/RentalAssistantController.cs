using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Hinet.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hinet.Controllers;

public sealed class RentalAccessAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true || !context.HttpContext.Items.ContainsKey("RentalRuntime"))
            context.Result = new UnauthorizedResult();
    }
}

[ApiController, Authorize, RentalAccess, Route("api/rental-assistant")]
public class RentalAssistantController(
    RentalDb db, RentalRequestRuntime requestRuntime) : ControllerBase
{
    private RentalAi ai => requestRuntime.Value.Ai;
    private RentalFacebook browser => requestRuntime.Value.Browser;
    private IOptions<RentalOptions> options => Options.Create(requestRuntime.Value.Options);
    private RentalMedia media => requestRuntime.Value.Media;
    private RentalMessengerSetup messengerSetup => requestRuntime.Value.Messenger;
    private RentalGroupDiscovery groupDiscovery => requestRuntime.Value.Discovery;
    private RentalWorkerStatus workerStatus => requestRuntime.Value.Status;
    private Task<PostingQueueSnapshot> ReadQueue(CancellationToken ct) => RentalPostingQueue.Read(db, options.Value, workerStatus, ct,
        messengerSetup.Snapshot().State is "Queued" or "Running" ? "messengerSetup" : groupDiscovery.Snapshot().State is "Queued" or "Running" ? "groupSearch" : null);
    [HttpGet("posting/queue")]
    public async Task<IActionResult> PostingQueue(CancellationToken ct) => Ok(await ReadQueue(ct));
    public record PostingSettingsRequest([Range(1, 86400)] int IntervalSeconds);
    [HttpPut("posting/settings")]
    public async Task<IActionResult> PostingSettings(PostingSettingsRequest input, CancellationToken ct)
    {
        var state = await db.State.SingleAsync(ct);
        state.PostIntervalSeconds = input.IntervalSeconds;
        await db.SaveChangesAsync(ct);
        return Ok(await ReadQueue(ct));
    }
    public record GroupSearchRequest([Required, MaxLength(50)] string Category, [MaxLength(100)] string Location = "", [MaxLength(500)] string Keyword = "");
    [HttpGet("groups/discover")]
    public IActionResult GroupSearchStatus() => Ok(groupDiscovery.Snapshot());
    [HttpPost("groups/discover")]
    public IActionResult StartGroupSearch(GroupSearchRequest input)
    {
        if (!options.Value.Enabled || !options.Value.BrowserEnabled) return BadRequest("Bật worker và BrowserEnabled để tìm nhóm bằng Facebook.");
        try { groupDiscovery.Start(input.Category, input.Location ?? "", input.Keyword ?? ""); return Ok(groupDiscovery.Snapshot()); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
    public record MessengerSetupRequest([Required, MaxLength(400)] string Url);
    [HttpGet("messenger/setup")]
    public IActionResult MessengerSetupStatus() => Ok(messengerSetup.Snapshot());
    [HttpPost("messenger/setup")]
    public IActionResult StartMessengerSetup(MessengerSetupRequest input)
    {
        if (!options.Value.Enabled || !options.Value.BrowserEnabled) return BadRequest("Bật worker và BrowserEnabled để mở thiết lập Messenger.");
        try { messengerSetup.Start(input.Url); return Ok(messengerSetup.Snapshot()); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
    [HttpPost("messenger/setup/cancel")]
    public IActionResult CancelMessengerSetup() { messengerSetup.Cancel(); return Ok(); }
    [HttpPost("images"), RequestSizeLimit(RentalMedia.MaxRequestBytes)]
    public async Task<IActionResult> UploadImages(CancellationToken ct)
    {
        if (!Request.HasFormContentType) return BadRequest("Chọn ảnh để tải lên.");
        try { return Ok(await media.Upload((await Request.ReadFormAsync(ct)).Files, ct)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct) => Ok(new
    {
        state = await db.State.AsNoTracking().SingleAsync(ct),
        browser = browser.Status,
        enabled = options.Value.Enabled,
        browserEnabled = options.Value.BrowserEnabled,
        aiConfigured = !string.IsNullOrWhiteSpace(options.Value.AiApiKey) && !string.IsNullOrWhiteSpace(options.Value.AiModel),
        telegramConfigured = !string.IsNullOrWhiteSpace(options.Value.TelegramToken) && options.Value.TelegramOwnerId > 0,
        rooms = await db.Rooms.AsNoTracking().ToListAsync(ct),
        groups = RentalGroupNames.CleanGroups(await db.Groups.AsNoTracking().ToListAsync(ct)),
        threads = await db.Threads.AsNoTracking().ToListAsync(ct),
        posts = await db.Posts.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct),
        deliveries = await db.Deliveries.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(ct),
        appointments = await db.Appointments.AsNoTracking().OrderByDescending(x => x.VisitAt).Take(200).ToListAsync(ct)
    });

    public record PauseRequest(bool Paused);
    public record TrackingRequest([Required, MaxLength(500)] string PostUrl);
    [HttpPut("deliveries/{id:guid}/tracking")]
    public async Task<IActionResult> Track(Guid id, TrackingRequest input, CancellationToken ct)
    {
        if (!RentalEngagement.ValidPostUrl(input.PostUrl)) return BadRequest("Dùng link https://www.facebook.com/groups/TEN_NHOM/posts/ID_BAI hoặc /permalink/ID_BAI.");
        var row = await db.Deliveries.FindAsync(new object[] { id }, ct);
        if (row is null) return NotFound();
        if (row.Status != "Submitted") return BadRequest("Chỉ theo dõi tương tác cho lượt đã gửi.");
        var url = RentalEngagement.Canonical(input.PostUrl);
        if (await db.Deliveries.AnyAsync(d => d.Id != id && d.PostUrl == url, ct)) return Conflict("Link này đã được gắn cho một lượt gửi khác.");
        if (row.PostUrl != url) { row.Reactions = null; row.Comments = null; row.Shares = null; row.MetricsCheckedAt = null; }
        row.PostUrl = url; row.MetricsRequested = true; row.MetricsError = "";
        row.LinkStatus = "Found";
        await db.SaveChangesAsync(ct);
        return Ok();
    }
    [HttpPost("metrics/sync")]
    public async Task<IActionResult> SyncMetrics(CancellationToken ct)
    {
        if (!options.Value.Enabled || !options.Value.BrowserEnabled) return BadRequest("Bật worker và trình duyệt Facebook trong cấu hình để đồng bộ.");
        var count = await db.Deliveries.Where(d => d.Status == "Submitted" && d.PostUrl != "")
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.MetricsRequested, true), ct);
        count += await db.Deliveries.Where(d => d.Status == "Submitted" && d.PostUrl == "")
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.LinkCheckedAt, (DateTimeOffset?)null).SetProperty(d => d.LinkStatus, "AwaitingLink"), ct);
        return Ok(new { queued = count });
    }
    [HttpPost("deliveries/{id:guid}/find-link")]
    public async Task<IActionResult> FindLink(Guid id, CancellationToken ct)
    {
        if (!options.Value.Enabled || !options.Value.BrowserEnabled) return BadRequest("Bật worker và trình duyệt Facebook để tự tìm link bài.");
        var count = await db.Deliveries.Where(d => d.Id == id && d.Status == "Submitted" && d.PostUrl == "")
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.LinkCheckedAt, (DateTimeOffset?)null).SetProperty(d => d.LinkStatus, "AwaitingLink"), ct);
        return count == 1 ? Ok() : BadRequest("Bài đã có link hoặc chưa được gửi.");
    }
    [HttpPut("state")]
    public async Task<IActionResult> State(PauseRequest input, CancellationToken ct)
    {
        var state = await db.State.SingleAsync(ct);
        state.Paused = input.Paused;
        await db.SaveChangesAsync(ct);
        return Ok();
    }
    [HttpPut("rooms/{id:guid}")]
    public async Task<IActionResult> Room(Guid id, RentalRoom input, CancellationToken ct)
    {
        try { media.ValidateImages(input.Listing.Images); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        var room = await db.Rooms.FindAsync(new object[] { id }, ct);
        if (room is null) { room = new RentalRoom { Id = id }; db.Rooms.Add(room); }
        room.Name = input.Name.Trim(); room.Address = input.Address.Trim();
        room.MonthlyRent = input.MonthlyRent; room.Details = input.Details; room.Available = input.Available;
        room.Listing = input.Listing;
        await db.SaveChangesAsync(ct);
        return Ok(room);
    }
    [HttpPut("groups/{id:guid}")]
    public async Task<IActionResult> Group(Guid id, RentalGroup input, CancellationToken ct)
    {
        if (!RentalFacebook.ValidUrl(input.Url, true)) return BadRequest("Cần URL https://www.facebook.com/groups/ID, không có tham số.");
        input.Url = RentalFacebook.CanonicalUrl(input.Url);
        if (await db.Groups.AnyAsync(x => x.Url == input.Url && x.Id != id, ct)) return Conflict("Nhóm đã tồn tại.");
        var group = await db.Groups.FindAsync(new object[] { id }, ct);
        if (group is null) { group = new RentalGroup { Id = id }; db.Groups.Add(group); }
        group.Name = RentalGroupNames.Clean(input.Name); group.Url = input.Url; group.Enabled = input.Enabled;
        group.Category = RentalGroupDiscovery.Categories.ContainsKey(input.Category ?? "") ? input.Category : "other";
        await db.SaveChangesAsync(ct);
        return Ok(group);
    }
    [HttpPut("threads/{id:guid}")]
    public async Task<IActionResult> Thread(Guid id, RentalThread input, CancellationToken ct)
    {
        if (!RentalFacebook.ValidUrl(input.Url, false)) return BadRequest("Cần URL https://www.facebook.com/messages/t/ID, không có tham số.");
        input.Url = RentalFacebook.CanonicalUrl(input.Url);
        if (await db.Threads.AnyAsync(x => x.Url == input.Url && x.Id != id, ct)) return Conflict("Hội thoại đã tồn tại.");
        var thread = await db.Threads.FindAsync(new object[] { id }, ct);
        if (thread is null) { thread = new RentalThread { Id = id }; db.Threads.Add(thread); }
        if (thread.Url != input.Url) { thread.LastMessageId = ""; thread.PendingAppointment = ""; thread.Status = "New"; }
        thread.CustomerName = input.CustomerName.Trim(); thread.Url = input.Url; thread.AutoReply = input.AutoReply;
        // A NeedsReview reply is never automatically retried. The next distinct inbound message resumes processing.
        if (thread.Status == "NeedsReview") thread.Status = "Watching";
        await db.SaveChangesAsync(ct);
        return Ok(thread);
    }
    public record DraftRequest(Guid RoomId, [Required, MaxLength(2000)] string Instruction, List<Guid>? GroupIds = null);
    [HttpPost("posts")]
    public async Task<IActionResult> Draft(DraftRequest input, CancellationToken ct)
    {
        var room = await db.Rooms.SingleOrDefaultAsync(x => x.Id == input.RoomId && x.Available, ct);
        if (room is null) return BadRequest("Phòng không tồn tại hoặc đã hết.");
        try { if (input.GroupIds is not null) RentalGroupTargets.Select(await db.Groups.AsNoTracking().ToListAsync(ct), input.GroupIds); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        var post = new RentalPost { RoomId = room.Id, Content = await ai.Draft(room, input.Instruction, ct), Images = room.Listing.Images.ToList() };
        post.HasGroupSelection = input.GroupIds is not null;
        post.TargetGroupIds = input.GroupIds?.Distinct().ToList() ?? new();
        db.Posts.Add(post); await db.SaveChangesAsync(ct);
        return Ok(post);
    }
    public record EditPostRequest([Required, MaxLength(10000)] string Content);
    [HttpPut("posts/{id:guid}")]
    public async Task<IActionResult> EditPost(Guid id, EditPostRequest input, CancellationToken ct)
    {
        var post = await db.Posts.FindAsync(new object[] { id }, ct);
        if (post is null) return NotFound();
        if (post.Status != "Draft") return Conflict("Chỉ sửa bản nháp.");
        post.Content = input.Content; await db.SaveChangesAsync(ct);
        return Ok(post);
    }
    [HttpPost("posts/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ApproveRequest? input)
    {
        if (!await db.Posts.AnyAsync(x => x.Id == id, ct)) return NotFound();
        if (!await db.Groups.AnyAsync(x => x.Enabled, ct)) return BadRequest("Hãy bật ít nhất một nhóm được phép đăng.");
        try { await RentalActions.Approve(db, id, ct, input?.GroupIds); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        return Ok();
    }
    public record ApproveRequest(List<Guid>? GroupIds);
    [HttpPost("deliveries/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var changed = await db.Deliveries.Where(x => x.Id == id && x.Status == "Queued")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Cancelled"), ct);
        return changed == 1 ? Ok() : Conflict("Chỉ hủy lượt đang chờ.");
    }
}
