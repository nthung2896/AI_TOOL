using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hinet.Model.Migrations
{
    /// <inheritdoc />
    public partial class SeedPlatformContractMenu : Migration
    {
        // Modules
        private const string M_NENTANG = "30000000-0000-0000-0000-000000000010";
        private const string M_HOPDONG = "30000000-0000-0000-0000-000000000011";

        // Operations - Nền tảng TMĐT
        private const string O_NT_DANGKY   = "40000000-0000-0000-0000-000000000100";
        private const string O_NT_DANHSACH = "40000000-0000-0000-0000-000000000101";
        private const string O_NT_XULY     = "40000000-0000-0000-0000-000000000102";

        // Operations - Hợp đồng điện tử
        private const string O_HD_DANGKY   = "40000000-0000-0000-0000-000000000200";
        private const string O_HD_DANHSACH = "40000000-0000-0000-0000-000000000201";
        private const string O_HD_XULY     = "40000000-0000-0000-0000-000000000202";

        // Role Ids (khớp 20260519050000_SeedSystemRoles)
        private const string R_ADMIN           = "22222222-2222-2222-2222-222222222222";
        private const string R_DOANHNGHIEP     = "22222222-0000-0000-0000-000000000001";
        private const string R_CHUYENVIEN_SO   = "22222222-0000-0000-0000-000000000002";
        private const string R_TRUONGPHONG_SO  = "22222222-0000-0000-0000-000000000003";
        private const string R_LANHDAO_SO      = "22222222-0000-0000-0000-000000000004";
        private const string R_CHUYENVIEN_CUC  = "22222222-0000-0000-0000-000000000005";
        private const string R_TRUONGPHONG_CUC = "22222222-0000-0000-0000-000000000006";
        private const string R_LANHDAO_CUC     = "22222222-0000-0000-0000-000000000007";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
-- ============ MODULES ============
INSERT INTO [Module] ([Id], [Code], [Name], [Order], [IsShow], [Icon], [ClassCss], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT CAST(v.id AS uniqueidentifier), v.code, v.name, v.ord, 1, v.icon, v.icon, SYSDATETIME(), SYSDATETIME(), 0
FROM (VALUES
    (N'{M_NENTANG}', N'NENTANG', N'Quản lý nền tảng TMĐT',          10, N'<AppstoreOutlined />'),
    (N'{M_HOPDONG}', N'HOPDONG', N'Chứng thực hợp đồng điện tử',    11, N'<FileProtectOutlined />')
) AS v(id, code, name, ord, icon)
WHERE NOT EXISTS (SELECT 1 FROM [Module] m WHERE m.[Id] = CAST(v.id AS uniqueidentifier));

-- ============ OPERATIONS ============
INSERT INTO [Operation] ([Id], [ModuleId], [Name], [Url], [Code], [Order], [IsShow], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT CAST(v.id AS uniqueidentifier), CAST(v.mid AS uniqueidentifier), v.name, v.url, v.code, v.ord, 1, SYSDATETIME(), SYSDATETIME(), 0
FROM (VALUES
    -- Nền tảng
    (N'{O_NT_DANGKY}',   N'{M_NENTANG}', N'Đăng ký nền tảng',         N'/nen-tang/dang-ky',  N'NT_DANGKY',   1),
    (N'{O_NT_DANHSACH}', N'{M_NENTANG}', N'Danh sách nền tảng',       N'/nen-tang',          N'NT_DANHSACH', 2),
    (N'{O_NT_XULY}',     N'{M_NENTANG}', N'Xử lý hồ sơ nền tảng',     N'/nen-tang/xu-ly',    N'NT_XULY',     3),
    -- Hợp đồng điện tử
    (N'{O_HD_DANGKY}',   N'{M_HOPDONG}', N'Đăng ký chứng thực HĐĐT',  N'/hop-dong/dang-ky',  N'HD_DANGKY',   1),
    (N'{O_HD_DANHSACH}', N'{M_HOPDONG}', N'Danh sách hợp đồng',       N'/hop-dong',          N'HD_DANHSACH', 2),
    (N'{O_HD_XULY}',     N'{M_HOPDONG}', N'Xử lý hồ sơ hợp đồng',     N'/hop-dong/xu-ly',    N'HD_XULY',     3)
) AS v(id, mid, name, url, code, ord)
WHERE NOT EXISTS (SELECT 1 FROM [Operation] o WHERE o.[Id] = CAST(v.id AS uniqueidentifier));

-- ============ ROLEOPERATION ============
-- Admin: full quyền 6 operation
INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT NEWID(), CAST(N'{R_ADMIN}' AS uniqueidentifier), op.[Id], 1, SYSDATETIME(), SYSDATETIME(), 0
FROM [Operation] op
WHERE op.[Id] IN (
    CAST(N'{O_NT_DANGKY}' AS uniqueidentifier), CAST(N'{O_NT_DANHSACH}' AS uniqueidentifier), CAST(N'{O_NT_XULY}' AS uniqueidentifier),
    CAST(N'{O_HD_DANGKY}' AS uniqueidentifier), CAST(N'{O_HD_DANHSACH}' AS uniqueidentifier), CAST(N'{O_HD_XULY}' AS uniqueidentifier)
)
AND NOT EXISTS (
    SELECT 1 FROM [RoleOperation] ro
    WHERE ro.[RoleId] = CAST(N'{R_ADMIN}' AS uniqueidentifier) AND ro.[OperationId] = op.[Id]
);

-- Doanh nghiệp: chỉ thấy form đăng ký và danh sách của mình
INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT NEWID(), CAST(N'{R_DOANHNGHIEP}' AS uniqueidentifier), op.[Id], 1, SYSDATETIME(), SYSDATETIME(), 0
FROM [Operation] op
WHERE op.[Id] IN (
    CAST(N'{O_NT_DANGKY}' AS uniqueidentifier), CAST(N'{O_NT_DANHSACH}' AS uniqueidentifier),
    CAST(N'{O_HD_DANGKY}' AS uniqueidentifier), CAST(N'{O_HD_DANHSACH}' AS uniqueidentifier)
)
AND NOT EXISTS (
    SELECT 1 FROM [RoleOperation] ro
    WHERE ro.[RoleId] = CAST(N'{R_DOANHNGHIEP}' AS uniqueidentifier) AND ro.[OperationId] = op.[Id]
);

-- Các role cán bộ (Sở + Cục): thấy danh sách + xử lý (cả nền tảng + hợp đồng)
INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT NEWID(), CAST(r.role_id AS uniqueidentifier), op.[Id], 1, SYSDATETIME(), SYSDATETIME(), 0
FROM [Operation] op
CROSS JOIN (VALUES
    (N'{R_CHUYENVIEN_SO}'),
    (N'{R_TRUONGPHONG_SO}'),
    (N'{R_LANHDAO_SO}'),
    (N'{R_CHUYENVIEN_CUC}'),
    (N'{R_TRUONGPHONG_CUC}'),
    (N'{R_LANHDAO_CUC}')
) AS r(role_id)
WHERE op.[Id] IN (
    CAST(N'{O_NT_DANHSACH}' AS uniqueidentifier), CAST(N'{O_NT_XULY}' AS uniqueidentifier),
    CAST(N'{O_HD_DANHSACH}' AS uniqueidentifier), CAST(N'{O_HD_XULY}' AS uniqueidentifier)
)
AND NOT EXISTS (
    SELECT 1 FROM [RoleOperation] ro
    WHERE ro.[RoleId] = CAST(r.role_id AS uniqueidentifier) AND ro.[OperationId] = op.[Id]
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM [RoleOperation] WHERE [OperationId] IN (
    CAST(N'{O_NT_DANGKY}' AS uniqueidentifier), CAST(N'{O_NT_DANHSACH}' AS uniqueidentifier), CAST(N'{O_NT_XULY}' AS uniqueidentifier),
    CAST(N'{O_HD_DANGKY}' AS uniqueidentifier), CAST(N'{O_HD_DANHSACH}' AS uniqueidentifier), CAST(N'{O_HD_XULY}' AS uniqueidentifier)
);
DELETE FROM [Operation] WHERE [Id] IN (
    CAST(N'{O_NT_DANGKY}' AS uniqueidentifier), CAST(N'{O_NT_DANHSACH}' AS uniqueidentifier), CAST(N'{O_NT_XULY}' AS uniqueidentifier),
    CAST(N'{O_HD_DANGKY}' AS uniqueidentifier), CAST(N'{O_HD_DANHSACH}' AS uniqueidentifier), CAST(N'{O_HD_XULY}' AS uniqueidentifier)
);
DELETE FROM [Module] WHERE [Id] IN (
    CAST(N'{M_NENTANG}' AS uniqueidentifier), CAST(N'{M_HOPDONG}' AS uniqueidentifier)
);
");
        }
    }
}
