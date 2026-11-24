using Hinet.Model;
using Hinet.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hinet.Api;

// Use the same user/role API grants edited by QLRole and QLNguoiDung.
public sealed class RentalAdministration(HinetContext db)
{
    public static bool Matches(string path, string? grant)
    {
        if (string.IsNullOrWhiteSpace(grant)) return false;
        grant = grant.TrimEnd('/');
        return grant.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) || grant.Equals("/api", StringComparison.OrdinalIgnoreCase)
            ? path.Equals(grant, StringComparison.OrdinalIgnoreCase) || path.StartsWith(grant + "/", StringComparison.OrdinalIgnoreCase)
            : false;
    }

    public async Task<List<string>> Grants(Guid userId, CancellationToken ct)
    {
        var roles = db.UserRole.Where(r => r.UserId == userId && !r.IsDeleted)
            .Join(db.Role.Where(r => !r.IsDeleted && r.IsActive), r => r.RoleId, r => r.Id, (ur, r) => r.Id);
        return await db.ApiPermissions.AsNoTracking().Where(p => !p.IsDeleted && p.Path != null &&
            (p.UserId == userId || p.RoleId.HasValue && roles.Contains(p.RoleId.Value)))
            .Select(p => p.Path!).Distinct().ToListAsync(ct);
    }

    public sealed record MenuEntry(Guid Id, string Name, string Url, string Code, string? Icon, int Order);
    public sealed record MenuModule(Guid Id, string Name, string Code, string? Icon, int Order, List<MenuEntry> Entries);

    public async Task<List<MenuModule>> Menu(Guid userId, bool owner, CancellationToken ct)
    {
        var roles = db.UserRole.Where(r => r.UserId == userId && !r.IsDeleted)
            .Join(db.Role.Where(r => !r.IsDeleted && r.IsActive), r => r.RoleId, r => r.Id, (ur, r) => r.Id);
        var allowed = db.RoleOperation.Where(r => !r.IsDeleted && r.IsAccess == 1 && roles.Contains(r.RoleId)).Select(r => r.OperationId);
        var rows = await (from module in db.Module.AsNoTracking()
                          join operation in db.Operation.AsNoTracking() on module.Id equals operation.ModuleId
                          where !module.IsDeleted && module.IsShow && !operation.IsDeleted && operation.IsShow && (owner || allowed.Contains(operation.Id))
                          orderby module.Order, module.Name, module.Id, operation.Order, operation.Name, operation.Id
                          select new { Module = module, Operation = operation }).ToListAsync(ct);
        return rows.GroupBy(r => r.Module.Id).Select(group =>
        {
            var module = group.First().Module;
            return new MenuModule(module.Id, module.Name, module.Code, module.Icon, module.Order,
                group.Select(r => new MenuEntry(r.Operation.Id, r.Operation.Name, r.Operation.Url, r.Operation.Code, r.Operation.Icon, r.Operation.Order)).ToList());
        }).ToList();
    }

    public async Task Seed(IEnumerable<Guid> customerIds, CancellationToken ct)
    {
        // Initial data only. Never overwrite edits, hidden items or soft-deleted items.
        async Task<Module> Module(string code, string name, int order)
        {
            var item = await db.Module.FirstOrDefaultAsync(m => m.Code == code, ct);
            if (item is not null) return item;
            item = new Module { Id = Guid.NewGuid(), Code = code, Name = name, IsShow = true, Order = order };
            db.Module.Add(item); return item;
        }
        var workspace = await Module("RENTAL_WORKSPACE", "Nghiệp vụ cho thuê", 0);
        var admin = await Module("RENTAL_ADMIN", "Quản trị hệ thống", 100);
        var defaults = new[] {
            (workspace.Id, "RENTAL_OVERVIEW", "Tổng quan", "/dashboard#overview", "AppstoreOutlined"),
            (workspace.Id, "RENTAL_ROOMS", "Quản lý phòng trọ", "/dashboard#rooms", "HomeOutlined"),
            (workspace.Id, "RENTAL_POSTS", "Soạn & đăng bài", "/dashboard#posts", "EditOutlined"),
            (workspace.Id, "RENTAL_GROUPS", "Nhóm Facebook", "/dashboard#groups", "TeamOutlined"),
            (workspace.Id, "RENTAL_THREADS", "Khách & Messenger", "/dashboard#threads", "MessageOutlined"),
            (workspace.Id, "RENTAL_APPOINTMENTS", "Lịch xem phòng", "/dashboard#appointments", "CalendarOutlined"),
            (admin.Id, "RENTAL_CUSTOMERS", "Tài khoản & khách thuê", "/rental-customers", "TeamOutlined"),
            (admin.Id, "RENTAL_USERS", "Người dùng", "/QLNguoiDung", "TeamOutlined"),
            (admin.Id, "RENTAL_ROLES", "Vai trò & phân quyền", "/QLRole", "TeamOutlined"),
            (admin.Id, "RENTAL_MODULES", "Phân hệ", "/QLModule", "AppstoreOutlined"),
            (admin.Id, "RENTAL_OPERATIONS", "Chức năng", "/QLOperation", "AppstoreOutlined")
        };
        var workspaceOperations = new List<Guid>();
        foreach (var (moduleId, code, name, url, icon) in defaults)
        {
            var item = await db.Operation.FirstOrDefaultAsync(o => o.Code == code || o.Url == url, ct);
            if (item is null)
            {
                item = new Operation { Id = Guid.NewGuid(), ModuleId = moduleId, Code = code, Name = name, Url = url, Icon = icon, IsShow = true, Order = Array.FindIndex(defaults, d => d.Item2 == code) };
                db.Operation.Add(item);
            }
            if (moduleId == workspace.Id) workspaceOperations.Add(item.Id);
        }
        var role = await db.Role.FirstOrDefaultAsync(r => r.Code == "RENTAL_CUSTOMER", ct);
        if (role is null)
        {
            role = new Role { Id = Guid.NewGuid(), Code = "RENTAL_CUSTOMER", Name = "Khách thuê", IsActive = true };
            db.Role.Add(role);
            foreach (var id in workspaceOperations) db.RoleOperation.Add(new RoleOperation { Id = Guid.NewGuid(), RoleId = role.Id, OperationId = id, IsAccess = 1 });
        }
        await db.SaveChangesAsync(ct);
        foreach (var id in customerIds) await AssignCustomerRole(id, ct);
    }

    public async Task AssignCustomerRole(Guid userId, CancellationToken ct)
    {
        var role = await db.Role.FirstOrDefaultAsync(r => r.Code == "RENTAL_CUSTOMER" && !r.IsDeleted && r.IsActive, ct);
        if (role is null || await db.UserRole.AnyAsync(r => r.UserId == userId && r.RoleId == role.Id, ct)) return;
        db.UserRole.Add(new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleId = role.Id });
        await db.SaveChangesAsync(ct);
    }
}
