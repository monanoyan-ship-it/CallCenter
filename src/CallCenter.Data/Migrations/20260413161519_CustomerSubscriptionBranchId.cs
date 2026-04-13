using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class CustomerSubscriptionBranchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "CustomerSubscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSubscriptions_BranchId",
                table: "CustomerSubscriptions",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerSubscriptions_SlnBranches_BranchId",
                table: "CustomerSubscriptions",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerSubscriptions_SlnBranches_BranchId",
                table: "CustomerSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerSubscriptions_BranchId",
                table: "CustomerSubscriptions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "CustomerSubscriptions");
        }
    }
}
