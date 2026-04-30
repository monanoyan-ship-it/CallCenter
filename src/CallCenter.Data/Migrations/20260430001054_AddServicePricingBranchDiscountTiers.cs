using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePricingBranchDiscountTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServicePricingBranchDiscountTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PeriodId = table.Column<int>(type: "integer", nullable: false),
                    MinBranches = table.Column<int>(type: "integer", nullable: false),
                    MaxBranches = table.Column<int>(type: "integer", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePricingBranchDiscountTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePricingBranchDiscountTiers_ServicePricingPeriods_Per~",
                        column: x => x.PeriodId,
                        principalTable: "ServicePricingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePricingBranchDiscountTiers_PeriodId_SortOrder",
                table: "ServicePricingBranchDiscountTiers",
                columns: new[] { "PeriodId", "SortOrder" });

            migrationBuilder.Sql("""
                INSERT INTO "ServicePricingBranchDiscountTiers" ("PeriodId", "MinBranches", "MaxBranches", "DiscountPercent", "SortOrder")
                SELECT p."Id", 2, 10, 10, 1 FROM "ServicePricingPeriods" p;
                INSERT INTO "ServicePricingBranchDiscountTiers" ("PeriodId", "MinBranches", "MaxBranches", "DiscountPercent", "SortOrder")
                SELECT p."Id", 11, 20, 15, 2 FROM "ServicePricingPeriods" p;
                INSERT INTO "ServicePricingBranchDiscountTiers" ("PeriodId", "MinBranches", "MaxBranches", "DiscountPercent", "SortOrder")
                SELECT p."Id", 21, 999, 20, 3 FROM "ServicePricingPeriods" p;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServicePricingBranchDiscountTiers");
        }
    }
}
