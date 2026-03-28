using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnClientLoyalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    SlnClientId = table.Column<int>(type: "integer", nullable: false),
                    TotalEarned = table.Column<int>(type: "integer", nullable: false),
                    TotalSpent = table.Column<int>(type: "integer", nullable: false),
                    CurrentBalance = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnClientLoyalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnClientLoyalties_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnClientLoyalties_SlnClients_SlnClientId",
                        column: x => x.SlnClientId,
                        principalTable: "SlnClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnLoyaltyConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    PointsPerTL = table.Column<decimal>(type: "numeric", nullable: false),
                    PointValue = table.Column<decimal>(type: "numeric", nullable: false),
                    MinRedeemPoints = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnLoyaltyConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyConfigs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnLoyaltyTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientLoyaltyId = table.Column<int>(type: "integer", nullable: false),
                    TransactionTypeId = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RelatedInvoiceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnLoyaltyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyTransactions_SlnClientLoyalties_ClientLoyaltyId",
                        column: x => x.ClientLoyaltyId,
                        principalTable: "SlnClientLoyalties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnLoyaltyTransactions_SlnInvoices_RelatedInvoiceId",
                        column: x => x.RelatedInvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientLoyalties_CustomerId",
                table: "SlnClientLoyalties",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnClientLoyalties_SlnClientId",
                table: "SlnClientLoyalties",
                column: "SlnClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyConfigs_CustomerId",
                table: "SlnLoyaltyConfigs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyTransactions_ClientLoyaltyId",
                table: "SlnLoyaltyTransactions",
                column: "ClientLoyaltyId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnLoyaltyTransactions_RelatedInvoiceId",
                table: "SlnLoyaltyTransactions",
                column: "RelatedInvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnLoyaltyConfigs");

            migrationBuilder.DropTable(
                name: "SlnLoyaltyTransactions");

            migrationBuilder.DropTable(
                name: "SlnClientLoyalties");
        }
    }
}
