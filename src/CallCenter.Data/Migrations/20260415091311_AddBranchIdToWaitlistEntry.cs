using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdToWaitlistEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnWaitlistEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnWaitlistEntries_BranchId",
                table: "SlnWaitlistEntries",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnWaitlistEntries_SlnBranches_BranchId",
                table: "SlnWaitlistEntries",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnWaitlistEntries_SlnBranches_BranchId",
                table: "SlnWaitlistEntries");

            migrationBuilder.DropIndex(
                name: "IX_SlnWaitlistEntries_BranchId",
                table: "SlnWaitlistEntries");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnWaitlistEntries");
        }
    }
}
