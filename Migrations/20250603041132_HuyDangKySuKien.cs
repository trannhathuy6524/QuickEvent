using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickEvent.Migrations
{
    /// <inheritdoc />
    public partial class HuyDangKySuKien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationDate",
                table: "Registrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Registrations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationDate",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Registrations");
        }
    }
}
