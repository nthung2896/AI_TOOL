using Hinet.Domain.Entites;
using Hinet.Model.Entities;
using Microsoft.AspNetCore.Http;
using VKS.Domain.Entites;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Hinet.Model
{
    public class HinetContext : IdentityDbContext<AppUser, AppRole, Guid>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HinetContext(DbContextOptions<HinetContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        public DbSet<DM_NhomDanhMuc> DM_NhomDanhMuc { get; set; }
        public DbSet<DM_DuLieuDanhMuc> DM_DuLieuDanhMuc { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<UserRole> UserRole { get; set; }
        public DbSet<Module> Module { get; set; }
        public DbSet<Operation> Operation { get; set; }
        public DbSet<RoleOperation> RoleOperation { get; set; }
        public DbSet<Notification> Notification { get; set; }
        public DbSet<Audit> Audit { get; set; }
        public DbSet<Department> Department { get; set; }
        public DbSet<Tinh> Tinh { get; set; }
        public DbSet<Huyen> Huyen { get; set; }

        public DbSet<ApiPermissions> ApiPermissions { get; set; }

        public DbSet<TaiLieuDinhKem> TaiLieuDinhKem { get; set; }

        //
        public DbSet<PlatformManage> PlatformManage { get; set; }



        public DbSet<CompanyInfo> CompanyInfo { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<AppUser>().HasData(
                new AppUser
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    UserName = "admin",
                    Type = "Admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin",
                    NormalizedEmail = "ADMIN",
                    EmailConfirmed = true,
                    // ⚠️ Password mặc định "12345678" CHỈ dùng cho dev/test.
                    // Production: ĐỔI password ngay sau khi seed, hoặc sửa giá trị ở đây trước khi chạy InitialCreate.
                    PasswordHash = "AQAAAAIAAYagAAAAEIGQyxefovd0J7Droo/w3ERUiuF1dJsODyfs7a7HwS7soszbaT5AMty5fS0Ifpb83w==",
                    SecurityStamp = "647c23b9-85c8-4092-8aa0-c22192791edb",
                    ConcurrencyStamp = "4797334c-789a-44ee-9aab-26bb4787babe"
                }
            );

            base.OnModelCreating(builder);
            builder.Entity<IdentityUserLogin<Guid>>().Property(x => x.LoginProvider).HasMaxLength(128);
            builder.Entity<IdentityUserLogin<Guid>>().Property(x => x.ProviderKey).HasMaxLength(128);
            builder.Entity<IdentityUserToken<Guid>>().Property(x => x.LoginProvider).HasMaxLength(128);
            builder.Entity<IdentityUserToken<Guid>>().Property(x => x.Name).HasMaxLength(128);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<AuditableEntity>();

            Guid.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
            var userName = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = DateTime.Now;
                    entry.Entity.CreatedId = userId;
                    entry.Entity.CreatedBy = userName;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = DateTime.Now;
                    entry.Entity.UpdatedId = userId;
                    entry.Entity.UpdatedBy = userName;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedDate = DateTime.Now;
                    entry.Entity.DeletedId = userId;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
