using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnWaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    PreferredPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    PreferredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreferredTimeSlot = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnWaitlistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnWaitlistEntries_CustomerPersonnel_PreferredPersonnelId",
                        column: x => x.PreferredPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnWaitlistEntries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnWaitlistEntries_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnWaitlistEntries_SlnServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "SlnServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnWaitlistEntries_CustomerId",
                table: "SlnWaitlistEntries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnWaitlistEntries_PreferredPersonnelId",
                table: "SlnWaitlistEntries",
                column: "PreferredPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnWaitlistEntries_ServiceId",
                table: "SlnWaitlistEntries",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnWaitlistEntries_SlnClientId",
                table: "SlnWaitlistEntries",
                column: "SlnClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnWaitlistEntries");
        }
    }
}
