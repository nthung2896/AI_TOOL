using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hinet.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddFileServerColumnsToTaiLieuDinhKem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CoChuKySo",
                table: "TaiLieuDinhKem",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DonViPhatHanh",
                table: "TaiLieuDinhKem",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsKySo",
                table: "TaiLieuDinhKem",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NgayKy",
                table: "TaiLieuDinhKem",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiKy",
                table: "TaiLieuDinhKem",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "4797334c-789a-44ee-9aab-26bb4787babe", "AQAAAAIAAYagAAAAEIGQyxefovd0J7Droo/w3ERUiuF1dJsODyfs7a7HwS7soszbaT5AMty5fS0Ifpb83w==", "647c23b9-85c8-4092-8aa0-c22192791edb" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoChuKySo",
                table: "TaiLieuDinhKem");

            migrationBuilder.DropColumn(
                name: "DonViPhatHanh",
                table: "TaiLieuDinhKem");

            migrationBuilder.DropColumn(
                name: "IsKySo",
                table: "TaiLieuDinhKem");

            migrationBuilder.DropColumn(
                name: "NgayKy",
                table: "TaiLieuDinhKem");

            migrationBuilder.DropColumn(
                name: "NguoiKy",
                table: "TaiLieuDinhKem");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c3964f0a-4b64-4288-8dd4-c6870cf19abf", "AQAAAAIAAYagAAAAENs1j3jxe+2SXbg6P9e7XgmlYKd7haP8tzxSpAwIzVDeN+tcc41bQZPEJGPdPCaN8Q==", "de28ca15-1432-45f6-805c-8d391505c025" });
        }
    }
}
