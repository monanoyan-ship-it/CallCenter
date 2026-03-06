using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCallCampaignEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CallCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallCampaigns_CustomerPersonnel_CreatedByPersonnelId",
                        column: x => x.CreatedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CallCampaigns_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    ContactId = table.Column<int>(type: "integer", nullable: false),
                    AssignedPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultNotes = table.Column<string>(type: "text", nullable: true),
                    CallRecordId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignContacts_CallCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "CallCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignContacts_CallRecords_CallRecordId",
                        column: x => x.CallRecordId,
                        principalTable: "CallRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CampaignContacts_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignContacts_CustomerPersonnel_AssignedPersonnelId",
                        column: x => x.AssignedPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CallCampaigns_CreatedByPersonnelId",
                table: "CallCampaigns",
                column: "CreatedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CallCampaigns_CustomerId_StatusId",
                table: "CallCampaigns",
                columns: new[] { "CustomerId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_CallCampaigns_Uid",
                table: "CallCampaigns",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContacts_AssignedPersonnelId_StatusId",
                table: "CampaignContacts",
                columns: new[] { "AssignedPersonnelId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContacts_CallRecordId",
                table: "CampaignContacts",
                column: "CallRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContacts_CampaignId_ContactId",
                table: "CampaignContacts",
                columns: new[] { "CampaignId", "ContactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignContacts_ContactId",
                table: "CampaignContacts",
                column: "ContactId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignContacts");

            migrationBuilder.DropTable(
                name: "CallCampaigns");
        }
    }
}
