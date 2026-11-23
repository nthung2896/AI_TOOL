using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hinet.Model.Migrations
{
    /// <inheritdoc />
    public partial class SeedDepartments : Migration
    {
        // 34 đơn vị hành chính cấp tỉnh của Việt Nam (sau sáp nhập 2025):
        // 6 thành phố trực thuộc Trung ương + 28 tỉnh.
        // Code: "SCT_<viết tắt>" cho Sở Công Thương. "CUC_TMDT_KTS" cho cấp Cục.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO [Department] ([Id], [Name], [ShortName], [Code], [Level], [Loai], [IsActive], [Priority], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT CAST(v.id AS uniqueidentifier), v.name, v.shortname, v.code, 1, N'KhoiCoQuanChiNhanh', 1, 1, SYSDATETIME(), SYSDATETIME(), 0
FROM (VALUES
    -- ===== Cấp Trung ương =====
    (N'33000000-0000-0000-0000-000000000000', N'Cục Thương mại điện tử và Kinh tế số',                N'Cục TMĐT & KTS',     N'CUC_TMDT_KTS'),

    -- ===== 6 thành phố trực thuộc Trung ương =====
    (N'33000000-0000-0000-0000-000000000001', N'Sở Công Thương thành phố Hà Nội',                     N'SCT Hà Nội',         N'SCT_HN'),
    (N'33000000-0000-0000-0000-000000000002', N'Sở Công Thương Thành phố Hồ Chí Minh',                N'SCT TP.HCM',         N'SCT_HCM'),
    (N'33000000-0000-0000-0000-000000000003', N'Sở Công Thương thành phố Hải Phòng',                  N'SCT Hải Phòng',      N'SCT_HP'),
    (N'33000000-0000-0000-0000-000000000004', N'Sở Công Thương thành phố Đà Nẵng',                    N'SCT Đà Nẵng',        N'SCT_DN'),
    (N'33000000-0000-0000-0000-000000000005', N'Sở Công Thương thành phố Cần Thơ',                    N'SCT Cần Thơ',        N'SCT_CT'),
    (N'33000000-0000-0000-0000-000000000006', N'Sở Công Thương thành phố Huế',                        N'SCT Huế',            N'SCT_HUE'),

    -- ===== 9 tỉnh giữ nguyên (không sáp nhập) =====
    (N'33000000-0000-0000-0000-000000000007', N'Sở Công Thương tỉnh Cao Bằng',                        N'SCT Cao Bằng',       N'SCT_CB'),
    (N'33000000-0000-0000-0000-000000000008', N'Sở Công Thương tỉnh Lạng Sơn',                        N'SCT Lạng Sơn',       N'SCT_LS'),
    (N'33000000-0000-0000-0000-000000000009', N'Sở Công Thương tỉnh Lai Châu',                        N'SCT Lai Châu',       N'SCT_LCH'),
    (N'33000000-0000-0000-0000-00000000000a', N'Sở Công Thương tỉnh Điện Biên',                       N'SCT Điện Biên',      N'SCT_DB'),
    (N'33000000-0000-0000-0000-00000000000b', N'Sở Công Thương tỉnh Sơn La',                          N'SCT Sơn La',         N'SCT_SL'),
    (N'33000000-0000-0000-0000-00000000000c', N'Sở Công Thương tỉnh Quảng Ninh',                      N'SCT Quảng Ninh',     N'SCT_QN'),
    (N'33000000-0000-0000-0000-00000000000d', N'Sở Công Thương tỉnh Thanh Hóa',                       N'SCT Thanh Hóa',      N'SCT_TH'),
    (N'33000000-0000-0000-0000-00000000000e', N'Sở Công Thương tỉnh Nghệ An',                         N'SCT Nghệ An',        N'SCT_NA'),
    (N'33000000-0000-0000-0000-00000000000f', N'Sở Công Thương tỉnh Hà Tĩnh',                         N'SCT Hà Tĩnh',        N'SCT_HT'),

    -- ===== 19 tỉnh hình thành sau sáp nhập =====
    (N'33000000-0000-0000-0000-000000000010', N'Sở Công Thương tỉnh Tuyên Quang',                     N'SCT Tuyên Quang',    N'SCT_TQ'),
    (N'33000000-0000-0000-0000-000000000011', N'Sở Công Thương tỉnh Lào Cai',                         N'SCT Lào Cai',        N'SCT_LC'),
    (N'33000000-0000-0000-0000-000000000012', N'Sở Công Thương tỉnh Thái Nguyên',                     N'SCT Thái Nguyên',    N'SCT_TN'),
    (N'33000000-0000-0000-0000-000000000013', N'Sở Công Thương tỉnh Phú Thọ',                         N'SCT Phú Thọ',        N'SCT_PT'),
    (N'33000000-0000-0000-0000-000000000014', N'Sở Công Thương tỉnh Bắc Ninh',                        N'SCT Bắc Ninh',       N'SCT_BN'),
    (N'33000000-0000-0000-0000-000000000015', N'Sở Công Thương tỉnh Hưng Yên',                        N'SCT Hưng Yên',       N'SCT_HY'),
    (N'33000000-0000-0000-0000-000000000016', N'Sở Công Thương tỉnh Ninh Bình',                       N'SCT Ninh Bình',      N'SCT_NB'),
    (N'33000000-0000-0000-0000-000000000017', N'Sở Công Thương tỉnh Quảng Trị',                       N'SCT Quảng Trị',      N'SCT_QT'),
    (N'33000000-0000-0000-0000-000000000018', N'Sở Công Thương tỉnh Quảng Ngãi',                      N'SCT Quảng Ngãi',     N'SCT_QNG'),
    (N'33000000-0000-0000-0000-000000000019', N'Sở Công Thương tỉnh Gia Lai',                         N'SCT Gia Lai',        N'SCT_GL'),
    (N'33000000-0000-0000-0000-00000000001a', N'Sở Công Thương tỉnh Khánh Hòa',                       N'SCT Khánh Hòa',      N'SCT_KH'),
    (N'33000000-0000-0000-0000-00000000001b', N'Sở Công Thương tỉnh Lâm Đồng',                        N'SCT Lâm Đồng',       N'SCT_LD'),
    (N'33000000-0000-0000-0000-00000000001c', N'Sở Công Thương tỉnh Đắk Lắk',                         N'SCT Đắk Lắk',        N'SCT_DLK'),
    (N'33000000-0000-0000-0000-00000000001d', N'Sở Công Thương tỉnh Đồng Nai',                        N'SCT Đồng Nai',       N'SCT_DNI'),
    (N'33000000-0000-0000-0000-00000000001e', N'Sở Công Thương tỉnh Tây Ninh',                        N'SCT Tây Ninh',       N'SCT_TNI'),
    (N'33000000-0000-0000-0000-00000000001f', N'Sở Công Thương tỉnh Vĩnh Long',                       N'SCT Vĩnh Long',      N'SCT_VL'),
    (N'33000000-0000-0000-0000-000000000020', N'Sở Công Thương tỉnh Đồng Tháp',                       N'SCT Đồng Tháp',      N'SCT_DT'),
    (N'33000000-0000-0000-0000-000000000021', N'Sở Công Thương tỉnh Cà Mau',                          N'SCT Cà Mau',         N'SCT_CM'),
    (N'33000000-0000-0000-0000-000000000022', N'Sở Công Thương tỉnh An Giang',                        N'SCT An Giang',       N'SCT_AG')
) AS v(id, name, shortname, code)
WHERE NOT EXISTS (SELECT 1 FROM [Department] d WHERE d.[Code] = v.code AND d.[IsDeleted] = 0);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [Department]
WHERE [Code] LIKE N'SCT_%' OR [Code] = N'CUC_TMDT_KTS';
");
        }
    }
}
