using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonTreatmentHealthRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Allergies",
                table: "SlnClients",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Contraindications",
                table: "SlnClients",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HealthInfoRequiresReview",
                table: "SlnClients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "HealthInfoReviewedAt",
                table: "SlnClients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HealthInfoReviewedByPersonnelId",
                table: "SlnClients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HealthInfoUpdatedAt",
                table: "SlnClients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalNotes",
                table: "SlnClients",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkinSensitivity",
                table: "SlnClients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SlnTreatmentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    SlnAppointmentId = table.Column<int>(type: "integer", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    PersonnelId = table.Column<int>(type: "integer", nullable: true),
                    TreatmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SkinTypeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AllergiesSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ContraindicationsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SessionNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DeviceParameters = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProductNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AftercareNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByPersonnelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnTreatmentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnTreatmentRecords_CustomerPersonnel_CreatedByPersonnelId",
                        column: x => x.CreatedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnTreatmentRecords_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnTreatmentRecords_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnTreatmentRecords_SlnAppointments_SlnAppointmentId",
                        column: x => x.SlnAppointmentId,
                        principalTable: "SlnAppointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SlnTreatmentRecords_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnTreatmentRecords_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnClients_HealthInfoReviewedByPersonnelId",
                table: "SlnClients",
                column: "HealthInfoReviewedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnTreatmentRecords_CreatedByPersonnelId",
                table: "SlnTreatmentRecords",
                column: "CreatedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnTreatmentRecords_CustomerId_SlnClientId_TreatmentDate",
                table: "SlnTreatmentRecords",
                columns: new[] { "CustomerId", "SlnClientId", "TreatmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnTreatmentRecords_PersonnelId",
                table: "SlnTreatmentRecords",
                column: "PersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnTreatmentRecords_ServiceId",
                table: "SlnTreatmentRecords",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnTreatmentRecords_SlnAppointmentId",
                table: "SlnTreatmentRecords",
                column: "SlnAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnTreatmentRecords_SlnClientId",
                table: "SlnTreatmentRecords",
                column: "SlnClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_SlnClients_CustomerPersonnel_HealthInfoReviewedByPersonnelId",
                table: "SlnClients",
                column: "HealthInfoReviewedByPersonnelId",
                principalTable: "CustomerPersonnel",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnClients_CustomerPersonnel_HealthInfoReviewedByPersonnelId",
                table: "SlnClients");

            migrationBuilder.DropTable(
                name: "SlnTreatmentRecords");

            migrationBuilder.DropIndex(
                name: "IX_SlnClients_HealthInfoReviewedByPersonnelId",
                table: "SlnClients");

            migrationBuilder.DropColumn(
                name: "Allergies",
                table: "SlnClients");

            migrationBuilder.DropColumn(
                name: "Contraindications",
                table: "SlnClients");

            migrationBuilder.DropColumn(
                name: "HealthInfoRequiresReview",
                table: "SlnClients");

            migrationBuilder.DropColumn(
                name: "HealthInfoReviewedAt",
                table: "SlnClients");

            migrationBuilder.DropColumn(
                name: "HealthInfoReviewedByPersonnelId",
                table: "SlnClients");

            migrationBuilder.DropColumn(
                name: "HealthInfoUpdatedAt",
                table: "SlnClients");

            migrationBuilder.DropColumn(
                name: "MedicalNotes",
                table: "SlnClients");

            migrationBuilder.DropColumn(
                name: "SkinSensitivity",
                table: "SlnClients");
        }
    }
}
