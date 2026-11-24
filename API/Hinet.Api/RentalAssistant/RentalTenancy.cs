using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Hinet.Model.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Hinet.Api;

// The account catalog is separate from customer databases. Schema names are generated
// exclusively from server-owned GUIDs, never request headers or customer input.
public sealed class RentalTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool Legacy { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string ProtectedSettings { get; set; } = "";
    public string? BotHash { get; set; }
    public long? TokensValidAfter { get; set; }
    public bool Active => Enabled && ExpiresAt > DateTimeOffset.UtcNow;
    public string Schema => Legacy ? "rental" : "customer_" + Id.ToString("N");
}

public sealed class RentalCatalog(DbContextOptions<RentalCatalog> options) : DbContext(options)
{
    public DbSet<RentalTenant> Tenants => Set<RentalTenant>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<RentalTenant>().ToTable("Customers", "rental");
        b.Entity<RentalTenant>().Ignore(x => x.Active).Ignore(x => x.Schema);
        b.Entity<RentalTenant>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<RentalTenant>().HasIndex(x => x.Legacy).IsUnique().HasFilter("[Legacy] = 1");
        b.Entity<RentalTenant>().Property(x => x.Name).HasMaxLength(150);
        b.Entity<RentalTenant>().Property(x => x.BotHash).HasMaxLength(64);
        b.Entity<RentalTenant>().HasIndex(x => x.BotHash).IsUnique().HasFilter("[BotHash] IS NOT NULL");
    }
}

public record RentalCustomerSettings(bool BrowserEnabled = false, string TelegramToken = "", long TelegramOwnerId = 0, bool Configured = false);

public sealed class RentalSchemaCacheKey : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) => (context.GetType(), ((RentalDb)context).Schema, designTime);
    public object Create(DbContext context) => Create(context, false);
}

public sealed class RentalCustomerDbFactory(string connection, string schema) : IDbContextFactory<RentalDb>
{
    private readonly DbContextOptions<RentalDb> options = new DbContextOptionsBuilder<RentalDb>()
        .UseSqlServer(connection).ReplaceService<IModelCacheKeyFactory, RentalSchemaCacheKey>().Options;
    public RentalDb CreateDbContext() => new(options) { Schema = schema };
    public Task<RentalDb> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
}

public sealed class RentalCustomerRuntime : IAsyncDisposable
{
    public required RentalTenant Tenant { get; init; }
    public required RentalOptions Options { get; init; }
    public required RentalCustomerDbFactory Factory { get; init; }
    public required RentalFacebook Browser { get; init; }
    public required RentalMedia Media { get; init; }
    public required RentalAi Ai { get; init; }
    public required RentalMessengerSetup Messenger { get; init; }
    public required RentalGroupDiscovery Discovery { get; init; }
    public required RentalWorkerStatus Status { get; init; }
    public required RentalWorker Worker { get; init; }
    public async ValueTask DisposeAsync()
    {
        // Never dispose a browser until its worker has stopped using it.
        await Worker.StopAsync(CancellationToken.None);
        Worker.Dispose();
        using var lease = await Browser.Acquire();
        await Browser.DisposeAsync();
    }
}

