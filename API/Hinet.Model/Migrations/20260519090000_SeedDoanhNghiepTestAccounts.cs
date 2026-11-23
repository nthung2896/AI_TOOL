using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hinet.Model.Migrations
{
    /// <inheritdoc />
    public partial class SeedDoanhNghiepTestAccounts : Migration
    {
        // ====== Users (test DN - đăng nhập KHÔNG qua SSO) ======
        private const string U_DN_ABC          = "55555555-0000-0000-0000-000000000001";
        private const string U_DN_XYZ_FDI      = "55555555-0000-0000-0000-000000000002";
        private const string U_DN_CANHAN       = "55555555-0000-0000-0000-000000000003";
        private const string U_DN_NUOCNGOAI    = "55555555-0000-0000-0000-000000000004";

        // ====== CompanyInfo (gắn theo từng user) ======
        private const string C_ABC          = "66666666-0000-0000-0000-000000000001";
        private const string C_XYZ          = "66666666-0000-0000-0000-000000000002";
        private const string C_CANHAN       = "66666666-0000-0000-0000-000000000003";
        private const string C_NUOCNGOAI    = "66666666-0000-0000-0000-000000000004";

        // Role DoanhNghiep (đã seed ở 20260519050000)
        private const string R_DOANHNGHIEP  = "22222222-0000-0000-0000-000000000001";

        // Sở Công Thương Hà Nội (đã seed ở 20260519050001)
        private const string D_SCT_HN       = "33000000-0000-0000-0000-000000000001";

        // Password "12345678" — dùng chung hash đã verify (Identity v3 hash đã chứa salt)
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
    [Name], [Gender], [Type], [IsSSO],
    [CreatedDate], [UpdatedDate], [IsDeleted]
)
SELECT
    CAST(v.id AS uniqueidentifier), v.username, UPPER(v.username), v.email, UPPER(v.email),
    1, N'{PASSWORD_HASH}', CONVERT(nvarchar(36), NEWID()), CONVERT(nvarchar(36), NEWID()),
    0, 0, 1, 0,
    v.fullname, 0, N'DoanhNghiep', 0,
    SYSDATETIME(), SYSDATETIME(), 0
FROM (VALUES
    (N'{U_DN_ABC}',       N'dn_abc',       N'lienhe@congtyabc.vn',    N'Công ty TNHH ABC'),
    (N'{U_DN_XYZ_FDI}',   N'dn_xyz',       N'admin@xyzgroup.vn',      N'Công ty CP XYZ (FDI)'),
    (N'{U_DN_CANHAN}',    N'dn_canhan',    N'nguyenvana@gmail.com',   N'Nguyễn Văn A (Cá nhân)'),
    (N'{U_DN_NUOCNGOAI}', N'dn_global',    N'sea@globaltradeasia.com',N'Global Trade Asia Pte Ltd')
) AS v(id, username, email, fullname)
WHERE NOT EXISTS (SELECT 1 FROM [AspNetUsers] u WHERE u.[NormalizedUserName] = UPPER(v.username));

-- ============ USERROLE: gán role DoanhNghiep ============
INSERT INTO [UserRole] ([Id], [UserId], [RoleId], [DepartmentId], [CreatedDate], [UpdatedDate], [IsDeleted])
SELECT NEWID(), CAST(v.uid AS uniqueidentifier), CAST(N'{R_DOANHNGHIEP}' AS uniqueidentifier), CAST(N'00000000-0000-0000-0000-000000000000' AS uniqueidentifier), SYSDATETIME(), SYSDATETIME(), 0
FROM (VALUES
    (N'{U_DN_ABC}'),
    (N'{U_DN_XYZ_FDI}'),
    (N'{U_DN_CANHAN}'),
    (N'{U_DN_NUOCNGOAI}')
) AS v(uid)
WHERE NOT EXISTS (
    SELECT 1 FROM [UserRole] ur
    WHERE ur.[UserId] = CAST(v.uid AS uniqueidentifier) AND ur.[RoleId] = CAST(N'{R_DOANHNGHIEP}' AS uniqueidentifier)
);

-- ============ COMPANYINFO ============
-- 1. Công ty TNHH ABC (Company, trong nước, Hà Nội)
INSERT INTO [CompanyInfo] (
    [Id], [Status], [Name], [EnglishName], [ShortName], [TaxCode],
    [Address], [CityId], [CityName], [TinhIdNew],
    [Phone], [Fax], [Email],
    [RepresenterName], [RepresenterMobile], [RepresenterPhone], [RepresenterEmail], [RepresenterCCCD],
    [TypeOrganization], [IsNuocNgoai], [IsVonDauTuNuocNgoai],
    [QuocGiaId], [Detail], [DKKD],
    [CreatedDate], [UpdatedDate], [IsDeleted]
)
SELECT
    CAST(N'{C_ABC}' AS uniqueidentifier), 1, N'Công ty TNHH ABC', N'ABC Co., Ltd', N'ABC', N'0100123456',
    N'Số 1 Trần Hưng Đạo, Hoàn Kiếm', N'1', N'Hà Nội', CAST(N'{D_SCT_HN}' AS uniqueidentifier),
    N'024.39001234', N'024.39001235', N'lienhe@congtyabc.vn',
    N'Nguyễn Văn An', N'0901111111', N'024.39001234', N'an.nguyen@congtyabc.vn', N'001190001234',
    N'Company', 0, 0,
    N'VN', N'Doanh nghiệp trong nước hoạt động TMĐT', N'/uploads/dkkd/abc.pdf',
    SYSDATETIME(), SYSDATETIME(), 0
WHERE NOT EXISTS (SELECT 1 FROM [CompanyInfo] WHERE [Id] = CAST(N'{C_ABC}' AS uniqueidentifier));

-- 2. Công ty CP XYZ (Company, có vốn FDI)
INSERT INTO [CompanyInfo] (
    [Id], [Status], [Name], [EnglishName], [ShortName], [TaxCode],
    [Address], [CityId], [CityName], [TinhIdNew],
    [Phone], [Email],
    [RepresenterName], [RepresenterMobile], [RepresenterEmail], [RepresenterCCCD],
    [TypeOrganization], [IsNuocNgoai], [IsVonDauTuNuocNgoai],
    [QuocGiaId], [DKKD],
    [CreatedDate], [UpdatedDate], [IsDeleted]
)
SELECT
    CAST(N'{C_XYZ}' AS uniqueidentifier), 1, N'Công ty Cổ phần XYZ', N'XYZ Joint Stock Company', N'XYZ', N'0300999888',
    N'Lô A2 KCN Tân Bình, Phường 15', N'50', N'TP.HCM', CAST(N'{D_SCT_HN}' AS uniqueidentifier),
    N'028.38003344', N'admin@xyzgroup.vn',
    N'Trần Thị Bình', N'0902222222', N'binh.tran@xyzgroup.vn', N'079190002222',
    N'Company', 0, 1,
    N'VN', N'/uploads/dkkd/xyz.pdf',
    SYSDATETIME(), SYSDATETIME(), 0
WHERE NOT EXISTS (SELECT 1 FROM [CompanyInfo] WHERE [Id] = CAST(N'{C_XYZ}' AS uniqueidentifier));

-- 3. Cá nhân kinh doanh (Personal)
INSERT INTO [CompanyInfo] (
    [Id], [Status], [Name], [TaxCode],
    [Address], [CityId], [CityName], [TinhIdNew],
    [Phone], [Email],
    [RepresenterName], [RepresenterMobile], [RepresenterEmail], [RepresenterCCCD],
    [TypeOrganization], [IsNuocNgoai], [IsVonDauTuNuocNgoai],
    [QuocGiaId],
    [CreatedDate], [UpdatedDate], [IsDeleted]
)
SELECT
    CAST(N'{C_CANHAN}' AS uniqueidentifier), 1, N'Nguyễn Văn A', N'8123456789',
    N'15 Phố Huế, Hai Bà Trưng', N'1', N'Hà Nội', CAST(N'{D_SCT_HN}' AS uniqueidentifier),
    NULL, N'nguyenvana@gmail.com',
    N'Nguyễn Văn A', N'0903333333', N'nguyenvana@gmail.com', N'001190003333',
    N'Personal', 0, 0,
    N'VN',
    SYSDATETIME(), SYSDATETIME(), 0
WHERE NOT EXISTS (SELECT 1 FROM [CompanyInfo] WHERE [Id] = CAST(N'{C_CANHAN}' AS uniqueidentifier));

-- 4. Tổ chức nước ngoài (Organization, IsNuocNgoai)
INSERT INTO [CompanyInfo] (
    [Id], [Status], [Name], [EnglishName], [TaxCode],
    [Address], [CityName],
    [Phone], [Email],
    [RepresenterName], [RepresenterMobile], [RepresenterEmail],
    [TypeOrganization], [IsNuocNgoai], [IsVonDauTuNuocNgoai],
    [QuocGiaId],
    [CreatedDate], [UpdatedDate], [IsDeleted]
)
SELECT
    CAST(N'{C_NUOCNGOAI}' AS uniqueidentifier), 1, N'Global Trade Asia Pte Ltd', N'Global Trade Asia Pte Ltd', N'SG-201912345R',
    N'8 Marina Boulevard, Marina Bay Financial Centre', N'Singapore',
    N'+65 6123 4567', N'sea@globaltradeasia.com',
    N'John Smith', N'+6591234567', N'john.smith@globaltradeasia.com',
    N'Organization', 1, 1,
    N'SG',
    SYSDATETIME(), SYSDATETIME(), 0
WHERE NOT EXISTS (SELECT 1 FROM [CompanyInfo] WHERE [Id] = CAST(N'{C_NUOCNGOAI}' AS uniqueidentifier));
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM [CompanyInfo] WHERE [Id] IN (
    CAST(N'{C_ABC}' AS uniqueidentifier), CAST(N'{C_XYZ}' AS uniqueidentifier), CAST(N'{C_CANHAN}' AS uniqueidentifier), CAST(N'{C_NUOCNGOAI}' AS uniqueidentifier)
);
DELETE FROM [UserRole] WHERE [UserId] IN (
    CAST(N'{U_DN_ABC}' AS uniqueidentifier), CAST(N'{U_DN_XYZ_FDI}' AS uniqueidentifier), CAST(N'{U_DN_CANHAN}' AS uniqueidentifier), CAST(N'{U_DN_NUOCNGOAI}' AS uniqueidentifier)
);
DELETE FROM [AspNetUsers] WHERE [Id] IN (
    CAST(N'{U_DN_ABC}' AS uniqueidentifier), CAST(N'{U_DN_XYZ_FDI}' AS uniqueidentifier), CAST(N'{U_DN_CANHAN}' AS uniqueidentifier), CAST(N'{U_DN_NUOCNGOAI}' AS uniqueidentifier)
);
");
        }
    }
}
