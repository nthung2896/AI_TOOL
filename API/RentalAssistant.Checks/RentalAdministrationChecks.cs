using Hinet.Api;
using Hinet.Model;
using Hinet.Model.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public static class RentalAdministrationChecks
{
    public static async Task Run(Action<bool, string> check, bool seedMenu = false)
    {
        check(RentalAdministration.Matches("/api/RentalCustomer/Get/123", "/api/rentalcustomer/get"), "action grant permits IDs and ignores case");
        check(!RentalAdministration.Matches("/api/RentalCustomer/Delete/123", "/api/RentalCustomer/Get"), "read grant never permits deleting");
        check(!RentalAdministration.Matches("/api/RentalCustomerEvil/Create", "/api/RentalCustomer"), "controller grant respects path boundaries");
        check(!RentalAdministration.Matches("/api/RentalCustomer/GetData", "") && RentalAdministration.Matches("/api/RentalCustomer/GetData", "/api"), "empty grants deny and full grants work");
        var config = new ConfigurationBuilder().SetBasePath(Path.GetFullPath("API/Hinet.Api"))
            .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", true).Build();
        var services = new ServiceCollection();
        services.AddLogging(); services.AddHttpContextAccessor(); services.AddDataProtection();
        services.AddDbContext<HinetContext>(o => o.UseSqlServer(config["Connections:DefaultConnection"]));
        services.AddDbContext<RentalCatalog>(o => o.UseSqlServer(config["Connections:DefaultConnection"]));
        services.AddIdentityCore<AppUser>().AddRoles<AppRole>().AddEntityFrameworkStores<HinetContext>().AddDefaultTokenProviders();
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HinetContext>();
        var catalog = scope.ServiceProvider.GetRequiredService<RentalCatalog>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        using var host = new RentalTenantHost(provider, config, new TestEnvironment(), new EphemeralDataProtectionProvider(), provider.GetRequiredService<ILogger<RentalTenantHost>>());
        var service = new RentalCustomerService(catalog, users, host, new RentalAdministration(db));
        if (seedMenu)
        {
            var admin = new RentalAdministration(db);
            var ids = await catalog.Tenants.Where(t => !t.Legacy).Select(t => t.UserId).ToListAsync();
            await admin.Seed(ids, default);
            var count = await db.Operation.CountAsync();
            await admin.Seed(ids, default);
            check(count == await db.Operation.CountAsync(), "menu defaults stored in SQL and repeated seeding creates no duplicates");
            check((await admin.Menu(Guid.Empty, true, default)).SelectMany(m => m.Entries).Any(e => e.Url == "/dashboard#threads"), "workspace menu is read from stored SQL rows");
            return;
        }
        var suffix = Guid.NewGuid().ToString("N");
        var roleId = Guid.NewGuid(); var membershipId = Guid.NewGuid(); var permissionId = Guid.NewGuid();
        var moduleId = Guid.NewGuid(); var operationId = Guid.NewGuid(); var secondOperationId = Guid.NewGuid(); var roleOperationId = Guid.NewGuid();
        Guid? tenantId = null; Guid? userId = null;
        try
        {
            var input = new RentalCustomerRequest(null, "CRUD " + suffix, "crud_" + suffix, suffix + "@example.invalid", "RentalCheck9!", true, DateTimeOffset.UtcNow.AddDays(1));
            var created = await service.Create(input, default); tenantId = created.Id;
            userId = (await users.FindByNameAsync(input.UserName))!.Id;
            check(created.Active && !created.Legacy, "create provisions an active independent customer account");
            var page = await service.Search(new() { Keyword = suffix, PageIndex = 1, PageSize = 1 }, default);
            check(page.TotalCount == 1 && page.Items.Single().Id == tenantId, "search and pagination return the matching account");
            var updated = await service.Update(input with { Id = created.Id, Name = "Changed " + suffix, Enabled = false }, default);
            check(!updated.Active && (await service.Get(created.Id, default))!.Name == updated.Name, "update persists customer details and disables access");
            check((await service.Search(new() { Keyword = suffix, Status = "locked" }, default)).TotalCount == 1, "status search returns locked accounts");
            await service.Reset(created.Id, "ChangedRental9!", default);
            check(await users.CheckPasswordAsync((await users.FindByIdAsync(userId.ToString()!))!, "ChangedRental9!"), "password reset updates the identity password");
            var access = new RentalAdministration(db);
            check((await access.Grants(userId.Value, default)).Count == 0, "new customer receives no administration permissions");
            await service.Update(input with { Id = created.Id }, default);
            async Task<int> Request(string path)
            {
                var http = new DefaultHttpContext();
                http.Request.Path = path;
                var identity = await users.FindByIdAsync(userId.ToString()!);
                http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()!), new Claim("rental_session", identity!.SecurityStamp!) }, "check"));
                var reached = false;
                var middleware = new RentalTenantMiddleware(_ => { reached = true; return Task.CompletedTask; });
                await middleware.InvokeAsync(http, catalog, host, users, config, access);
                return reached ? 200 : http.Response.StatusCode;
            }
            check(await Request("/api/RentalCustomer/GetData") == 403, "middleware blocks an ordinary customer from the management API");
            db.Role.Add(new Role { Id = roleId, Code = "CHECK_" + suffix, Name = "Temporary check", IsActive = true });
            db.UserRole.Add(new UserRole { Id = membershipId, UserId = userId.Value, RoleId = roleId });
            db.ApiPermissions.Add(new ApiPermissions { Id = permissionId, RoleId = roleId, Path = "/api/RentalCustomer/GetData" });
            await db.SaveChangesAsync();
            db.Module.Add(new Module { Id = moduleId, Code = suffix, Name = "DB module " + suffix, IsShow = true, Order = 9 });
            db.Operation.AddRange(
                new Operation { Id = operationId, ModuleId = moduleId, Code = suffix + "A", Name = "Dynamic label", Url = "/custom-database-path", IsShow = true, Order = 20, Icon = "HomeOutlined" },
                new Operation { Id = secondOperationId, ModuleId = moduleId, Code = suffix + "B", Name = "First by order", Url = "/another-database-path", IsShow = true, Order = 10 });
            db.RoleOperation.Add(new RoleOperation { Id = roleOperationId, RoleId = roleId, OperationId = operationId, IsAccess = 1 });
            await db.SaveChangesAsync();
            var ownerMenu = (await access.Menu(userId.Value, true, default)).Single(m => m.Id == moduleId);
            check(ownerMenu.Entries.Select(e => e.Id).SequenceEqual(new[] { secondOperationId, operationId }), "menu ordering comes from database, including arbitrary new URLs");
            var customerMenu = (await access.Menu(userId.Value, false, default)).Single(m => m.Id == moduleId);
            check(customerMenu.Entries.Count == 1 && customerMenu.Entries[0].Id == operationId, "menu uses existing role-operation permissions");
            await db.Operation.Where(o => o.Id == operationId).ExecuteUpdateAsync(s => s.SetProperty(o => o.Name, "Renamed in DB").SetProperty(o => o.Url, "/renamed-in-db"));
            var renamed = (await access.Menu(userId.Value, false, default)).Single(m => m.Id == moduleId).Entries.Single();
            check(renamed.Name == "Renamed in DB" && renamed.Url == "/renamed-in-db" && renamed.Icon == "HomeOutlined", "renaming or changing URL in SQL immediately changes the menu");
            await db.Operation.Where(o => o.Id == operationId).ExecuteUpdateAsync(s => s.SetProperty(o => o.IsShow, false));
            check(!(await access.Menu(userId.Value, false, default)).Any(m => m.Id == moduleId), "hidden operation and empty module disappear even if permission exists");
            await db.Module.Where(m => m.Id == moduleId).ExecuteUpdateAsync(s => s.SetProperty(m => m.IsShow, false));
            check(!(await access.Menu(userId.Value, true, default)).Any(m => m.Id == moduleId), "hidden module stays hidden for owner too");
            await db.Module.Where(m => m.Id == moduleId).ExecuteUpdateAsync(s => s.SetProperty(m => m.IsShow, true));
            await db.Operation.Where(o => o.Id == secondOperationId).ExecuteUpdateAsync(s => s.SetProperty(o => o.IsDeleted, true));
            check(!(await access.Menu(userId.Value, true, default)).Any(m => m.Id == moduleId), "soft-deleted operations are not rendered");
            var grants = await access.Grants(userId.Value, default);
            check(grants.Any(g => RentalAdministration.Matches("/api/RentalCustomer/GetData", g)) && !grants.Any(g => RentalAdministration.Matches("/api/RentalCustomer/Delete/123", g)), "existing role API grants allow listing and deny deletion");
            check(await Request("/api/RentalCustomer/GetData") == 200 && await Request("/api/RentalCustomer/Delete/123") == 403, "middleware enforces the assigned role's individual action permissions");
            var workspace = await catalog.Tenants.SingleAsync(t => t.Id == created.Id);
            await catalog.Tenants.Where(t => t.Id == created.Id).ExecuteDeleteAsync();
            check(await Request("/api/RentalCustomer/GetData") == 200, "existing administrator can use assigned management APIs without a rental workspace");
            check(await Request("/api/rental-assistant/overview") == 403, "administrator without a workspace cannot access customer data");
            catalog.Entry(workspace).State = EntityState.Added;
            await catalog.SaveChangesAsync();
            await db.ApiPermissions.Where(p => p.Id == permissionId).ExecuteDeleteAsync();
            check((await access.Grants(userId.Value, default)).Count == 0, "revoking a grant takes effect without re-login");
            check(await Request("/api/RentalCustomer/GetData") == 403, "middleware immediately enforces permission revocation");
            bool selfDeleteDenied = false;
            try { await service.Delete(created.Id, userId.Value, default); } catch (ArgumentException) { selfDeleteDenied = true; }
            check(selfDeleteDenied && await service.Get(created.Id, default) is not null, "administrator cannot delete their own account");
            await service.Delete(created.Id, Guid.NewGuid(), default);
            check(await service.Get(created.Id, default) is null && (await service.Search(new() { Keyword = suffix }, default)).TotalCount == 0, "soft-deleted customer disappears from details and search");
            check(!(await catalog.Tenants.AsNoTracking().SingleAsync(t => t.Id == created.Id)).Active && (await users.FindByIdAsync(userId.ToString()!))!.IsDeleted, "deletion disables lease and marks identity deleted");
        }
        finally
        {
            // Remove only records generated by this check. No customer schemas or browser sessions are created.
            await db.ApiPermissions.Where(p => p.Id == permissionId).ExecuteDeleteAsync();
            await db.UserRole.Where(p => p.Id == membershipId).ExecuteDeleteAsync();
            await db.RoleOperation.Where(p => p.Id == roleOperationId).ExecuteDeleteAsync();
            await db.Operation.Where(p => p.Id == operationId || p.Id == secondOperationId).ExecuteDeleteAsync();
            await db.Module.Where(p => p.Id == moduleId).ExecuteDeleteAsync();
            await db.Role.Where(p => p.Id == roleId).ExecuteDeleteAsync();
            if (tenantId.HasValue) await catalog.Tenants.Where(t => t.Id == tenantId).ExecuteDeleteAsync();
            if (userId.HasValue) { await db.UserRole.Where(u => u.UserId == userId).ExecuteDeleteAsync(); await db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(); }
        }
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Hinet.Api";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetFullPath(".verification/rental-admin-checks");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
