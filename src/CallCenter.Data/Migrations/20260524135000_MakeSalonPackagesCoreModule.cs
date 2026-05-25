using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260524135000_MakeSalonPackagesCoreModule")]
    public partial class MakeSalonPackagesCoreModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "CustomerPortalModules" (
                    "CustomerId",
                    "ModuleId",
                    "IsActive",
                    "ActivatedAt",
                    "DeactivatedAt",
                    "Notes",
                    "MonthlyPrice",
                    "TrialEndsAt"
                )
                SELECT
                    cp."CustomerId",
                    217,
                    TRUE,
                    NOW(),
                    NULL,
                    'Seans Paketleri temel pakete alindi',
                    NULL,
                    NULL
                FROM "CustomerProducts" cp
                WHERE cp."ProductTypeId" = 2
                  AND cp."IsActive" = TRUE
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "CustomerPortalModules" cpm
                      WHERE cpm."CustomerId" = cp."CustomerId"
                        AND cpm."ModuleId" = 217
                  );

                UPDATE "CustomerPortalModules"
                SET "IsActive" = TRUE,
                    "DeactivatedAt" = NULL,
                    "ActivatedAt" = CASE
                        WHEN "ActivatedAt" IS NULL THEN NOW()
                        ELSE "ActivatedAt"
                    END,
                    "Notes" = CASE
                        WHEN "Notes" IS NULL OR "Notes" = '' THEN 'Seans Paketleri temel pakete alindi'
                        ELSE "Notes"
                    END
                WHERE "ModuleId" = 217;

                WITH removable AS (
                    SELECT
                        line."Id" AS "LineId",
                        line."CustomerBillingPeriodId",
                        line."LineAmount"
                    FROM "CustomerBillingPeriodModuleLines" line
                    INNER JOIN "CustomerBillingPeriods" period
                        ON period."Id" = line."CustomerBillingPeriodId"
                    WHERE line."PackageGroupId" = 3
                      AND period."BillingKindId" = 2
                      AND period."IsPaid" = FALSE
                      AND period."StatusId" IN (1, 2, 4)
                      AND NOT EXISTS (
                          SELECT 1
                          FROM "CustomerPortalModules" active_marketing
                          WHERE active_marketing."CustomerId" = period."CustomerId"
                            AND active_marketing."IsActive" = TRUE
                            AND active_marketing."ModuleId" IN (216, 218, 219, 212, 222, 227, 223)
                      )
                ),
                deleted AS (
                    DELETE FROM "CustomerBillingPeriodModuleLines" line
                    USING removable
                    WHERE line."Id" = removable."LineId"
                    RETURNING line."CustomerBillingPeriodId", line."LineAmount"
                ),
                totals AS (
                    SELECT "CustomerBillingPeriodId", SUM("LineAmount") AS "RemovedAmount"
                    FROM deleted
                    GROUP BY "CustomerBillingPeriodId"
                )
                UPDATE "CustomerBillingPeriods" period
                SET "ServiceAmount" = GREATEST(period."ServiceAmount" - totals."RemovedAmount", 0),
                    "Amount" = GREATEST(period."Amount" - totals."RemovedAmount", 0),
                    "StatusId" = CASE
                        WHEN GREATEST(period."Amount" - totals."RemovedAmount", 0) = 0 THEN 3
                        ELSE period."StatusId"
                    END,
                    "IsPaid" = CASE
                        WHEN GREATEST(period."Amount" - totals."RemovedAmount", 0) = 0 THEN TRUE
                        ELSE period."IsPaid"
                    END,
                    "PaidAt" = CASE
                        WHEN GREATEST(period."Amount" - totals."RemovedAmount", 0) = 0 THEN COALESCE(period."PaidAt", NOW())
                        ELSE period."PaidAt"
                    END,
                    "Notes" = CASE
                        WHEN period."Notes" IS NULL OR period."Notes" = '' THEN 'Seans Paketleri temel pakete alindigi icin pazarlama paket satiri duzeltildi'
                        ELSE period."Notes" || ' | Seans Paketleri temel pakete alindigi icin pazarlama paket satiri duzeltildi'
                    END
                FROM totals
                WHERE period."Id" = totals."CustomerBillingPeriodId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only entitlement migration. We do not disable existing package access on rollback.
        }
    }
}
