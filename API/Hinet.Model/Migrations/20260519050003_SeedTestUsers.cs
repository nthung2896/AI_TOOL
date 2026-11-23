using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hinet.Model.Migrations
{
    /// <inheritdoc />
    public partial class SeedTestUsers : Migration
    {
        // ====== Users ======
        private const string U_DOANHNGHIEP     = "44444444-0000-0000-0000-000000000001";
        private const string U_CHUYENVIEN_SO   = "44444444-0000-0000-0000-000000000002";
        private const string U_TRUONGPHONG_SO  = "44444444-0000-0000-0000-000000000003";
        private const string U_LANHDAO_SO      = "44444444-0000-0000-0000-000000000004";
        private const string U_CHUYENVIEN_CUC  = "44444444-0000-0000-0000-000000000005";
        private const string U_TRUONGPHONG_CUC = "44444444-0000-0000-0000-000000000006";
        private const string U_LANHDAO_CUC     = "44444444-0000-0000-0000-000000000007";

        // ====== Roles (khớp SeedSystemRoles) ======
        private const string R_DOANHNGHIEP     = "22222222-0000-0000-0000-000000000001";
        private const string R_CHUYENVIEN_SO   = "22222222-0000-0000-0000-000000000002";
        private const string R_TRUONGPHONG_SO  = "22222222-0000-0000-0000-000000000003";
        private const string R_LANHDAO_SO      = "22222222-0000-0000-0000-000000000004";
        private const string R_CHUYENVIEN_CUC  = "22222222-0000-0000-0000-000000000005";
        private const string R_TRUONGPHONG_CUC = "22222222-0000-0000-0000-000000000006";
        private const string R_LANHDAO_CUC     = "22222222-0000-0000-0000-000000000007";

        // ====== Departments (khớp SeedDepartments) ======
        private const string D_CUC    = "33000000-0000-0000-0000-000000000000"; // Cục TMĐT & KTS
        private const string D_SCT_HN = "33000000-0000-0000-0000-000000000001"; // Sở Công Thương Hà Nội
        private const string D_NONE   = "00000000-0000-0000-0000-000000000000"; // Doanh nghiệp - không thuộc đơn vị hành chính

        // Password "12345678" — dùng chung hash với tài khoản admin (Identity v3 hash đã chứa salt).
        private const string PASSWORD_HASH = "AQAAAAIAAYagAAAAEDVG+SnNm+LznDZES0TB8S9gOaJgn+bWCUAbfWMQbCgIdlGe/RNzGKHes6yoGZr+Lg==";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
-- ============ ASPNETUSERS ============
INSERT INTO [AspNetUsers] (
    [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
    [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
    [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount],
    [Name], [Gender], [Type], [DonViId],
    [CreatedDate], [UpdatedDate], [IsDeleted]
)
SELECT
    CAST(v.id AS uniqueidentifier), v.username, UPPER(v.username), v.email, UPPER(v.email),
    1, N'{PASSWORD_HASH}', CONVERT(nvarchar(36), NEWID()), CONVERT(nvarchar(36), NEWID()),
    0, 0, 1, 0,
    v.fullname, 0, v.type, CAST(NULLIF(NULLIF(v.donvi, N''), N'{D_NONE}') AS uniqueidentifier),
    SYSDATETIME(), SYSDATETIME(), 0
FROM (VALUES
    (N'{U_DOANHNGHIEP}',     N'doanhnghiep',    N'doanhnghiep@test.local',    N'Doanh nghiệp Test',          N'DoanhNghiep', N'{D_NONE}'),
    (N'{U_CHUYENVIEN_SO}',   N'chuyenvienso',   N'chuyenvienso@test.local',   N'Chuyên viên Sở Hà Nội',     N'CanBo',       N'{D_SCT_HN}'),
    (N'{U_TRUONGPHONG_SO}',  N'truongphongso',  N'truongphongso@test.local',  N'Trưởng phòng Sở Hà Nội',    N'CanBo',       N'{D_SCT_HN}'),
    (N'{U_LANHDAO_SO}',      N'lanhdaoso',      N'lanhdaoso@test.local',      N'Lãnh đạo Sở Hà Nội',        N'CanBo',       N'{D_SCT_HN}'),
    (N'{U_CHUYENVIEN_CUC}',  N'chuyenviencuc',  N'chuyenviencuc@test.local',  N'Chuyên viên Cục TMĐT',      N'CanBo',       N'{D_CUC}'),
    (N'{U_TRUONGPHONG_CUC}', N'truongphongcuc', N'truongphongcuc@test.local', N'Trưởng phòng Cục TMĐT',     N'CanBo',       N'{D_CUC}'),
    (N'{U_LANHDAO_CUC}',     N'lanhdaocuc',     N'lanhdaocuc@test.local',     N'Lãnh đạo Cục TMĐT',         N'CanBo',       N'{D_CUC}')
) AS v(id, username, email, fullname, type, donvi)
WHERE NOT EXISTS (SELECT 1 FROM [AspNetUsers] u WHERE u.[NormalizedUserName] = UPPER(v.username));

-- ============ USERROLE ============
INSERT INTO [UserRole] ([Id], [UserId], [RoleId], [DepartmentId], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT NEWID(), CAST(v.uid AS uniqueidentifier), CAST(v.rid AS uniqueidentifier), CAST(v.did AS uniqueidentifier), SYSDATETIME(), SYSDATETIME(), 0
FROM (VALUES
    (N'{U_DOANHNGHIEP}',     N'{R_DOANHNGHIEP}',     N'{D_NONE}'),
    (N'{U_CHUYENVIEN_SO}',   N'{R_CHUYENVIEN_SO}',   N'{D_SCT_HN}'),
    (N'{U_TRUONGPHONG_SO}',  N'{R_TRUONGPHONG_SO}',  N'{D_SCT_HN}'),
    (N'{U_LANHDAO_SO}',      N'{R_LANHDAO_SO}',      N'{D_SCT_HN}'),
    (N'{U_CHUYENVIEN_CUC}',  N'{R_CHUYENVIEN_CUC}',  N'{D_CUC}'),
    (N'{U_TRUONGPHONG_CUC}', N'{R_TRUONGPHONG_CUC}', N'{D_CUC}'),
    (N'{U_LANHDAO_CUC}',     N'{R_LANHDAO_CUC}',     N'{D_CUC}')
) AS v(uid, rid, did)
WHERE NOT EXISTS (
    SELECT 1 FROM [UserRole] ur
    WHERE ur.[UserId] = CAST(v.uid AS uniqueidentifier) AND ur.[RoleId] = CAST(v.rid AS uniqueidentifier)
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM [UserRole] WHERE [UserId] IN (
    CAST(N'{U_DOANHNGHIEP}' AS uniqueidentifier), CAST(N'{U_CHUYENVIEN_SO}' AS uniqueidentifier), CAST(N'{U_TRUONGPHONG_SO}' AS uniqueidentifier), CAST(N'{U_LANHDAO_SO}' AS uniqueidentifier),
    CAST(N'{U_CHUYENVIEN_CUC}' AS uniqueidentifier), CAST(N'{U_TRUONGPHONG_CUC}' AS uniqueidentifier), CAST(N'{U_LANHDAO_CUC}' AS uniqueidentifier)
);
DELETE FROM [AspNetUsers] WHERE [Id] IN (
    CAST(N'{U_DOANHNGHIEP}' AS uniqueidentifier), CAST(N'{U_CHUYENVIEN_SO}' AS uniqueidentifier), CAST(N'{U_TRUONGPHONG_SO}' AS uniqueidentifier), CAST(N'{U_LANHDAO_SO}' AS uniqueidentifier),
    CAST(N'{U_CHUYENVIEN_CUC}' AS uniqueidentifier), CAST(N'{U_TRUONGPHONG_CUC}' AS uniqueidentifier), CAST(N'{U_LANHDAO_CUC}' AS uniqueidentifier)
);
");
        }
    }
}
