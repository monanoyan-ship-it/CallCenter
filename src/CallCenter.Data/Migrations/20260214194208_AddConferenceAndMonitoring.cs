using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConferenceAndMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CallMonitoringSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    CallRecordId = table.Column<int>(type: "integer", nullable: false),
                    SupervisorId = table.Column<int>(type: "integer", nullable: false),
                    ModeId = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallMonitoringSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallMonitoringSessions_CallRecords_CallRecordId",
                        column: x => x.CallRecordId,
                        principalTable: "CallRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CallMonitoringSessions_Users_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConferenceRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false),
                    MediaServerRoomId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConferenceRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConferenceRooms_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ConferenceRooms_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConferenceParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConferenceRoomId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    ExternalNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    IsMuted = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConferenceParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConferenceParticipants_ConferenceRooms_ConferenceRoomId",
                        column: x => x.ConferenceRoomId,
                        principalTable: "ConferenceRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConferenceParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CallMonitoringSessions_CallRecordId",
                table: "CallMonitoringSessions",
                column: "CallRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CallMonitoringSessions_SupervisorId",
                table: "CallMonitoringSessions",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_CallMonitoringSessions_Uid",
                table: "CallMonitoringSessions",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConferenceParticipants_ConferenceRoomId",
                table: "ConferenceParticipants",
                column: "ConferenceRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ConferenceParticipants_UserId",
                table: "ConferenceParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConferenceRooms_CreatedByUserId",
                table: "ConferenceRooms",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConferenceRooms_CustomerId",
                table: "ConferenceRooms",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ConferenceRooms_StatusId",
                table: "ConferenceRooms",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ConferenceRooms_Uid",
                table: "ConferenceRooms",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallMonitoringSessions");

            migrationBuilder.DropTable(
                name: "ConferenceParticipants");

            migrationBuilder.DropTable(
                name: "ConferenceRooms");
        }
    }
}
