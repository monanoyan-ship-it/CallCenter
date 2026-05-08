using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIyzicoSubMerchantFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SlnSalonProfiles"
                    ADD COLUMN IF NOT EXISTS "IyzicoSubMerchantKey" text,
                    ADD COLUMN IF NOT EXISTS "IyzicoSubMerchantType" text,
                    ADD COLUMN IF NOT EXISTS "IyzicoIban" text,
                    ADD COLUMN IF NOT EXISTS "IyzicoLegalCompanyTitle" text,
                    ADD COLUMN IF NOT EXISTS "IyzicoTaxOffice" text,
                    ADD COLUMN IF NOT EXISTS "IyzicoTaxNumber" text,
                    ADD COLUMN IF NOT EXISTS "IyzicoIdentityNumber" text,
                    ADD COLUMN IF NOT EXISTS "IyzicoContactName" text,
                    ADD COLUMN IF NOT EXISTS "IyzicoContactSurname" text,
                    ADD COLUMN IF NOT EXISTS "IyzicoOnboardingStatus" integer NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS "IyzicoOnboardedAt" timestamp with time zone,
                    ADD COLUMN IF NOT EXISTS "IyzicoOnboardingError" text;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Customers"
                    ADD COLUMN IF NOT EXISTS "MarketplaceCommissionPercent" numeric NOT NULL DEFAULT 5,
                    ADD COLUMN IF NOT EXISTS "MarketplaceWithholdingPercent" numeric NOT NULL DEFAULT 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SlnSalonProfiles"
                    DROP COLUMN IF EXISTS "IyzicoSubMerchantKey",
                    DROP COLUMN IF EXISTS "IyzicoSubMerchantType",
                    DROP COLUMN IF EXISTS "IyzicoIban",
                    DROP COLUMN IF EXISTS "IyzicoLegalCompanyTitle",
                    DROP COLUMN IF EXISTS "IyzicoTaxOffice",
                    DROP COLUMN IF EXISTS "IyzicoTaxNumber",
                    DROP COLUMN IF EXISTS "IyzicoIdentityNumber",
                    DROP COLUMN IF EXISTS "IyzicoContactName",
                    DROP COLUMN IF EXISTS "IyzicoContactSurname",
                    DROP COLUMN IF EXISTS "IyzicoOnboardingStatus",
                    DROP COLUMN IF EXISTS "IyzicoOnboardedAt",
                    DROP COLUMN IF EXISTS "IyzicoOnboardingError";
                """);
            migrationBuilder.Sql("""
                ALTER TABLE "Customers"
                    DROP COLUMN IF EXISTS "MarketplaceCommissionPercent",
                    DROP COLUMN IF EXISTS "MarketplaceWithholdingPercent";
                """);
        }
    }
}
