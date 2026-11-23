using System.Security.Claims;
using Hinet.Api;
using Hinet.Controllers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

static class SaasChecks
{
    public static void Run(Action<bool, string> check)
    {
        const string connection = "Server=localhost;Database=AI_TOOL;Integrated Security=true;TrustServerCertificate=true";
        var a = new RentalTenant { UserId = Guid.NewGuid(), ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        var b = new RentalTenant { UserId = Guid.NewGuid(), ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
        using var da = new RentalCustomerDbFactory(connection, a.Schema).CreateDbContext();
        using var db = new RentalCustomerDbFactory(connection, b.Schema).CreateDbContext();
        var sa = da.Database.GenerateCreateScript(); var sb = db.Database.GenerateCreateScript();
        check(a.Schema != b.Schema && sa.Contains($"[{a.Schema}].[Rooms]") && !sa.Contains($"[{b.Schema}]"), "customer A SQL targets only its schema");
        check(sb.Contains($"[{b.Schema}].[Rooms]") && !sb.Contains($"[{a.Schema}]"), "EF model cache cannot reuse another customer's schema");
        check(!ReferenceEquals(da.Model, db.Model), "different customers receive distinct cached EF models");
        check(sa.Contains($"[{a.Schema}].[State]") && sa.Contains($"[{a.Schema}].[Threads]"), "queue state and inbox are isolated too");
        check(a.Active && !new RentalTenant { Enabled = false, ExpiresAt = a.ExpiresAt }.Active && !new RentalTenant { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) }.Active, "disabled and expired leases fail closed");
        check(new RentalTenant { Legacy = true }.Schema == "rental", "existing owner's data remains in original schema");
        check(RentalTenantHost.CustomerDirectory("root", a) != RentalTenantHost.CustomerDirectory("root", b), "browser, media and selectors use separate directories");
        check(RentalTenantHost.BotFingerprint("token") == RentalTenantHost.BotFingerprint(" token ") && RentalTenantHost.BotFingerprint("") is null, "Telegram bot uniqueness uses normalized fingerprint");
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, a.UserId.ToString()) }, "test"));
        var context = new ActionExecutingContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
        new RentalAccessAttribute().OnActionExecuting(context);
        check(context.Result is UnauthorizedResult, "JWT without validated customer runtime is rejected");
        http.Items["RentalRuntime"] = new object(); context.Result = null;
        new RentalAccessAttribute().OnActionExecuting(context);
        check(context.Result is null, "validated authenticated customer context is accepted");
        var protector = new EphemeralDataProtectionProvider().CreateProtector("test").ToTimeLimitedDataProtector();
        var cookie = protector.Protect("media:" + a.UserId, TimeSpan.FromMinutes(1));
        check(protector.Unprotect(cookie) == "media:" + a.UserId && !cookie.Contains(a.UserId.ToString()), "media cookie is protected and time limited");
    }

    // Real SQL integration uses newly generated schemas only; it never adopts owner data,
    // starts Facebook, polls Telegram or changes existing user accounts.
    public static async Task Integration(Action<bool, string> check)
    {
        var root = Path.GetFullPath("API/Hinet.Api");
        var config = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", true).Build();
        var services = new ServiceCollection().AddLogging().AddHttpClient().BuildServiceProvider();
        var env = new TestEnvironment { ContentRootPath = Path.GetFullPath(".verification/saas-integration") };
        Directory.CreateDirectory(env.ContentRootPath);
        var protection = new EphemeralDataProtectionProvider();
        using var host = new RentalTenantHost(services, config, env, protection, services.GetRequiredService<ILogger<RentalTenantHost>>());
        var tenants = new[] { new RentalTenant { ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) }, new RentalTenant { ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) } };
        try
        {
            var first = await host.Get(tenants[0], default);
            var second = await host.Get(tenants[1], default);
            check(!ReferenceEquals(first.Browser, second.Browser) && first.Options.ProfilePath != second.Options.ProfilePath, "real runtimes have separate browser instances and profiles");
            await using var a = first.Factory.CreateDbContext(); await using var b = second.Factory.CreateDbContext();
            var roomA = new RentalRoom { Name = "A private", Address = "A", MonthlyRent = 1 };
            var roomB = new RentalRoom { Name = "B private", Address = "B", MonthlyRent = 1 };
            a.Rooms.Add(roomA); b.Rooms.Add(roomB); await a.SaveChangesAsync(); await b.SaveChangesAsync();
            check(await a.Rooms.CountAsync() == 1 && await b.Rooms.CountAsync() == 1, "real SQL room lists remain separate");
            check(await b.Rooms.FindAsync(roomA.Id) is null && await a.Rooms.FindAsync(roomB.Id) is null, "guessing another customer's object ID cannot read it");
            var state = await a.State.SingleAsync(); state.PostIntervalSeconds = 123; await a.SaveChangesAsync();
            check((await b.State.SingleAsync()).PostIntervalSeconds is null, "countdown settings cannot alter another customer");
            check(first.Options.TelegramToken == "" && second.Options.TelegramToken == "" && !first.Options.BrowserEnabled, "new tenants never inherit owner's Telegram or Facebook settings");
            var secret = host.Protect(new(true, "123456:fake-token-for-isolation-test", 123));
            tenants[0].ProtectedSettings = secret;
            check(host.Settings(tenants[0]).TelegramOwnerId == 123 && !secret.Contains("fake-token"), "customer bot credentials round-trip encrypted");
            var image = Guid.NewGuid().ToString("N") + ".png";
            var imageDir = Path.Combine(RentalTenantHost.CustomerDirectory(env.ContentRootPath, tenants[0]), "images");
            Directory.CreateDirectory(imageDir); await File.WriteAllBytesAsync(Path.Combine(imageDir, image), new byte[] { 1 });
            check(File.Exists(first.Media.Resolve(image)), "owner can resolve own media");
            bool denied = false; try { second.Media.Resolve(image); } catch (ArgumentException) { denied = true; }
            check(denied, "customer cannot resolve another customer's media ID");
        }
        finally
        {
            foreach (var tenant in tenants)
            {
                await host.Restart(tenant.Id);
                if (!System.Text.RegularExpressions.Regex.IsMatch(tenant.Schema, @"\Acustomer_[a-f0-9]{32}\z")) throw new InvalidOperationException();
                await using var db = new RentalCustomerDbFactory(config["Connections:DefaultConnection"]!, tenant.Schema).CreateDbContext();
                var tables = new[] { "Appointments", "Deliveries", "Posts", "Threads", "Groups", "Rooms", "State" };
                foreach (var table in tables) await RentalSchemaUpgrade.ExecuteLiteralAsync(db, $"DROP TABLE IF EXISTS [{tenant.Schema}].[{table}];");
                await RentalSchemaUpgrade.ExecuteLiteralAsync(db, $"IF SCHEMA_ID(N'{tenant.Schema}') IS NOT NULL EXEC(N'DROP SCHEMA [{tenant.Schema}]');");
            }
            await services.DisposeAsync();
        }
    }
    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Checks";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
