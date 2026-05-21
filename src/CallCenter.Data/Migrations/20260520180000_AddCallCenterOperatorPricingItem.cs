using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260520180000_AddCallCenterOperatorPricingItem")]
    public partial class AddCallCenterOperatorPricingItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "ServicePricingItems" (
                    "PeriodId",
                    "ProductTypeId",
                    "ServiceId",
                    "PackageGroupId",
                    "ServiceName",
                    "MonthlyPrice",
                    "PreviousPrice"
                )
                SELECT
                    p."Id",
                    1,
                    0,
                    NULL,
                    'Operator Lisansi',
                    700.00,
                    700.00
                FROM "ServicePricingPeriods" p
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "ServicePricingItems" i
                    WHERE i."PeriodId" = p."Id"
                      AND i."ProductTypeId" = 1
                      AND i."ServiceId" = 0
                      AND i."PackageGroupId" IS NULL
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "ServicePricingItems"
                WHERE "ProductTypeId" = 1
                  AND "ServiceId" = 0
                  AND "PackageGroupId" IS NULL
                  AND "ServiceName" = 'Operator Lisansi'
                  AND "MonthlyPrice" = 700.00
                  AND "PreviousPrice" = 700.00;
                """);
        }
    }
}
