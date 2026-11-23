using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hinet.Model.Migrations
{
    /// <inheritdoc />
    public partial class SeedCompanyInfoMenu : Migration
    {
        private const string M_QLDN     = "30000000-0000-0000-0000-000000000020";
        private const string O_QLDN     = "40000000-0000-0000-0000-000000000300";
        private const string R_ADMIN    = "22222222-2222-2222-2222-222222222222";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
-- ============ MODULE ============
INSERT INTO [Module] ([Id], [Code], [Name], [Order], [IsShow], [Icon], [ClassCss], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT CAST(N'{M_QLDN}' AS uniqueidentifier), N'QLDN', N'Quản lý doanh nghiệp', 5, 1, N'<BankOutlined />', N'<BankOutlined />', SYSDATETIME(), SYSDATETIME(), 0
WHERE NOT EXISTS (SELECT 1 FROM [Module] WHERE [Id] = CAST(N'{M_QLDN}' AS uniqueidentifier));

-- ============ OPERATION ============
INSERT INTO [Operation] ([Id], [ModuleId], [Name], [Url], [Code], [Order], [IsShow], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT CAST(N'{O_QLDN}' AS uniqueidentifier), CAST(N'{M_QLDN}' AS uniqueidentifier), N'Doanh nghiệp', N'/QLDoanhNghiep', N'QLDOANHNGHIEP', 1, 1, SYSDATETIME(), SYSDATETIME(), 0
WHERE NOT EXISTS (SELECT 1 FROM [Operation] WHERE [Id] = CAST(N'{O_QLDN}' AS uniqueidentifier));

-- ============ ROLEOPERATION: Admin được quyền truy cập ============
INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT NEWID(), CAST(N'{R_ADMIN}' AS uniqueidentifier), CAST(N'{O_QLDN}' AS uniqueidentifier), 1, SYSDATETIME(), SYSDATETIME(), 0
WHERE NOT EXISTS (
    SELECT 1 FROM [RoleOperation]
    WHERE [RoleId] = CAST(N'{R_ADMIN}' AS uniqueidentifier) AND [OperationId] = CAST(N'{O_QLDN}' AS uniqueidentifier)
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM [RoleOperation] WHERE [OperationId] = CAST(N'{O_QLDN}' AS uniqueidentifier);
DELETE FROM [Operation] WHERE [Id] = CAST(N'{O_QLDN}' AS uniqueidentifier);
DELETE FROM [Module] WHERE [Id] = CAST(N'{M_QLDN}' AS uniqueidentifier);
");
        }
    }
}
