using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Hinet.Api;

public class RentalOptions
{
    public string AiMode { get; set; } = "api";
    public bool Enabled { get; set; }
    public string ManagementKey { get; set; } = "";
    public string TelegramToken { get; set; } = "";
    public long TelegramOwnerId { get; set; }
    public string AiBaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string AiApiKey { get; set; } = "";
    public string AiModel { get; set; } = "";
    public bool BrowserEnabled { get; set; }
    public bool AutoReplyNewConversations { get; set; }
    public string ProfilePath { get; set; } = "App_Data/facebook-profile";
    public int PostIntervalSeconds { get; set; } = 300;
    public int InboxIntervalSeconds { get; set; } = 60;
    public int MetricsIntervalSeconds { get; set; } = 180;
    // Selectors are deliberately configurable: Facebook changes markup and language.
    public string ComposeButton { get; set; } = "[role=button]:has-text('Write something'), [role=button]:has-text('Bạn viết gì đi')";
    public string PostEditor { get; set; } = "[role=dialog] [contenteditable=true][role=textbox]";
    public string PublishButton { get; set; } = "[role=dialog] [role=button]:visible:is([aria-label='Post'], [aria-label='Đăng']), [role=dialog] [role=button]:visible:text-is('Post'), [role=dialog] [role=button]:visible:text-is('Đăng')";
    // Configure these against the logged-in account before enabling inbox auto-reply.
    public string IncomingMessages { get; set; } = "";
    public string OutgoingMessages { get; set; } = "";
    public string MessageIdAttribute { get; set; } = "data-message-id";
    public string MessageEditor { get; set; } = "[contenteditable=true][role=textbox]";
}

