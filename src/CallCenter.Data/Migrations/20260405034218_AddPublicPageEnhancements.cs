using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicPageEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "SlnSalonProfiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "SlnSalonProfiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowMap",
                table: "SlnSalonProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowReviews",
                table: "SlnSalonProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowTeam",
                table: "SlnSalonProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "CustomerPersonnel",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specialty",
                table: "CustomerPersonnel",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "ShowMap",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "ShowReviews",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "ShowTeam",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "Specialty",
                table: "CustomerPersonnel");
        }
    }
}
