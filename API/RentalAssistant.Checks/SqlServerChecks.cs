using Hinet.Model;
using Hinet.Model.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

internal static class SqlServerChecks
{
    public static void Run(Action<bool, string> check)
    {
        using var db = new HinetContext(new DbContextOptionsBuilder<HinetContext>()
            .UseSqlServer("Server=localhost;Database=AI_TOOL;Integrated Security=True;TrustServerCertificate=True")
            .Options, new HttpContextAccessor());
        var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        check(db.Database.GetMigrations().Count() == 12, "all 12 account migrations discovered");
        check(!script.Contains("::uuid") && !script.Contains("gen_random_uuid") && !script.Contains("NOW()")
            && !script.Contains("timestamp without time zone") && !script.Contains("Npgsql"), "migration script contains no PostgreSQL constructs");
        check(script.Contains("IDENTITY") && script.Contains("uniqueidentifier") && script.Contains("nvarchar(128)"), "SQL Server identities and bounded Identity keys generated");
        check(script.Contains("N'Quản trị viên'") && script.Contains("NEWID()"), "Unicode role seed and SQL Server GUIDs generated");
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot!;
        var differences = db.GetService<IMigrationsModelDiffer>().GetDifferences(
            db.GetService<IModelRuntimeInitializer>().Initialize(snapshot.Model).GetRelationalModel(),
            db.GetService<IDesignTimeModel>().Model.GetRelationalModel());
        check(differences.Count == 0, "SQL Server snapshot matches the current model: " + string.Join(", ", differences.Select(x => x.GetType().Name)));
        var user = db.GetService<IDesignTimeModel>().Model.GetEntityTypes().Single(x => x.ClrType == typeof(AppUser))
            .GetSeedData().Single();
        var result = new PasswordHasher<AppUser>().VerifyHashedPassword(new AppUser(), (string)user["PasswordHash"]!, "12345678");
        check(result != PasswordVerificationResult.Failed, "seeded admin password matches 12345678");
    }
}

