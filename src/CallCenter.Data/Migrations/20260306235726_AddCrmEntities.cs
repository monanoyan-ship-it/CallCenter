using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrmDeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StageId = table.Column<int>(type: "integer", nullable: false),
                    Probability = table.Column<int>(type: "integer", nullable: false),
                    ExpectedCloseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualCloseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ContactId = table.Column<int>(type: "integer", nullable: true),
                    OwnerPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmDeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmDeals_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmDeals_CustomerPersonnel_CreatedByPersonnelId",
                        column: x => x.CreatedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmDeals_CustomerPersonnel_OwnerPersonnelId",
                        column: x => x.OwnerPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmDeals_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    PriorityId = table.Column<int>(type: "integer", nullable: false),
                    ContactId = table.Column<int>(type: "integer", nullable: true),
                    AssignedToPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmTickets_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmTickets_CustomerPersonnel_AssignedToPersonnelId",
                        column: x => x.AssignedToPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmTickets_CustomerPersonnel_CreatedByPersonnelId",
                        column: x => x.CreatedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmTickets_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TypeId = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Detail = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ContactId = table.Column<int>(type: "integer", nullable: true),
                    TicketId = table.Column<int>(type: "integer", nullable: true),
                    DealId = table.Column<int>(type: "integer", nullable: true),
                    CallRecordId = table.Column<int>(type: "integer", nullable: true),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmActivities_CallRecords_CallRecordId",
                        column: x => x.CallRecordId,
                        principalTable: "CallRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmActivities_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmActivities_CrmDeals_DealId",
                        column: x => x.DealId,
                        principalTable: "CrmDeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmActivities_CrmTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "CrmTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmActivities_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmActivities_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContactId = table.Column<int>(type: "integer", nullable: true),
                    TicketId = table.Column<int>(type: "integer", nullable: true),
                    DealId = table.Column<int>(type: "integer", nullable: true),
                    AssignedToPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmTasks_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmTasks_CrmDeals_DealId",
                        column: x => x.DealId,
                        principalTable: "CrmDeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmTasks_CrmTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "CrmTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmTasks_CustomerPersonnel_AssignedToPersonnelId",
                        column: x => x.AssignedToPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmTasks_CustomerPersonnel_CreatedByPersonnelId",
                        column: x => x.CreatedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmTasks_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_CallRecordId",
                table: "CrmActivities",
                column: "CallRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_ContactId",
                table: "CrmActivities",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_CustomerId_CreatedAt",
                table: "CrmActivities",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_DealId",
                table: "CrmActivities",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_PersonnelId",
                table: "CrmActivities",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_TicketId",
                table: "CrmActivities",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmDeals_ContactId",
                table: "CrmDeals",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmDeals_CreatedByPersonnelId",
                table: "CrmDeals",
                column: "CreatedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmDeals_CustomerId_StageId",
                table: "CrmDeals",
                columns: new[] { "CustomerId", "StageId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmDeals_OwnerPersonnelId",
                table: "CrmDeals",
                column: "OwnerPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmDeals_Uid",
                table: "CrmDeals",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_AssignedToPersonnelId_DueDate",
                table: "CrmTasks",
                columns: new[] { "AssignedToPersonnelId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_ContactId",
                table: "CrmTasks",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_CreatedByPersonnelId",
                table: "CrmTasks",
                column: "CreatedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_CustomerId_StatusId",
                table: "CrmTasks",
                columns: new[] { "CustomerId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_DealId",
                table: "CrmTasks",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_TicketId",
                table: "CrmTasks",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTickets_AssignedToPersonnelId",
                table: "CrmTickets",
                column: "AssignedToPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTickets_ContactId",
                table: "CrmTickets",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTickets_CreatedByPersonnelId",
                table: "CrmTickets",
                column: "CreatedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTickets_CustomerId_StatusId",
                table: "CrmTickets",
                columns: new[] { "CustomerId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmTickets_Uid",
                table: "CrmTickets",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrmActivities");

            migrationBuilder.DropTable(
                name: "CrmTasks");

            migrationBuilder.DropTable(
                name: "CrmDeals");

            migrationBuilder.DropTable(
                name: "CrmTickets");
        }
    }
}
