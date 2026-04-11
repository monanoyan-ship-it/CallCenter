using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeZoneAndRecipeRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "PlatformUsers",
                type: "text",
                nullable: false,
                defaultValue: "Europe/Istanbul");

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Customers",
                type: "text",
                nullable: false,
                defaultValue: "Europe/Istanbul");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "PlatformUsers");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Customers");
        }
    }
}
