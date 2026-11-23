using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hinet.Model.Migrations
{
    /// <inheritdoc />
    public partial class add_class_PlatformManage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformManage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AppManageTypeId = table.Column<int>(type: "int", nullable: true),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    TypeOrganization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyTaxCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyFax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ISPId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StaffNumber = table.Column<int>(type: "int", nullable: true),
                    RepresenterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterMobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterJob = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmitName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestChangeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestChangeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebsiteNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Seal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DomainAdd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TinhId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XaId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuocGiaId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TienIchKhac = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoaiHangHoaKhac = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldSysId = table.Column<long>(type: "bigint", nullable: true),
                    DateLine = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhuongThucLienHe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterNameOnline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterJobOnline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterMobileOnline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterEmailOnline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressOnline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhuongThucLienHeOnline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterNameDaiDien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterJobDaiDien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterMobileDaiDien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterEmailDaiDien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressDaiDien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhuongThucLienHeDaiDien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    API = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    URLApp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserApp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PassApp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsNuocNgoai = table.Column<bool>(type: "bit", nullable: true),
                    SoCongVan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CCCD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterCCCD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataSignedUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignDateUser = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ISPIdKhac = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TinhIdNew = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformManage", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "ed988174-d99c-41f1-a844-8a0c7931dcd8", "AQAAAAIAAYagAAAAEDk4MbtTJQ+dtf67aGmm3DWXbf+N33xxPjn3NhZR4dn/UpWJpTgjTwXpOIXfRx+XSw==", "1ef5a021-6bd9-4bf6-81b7-8d1176355b80" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformManage");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "01f581f4-9ad0-47d3-a36d-44e406a6d7b2", "AQAAAAIAAYagAAAAEDVG+SnNm+LznDZES0TB8S9gOaJgn+bWCUAbfWMQbCgIdlGe/RNzGKHes6yoGZr+Lg==", "ecd6847d-c789-4e75-be8e-a8b342847a01" });
        }
    }
}
