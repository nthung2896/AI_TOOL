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

public class RentalRoom
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(400)] public string Address { get; set; } = "";
    [Range(1, 1000000000)] public decimal MonthlyRent { get; set; }
    [MaxLength(5000)] public string Details { get; set; } = "";
    public bool Available { get; set; } = true;
}
public class RentalGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(400)] public string Url { get; set; } = "";
    public bool Enabled { get; set; }
}
public class RentalThread
{
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
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public long? TelegramUpdateId { get; set; }
    public string Content { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class RentalDelivery
{
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
        b.Entity<RentalGroup>().HasIndex(x => x.Url).IsUnique();
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
        services.AddDbContextFactory<RentalDb>(o => o.UseSqlServer(config["Connections:DefaultConnection"]));
        services.AddHttpClient("RentalExternal", c => c.Timeout = TimeSpan.FromSeconds(90));
        services.AddSingleton<RentalAi>();
        services.AddSingleton<RentalFacebook>();
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
                if (!string.IsNullOrWhiteSpace(batch)) await db.Database.ExecuteSqlRawAsync(batch);
            await transaction.CommitAsync();
        }
        if (!await db.State.AnyAsync()) { db.State.Add(new RentalState()); await db.SaveChangesAsync(); }
        Console.WriteLine("AI_TOOL: rental schema ready. Existing application migrations were not run.");
    }
}