public sealed class RentalTenantHost(IServiceProvider services, IConfiguration config, IWebHostEnvironment env,
    IDataProtectionProvider protection, ILogger<RentalTenantHost> log) : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, RentalCustomerRuntime> runtimes = new();
    private readonly SemaphoreSlim gate = new(1);
    public IDataProtector Protector { get; } = protection.CreateProtector("AI_TOOL.CustomerSettings.v1");
    public ITimeLimitedDataProtector MediaProtector { get; } = protection.CreateProtector("AI_TOOL.CustomerMedia.v1").ToTimeLimitedDataProtector();
    public RentalCustomerSettings Settings(RentalTenant tenant)
    {
        var saved = string.IsNullOrEmpty(tenant.ProtectedSettings) ? new() : JsonSerializer.Deserialize<RentalCustomerSettings>(Protector.Unprotect(tenant.ProtectedSettings)) ?? new RentalCustomerSettings();
        if (!tenant.Legacy || saved.Configured) return saved;
        // Preserve environment-specific owner settings until the owner explicitly saves
        // the web form. Starting a staging/test process must not replace production secrets.
        var legacy = config.GetSection("RentalAssistant").Get<RentalOptions>() ?? new();
        return new(legacy.BrowserEnabled, legacy.TelegramToken, legacy.TelegramOwnerId);
    }
    public string Protect(RentalCustomerSettings settings) => Protector.Protect(JsonSerializer.Serialize(settings));
    public static string CustomerDirectory(string root, RentalTenant tenant) => Path.Combine(root, "App_Data", "customers", tenant.Id.ToString("N"));

    public async Task<RentalCustomerRuntime> Get(RentalTenant tenant, CancellationToken ct)
    {
        if (!tenant.Active) throw new UnauthorizedAccessException("Tài khoản đã khóa hoặc hết hạn.");
        await gate.WaitAsync(ct);
        try
        {
            if (runtimes.TryGetValue(tenant.Id, out var existing)) return existing;
            var o = config.GetSection("RentalAssistant").Get<RentalOptions>() ?? new();
            var directory = CustomerDirectory(env.ContentRootPath, tenant);
            Directory.CreateDirectory(directory);
            var settings = Settings(tenant);
            o.BrowserEnabled = settings.BrowserEnabled;
            o.TelegramToken = settings.TelegramToken;
            o.TelegramOwnerId = settings.TelegramOwnerId;
            if (!tenant.Legacy)
            {
                o.ProfilePath = Path.Combine(directory, "facebook-profile");
                o.MessengerSettingsPath = Path.Combine(directory, "messenger-selectors.json");
                o.IncomingMessages = ""; o.OutgoingMessages = "";
            }
            o.Schema = tenant.Schema;
            o.Headless = !tenant.Legacy;
            o.AccountExpiresAt = tenant.ExpiresAt;
            o.AccountId = tenant.Id;
            o.Enabled = true;
            o.ManagementKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); // Internal worker guard only, never exposed.
            RentalMessengerSetup.Load(o.MessengerSettingsPath ?? Path.Combine(env.ContentRootPath, "App_Data", "messenger-selectors.json"), o);
            var factory = new RentalCustomerDbFactory(config["Connections:DefaultConnection"]!, tenant.Schema);
            await using (var db = factory.CreateDbContext())
            {
                await db.Database.OpenConnectionAsync(ct);
                // Transactional application lock also serializes provisioning across server processes.
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_getapplock @Resource={0}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=60000", new object[] { "provision-" + tenant.Schema }, ct);
                using var check = db.Database.GetDbConnection().CreateCommand();
                check.Transaction = transaction.GetDbTransaction();
                check.CommandText = "SELECT OBJECT_ID(@table, 'U')";
                var p = check.CreateParameter(); p.ParameterName = "@table"; p.Value = tenant.Schema + ".Rooms"; check.Parameters.Add(p);
                if (await check.ExecuteScalarAsync(ct) is null or DBNull)
                    foreach (var batch in System.Text.RegularExpressions.Regex.Split(db.Database.GenerateCreateScript(), @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline))
                        if (!string.IsNullOrWhiteSpace(batch)) await RentalSchemaUpgrade.ExecuteLiteralAsync(db, batch, ct);
                await RentalSchemaUpgrade.ExecuteLiteralAsync(db, RentalSchemaUpgrade.Sql.Replace("rental.", tenant.Schema + ".").Replace("[rental]", "[" + tenant.Schema + "]"), ct);
                if (!await db.State.AnyAsync(ct)) { db.State.Add(new RentalState { Id = 0 }); await db.SaveChangesAsync(ct); }
                await transaction.CommitAsync(ct);
            }
            var opts = Microsoft.Extensions.Options.Options.Create(o);
            var media = new RentalMedia(tenant.Legacy ? Path.Combine(env.ContentRootPath, "App_Data", "rental-images") : Path.Combine(directory, "images"));
            var browser = new RentalFacebook(opts, media);
            var ai = new RentalAi(services.GetRequiredService<IHttpClientFactory>(), opts);
            var messenger = new RentalMessengerSetup(env, opts);
            var discovery = new RentalGroupDiscovery();
            var status = new RentalWorkerStatus();
            var worker = new RentalWorker(factory, opts, ai, browser, services.GetRequiredService<ILogger<RentalWorker>>(), messenger, discovery, status);
            var runtime = new RentalCustomerRuntime { Tenant = tenant, Options = o, Factory = factory, Browser = browser, Ai = ai, Media = media, Messenger = messenger, Discovery = discovery, Status = status, Worker = worker };
            runtimes[tenant.Id] = runtime;
            if (config.GetValue("RentalSaas:RunWorkers", true)) await worker.StartAsync(CancellationToken.None);
            return runtime;
        }
        finally { gate.Release(); }
    }
    public async Task Restart(Guid id)
    {
        await gate.WaitAsync();
        try { if (runtimes.TryRemove(id, out var runtime)) await runtime.DisposeAsync(); }
        finally { gate.Release(); }
    }
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RentalCatalog>();
            await db.Database.ExecuteSqlRawAsync("""
                IF SCHEMA_ID(N'rental') IS NULL EXEC(N'CREATE SCHEMA rental');
                IF OBJECT_ID(N'rental.Customers', N'U') IS NULL
                CREATE TABLE rental.Customers (Id uniqueidentifier NOT NULL PRIMARY KEY, UserId uniqueidentifier NOT NULL,
                    Name nvarchar(150) NOT NULL, Enabled bit NOT NULL, Legacy bit NOT NULL, ExpiresAt datetimeoffset NOT NULL,
                    ProtectedSettings nvarchar(max) NOT NULL, CONSTRAINT UQ_RentalCustomer_User UNIQUE(UserId));
                IF COL_LENGTH(N'rental.Customers', N'BotHash') IS NULL ALTER TABLE rental.Customers ADD BotHash nvarchar(64) NULL;
                IF COL_LENGTH(N'rental.Customers', N'TokensValidAfter') IS NULL ALTER TABLE rental.Customers ADD TokensValidAfter bigint NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_RentalCustomer_Bot' AND object_id=OBJECT_ID(N'rental.Customers'))
                    CREATE UNIQUE INDEX UX_RentalCustomer_Bot ON rental.Customers(BotHash) WHERE BotHash IS NOT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_RentalCustomer_Legacy' AND object_id=OBJECT_ID(N'rental.Customers'))
                    CREATE UNIQUE INDEX UX_RentalCustomer_Legacy ON rental.Customers(Legacy) WHERE Legacy = 1;
                """, ct);
            // Adopt the existing installation once, for the explicitly configured owner only.
            var ownerName = config["RentalSaas:OwnerUserName"] ?? "admin";
            var owner = await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByNameAsync(ownerName);
            if (owner is null) throw new InvalidOperationException("RentalSaas:OwnerUserName must identify an existing account. Initialize account tables first.");
            if (owner is not null && !await db.Tenants.AnyAsync(x => x.UserId == owner.Id, ct))
            {
                if (await db.Tenants.AnyAsync(x => x.Legacy, ct)) throw new InvalidOperationException("Existing owner workspace already adopted. Transfer ownership explicitly before changing OwnerUserName.");
                var legacy = config.GetSection("RentalAssistant").Get<RentalOptions>() ?? new();
                db.Tenants.Add(new() { UserId = owner.Id, Name = "Chủ hệ thống", Legacy = true, ExpiresAt = DateTimeOffset.UtcNow.AddYears(100),
                    ProtectedSettings = Protect(new(legacy.BrowserEnabled, legacy.TelegramToken, legacy.TelegramOwnerId)), BotHash = BotFingerprint(legacy.TelegramToken) });
                await db.SaveChangesAsync(ct);
            }
            await scope.ServiceProvider.GetRequiredService<RentalAdministration>().Seed(await db.Tenants.Where(t => !t.Legacy).Select(t => t.UserId).ToListAsync(ct), ct);
        }
        try
        {
            while (!ct.IsCancellationRequested)
            {
                using var scope = services.CreateScope();
                var tenants = await scope.ServiceProvider.GetRequiredService<RentalCatalog>().Tenants.AsNoTracking().ToListAsync(ct);
                foreach (var runtime in runtimes.Values)
                    if (!tenants.Any(t => t.Id == runtime.Tenant.Id && t.Active)) await Restart(runtime.Tenant.Id);
                foreach (var tenant in tenants.Where(t => t.Active))
                    try { await Get(tenant, ct); }
                    catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError("Customer runtime could not start: {Id} ({Type})", tenant.Id, ex.GetType().Name); }
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        finally { foreach (var id in runtimes.Keys) await Restart(id); }
    }
    public static string? BotFingerprint(string token) => string.IsNullOrWhiteSpace(token) ? null : Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token.Trim())));
}

