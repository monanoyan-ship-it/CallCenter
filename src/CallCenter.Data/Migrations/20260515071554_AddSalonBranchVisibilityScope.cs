using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonBranchVisibilityScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnWinbackRules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnWhatsAppMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnReviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnEmailCampaigns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnClients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnCampaigns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnBeforeAfterPhotos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnAutoReminders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnWinbackRules_BranchId",
                table: "SlnWinbackRules",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnWhatsAppMessages_BranchId",
                table: "SlnWhatsAppMessages",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnReviews_BranchId",
                table: "SlnReviews",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnEmailCampaigns_BranchId",
                table: "SlnEmailCampaigns",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClients_BranchId",
                table: "SlnClients",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnCampaigns_BranchId",
                table: "SlnCampaigns",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnBeforeAfterPhotos_BranchId",
                table: "SlnBeforeAfterPhotos",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnAutoReminders_BranchId",
                table: "SlnAutoReminders",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnAutoReminders_SlnBranches_BranchId",
                table: "SlnAutoReminders",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnBeforeAfterPhotos_SlnBranches_BranchId",
                table: "SlnBeforeAfterPhotos",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnCampaigns_SlnBranches_BranchId",
                table: "SlnCampaigns",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnClients_SlnBranches_BranchId",
                table: "SlnClients",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnEmailCampaigns_SlnBranches_BranchId",
                table: "SlnEmailCampaigns",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnReviews_SlnBranches_BranchId",
                table: "SlnReviews",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnWhatsAppMessages_SlnBranches_BranchId",
                table: "SlnWhatsAppMessages",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnWinbackRules_SlnBranches_BranchId",
                table: "SlnWinbackRules",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnAutoReminders_SlnBranches_BranchId",
                table: "SlnAutoReminders");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnBeforeAfterPhotos_SlnBranches_BranchId",
                table: "SlnBeforeAfterPhotos");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnCampaigns_SlnBranches_BranchId",
                table: "SlnCampaigns");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnClients_SlnBranches_BranchId",
                table: "SlnClients");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnEmailCampaigns_SlnBranches_BranchId",
                table: "SlnEmailCampaigns");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnReviews_SlnBranches_BranchId",
                table: "SlnReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnWhatsAppMessages_SlnBranches_BranchId",
                table: "SlnWhatsAppMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_SlnWinbackRules_SlnBranches_BranchId",
                table: "SlnWinbackRules");

            migrationBuilder.DropIndex(
                name: "IX_SlnWinbackRules_BranchId",
                table: "SlnWinbackRules");

            migrationBuilder.DropIndex(
                name: "IX_SlnWhatsAppMessages_BranchId",
                table: "SlnWhatsAppMessages");

            migrationBuilder.DropIndex(
                name: "IX_SlnReviews_BranchId",
                table: "SlnReviews");

            migrationBuilder.DropIndex(
                name: "IX_SlnEmailCampaigns_BranchId",
                table: "SlnEmailCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_SlnClients_BranchId",
                table: "SlnClients");

            migrationBuilder.DropIndex(
                name: "IX_SlnCampaigns_BranchId",
                table: "SlnCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_SlnBeforeAfterPhotos_BranchId",
                table: "SlnBeforeAfterPhotos");

            migrationBuilder.DropIndex(
                name: "IX_SlnAutoReminders_BranchId",
                table: "SlnAutoReminders");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnWinbackRules");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnWhatsAppMessages");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnReviews");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnEmailCampaigns");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnClients");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnCampaigns");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnBeforeAfterPhotos");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnAutoReminders");
        }
    }
}
