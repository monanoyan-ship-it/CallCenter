using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonServiceSessionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionIndex",
                table: "SlnTreatmentRecords");

            migrationBuilder.DropColumn(
                name: "TotalSessions",
                table: "SlnTreatmentRecords");

            migrationBuilder.AddColumn<int>(
                name: "ServiceSessionPlanId",
                table: "SlnTreatmentRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SlnServiceSessionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    SourceInvoiceId = table.Column<int>(type: "integer", nullable: true),
                    SourceInvoiceItemId = table.Column<int>(type: "integer", nullable: true),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    UsedSessions = table.Column<int>(type: "integer", nullable: false),
                    RemainingSessions = table.Column<int>(type: "integer", nullable: false),
                    SaleAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SoldByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    SoldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnServiceSessionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionPlans_CustomerPersonnel_SoldByPersonnelId",
                        column: x => x.SoldByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionPlans_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionPlans_SlnBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "SlnBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionPlans_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionPlans_SlnInvoiceItems_SourceInvoiceItemId",
                        column: x => x.SourceInvoiceItemId,
                        principalTable: "SlnInvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionPlans_SlnInvoices_SourceInvoiceId",
                        column: x => x.SourceInvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionPlans_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlnServiceSessionRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    SessionNumber = table.Column<int>(type: "integer", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PersonnelId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceItemId = table.Column<int>(type: "integer", nullable: true),
                    SlnAppointmentId = table.Column<int>(type: "integer", nullable: true),
                    TreatmentRecordId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnServiceSessionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionRecords_CustomerPersonnel_CreatedByPersonn~",
                        column: x => x.CreatedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionRecords_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionRecords_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionRecords_SlnAppointments_SlnAppointmentId",
                        column: x => x.SlnAppointmentId,
                        principalTable: "SlnAppointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionRecords_SlnBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "SlnBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionRecords_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionRecords_SlnInvoiceItems_InvoiceItemId",
                        column: x => x.InvoiceItemId,
                        principalTable: "SlnInvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionRecords_SlnInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionRecords_SlnServiceSessionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SlnServiceSessionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionRecords_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SlnServiceSessionRecords_SlnTreatmentRecords_TreatmentRecor~",
                        column: x => x.TreatmentRecordId,
                        principalTable: "SlnTreatmentRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnTreatmentRecords_ServiceSessionPlanId",
                table: "SlnTreatmentRecords",
                column: "ServiceSessionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionPlans_BranchId",
                table: "SlnServiceSessionPlans",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionPlans_CustomerId_BranchId",
                table: "SlnServiceSessionPlans",
                columns: new[] { "CustomerId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionPlans_CustomerId_SlnClientId_ServiceId",
                table: "SlnServiceSessionPlans",
                columns: new[] { "CustomerId", "SlnClientId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionPlans_ServiceId",
                table: "SlnServiceSessionPlans",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionPlans_SlnClientId",
                table: "SlnServiceSessionPlans",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionPlans_SoldByPersonnelId",
                table: "SlnServiceSessionPlans",
                column: "SoldByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionPlans_SourceInvoiceId",
                table: "SlnServiceSessionPlans",
                column: "SourceInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionPlans_SourceInvoiceItemId",
                table: "SlnServiceSessionPlans",
                column: "SourceInvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionRecords_BranchId",
                table: "SlnServiceSessionRecords",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionRecords_CreatedByPersonnelId",
                table: "SlnServiceSessionRecords",
                column: "CreatedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionRecords_CustomerId_SlnClientId_ServiceId_P~",
                table: "SlnServiceSessionRecords",
                columns: new[] { "CustomerId", "SlnClientId", "ServiceId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionRecords_InvoiceId",
                table: "SlnServiceSessionRecords",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionRecords_InvoiceItemId",
                table: "SlnServiceSessionRecords",
                column: "InvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionRecords_PersonnelId",
                table: "SlnServiceSessionRecords",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionRecords_PlanId_SessionNumber",
                table: "SlnServiceSessionRecords",
                columns: new[] { "PlanId", "SessionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionRecords_ServiceId",
                table: "SlnServiceSessionRecords",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionRecords_SlnAppointmentId",
                table: "SlnServiceSessionRecords",
                column: "SlnAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionRecords_SlnClientId",
                table: "SlnServiceSessionRecords",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnServiceSessionRecords_TreatmentRecordId",
                table: "SlnServiceSessionRecords",
                column: "TreatmentRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnTreatmentRecords_SlnServiceSessionPlans_ServiceSessionPl~",
                table: "SlnTreatmentRecords",
                column: "ServiceSessionPlanId",
                principalTable: "SlnServiceSessionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnTreatmentRecords_SlnServiceSessionPlans_ServiceSessionPl~",
                table: "SlnTreatmentRecords");

            migrationBuilder.DropTable(
                name: "SlnServiceSessionRecords");

            migrationBuilder.DropTable(
                name: "SlnServiceSessionPlans");

            migrationBuilder.DropIndex(
                name: "IX_SlnTreatmentRecords_ServiceSessionPlanId",
                table: "SlnTreatmentRecords");

            migrationBuilder.DropColumn(
                name: "ServiceSessionPlanId",
                table: "SlnTreatmentRecords");

            migrationBuilder.AddColumn<int>(
                name: "TotalSessions",
                table: "SlnTreatmentRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionIndex",
                table: "SlnTreatmentRecords",
                type: "integer",
                nullable: true);
        }
    }
}
