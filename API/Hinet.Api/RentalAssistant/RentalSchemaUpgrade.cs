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
