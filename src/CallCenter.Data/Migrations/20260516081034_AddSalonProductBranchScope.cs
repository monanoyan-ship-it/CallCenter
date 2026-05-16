using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonProductBranchScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnProducts_BranchId",
                table: "SlnProducts",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnProducts_SlnBranches_BranchId",
                table: "SlnProducts",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnProducts_SlnBranches_BranchId",
                table: "SlnProducts");

            migrationBuilder.DropIndex(
                name: "IX_SlnProducts_BranchId",
                table: "SlnProducts");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnProducts");
        }
    }
}