public sealed class RentalTenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, RentalCatalog catalog, RentalTenantHost host, UserManager<AppUser> users, IConfiguration config, RentalAdministration administration)
    {
        var path = ctx.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("/rental-media", StringComparison.OrdinalIgnoreCase)) { await next(ctx); return; }
        var isRental = path.StartsWith("/api/rental-assistant", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/rental-account", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/rental-media", StringComparison.OrdinalIgnoreCase);
        if (path.StartsWith("/rental-media", StringComparison.OrdinalIgnoreCase) && ctx.User.Identity?.IsAuthenticated != true && ctx.Request.Cookies.TryGetValue("rental_media", out var mediaCookie))
        {
            try
            {
                var value = host.MediaProtector.Unprotect(mediaCookie);
                var parts = value.Split(':');
                if (parts.Length is 3 or 4 && parts[0] == "media" && Guid.TryParse(parts[1], out var id) && long.TryParse(parts[2], out _))
                {
                    var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id.ToString()), new("iat", parts[2]) };
                    if (parts.Length == 4 && parts[3].Length > 0) claims.Add(new("rental_session", parts[3]));
                    ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "CustomerMedia"));
                }
            }
            catch (CryptographicException) { }
        }
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            if (!path.Equals("/api/Account/Login", StringComparison.OrdinalIgnoreCase)) { ctx.Response.StatusCode = 401; return; }
            await next(ctx); return;
        }
        if (!Guid.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) { ctx.Response.StatusCode = 401; return; }
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted || await users.IsLockedOutAsync(user)) { ctx.Response.StatusCode = 403; return; }
        var owner = string.Equals(user.NormalizedUserName, (config["RentalSaas:OwnerUserName"] ?? "admin").ToUpperInvariant(), StringComparison.Ordinal);
        ctx.Items["RentalOwner"] = owner;
        var tenant = await catalog.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ctx.RequestAborted);
        if (tenant is null)
        {
            // Existing administration accounts do not need a rental workspace.
            // They still require explicit grants, and cannot access a customer's APIs.
            var session = ctx.User.FindFirstValue("rental_session");
            if (session is not null && !string.Equals(session, user.SecurityStamp, StringComparison.Ordinal)) { ctx.Response.StatusCode = 401; return; }
            var grants = await administration.Grants(userId, ctx.RequestAborted);
            var ownAdministrationMenu = path.Equals("/api/rental-account/administration", StringComparison.OrdinalIgnoreCase);
            var ownAccount = path.Equals("/api/Account/GetInfo", StringComparison.OrdinalIgnoreCase) || path.Equals("/api/Account/Logout", StringComparison.OrdinalIgnoreCase);
            if (grants.Count > 0 && (ownAdministrationMenu || !isRental && (ownAccount || grants.Any(grant => RentalAdministration.Matches(path, grant)))))
            { await next(ctx); return; }
            ctx.Response.StatusCode = 403; return;
        }
        if (!tenant.Active) { ctx.Response.StatusCode = 403; await ctx.Response.WriteAsync("Tài khoản chưa kích hoạt, đã khóa hoặc hết hạn."); return; }
        var stamp = ctx.User.FindFirstValue("rental_session");
        if (stamp is not null ? !string.Equals(stamp, user.SecurityStamp, StringComparison.Ordinal)
            : tenant.TokensValidAfter is long cutoff && (!long.TryParse(ctx.User.FindFirstValue("iat"), out var issued) || issued <= cutoff))
        { ctx.Response.StatusCode = 401; return; }
        ctx.Items["RentalTenant"] = tenant;
        if (!isRental)
        {
            // Explicit existing user/role API grants are required for delegated administration.
            if (!owner && !path.Equals("/api/Account/GetInfo", StringComparison.OrdinalIgnoreCase) && !path.Equals("/api/Account/Logout", StringComparison.OrdinalIgnoreCase))
            {
                var grants = await administration.Grants(userId, ctx.RequestAborted);
                if (!grants.Any(grant => RentalAdministration.Matches(path, grant))) { ctx.Response.StatusCode = 403; return; }
            }
            await next(ctx); return;
        }
        // Administration endpoints do not need to start a customer's browser or worker.
        if (!path.StartsWith("/api/rental-account", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/rental-account/facebook", StringComparison.OrdinalIgnoreCase))
            ctx.Items["RentalRuntime"] = await host.Get(tenant, ctx.RequestAborted);
        await next(ctx);
    }
}

public static class RentalTenancyRegistration
{
    public static void AddRentalTenancy(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<RentalAdministration>();
        services.AddScoped<RentalCustomerService>();
        services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Path.GetFullPath("App_Data"), "protection-keys"))).SetApplicationName("AI_TOOL");
        services.AddDbContext<RentalCatalog>(o => o.UseSqlServer(config["Connections:DefaultConnection"]));
        services.AddSingleton<RentalTenantHost>();
        services.AddHostedService(sp => sp.GetRequiredService<RentalTenantHost>());
        RentalCustomerRuntime Runtime(IServiceProvider sp) => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.Items["RentalRuntime"] as RentalCustomerRuntime
            ?? throw new UnauthorizedAccessException("Customer context required.");
        services.AddScoped(sp => Runtime(sp).Factory.CreateDbContext());
        services.AddScoped(sp => new RentalRequestRuntime(Runtime(sp)));
    }
}
public sealed record RentalRequestRuntime(RentalCustomerRuntime Value);
