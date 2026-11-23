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
        var key = context.HttpContext.RequestServices.GetRequiredService<IOptions<RentalOptions>>().Value.ManagementKey;
        var supplied = context.HttpContext.Request.Headers["X-Rental-Key"].ToString();
        if (key.Length < 32 || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(key)), SHA256.HashData(Encoding.UTF8.GetBytes(supplied))))
            context.Result = new UnauthorizedResult();
    }
}

[ApiController, AllowAnonymous, RentalAccess, Route("api/rental-assistant")]
public class RentalAssistantController(
    RentalDb db, RentalAi ai, RentalFacebook browser, IOptions<RentalOptions> options) : ControllerBase
{
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
        groups = await db.Groups.AsNoTracking().ToListAsync(ct),
        threads = await db.Threads.AsNoTracking().ToListAsync(ct),
        posts = await db.Posts.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct),
        deliveries = await db.Deliveries.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(ct),
        appointments = await db.Appointments.AsNoTracking().OrderByDescending(x => x.VisitAt).Take(200).ToListAsync(ct)
    });

    public record PauseRequest(bool Paused);
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
        var room = await db.Rooms.FindAsync(new object[] { id }, ct);
        if (room is null) { room = new RentalRoom { Id = id }; db.Rooms.Add(room); }
        room.Name = input.Name.Trim(); room.Address = input.Address.Trim();
        room.MonthlyRent = input.MonthlyRent; room.Details = input.Details; room.Available = input.Available;
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
        group.Name = input.Name.Trim(); group.Url = input.Url; group.Enabled = input.Enabled;
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
    public record DraftRequest(Guid RoomId, [Required, MaxLength(2000)] string Instruction);
    [HttpPost("posts")]
    public async Task<IActionResult> Draft(DraftRequest input, CancellationToken ct)
    {
        var room = await db.Rooms.SingleOrDefaultAsync(x => x.Id == input.RoomId && x.Available, ct);
        if (room is null) return BadRequest("Phòng không tồn tại hoặc đã hết.");
        var post = new RentalPost { RoomId = room.Id, Content = await ai.Draft(room, input.Instruction, ct) };
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
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        if (!await db.Posts.AnyAsync(x => x.Id == id, ct)) return NotFound();
        if (!await db.Groups.AnyAsync(x => x.Enabled, ct)) return BadRequest("Hãy bật ít nhất một nhóm được phép đăng.");
        await RentalActions.Approve(db, id, ct);
        return Ok();
    }
    [HttpPost("deliveries/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var changed = await db.Deliveries.Where(x => x.Id == id && x.Status == "Queued")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Cancelled"), ct);
        return changed == 1 ? Ok() : Conflict("Chỉ hủy lượt đang chờ.");
    }
}
