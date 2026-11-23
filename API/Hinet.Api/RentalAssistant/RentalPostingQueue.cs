using Microsoft.EntityFrameworkCore;
namespace Hinet.Api;

public record PostingQueueItem(Guid DeliveryId, Guid PostId, Guid GroupId, string RoomName, string GroupName, string GroupUrl, string PostPreview, DateTimeOffset CreatedAt);
public record PostingQueueSnapshot(DateTimeOffset ServerNow, int IntervalSeconds, bool Paused, bool Enabled, bool BrowserEnabled,
    DateTimeOffset? LastStartedAt, DateTimeOffset? NextAllowedAt, int QueuedCount, int EligibleCount, PostingQueueItem? Sending,
    IReadOnlyList<PostingQueueItem> Upcoming, WorkerActivity Activity);

public static class RentalPostingQueue
{
    public static int Interval(RentalState state, RentalOptions options) => Math.Clamp(state.PostIntervalSeconds ?? options.PostIntervalSeconds, 1, 86400);
    public static DateTimeOffset? NextAllowed(RentalState state, RentalOptions options) => state.LastPostAt?.AddSeconds(Interval(state, options));
    public static bool IsDue(RentalState state, RentalOptions options, DateTimeOffset now) => NextAllowed(state, options) is not { } due || now >= due;
    public static async Task<PostingQueueSnapshot> Read(RentalDb db, RentalOptions options, RentalWorkerStatus activity, CancellationToken ct, string? reservedTask = null)
    {
        var state = await db.State.AsNoTracking().SingleAsync(ct);
        var queued = await db.Deliveries.CountAsync(d => d.Status == "Queued", ct);
        var joined = from d in db.Deliveries.AsNoTracking()
                     join p in db.Posts.AsNoTracking() on d.PostId equals p.Id
                     join g in db.Groups.AsNoTracking() on d.GroupId equals g.Id
                     join r in db.Rooms.AsNoTracking() on p.RoomId equals r.Id
                     select new { Delivery = d, Room = r, Group = g, Post = p };
        var eligible = joined.Where(x => x.Delivery.Status == "Queued" && x.Group.Enabled && x.Room.Available);
        var count = await eligible.CountAsync(ct);
        var next = await eligible.OrderBy(x => x.Delivery.CreatedAt).ThenBy(x => x.Delivery.Id)
            .Select(x => new PostingQueueItem(x.Delivery.Id, x.Delivery.PostId, x.Delivery.GroupId, x.Room.Name, x.Group.Name, x.Group.Url, x.Post.Content, x.Delivery.CreatedAt)).Take(5).ToListAsync(ct);
        var sending = await joined.Where(x => x.Delivery.Status == "Sending").OrderBy(x => x.Delivery.CreatedAt).ThenBy(x => x.Delivery.Id)
            .Select(x => new PostingQueueItem(x.Delivery.Id, x.Delivery.PostId, x.Delivery.GroupId, x.Room.Name, x.Group.Name, x.Group.Url, x.Post.Content, x.Delivery.CreatedAt)).FirstOrDefaultAsync(ct);
        PostingQueueItem Clean(PostingQueueItem row)
        {
            var preview = row.PostPreview.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
            return row with { GroupName = RentalGroupNames.Clean(row.GroupName), PostPreview = preview[..Math.Min(preview.Length,160)] };
        }
        next = next.Select(Clean).ToList();
        if (sending is not null) sending = Clean(sending);
        var status = activity.Snapshot();
        if (reservedTask is not null && status.Task == "idle") status = status with { Task = reservedTask };
        return new(DateTimeOffset.UtcNow, Interval(state, options), state.Paused, options.Enabled, options.BrowserEnabled, state.LastPostAt,
            NextAllowed(state, options), queued, count, sending, next, status);
    }
}
