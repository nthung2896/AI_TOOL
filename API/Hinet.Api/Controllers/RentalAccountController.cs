using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Hinet.Api;
using Hinet.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hinet.Controllers;

[ApiController, Authorize, Route("api/rental-account")]
public sealed class RentalAccountController(RentalCatalog catalog, RentalTenantHost host, UserManager<AppUser> users) : ControllerBase
{
    private bool Owner => HttpContext.Items["RentalOwner"] is true;
    private RentalTenant Tenant => (RentalTenant)HttpContext.Items["RentalTenant"]!;
    private RentalCustomerRuntime Runtime => (RentalCustomerRuntime)HttpContext.Items["RentalRuntime"]!;
    [HttpGet("me")]
    public IActionResult Me()
    {
        Response.Cookies.Append("rental_media", host.MediaProtector.Protect("media:" + Tenant.UserId, TimeSpan.FromHours(1)), new CookieOptions
        { HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax, Path = "/rental-media", MaxAge = TimeSpan.FromHours(1) });
        Response.Headers.CacheControl = "private, no-store";
        var s = host.Settings(Tenant);
        return Ok(new { Tenant.Id, Tenant.Name, Tenant.ExpiresAt, Tenant.Enabled, isOwner = Owner, browserEnabled = s.BrowserEnabled,
            telegramOwnerId = s.TelegramOwnerId, telegramConfigured = s.TelegramToken.Length > 0, legacy = Tenant.Legacy });
    }
    public record SettingsRequest(bool BrowserEnabled, [MaxLength(200)] string? TelegramToken, [Range(0, long.MaxValue)] long TelegramOwnerId, bool ClearTelegram = false);
    [HttpPut("settings")]
    public async Task<IActionResult> Settings(SettingsRequest input, CancellationToken ct)
    {
        if (Tenant.Legacy) return BadRequest("Tài khoản chủ đang giữ cấu hình cũ. Cấu hình khách thuê được quản lý riêng trên trang này.");
        var row = await catalog.Tenants.SingleAsync(t => t.Id == Tenant.Id, ct);
        var old = host.Settings(row);
        var token = input.ClearTelegram ? "" : string.IsNullOrWhiteSpace(input.TelegramToken) ? old.TelegramToken : input.TelegramToken.Trim();
        if (token.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(token, @"\A\d+:[A-Za-z0-9_-]{20,100}\z")) return BadRequest("Telegram bot token không hợp lệ.");
        // One bot per customer prevents competing getUpdates consumers and mixed notifications.
        foreach (var other in await catalog.Tenants.AsNoTracking().Where(t => t.Id != row.Id).ToListAsync(ct))
            if (token.Length > 0 && host.Settings(other).TelegramToken == token) return Conflict("Bot Telegram đã được sử dụng bởi tài khoản khác.");
        row.ProtectedSettings = host.Protect(new(input.BrowserEnabled, token, input.TelegramOwnerId));
        row.BotHash = RentalTenantHost.BotFingerprint(token);
        try { await catalog.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict("Bot Telegram đã được sử dụng hoặc cấu hình chưa lưu được."); }
        await host.Restart(row.Id);
        return Ok(new { message = "Đã lưu cấu hình riêng và khởi động lại worker." });
    }
    public record PasswordRequest([Required] string OldPassword, [Required, MinLength(8), MaxLength(128)] string NewPassword);
    [HttpPut("password")]
    public async Task<IActionResult> Password(PasswordRequest input)
    {
        var user = await users.FindByIdAsync(Tenant.UserId.ToString());
        var result = await users.ChangePasswordAsync(user!, input.OldPassword, input.NewPassword);
        return result.Succeeded ? Ok() : BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
    [HttpGet("customers")]
    public async Task<IActionResult> Customers(CancellationToken ct)
    {
        if (!Owner) return Forbid();
        var list = await catalog.Tenants.AsNoTracking().OrderByDescending(x => x.ExpiresAt).ToListAsync(ct);
        var output = new List<object>();
        foreach (var t in list)
        {
            var u = await users.FindByIdAsync(t.UserId.ToString());
            output.Add(new { t.Id, t.Name, t.Enabled, t.ExpiresAt, t.Legacy, active = t.Active, userName = u?.UserName });
        }
        return Ok(output);
    }
    public record CreateCustomer([Required, MaxLength(150)] string Name, [Required, MaxLength(100)] string UserName,
        [Required, EmailAddress] string Email, [Required, MinLength(8), MaxLength(128)] string Password, DateTimeOffset ExpiresAt);
    [HttpPost("customers")]
    public async Task<IActionResult> Create(CreateCustomer input, CancellationToken ct)
    {
        if (!Owner) return Forbid();
        if (input.ExpiresAt <= DateTimeOffset.UtcNow) return BadRequest("Hạn sử dụng phải ở tương lai.");
        var user = new AppUser { Id = Guid.NewGuid(), UserName = input.UserName.Trim(), Email = input.Email.Trim(), Name = input.Name.Trim(), CreatedDate = DateTime.UtcNow, LockoutEnabled = true };
        var result = await users.CreateAsync(user, input.Password);
        if (!result.Succeeded) return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));
        try
        {
            var tenant = new RentalTenant { UserId = user.Id, Name = input.Name.Trim(), ExpiresAt = input.ExpiresAt };
            catalog.Tenants.Add(tenant);
            await catalog.SaveChangesAsync(ct);
            return Ok(new { tenant.Id, user.UserName });
        }
        catch { await users.DeleteAsync(user); throw; }
    }
    public record LeaseRequest(bool Enabled, DateTimeOffset ExpiresAt);
    [HttpPut("customers/{id:guid}")]
    public async Task<IActionResult> Lease(Guid id, LeaseRequest input, CancellationToken ct)
    {
        if (!Owner) return Forbid();
        var row = await catalog.Tenants.FindAsync(new object[] { id }, ct);
        if (row is null) return NotFound();
        if (row.Legacy) return BadRequest("Không khóa tài khoản chủ tại đây.");
        row.Enabled = input.Enabled; row.ExpiresAt = input.ExpiresAt;
        await catalog.SaveChangesAsync(ct);
        await host.Restart(id);
        return Ok();
    }
    public record ResetRequest([Required, MinLength(8), MaxLength(128)] string Password);
    [HttpPut("customers/{id:guid}/password")]
    public async Task<IActionResult> Reset(Guid id, ResetRequest input, CancellationToken ct)
    {
        if (!Owner) return Forbid();
        var row = await catalog.Tenants.FindAsync(new object[] { id }, ct);
        if (row is null || row.Legacy) return BadRequest();
        var user = await users.FindByIdAsync(row.UserId.ToString());
        if (user is null) return NotFound();
        var result = await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), input.Password);
        return result.Succeeded ? Ok() : BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
    [HttpPost("facebook/start")]
    public async Task<IActionResult> StartLogin() { await Runtime.Browser.BeginRemoteLogin(); return Ok(); }
    [HttpGet("facebook/screen")]
    public async Task<IActionResult> Screen()
    {
        Response.Headers.CacheControl = "private, no-store";
        return File(await Runtime.Browser.RemoteScreen(), "image/jpeg");
    }
    public record BrowserInput([Required] string Kind, int X = 0, int Y = 0, [MaxLength(1000)] string Text = "");
    [HttpPost("facebook/input")]
    public async Task<IActionResult> Input(BrowserInput input)
    {
        try { await Runtime.Browser.RemoteInput(input.Kind, input.X, input.Y, input.Text); return Ok(); }
        catch (ArgumentException) { return BadRequest(); }
    }
    [HttpPost("facebook/finish")]
    public IActionResult FinishLogin() { Runtime.Browser.EndRemoteLogin(); return Ok(); }
}
