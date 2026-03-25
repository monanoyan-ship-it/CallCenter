using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameCrmPrefixForQualityAndContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ═══ Tablo rename'leri (veri korunur) ═══

            // Contact -> CrmContact
            migrationBuilder.RenameTable(name: "Contacts", newName: "CrmContacts");

            // Quality -> CrmQuality
            migrationBuilder.RenameTable(name: "QualityChecklists", newName: "CrmQualityChecklists");
            migrationBuilder.RenameTable(name: "QualityQuestions", newName: "CrmQualityQuestions");
            migrationBuilder.RenameTable(name: "QualityQuestionSubCriteria", newName: "CrmQualityQuestionSubCriteria");
            migrationBuilder.RenameTable(name: "QualityEvaluations", newName: "CrmQualityEvaluations");
            migrationBuilder.RenameTable(name: "QualityAnswers", newName: "CrmQualityAnswers");
            migrationBuilder.RenameTable(name: "QualityAnswerSubCriteriaSelections", newName: "CrmQualityAnswerSubCriteriaSelections");
            migrationBuilder.RenameTable(name: "QualityScoreThresholds", newName: "CrmQualityScoreThresholds");

            // ═══ FK kolon rename'leri (ContactId -> CrmContactId) ═══

            migrationBuilder.RenameColumn(name: "ContactId", table: "CampaignContacts", newName: "CrmContactId");
            migrationBuilder.RenameColumn(name: "ContactId", table: "CrmActivities", newName: "CrmContactId");
            migrationBuilder.RenameColumn(name: "ContactId", table: "CrmContactTagLinks", newName: "CrmContactId");
            migrationBuilder.RenameColumn(name: "ContactId", table: "CrmDeals", newName: "CrmContactId");
            migrationBuilder.RenameColumn(name: "ContactId", table: "CrmSurveyResponses", newName: "CrmContactId");
            migrationBuilder.RenameColumn(name: "ContactId", table: "CrmTasks", newName: "CrmContactId");
            migrationBuilder.RenameColumn(name: "ContactId", table: "CrmTickets", newName: "CrmContactId");

            // IntegrationConnections
            migrationBuilder.RenameColumn(name: "ContactSyncDirectionId", table: "IntegrationConnections", newName: "CrmContactSyncDirectionId");

            // ═══ Index rename'leri ═══

            migrationBuilder.RenameIndex(name: "IX_CampaignContacts_ContactId", table: "CampaignContacts", newName: "IX_CampaignContacts_CrmContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmActivities_ContactId", table: "CrmActivities", newName: "IX_CrmActivities_CrmContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmContactTagLinks_ContactId", table: "CrmContactTagLinks", newName: "IX_CrmContactTagLinks_CrmContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmDeals_ContactId", table: "CrmDeals", newName: "IX_CrmDeals_CrmContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmSurveyResponses_ContactId", table: "CrmSurveyResponses", newName: "IX_CrmSurveyResponses_CrmContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmTasks_ContactId", table: "CrmTasks", newName: "IX_CrmTasks_CrmContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmTickets_ContactId", table: "CrmTickets", newName: "IX_CrmTickets_CrmContactId");

            // ═══ FK constraint rename'leri ═══

            migrationBuilder.DropForeignKey(name: "FK_CampaignContacts_Contacts_ContactId", table: "CampaignContacts");
            migrationBuilder.AddForeignKey(name: "FK_CampaignContacts_CrmContacts_CrmContactId", table: "CampaignContacts", column: "CrmContactId", principalTable: "CrmContacts", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropForeignKey(name: "FK_CrmActivities_Contacts_ContactId", table: "CrmActivities");
            migrationBuilder.AddForeignKey(name: "FK_CrmActivities_CrmContacts_CrmContactId", table: "CrmActivities", column: "CrmContactId", principalTable: "CrmContacts", principalColumn: "Id");

            migrationBuilder.DropForeignKey(name: "FK_CrmContactTagLinks_Contacts_ContactId", table: "CrmContactTagLinks");
            migrationBuilder.AddForeignKey(name: "FK_CrmContactTagLinks_CrmContacts_CrmContactId", table: "CrmContactTagLinks", column: "CrmContactId", principalTable: "CrmContacts", principalColumn: "Id", onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropForeignKey(name: "FK_CrmDeals_Contacts_ContactId", table: "CrmDeals");
            migrationBuilder.AddForeignKey(name: "FK_CrmDeals_CrmContacts_CrmContactId", table: "CrmDeals", column: "CrmContactId", principalTable: "CrmContacts", principalColumn: "Id");

            migrationBuilder.DropForeignKey(name: "FK_CrmSurveyResponses_Contacts_ContactId", table: "CrmSurveyResponses");
            migrationBuilder.AddForeignKey(name: "FK_CrmSurveyResponses_CrmContacts_CrmContactId", table: "CrmSurveyResponses", column: "CrmContactId", principalTable: "CrmContacts", principalColumn: "Id");

            migrationBuilder.DropForeignKey(name: "FK_CrmTasks_Contacts_ContactId", table: "CrmTasks");
            migrationBuilder.AddForeignKey(name: "FK_CrmTasks_CrmContacts_CrmContactId", table: "CrmTasks", column: "CrmContactId", principalTable: "CrmContacts", principalColumn: "Id");

            migrationBuilder.DropForeignKey(name: "FK_CrmTickets_Contacts_ContactId", table: "CrmTickets");
            migrationBuilder.AddForeignKey(name: "FK_CrmTickets_CrmContacts_CrmContactId", table: "CrmTickets", column: "CrmContactId", principalTable: "CrmContacts", principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // FK constraint geri al
            migrationBuilder.DropForeignKey(name: "FK_CampaignContacts_CrmContacts_CrmContactId", table: "CampaignContacts");
            migrationBuilder.DropForeignKey(name: "FK_CrmActivities_CrmContacts_CrmContactId", table: "CrmActivities");
            migrationBuilder.DropForeignKey(name: "FK_CrmContactTagLinks_CrmContacts_CrmContactId", table: "CrmContactTagLinks");
            migrationBuilder.DropForeignKey(name: "FK_CrmDeals_CrmContacts_CrmContactId", table: "CrmDeals");
            migrationBuilder.DropForeignKey(name: "FK_CrmSurveyResponses_CrmContacts_CrmContactId", table: "CrmSurveyResponses");
            migrationBuilder.DropForeignKey(name: "FK_CrmTasks_CrmContacts_CrmContactId", table: "CrmTasks");
            migrationBuilder.DropForeignKey(name: "FK_CrmTickets_CrmContacts_CrmContactId", table: "CrmTickets");

            // Index geri al
            migrationBuilder.RenameIndex(name: "IX_CampaignContacts_CrmContactId", table: "CampaignContacts", newName: "IX_CampaignContacts_ContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmActivities_CrmContactId", table: "CrmActivities", newName: "IX_CrmActivities_ContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmContactTagLinks_CrmContactId", table: "CrmContactTagLinks", newName: "IX_CrmContactTagLinks_ContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmDeals_CrmContactId", table: "CrmDeals", newName: "IX_CrmDeals_ContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmSurveyResponses_CrmContactId", table: "CrmSurveyResponses", newName: "IX_CrmSurveyResponses_ContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmTasks_CrmContactId", table: "CrmTasks", newName: "IX_CrmTasks_ContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmTickets_CrmContactId", table: "CrmTickets", newName: "IX_CrmTickets_ContactId");

            // Kolon geri al
            migrationBuilder.RenameColumn(name: "CrmContactSyncDirectionId", table: "IntegrationConnections", newName: "ContactSyncDirectionId");
            migrationBuilder.RenameColumn(name: "CrmContactId", table: "CrmTickets", newName: "ContactId");
            migrationBuilder.RenameColumn(name: "CrmContactId", table: "CrmTasks", newName: "ContactId");
            migrationBuilder.RenameColumn(name: "CrmContactId", table: "CrmSurveyResponses", newName: "ContactId");
            migrationBuilder.RenameColumn(name: "CrmContactId", table: "CrmDeals", newName: "ContactId");
            migrationBuilder.RenameColumn(name: "CrmContactId", table: "CrmContactTagLinks", newName: "ContactId");
            migrationBuilder.RenameColumn(name: "CrmContactId", table: "CrmActivities", newName: "ContactId");
            migrationBuilder.RenameColumn(name: "CrmContactId", table: "CampaignContacts", newName: "ContactId");

            // Tablo geri al
            migrationBuilder.RenameTable(name: "CrmQualityScoreThresholds", newName: "QualityScoreThresholds");
            migrationBuilder.RenameTable(name: "CrmQualityAnswerSubCriteriaSelections", newName: "QualityAnswerSubCriteriaSelections");
            migrationBuilder.RenameTable(name: "CrmQualityAnswers", newName: "QualityAnswers");
            migrationBuilder.RenameTable(name: "CrmQualityEvaluations", newName: "QualityEvaluations");
            migrationBuilder.RenameTable(name: "CrmQualityQuestionSubCriteria", newName: "QualityQuestionSubCriteria");
            migrationBuilder.RenameTable(name: "CrmQualityQuestions", newName: "QualityQuestions");
            migrationBuilder.RenameTable(name: "CrmQualityChecklists", newName: "QualityChecklists");
            migrationBuilder.RenameTable(name: "CrmContacts", newName: "Contacts");

            // FK geri al
            migrationBuilder.AddForeignKey(name: "FK_CampaignContacts_Contacts_ContactId", table: "CampaignContacts", column: "ContactId", principalTable: "Contacts", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_CrmActivities_Contacts_ContactId", table: "CrmActivities", column: "ContactId", principalTable: "Contacts", principalColumn: "Id");
            migrationBuilder.AddForeignKey(name: "FK_CrmContactTagLinks_Contacts_ContactId", table: "CrmContactTagLinks", column: "ContactId", principalTable: "Contacts", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_CrmDeals_Contacts_ContactId", table: "CrmDeals", column: "ContactId", principalTable: "Contacts", principalColumn: "Id");
            migrationBuilder.AddForeignKey(name: "FK_CrmSurveyResponses_Contacts_ContactId", table: "CrmSurveyResponses", column: "ContactId", principalTable: "Contacts", principalColumn: "Id");
            migrationBuilder.AddForeignKey(name: "FK_CrmTasks_Contacts_ContactId", table: "CrmTasks", column: "ContactId", principalTable: "Contacts", principalColumn: "Id");
            migrationBuilder.AddForeignKey(name: "FK_CrmTickets_Contacts_ContactId", table: "CrmTickets", column: "ContactId", principalTable: "Contacts", principalColumn: "Id");
        }
    }
}
