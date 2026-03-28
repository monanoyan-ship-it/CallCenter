using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNoShowPolicyAndAppointmentDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBlacklisted",
                table: "SlnClients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NoShowCount",
                table: "SlnClients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "SlnAppointments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "DepositRefunded",
                table: "SlnAppointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyAmount",
                table: "SlnAppointments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "SlnNoShowPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    RequireDeposit = table.Column<bool>(type: "boolean", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    FreeCancellationHours = table.Column<int>(type: "integer", nullable: false),
                    LateCancellationFee = table.Column<decimal>(type: "numeric", nullable: false),
                    NoShowFee = table.Column<decimal>(type: "numeric", nullable: false),
                    BlacklistThreshold = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnNoShowPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnNoShowPolicies_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnNoShowPolicies_CustomerId",
                table: "SlnNoShowPolicies",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnNoShowPolicies");

            migrationBuilder.DropColumn(
                name: "IsBlacklisted",
                table: "SlnClients");

            migrationBuilder.DropColumn(
                name: "NoShowCount",
                table: "SlnClients");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "SlnAppointments");

            migrationBuilder.DropColumn(
                name: "DepositRefunded",
                table: "SlnAppointments");

            migrationBuilder.DropColumn(
                name: "PenaltyAmount",
                table: "SlnAppointments");
        }
    }
}
