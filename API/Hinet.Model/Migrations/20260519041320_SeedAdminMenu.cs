using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hinet.Model.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminMenu : Migration
    {
        private const string AdminUserId = "11111111-1111-1111-1111-111111111111";
        private const string AdminRoleId = "22222222-2222-2222-2222-222222222222";

        // Modules
        private const string M_PHANQUYEN = "30000000-0000-0000-0000-000000000001";
        private const string M_DANHMUC = "30000000-0000-0000-0000-000000000002";
        private const string M_HANHCHINH = "30000000-0000-0000-0000-000000000003";
        private const string M_CANHAN = "30000000-0000-0000-0000-000000000004";

        // Operations
        private const string O_NGUOIDUNG = "40000000-0000-0000-0000-000000000001";
        private const string O_VAITRO = "40000000-0000-0000-0000-000000000002";
        private const string O_MODULE = "40000000-0000-0000-0000-000000000004";
        private const string O_OPERATION = "40000000-0000-0000-0000-000000000005";
        private const string O_NHOMDM = "40000000-0000-0000-0000-000000000006";
        private const string O_TINH = "40000000-0000-0000-0000-000000000008";
        private const string O_HUYEN = "40000000-0000-0000-0000-000000000009";
        private const string O_THONGBAO = "40000000-0000-0000-0000-00000000000a";
        private const string O_PROFILE = "40000000-0000-0000-0000-00000000000b";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reset password admin về '12345678' để chắc chắn hash khớp với configuration mới.
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid(AdminUserId),
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "01f581f4-9ad0-47d3-a36d-44e406a6d7b2", "AQAAAAIAAYagAAAAEDVG+SnNm+LznDZES0TB8S9gOaJgn+bWCUAbfWMQbCgIdlGe/RNzGKHes6yoGZr+Lg==", "ecd6847d-c789-4e75-be8e-a8b342847a01" });

            // Seed Role + UserRole + Module + Operation + RoleOperation cho admin.
            // SQL Server seed data.
            migrationBuilder.Sql($@"
-- ============ ROLE: Admin ============
INSERT INTO [Role] ([Id], [Name], [Code], [IsActive], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT CAST(N'{AdminRoleId}' AS uniqueidentifier), N'Quản trị viên', N'Admin', 1, SYSDATETIME(), SYSDATETIME(), 0
WHERE NOT EXISTS (SELECT 1 FROM [Role] WHERE [Id] = CAST(N'{AdminRoleId}' AS uniqueidentifier));

-- ============ USERROLE: admin -> Admin ============
INSERT INTO [UserRole] ([Id], [UserId], [RoleId], [DepartmentId], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT NEWID(), CAST(N'{AdminUserId}' AS uniqueidentifier), CAST(N'{AdminRoleId}' AS uniqueidentifier),
       CAST(N'00000000-0000-0000-0000-000000000000' AS uniqueidentifier), SYSDATETIME(), SYSDATETIME(), 0
WHERE NOT EXISTS (
    SELECT 1 FROM [UserRole]
    WHERE [UserId] = CAST(N'{AdminUserId}' AS uniqueidentifier) AND [RoleId] = CAST(N'{AdminRoleId}' AS uniqueidentifier)
);

-- ============ MODULES ============
INSERT INTO [Module] ([Id], [Code], [Name], [Order], [IsShow], [Icon], [ClassCss], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT CAST(v.id AS uniqueidentifier), v.code, v.name, v.ord, 1, v.icon, v.icon, SYSDATETIME(), SYSDATETIME(), 0
FROM (VALUES
    (N'{M_PHANQUYEN}', N'PHANQUYEN', N'Phân quyền hệ thống', 1, N'<SafetyOutlined />'),
    (N'{M_DANHMUC}',   N'DANHMUC',   N'Danh mục dùng chung', 2, N'<BookOutlined />'),
    (N'{M_HANHCHINH}', N'HANHCHINH', N'Đơn vị hành chính',   3, N'<ApartmentOutlined />'),
    (N'{M_CANHAN}',    N'CANHAN',    N'Cá nhân',             4, N'<UserOutlined />')
) AS v(id, code, name, ord, icon)
WHERE NOT EXISTS (SELECT 1 FROM [Module] m WHERE m.[Id] = CAST(v.id AS uniqueidentifier));

-- ============ OPERATIONS ============
INSERT INTO [Operation] ([Id], [ModuleId], [Name], [Url], [Code], [Order], [IsShow], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT CAST(v.id AS uniqueidentifier), CAST(v.mid AS uniqueidentifier), v.name, v.url, v.code, v.ord, 1, SYSDATETIME(), SYSDATETIME(), 0
FROM (VALUES
    (N'{O_NGUOIDUNG}', N'{M_PHANQUYEN}', N'Người dùng',        N'/QLNguoiDung',  N'NGUOIDUNG', 1),
    (N'{O_VAITRO}',    N'{M_PHANQUYEN}', N'Vai trò',           N'/QLRole',       N'VAITRO',    2),
    (N'{O_MODULE}',    N'{M_PHANQUYEN}', N'Module',            N'/QLModule',     N'MODULE',    3),
    (N'{O_OPERATION}', N'{M_PHANQUYEN}', N'Operation',         N'/QLOperation',  N'OPERATION', 4),
    (N'{O_NHOMDM}',    N'{M_DANHMUC}',   N'Nhóm danh mục',     N'/NhomDanhMuc',  N'NHOMDM',    1),
    (N'{O_TINH}',      N'{M_HANHCHINH}', N'Tỉnh / Thành phố',  N'/tinh',         N'TINH',      1),
    (N'{O_HUYEN}',     N'{M_HANHCHINH}', N'Quận / Huyện',      N'/huyen',        N'HUYEN',     2),
    (N'{O_THONGBAO}',  N'{M_CANHAN}',    N'Thông báo',         N'/notification', N'THONGBAO',  1),
    (N'{O_PROFILE}',   N'{M_CANHAN}',    N'Tài khoản',         N'/profile',      N'PROFILE',   2)
) AS v(id, mid, name, url, code, ord)
WHERE NOT EXISTS (SELECT 1 FROM [Operation] o WHERE o.[Id] = CAST(v.id AS uniqueidentifier));

-- ============ ROLEOPERATION: Admin được phép tất cả ============
INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT NEWID(), CAST(N'{AdminRoleId}' AS uniqueidentifier), op.[Id], 1, SYSDATETIME(), SYSDATETIME(), 0
FROM [Operation] op
WHERE NOT EXISTS (
    SELECT 1 FROM [RoleOperation] ro
    WHERE ro.[RoleId] = CAST(N'{AdminRoleId}' AS uniqueidentifier) AND ro.[OperationId] = op.[Id]
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM [RoleOperation] WHERE [RoleId] = CAST(N'{AdminRoleId}' AS uniqueidentifier);
DELETE FROM [UserRole] WHERE [UserId] = CAST(N'{AdminUserId}' AS uniqueidentifier) AND [RoleId] = CAST(N'{AdminRoleId}' AS uniqueidentifier);
DELETE FROM [Operation] WHERE [Id] IN (
    CAST(N'{O_NGUOIDUNG}' AS uniqueidentifier), CAST(N'{O_VAITRO}' AS uniqueidentifier), CAST(N'{O_MODULE}' AS uniqueidentifier), CAST(N'{O_OPERATION}' AS uniqueidentifier),
    CAST(N'{O_NHOMDM}' AS uniqueidentifier), CAST(N'{O_TINH}' AS uniqueidentifier), CAST(N'{O_HUYEN}' AS uniqueidentifier), CAST(N'{O_THONGBAO}' AS uniqueidentifier), CAST(N'{O_PROFILE}' AS uniqueidentifier)
);
DELETE FROM [Module] WHERE [Id] IN (
    CAST(N'{M_PHANQUYEN}' AS uniqueidentifier), CAST(N'{M_DANHMUC}' AS uniqueidentifier), CAST(N'{M_HANHCHINH}' AS uniqueidentifier), CAST(N'{M_CANHAN}' AS uniqueidentifier)
);
DELETE FROM [Role] WHERE [Id] = CAST(N'{AdminRoleId}' AS uniqueidentifier);
");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid(AdminUserId),
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8987c9fc-caf7-4b7c-b3f0-89689dc94106", "AQAAAAIAAYagAAAAEIEmX5aWoTyHzbhuGkjp2g+jgcKJfz1DB6Fq+bxQ6+GPUL2KQlOPmeB4WJlFmSq38A==", "31af2931-6caa-4b92-84a5-5546cbd0a685" });
        }
    }
}
