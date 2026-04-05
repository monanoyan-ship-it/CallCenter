using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeProductCategoryIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnProducts_SlnProductCategories_CategoryId",
                table: "SlnProducts");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "SlnProducts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnProducts_SlnProductCategories_CategoryId",
                table: "SlnProducts",
                column: "CategoryId",
                principalTable: "SlnProductCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnProducts_SlnProductCategories_CategoryId",
                table: "SlnProducts");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "SlnProducts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SlnProducts_SlnProductCategories_CategoryId",
                table: "SlnProducts",
                column: "CategoryId",
                principalTable: "SlnProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
