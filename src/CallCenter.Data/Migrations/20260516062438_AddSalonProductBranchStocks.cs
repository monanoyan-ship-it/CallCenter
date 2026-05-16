using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalonProductBranchStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlnProductBranchStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    StockQuantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlnProductBranchStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlnProductBranchStocks_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnProductBranchStocks_SlnBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "SlnBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlnProductBranchStocks_SlnProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SlnProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlnProductBranchStocks_BranchId",
                table: "SlnProductBranchStocks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnProductBranchStocks_CustomerId_BranchId_ProductId",
                table: "SlnProductBranchStocks",
                columns: new[] { "CustomerId", "BranchId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlnProductBranchStocks_ProductId",
                table: "SlnProductBranchStocks",
                column: "ProductId");

            migrationBuilder.Sql("""
                INSERT INTO "SlnProductBranchStocks" ("CustomerId", "ProductId", "BranchId", "StockQuantity", "CreatedAt", "UpdatedAt")
                SELECT p."CustomerId", p."Id", b."Id", p."StockQuantity", NOW(), NOW()
                FROM "SlnProducts" p
                JOIN LATERAL (
                    SELECT sb."Id"
                    FROM "SlnBranches" sb
                    WHERE sb."CustomerId" = p."CustomerId"
                      AND sb."IsActive" = TRUE
                    ORDER BY sb."IsHeadquarter" DESC, sb."Id"
                    LIMIT 1
                ) b ON TRUE
                WHERE p."StockQuantity" <> 0
                ON CONFLICT ("CustomerId", "BranchId", "ProductId")
                DO UPDATE SET
                    "StockQuantity" = EXCLUDED."StockQuantity",
                    "UpdatedAt" = NOW();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlnProductBranchStocks");
        }
    }
}
