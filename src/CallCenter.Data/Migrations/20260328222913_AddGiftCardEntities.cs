using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftCardEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnGiftCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    RecipientName = table.Column<string>(type: "text", nullable: true),
                    RecipientPhone = table.Column<string>(type: "text", nullable: true),
                    SenderName = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SoldByPersonnelId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnGiftCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnGiftCards_CustomerPersonnel_SoldByPersonnelId",
                        column: x => x.SoldByPersonnelId,
                        principalTable: "CustomerPersonnel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlnGiftCards_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlnGiftCardTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GiftCardId = table.Column<int>(type: "integer", nullable: false),
                    TransactionTypeId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RelatedInvoiceId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnGiftCardTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnGiftCardTransactions_SlnGiftCards_GiftCardId",
                        column: x => x.GiftCardId,
                        principalTable: "SlnGiftCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnGiftCardTransactions_SlnInvoices_RelatedInvoiceId",
                        column: x => x.RelatedInvoiceId,
                        principalTable: "SlnInvoices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnGiftCards_CustomerId",
                table: "SlnGiftCards",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnGiftCards_SoldByPersonnelId",
                table: "SlnGiftCards",
                column: "SoldByPersonnelId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnGiftCardTransactions_GiftCardId",
                table: "SlnGiftCardTransactions",
                column: "GiftCardId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnGiftCardTransactions_RelatedInvoiceId",
                table: "SlnGiftCardTransactions",
                column: "RelatedInvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnGiftCardTransactions");

            migrationBuilder.DropTable(
                name: "SlnGiftCards");
        }
    }
}
