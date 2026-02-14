using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstantMessagingAndMonitoringMediaSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaServerSessionId",
                table: "CallMonitoringSessions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InstantMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uid = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<int>(type: "integer", nullable: false),
                    ReceiverUserId = table.Column<int>(type: "integer", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    MessageTypeId = table.Column<int>(type: "integer", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstantMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstantMessages_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InstantMessages_Users_ReceiverUserId",
                        column: x => x.ReceiverUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstantMessages_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstantMessages_CustomerId",
                table: "InstantMessages",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_InstantMessages_ReceiverUserId_IsRead",
                table: "InstantMessages",
                columns: new[] { "ReceiverUserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_InstantMessages_SenderUserId_ReceiverUserId_SentAt",
                table: "InstantMessages",
                columns: new[] { "SenderUserId", "ReceiverUserId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InstantMessages_Uid",
                table: "InstantMessages",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstantMessages");

            migrationBuilder.DropColumn(
                name: "MediaServerSessionId",
                table: "CallMonitoringSessions");
        }
    }
}
