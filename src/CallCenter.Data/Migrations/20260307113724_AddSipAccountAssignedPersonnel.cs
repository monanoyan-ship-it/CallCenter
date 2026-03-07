using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSipAccountAssignedPersonnel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedPersonnelId",
                table: "SipAccounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SipAccounts_AssignedPersonnelId",
                table: "SipAccounts",
                column: "AssignedPersonnelId");

            migrationBuilder.AddForeignKey(
                name: "FK_SipAccounts_CustomerPersonnel_AssignedPersonnelId",
                table: "SipAccounts",
                column: "AssignedPersonnelId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SipAccounts_CustomerPersonnel_AssignedPersonnelId",
                table: "SipAccounts");

            migrationBuilder.DropIndex(
                name: "IX_SipAccounts_AssignedPersonnelId",
                table: "SipAccounts");

            migrationBuilder.DropColumn(
                name: "AssignedPersonnelId",
                table: "SipAccounts");
        }
    }
}
