using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordingAccessLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecordingAccessLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CallRecordId = table.Column<int>(type: "integer", nullable: false),
                    AccessedByUserId = table.Column<int>(type: "integer", nullable: true),
                    AccessedByUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    ActionTypeId = table.Column<int>(type: "integer", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordingAccessLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecordingAccessLogs_CallRecords_CallRecordId",
                        column: x => x.CallRecordId,
                        principalTable: "CallRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecordingAccessLogs_AccessedAt",
                table: "RecordingAccessLogs",
                column: "AccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RecordingAccessLogs_AccessedByUserId",
                table: "RecordingAccessLogs",
                column: "AccessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordingAccessLogs_CallRecordId",
                table: "RecordingAccessLogs",
                column: "CallRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordingAccessLogs_CustomerId",
                table: "RecordingAccessLogs",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecordingAccessLogs");
        }
    }
}
