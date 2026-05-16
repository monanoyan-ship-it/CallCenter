using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonMembershipPlanBranchScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnMembershipPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnMembershipPlans_BranchId",
                table: "SlnMembershipPlans",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnMembershipPlans_SlnBranches_BranchId",
                table: "SlnMembershipPlans",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnMembershipPlans_SlnBranches_BranchId",
                table: "SlnMembershipPlans");

            migrationBuilder.DropIndex(
                name: "IX_SlnMembershipPlans_BranchId",
                table: "SlnMembershipPlans");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnMembershipPlans");
        }
    }
}
