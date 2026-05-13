using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonPersonnelOps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnPersonnelLeaves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnPersonnelLeaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnPersonnelLeaves_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnPersonnelLeaves_CustomerPersonnel_ReviewedByPersonnelId",
                        column: x => x.ReviewedByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SlnPersonnelShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    ShiftDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnPersonnelShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnPersonnelShifts_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnPersonnelTimesheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonnelId = table.Column<int>(type: "integer", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClockInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClockOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnPersonnelTimesheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnPersonnelTimesheets_CustomerPersonnel_PersonnelId",
                        column: x => x.PersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelLeaves_PersonnelId_StartDate_EndDate",
                table: "SlnPersonnelLeaves",
                columns: new[] { "PersonnelId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelLeaves_ReviewedByPersonnelId",
                table: "SlnPersonnelLeaves",
                column: "ReviewedByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelShifts_PersonnelId_ShiftDate",
                table: "SlnPersonnelShifts",
                columns: new[] { "PersonnelId", "ShiftDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnPersonnelTimesheets_PersonnelId_WorkDate",
                table: "SlnPersonnelTimesheets",
                columns: new[] { "PersonnelId", "WorkDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnPersonnelLeaves");

            migrationBuilder.DropTable(
                name: "SlnPersonnelShifts");

            migrationBuilder.DropTable(
                name: "SlnPersonnelTimesheets");
        }
    }
}
