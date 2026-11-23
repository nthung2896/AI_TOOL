using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hinet.Model.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemRoles : Migration
    {
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
INSERT INTO [Role] ([Id], [Name], [Code], [Type], [IsActive], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT CAST(v.id AS uniqueidentifier), v.name, v.code, v.type, 1, SYSDATETIME(), SYSDATETIME(), 0
FROM (VALUES
    (N'{R_DOANHNGHIEP}',     N'Doanh nghiệp',     N'DoanhNghiep',     N'DoanhNghiep'),
    (N'{R_CHUYENVIEN_SO}',   N'Chuyên viên sở',   N'ChuyenVienSo',    N'CanBo'),
    (N'{R_TRUONGPHONG_SO}',  N'Trưởng phòng sở',  N'TruongPhongSo',   N'CanBo'),
    (N'{R_LANHDAO_SO}',      N'Lãnh đạo sở',      N'LanhDaoSo',       N'CanBo'),
    (N'{R_CHUYENVIEN_CUC}',  N'Chuyên viên cục',  N'ChuyenVienCuc',   N'CanBo'),
    (N'{R_TRUONGPHONG_CUC}', N'Trưởng phòng cục', N'TruongPhongCuc',  N'CanBo'),
    (N'{R_LANHDAO_CUC}',     N'Lãnh đạo cục',     N'LanhDaoCuc',      N'CanBo')
) AS v(id, name, code, type)
WHERE NOT EXISTS (SELECT 1 FROM [Role] r WHERE r.[Code] = v.code AND r.[IsDeleted] = 0);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [Role] WHERE [Code] IN (
    N'DoanhNghiep', N'ChuyenVienSo', N'TruongPhongSo', N'LanhDaoSo',
    N'ChuyenVienCuc', N'TruongPhongCuc', N'LanhDaoCuc'
);
");
        }
    }
}
