using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hinet.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyInfo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnglishName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CityName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterMobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepresenterCCCD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApproveDateOnline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TinhId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TinhIdNew = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    XaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuocGiaId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TypeOrganization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsNuocNgoai = table.Column<bool>(type: "bit", nullable: false),
                    IsVonDauTuNuocNgoai = table.Column<bool>(type: "bit", nullable: false),
                    DKKD = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_CompanyInfo", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "30cd8500-dd6b-42a1-ae24-4d77a4d50451", "AQAAAAIAAYagAAAAEOZrlXxI1ifte0PI9QHp8lCqMWvwr5nEBGSRE4aFzmQqRvMipBvA52iIlPIbJpj/3Q==", "b086b40f-671f-4a14-86a5-d7cefe27df44" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyInfo");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "01f581f4-9ad0-47d3-a36d-44e406a6d7b2", "AQAAAAIAAYagAAAAEDVG+SnNm+LznDZES0TB8S9gOaJgn+bWCUAbfWMQbCgIdlGe/RNzGKHes6yoGZr+Lg==", "ecd6847d-c789-4e75-be8e-a8b342847a01" });
        }
    }
}
