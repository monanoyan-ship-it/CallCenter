using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonProfileBanners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannersJson",
                table: "SlnSalonProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowBanners",
                table: "SlnSalonProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannersJson",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "ShowBanners",
                table: "SlnSalonProfiles");
        }
    }
}
