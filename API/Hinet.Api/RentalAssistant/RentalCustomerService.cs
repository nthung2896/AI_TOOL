using System.ComponentModel.DataAnnotations;
using Hinet.Model.Entities;
using Hinet.Service.Common;
using Hinet.Service.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hinet.Api;

public sealed class RentalCustomerSearch : SearchBase
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
}
public sealed record RentalCustomerDto(Guid Id, string Name, string? UserName, string? Email, bool Enabled, DateTimeOffset ExpiresAt, bool Legacy, bool Active);
public sealed record RentalCustomerRequest(
    Guid? Id, [Required, MaxLength(150)] string Name, [Required, MaxLength(100)] string UserName,
    [Required, EmailAddress, MaxLength(256)] string Email, [MinLength(8), MaxLength(128)] string? Password,
    bool Enabled, DateTimeOffset ExpiresAt);

public sealed class RentalCustomerService(RentalCatalog catalog, UserManager<AppUser> users, RentalTenantHost host, RentalAdministration administration)
{
    private static RentalCustomerDto Dto(RentalTenant tenant, AppUser? user) => new(tenant.Id, tenant.Name, user?.UserName, user?.Email, tenant.Enabled, tenant.ExpiresAt, tenant.Legacy, tenant.Active);

    public async Task<PagedList<RentalCustomerDto>> Search(RentalCustomerSearch search, CancellationToken ct)
    {
        // Both contexts use the same SQL database; materialize identity IDs explicitly
        // rather than composing a LINQ query across two DbContext instances.
        var userQuery = users.Users.AsNoTracking().Where(u => !u.IsDeleted);
        var visibleIds = await userQuery.Select(u => u.Id).ToListAsync(ct);
        var query = catalog.Tenants.AsNoTracking().Where(t => visibleIds.Contains(t.UserId));
        if (!string.IsNullOrWhiteSpace(search.Keyword))
        {
            var keyword = search.Keyword.Trim();
            var matchingIds = await userQuery.Where(u => u.UserName!.Contains(keyword) || u.Email!.Contains(keyword)).Select(u => u.Id).ToListAsync(ct);
            query = query.Where(t => t.Name.Contains(keyword) || matchingIds.Contains(t.UserId));
        }
        var now = DateTimeOffset.UtcNow;
        query = search.Status switch
        {
            "active" => query.Where(t => t.Enabled && t.ExpiresAt > now),
            "expired" => query.Where(t => t.Enabled && t.ExpiresAt <= now),
            "locked" => query.Where(t => !t.Enabled),
            _ => query
        };
        var total = await query.CountAsync(ct);
        var page = Math.Max(1, search.PageIndex);
        var size = Math.Clamp(search.PageSize, 1, 100);
        var tenants = await query.OrderByDescending(t => t.Legacy).ThenBy(t => t.Name).ThenBy(t => t.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        var pageIds = tenants.Select(t => t.UserId).ToList();
        var accounts = await userQuery.Where(u => pageIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);
        return new(tenants.Select(t => Dto(t, accounts.GetValueOrDefault(t.UserId))).ToList(), page, size, total);
    }

    public async Task<RentalCustomerDto?> Get(Guid id, CancellationToken ct)
    {
        var row = await catalog.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, ct);
        if (row is null) return null;
        var user = await users.FindByIdAsync(row.UserId.ToString());
        return user is null || user.IsDeleted ? null : Dto(row, user);
    }

    public async Task<RentalCustomerDto> Create(RentalCustomerRequest input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Password)) throw new ArgumentException("Nhập mật khẩu ban đầu.");
        if (input.ExpiresAt <= DateTimeOffset.UtcNow) throw new ArgumentException("Hạn sử dụng phải ở tương lai.");
        var user = new AppUser { Id = Guid.NewGuid(), UserName = input.UserName.Trim(), Name = input.Name.Trim(), Email = input.Email.Trim(), CreatedDate = DateTime.UtcNow, LockoutEnabled = true };
        Ensure(await users.CreateAsync(user, input.Password));
        var tenant = new RentalTenant { UserId = user.Id, Name = input.Name.Trim(), Enabled = input.Enabled, ExpiresAt = input.ExpiresAt };
        try { catalog.Tenants.Add(tenant); await catalog.SaveChangesAsync(ct); await administration.AssignCustomerRole(user.Id, ct); }
        catch { await users.DeleteAsync(user); throw; }
        return Dto(tenant, user);
    }

    private async Task<(RentalTenant Tenant, AppUser User)> Editable(Guid id, CancellationToken ct)
    {
        var tenant = await catalog.Tenants.SingleOrDefaultAsync(t => t.Id == id, ct) ?? throw new KeyNotFoundException("Không tìm thấy khách thuê.");
        if (tenant.Legacy) throw new ArgumentException("Không sửa hoặc xóa tài khoản chủ hệ thống tại đây.");
        var user = await users.FindByIdAsync(tenant.UserId.ToString());
        if (user is null || user.IsDeleted) throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        return (tenant, user);
    }

    public async Task<RentalCustomerDto> Update(RentalCustomerRequest input, CancellationToken ct)
    {
        var (tenant, user) = await Editable(input.Id ?? Guid.Empty, ct);
        user.UserName = input.UserName.Trim(); user.Name = input.Name.Trim(); user.Email = input.Email.Trim();
        Ensure(await users.UpdateAsync(user));
        tenant.Name = input.Name.Trim(); tenant.Enabled = input.Enabled; tenant.ExpiresAt = input.ExpiresAt;
        await catalog.SaveChangesAsync(ct);
        await host.Restart(tenant.Id);
        return Dto(tenant, user);
    }

    public async Task Delete(Guid id, Guid actorId, CancellationToken ct)
    {
        var (tenant, user) = await Editable(id, ct);
        if (user.Id == actorId) throw new ArgumentException("Không thể xóa tài khoản đang đăng nhập.");
        // Soft deletion preserves the customer's stored data and blocks existing sessions.
        user.IsDeleted = true; user.DeletedDate = DateTime.UtcNow; user.DeletedId = actorId;
        Ensure(await users.UpdateAsync(user));
        tenant.Enabled = false; tenant.ProtectedSettings = ""; tenant.BotHash = null;
        await catalog.SaveChangesAsync(ct);
        await host.Restart(id);
    }

    public async Task Reset(Guid id, string password, CancellationToken ct)
    {
        var (tenant, user) = await Editable(id, ct);
        Ensure(await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), password));
        tenant.TokensValidAfter = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await catalog.SaveChangesAsync(ct);
    }
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new ArgumentException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
