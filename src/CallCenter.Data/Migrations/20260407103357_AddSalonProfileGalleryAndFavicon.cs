using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonProfileGalleryAndFavicon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaviconUrl",
                table: "SlnSalonProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GalleryImagesJson",
                table: "SlnSalonProfiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaviconUrl",
                table: "SlnSalonProfiles");

            migrationBuilder.DropColumn(
                name: "GalleryImagesJson",
                table: "SlnSalonProfiles");
        }
    }
}
