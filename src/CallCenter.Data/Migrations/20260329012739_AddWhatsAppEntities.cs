using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnWhatsAppConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    BusinessAccountId = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberId = table.Column<string>(type: "text", nullable: true),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    WebhookVerifyToken = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SendAppointmentReminder = table.Column<bool>(type: "boolean", nullable: false),
                    SendAppointmentConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    SendBirthdayGreeting = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderHoursBefore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnWhatsAppConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnWhatsAppConfigs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnWhatsAppMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    DirectionId = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    TemplateName = table.Column<string>(type: "text", nullable: true),
                    MessageBody = table.Column<string>(type: "text", nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    WhatsAppMessageId = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    SlnClientId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnWhatsAppMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnWhatsAppMessages_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnWhatsAppMessages_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnWhatsAppConfigs_CustomerId",
                table: "SlnWhatsAppConfigs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnWhatsAppMessages_CustomerId",
                table: "SlnWhatsAppMessages",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnWhatsAppMessages_SlnClientId",
                table: "SlnWhatsAppMessages",
                column: "SlnClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnWhatsAppConfigs");

            migrationBuilder.DropTable(
                name: "SlnWhatsAppMessages");
        }
    }
}
