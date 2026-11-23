using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hinet.Model.Migrations
{
    /// <inheritdoc />
    public partial class GrantDoanhNghiepBasicMenu : Migration
    {
        // Role DN (đã seed ở 20260519050000)
        private const string R_DOANHNGHIEP = "22222222-0000-0000-0000-000000000001";

        // Operations cơ bản (đã seed ở 20260519041320_SeedAdminMenu)
        private const string O_THONGBAO = "40000000-0000-0000-0000-00000000000a"; // /notification
        private const string O_PROFILE  = "40000000-0000-0000-0000-00000000000b"; // /profile

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
-- Cấp quyền cho role DoanhNghiep truy cập Thông báo + Tài khoản cá nhân
INSERT INTO [RoleOperation] ([Id], [RoleId], [OperationId], [IsAccess], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT NEWID(), CAST(N'{R_DOANHNGHIEP}' AS uniqueidentifier), CAST(v.op AS uniqueidentifier), 1, SYSDATETIME(), SYSDATETIME(), 0
FROM (VALUES
    (N'{O_THONGBAO}'),
    (N'{O_PROFILE}')
) AS v(op)
WHERE NOT EXISTS (
    SELECT 1 FROM [RoleOperation] ro
    WHERE ro.[RoleId] = CAST(N'{R_DOANHNGHIEP}' AS uniqueidentifier) AND ro.[OperationId] = CAST(v.op AS uniqueidentifier)
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM [RoleOperation]
WHERE [RoleId] = CAST(N'{R_DOANHNGHIEP}' AS uniqueidentifier)
  AND [OperationId] IN (CAST(N'{O_THONGBAO}' AS uniqueidentifier), CAST(N'{O_PROFILE}' AS uniqueidentifier));
");
        }
    }
}
