using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickEvent.Migrations
{
    /// <inheritdoc />
    public partial class huy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QRCodeToken",
                table: "Registrations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QRCodeToken",
                table: "Registrations");
        }
    }
}
