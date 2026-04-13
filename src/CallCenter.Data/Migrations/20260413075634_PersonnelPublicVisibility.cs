using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersonnelPublicVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PublicShowFullName",
                table: "CustomerPersonnel",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PublicShowPhoto",
                table: "CustomerPersonnel",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PublicShowSpecialty",
                table: "CustomerPersonnel",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PublicShowTitle",
                table: "CustomerPersonnel",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PublicVisible",
                table: "CustomerPersonnel",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicShowFullName",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "PublicShowPhoto",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "PublicShowSpecialty",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "PublicShowTitle",
                table: "CustomerPersonnel");

            migrationBuilder.DropColumn(
                name: "PublicVisible",
                table: "CustomerPersonnel");
        }
    }
}
