using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonProfilePageSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SectionOrderJson",
                table: "SlnSalonProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowBooking",
                table: "SlnSalonProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowContact",
                table: "SlnSalonProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHours",
                table: "SlnSalonProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowMemberships",
                table: "SlnSalonProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowServices",
                table: "SlnSalonProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SectionOrderJson",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "ShowBooking",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "ShowContact",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "ShowHours",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "ShowMemberships",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "ShowServices",
                table: "SlnSalonProfiles");
        }
    }
}