public class RentalRoom : IValidatableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(400)] public string Address { get; set; } = "";
    [Range(1, 1000000000)] public decimal MonthlyRent { get; set; }
    [MaxLength(5000)] public string Details { get; set; } = "";
    public bool Available { get; set; } = true;
    public RentalListing Listing { get; set; } = new();
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Listing is null) { yield return new ValidationResult("Thiếu thông tin phòng."); yield break; }
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(Listing, new ValidationContext(Listing), errors, true);
        foreach (var error in errors) yield return error;
        if (Listing.Images is null || Listing.Images.Count > RentalMedia.MaxImages || Listing.Images.Any(id => !RentalMedia.ValidId(id)))
            yield return new ValidationResult("Danh sách ảnh không hợp lệ.");
    }
}
public class RentalGroup
{
    [MaxLength(50)] public string Category { get; set; } = "other";
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(400)] public string Url { get; set; } = "";
    public bool Enabled { get; set; }
}
public class RentalThread
{
    public bool NotifyIncoming { get; set; } = true;
    public bool NotificationBaselineSet { get; set; }
    public bool UnreadNoticeActive { get; set; }
    public string LastNotifiedMessageId { get; set; } = "";
    public string PendingTelegramNotice { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped] public bool HasUnreadMessages { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(150)] public string CustomerName { get; set; } = "";
    [Required, MaxLength(400)] public string Url { get; set; } = "";
    public bool AutoReply { get; set; }
    public string LastMessageId { get; set; } = "";
    public string LastReply { get; set; } = "";
    public string LastError { get; set; } = "";
    public string PendingAppointment { get; set; } = "";
    public DateTimeOffset? LastCheckedAt { get; set; }
    public string Status { get; set; } = "New";
}
public class RentalPost
{
    public List<Guid> TargetGroupIds { get; set; } = new();
    public bool HasGroupSelection { get; set; }
    public List<string> Images { get; set; } = new();
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public long? TelegramUpdateId { get; set; }
    public string Content { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class RentalDelivery
{
    public bool PublicationNoticeRequested { get; set; }
    public DateTimeOffset? PublicationNotifiedAt { get; set; }
    public string PendingTelegramNotice { get; set; } = "";
    [MaxLength(30)] public string TelegramNoticeKind { get; set; } = "";
    [MaxLength(500)] public string PostUrl { get; set; } = "";
    [MaxLength(40)] public string LinkStatus { get; set; } = "AwaitingLink";
    public DateTimeOffset? LinkCheckedAt { get; set; }
    public int? Reactions { get; set; }
    public int? Comments { get; set; }
    public int? Shares { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? MetricsCheckedAt { get; set; }
    public bool MetricsRequested { get; set; }
    public string MetricsError { get; set; } = "";
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Guid GroupId { get; set; }
    public string Status { get; set; } = "Queued";
    public string Detail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class RentalAppointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ThreadId { get; set; }
    public Guid RoomId { get; set; }
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTimeOffset VisitAt { get; set; }
    public string SourceMessageId { get; set; } = "";
    public string Status { get; set; } = "CustomerConfirmed";
}
public class RentalState
{
    [Range(1, 86400)] public int? PostIntervalSeconds { get; set; }
    [Key] public int Id { get; set; } = 1;
    public long TelegramOffset { get; set; }
    public bool Paused { get; set; } = true;
    public DateTimeOffset? LastPostAt { get; set; }
}
public class RentalDb(DbContextOptions<RentalDb> options) : DbContext(options)
{
    public DbSet<RentalRoom> Rooms => Set<RentalRoom>();
    public DbSet<RentalGroup> Groups => Set<RentalGroup>();
    public DbSet<RentalThread> Threads => Set<RentalThread>();
    public DbSet<RentalPost> Posts => Set<RentalPost>();
    public DbSet<RentalDelivery> Deliveries => Set<RentalDelivery>();
    public DbSet<RentalAppointment> Appointments => Set<RentalAppointment>();
    public DbSet<RentalState> State => Set<RentalState>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("rental");
        b.Entity<RentalRoom>().Property(x => x.MonthlyRent).HasPrecision(18, 0);
        var listing = b.Entity<RentalRoom>().Property(x => x.Listing)
            .HasConversion(v => RentalListing.Serialize(v), v => RentalListing.Deserialize(v)).HasDefaultValueSql("N'{}'");
        listing.Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<RentalListing>(
            (a, c) => a == null ? c == null : c != null && RentalListing.Serialize(a) == RentalListing.Serialize(c),
            v => v == null ? 0 : RentalListing.Serialize(v).GetHashCode(),
            v => v == null ? null! : RentalListing.Deserialize(RentalListing.Serialize(v))));
        var images = b.Entity<RentalPost>().Property(x => x.Images)
            .HasConversion(v => RentalListing.SerializeImages(v), v => RentalListing.DeserializeImages(v)).HasDefaultValueSql("N'[]'");
        images.Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
            (a, c) => a == null ? c == null : c != null && a.SequenceEqual(c),
            v => v == null ? 0 : RentalListing.SerializeImages(v).GetHashCode(),
            v => v == null ? null! : v.ToList()));
        b.Entity<RentalGroup>().HasIndex(x => x.Url).IsUnique();
        var targets = b.Entity<RentalPost>().Property(x => x.TargetGroupIds)
            .HasConversion(v => RentalGroupTargets.Serialize(v), v => RentalGroupTargets.Deserialize(v)).HasDefaultValueSql("N'[]'");
        targets.Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Guid>>(
            (a, c) => a == null ? c == null : c != null && a.SequenceEqual(c),
            v => v == null ? 0 : RentalGroupTargets.Serialize(v).GetHashCode(), v => v == null ? null! : v.ToList()));
        b.Entity<RentalThread>().HasIndex(x => x.Url).IsUnique();
        b.Entity<RentalDelivery>().HasIndex(x => new { x.PostId, x.GroupId }).IsUnique();
        b.Entity<RentalPost>().HasIndex(x => x.TelegramUpdateId).IsUnique().HasFilter("[TelegramUpdateId] IS NOT NULL");
        b.Entity<RentalAppointment>().Property(x => x.SourceMessageId).HasMaxLength(250);
        b.Entity<RentalAppointment>().HasIndex(x => new { x.ThreadId, x.SourceMessageId }).IsUnique();
    }
}
public static class RentalBootstrap
{
    public static IServiceCollection AddRentalAssistant(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RentalOptions>(config.GetSection("RentalAssistant"));
        services.AddSingleton<Microsoft.Extensions.Options.IPostConfigureOptions<RentalOptions>, RentalMessengerOptions>();
        services.AddSingleton<RentalMessengerSetup>();
        services.AddSingleton<RentalGroupDiscovery>();
        services.AddSingleton<RentalWorkerStatus>();
        services.AddDbContextFactory<RentalDb>(o => o.UseSqlServer(config["Connections:DefaultConnection"]));
        services.AddHttpClient("RentalExternal", c => c.Timeout = TimeSpan.FromSeconds(90));
        services.AddSingleton<RentalAi>();
        services.AddSingleton<RentalFacebook>();
        services.AddSingleton(sp => new RentalMedia(Path.Combine(sp.GetRequiredService<IWebHostEnvironment>().ContentRootPath, "App_Data", "rental-images")));
        services.AddHostedService<RentalSchemaUpgrade>();
        services.AddHostedService<RentalWorker>();
        return services;
    }
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var factory = services.GetRequiredService<IDbContextFactory<RentalDb>>();
        await using var db = await factory.CreateDbContextAsync();
        var target = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        var database = target.InitialCatalog;
        if (database != "AI_TOOL") throw new InvalidOperationException("Setup only supports database AI_TOOL.");
        target.InitialCatalog = "master";
        await using (var connection = new SqlConnection(target.ConnectionString))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "IF DB_ID(N'AI_TOOL') IS NULL CREATE DATABASE [AI_TOOL];";
            await command.ExecuteNonQueryAsync();
        }
        await db.Database.OpenConnectionAsync();
        using var check = db.Database.GetDbConnection().CreateCommand();
        check.CommandText = "SELECT OBJECT_ID(N'rental.Rooms', N'U')";
        if (await check.ExecuteScalarAsync() is DBNull or null)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            foreach (var batch in System.Text.RegularExpressions.Regex.Split(db.Database.GenerateCreateScript(), @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                if (!string.IsNullOrWhiteSpace(batch)) await RentalSchemaUpgrade.ExecuteLiteralAsync(db, batch);
            await transaction.CommitAsync();
        }
        await RentalSchemaUpgrade.ExecuteLiteralAsync(db, RentalSchemaUpgrade.Sql);
        if (!await db.State.AnyAsync()) { db.State.Add(new RentalState()); await db.SaveChangesAsync(); }
        Console.WriteLine("AI_TOOL: rental schema ready. Existing application migrations were not run.");
    }
}
