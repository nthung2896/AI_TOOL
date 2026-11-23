using Microsoft.EntityFrameworkCore;

namespace Hinet.Api;

// Additive upgrade preserves existing rooms and drafts; also used by the database setup command.
public sealed class RentalSchemaUpgrade(IDbContextFactory<RentalDb> factory) : IHostedService
{
    public const string Sql = """
        IF OBJECT_ID(N'rental.Rooms', N'U') IS NOT NULL AND COL_LENGTH(N'rental.Rooms', N'Listing') IS NULL
            ALTER TABLE [rental].[Rooms] ADD [Listing] nvarchar(max) NOT NULL CONSTRAINT DF_RentalRooms_Listing DEFAULT N'{}';
        IF OBJECT_ID(N'rental.Posts', N'U') IS NOT NULL AND COL_LENGTH(N'rental.Posts', N'Images') IS NULL
            ALTER TABLE [rental].[Posts] ADD [Images] nvarchar(max) NOT NULL CONSTRAINT DF_RentalPosts_Images DEFAULT N'[]';
        IF OBJECT_ID(N'rental.Threads', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'rental.Threads', N'NotifyIncoming') IS NULL ALTER TABLE [rental].[Threads] ADD [NotifyIncoming] bit NOT NULL DEFAULT 1;
            IF COL_LENGTH(N'rental.Threads', N'NotificationBaselineSet') IS NULL ALTER TABLE [rental].[Threads] ADD [NotificationBaselineSet] bit NOT NULL DEFAULT 0;
            IF COL_LENGTH(N'rental.Threads', N'UnreadNoticeActive') IS NULL ALTER TABLE [rental].[Threads] ADD [UnreadNoticeActive] bit NOT NULL DEFAULT 0;
            IF COL_LENGTH(N'rental.Threads', N'LastNotifiedMessageId') IS NULL ALTER TABLE [rental].[Threads] ADD [LastNotifiedMessageId] nvarchar(max) NOT NULL DEFAULT N'';
            IF COL_LENGTH(N'rental.Threads', N'PendingTelegramNotice') IS NULL ALTER TABLE [rental].[Threads] ADD [PendingTelegramNotice] nvarchar(max) NOT NULL DEFAULT N'';
        END;
        IF OBJECT_ID(N'rental.Deliveries', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'rental.Deliveries', N'PublicationNoticeRequested') IS NULL ALTER TABLE [rental].[Deliveries] ADD [PublicationNoticeRequested] bit NOT NULL DEFAULT 0;
            IF COL_LENGTH(N'rental.Deliveries', N'PublicationNotifiedAt') IS NULL ALTER TABLE [rental].[Deliveries] ADD [PublicationNotifiedAt] datetimeoffset NULL;
            IF COL_LENGTH(N'rental.Deliveries', N'PendingTelegramNotice') IS NULL ALTER TABLE [rental].[Deliveries] ADD [PendingTelegramNotice] nvarchar(max) NOT NULL DEFAULT N'';
            IF COL_LENGTH(N'rental.Deliveries', N'TelegramNoticeKind') IS NULL ALTER TABLE [rental].[Deliveries] ADD [TelegramNoticeKind] nvarchar(30) NOT NULL DEFAULT N'';
            IF COL_LENGTH(N'rental.Deliveries', N'PostUrl') IS NULL ALTER TABLE [rental].[Deliveries] ADD [PostUrl] nvarchar(500) NOT NULL DEFAULT N'';
            IF COL_LENGTH(N'rental.Deliveries', N'LinkStatus') IS NULL ALTER TABLE [rental].[Deliveries] ADD [LinkStatus] nvarchar(40) NOT NULL DEFAULT N'AwaitingLink';
            IF COL_LENGTH(N'rental.Deliveries', N'LinkCheckedAt') IS NULL ALTER TABLE [rental].[Deliveries] ADD [LinkCheckedAt] datetimeoffset NULL;
            IF COL_LENGTH(N'rental.Deliveries', N'Reactions') IS NULL ALTER TABLE [rental].[Deliveries] ADD [Reactions] int NULL;
            IF COL_LENGTH(N'rental.Deliveries', N'Comments') IS NULL ALTER TABLE [rental].[Deliveries] ADD [Comments] int NULL;
            IF COL_LENGTH(N'rental.Deliveries', N'Shares') IS NULL ALTER TABLE [rental].[Deliveries] ADD [Shares] int NULL;
            IF COL_LENGTH(N'rental.Deliveries', N'SubmittedAt') IS NULL ALTER TABLE [rental].[Deliveries] ADD [SubmittedAt] datetimeoffset NULL;
            IF COL_LENGTH(N'rental.Deliveries', N'MetricsCheckedAt') IS NULL ALTER TABLE [rental].[Deliveries] ADD [MetricsCheckedAt] datetimeoffset NULL;
            IF COL_LENGTH(N'rental.Deliveries', N'MetricsRequested') IS NULL ALTER TABLE [rental].[Deliveries] ADD [MetricsRequested] bit NOT NULL DEFAULT 0;
            IF COL_LENGTH(N'rental.Deliveries', N'MetricsError') IS NULL ALTER TABLE [rental].[Deliveries] ADD [MetricsError] nvarchar(max) NOT NULL DEFAULT N'';
        END;
        """;
    public async Task StartAsync(CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await ExecuteLiteralAsync(db, Sql, ct);
    }
    // ExecuteSqlRaw applies composite formatting even when no SQL parameters are supplied.
    // Keep Sql unchanged for exported scripts; escape literal braces only for the EF execution path.
    public static string EscapeLiteralSql(string sql) => sql.Replace("{", "{{").Replace("}", "}}");
    public static Task<int> ExecuteLiteralAsync(RentalDb db, string sql, CancellationToken ct = default) =>
        db.Database.ExecuteSqlRawAsync(EscapeLiteralSql(sql), ct);
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
