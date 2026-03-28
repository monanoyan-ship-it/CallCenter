using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCategoryIconAndColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "SlnServiceCategories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconClass",
                table: "SlnServiceCategories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "SlnServiceCategories");

            migrationBuilder.DropColumn(
                name: "IconClass",
                table: "SlnServiceCategories");
        }
    }
}
