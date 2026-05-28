using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260527153000_AddSlnGiftCardBranchScope")]
    public partial class AddSlnGiftCardBranchScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "SlnGiftCards",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "SlnGiftCards" g
                SET "BranchId" = i."BranchId"
                FROM "SlnInvoices" i
                WHERE g."BranchId" IS NULL
                  AND i."CustomerId" = g."CustomerId"
                  AND i."BranchId" IS NOT NULL
                  AND i."Notes" ~ ('(^|\|)GiftCardSale:' || g."Id"::text || '(\||$)');

                UPDATE "SlnGiftCards" g
                SET "BranchId" = cp."BranchId"
                FROM "CustomerPersonnel" cp
                WHERE g."BranchId" IS NULL
                  AND g."SoldByPersonnelId" = cp."Id"
                  AND cp."BranchId" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "TranslationKeys" ("Key", "Description", "Module", "PlatformId")
                SELECT 'salon.modules.package.salon_crm.usage_note', 'Salon CRM package usage note', 'modules', 2
                WHERE NOT EXISTS (
                    SELECT 1 FROM "TranslationKeys" WHERE "Key" = 'salon.modules.package.salon_crm.usage_note'
                );

                WITH desired_translations("Key", "LanguageCode", "Value") AS (
                    VALUES
                        ('salon.modules.package.salon_crm.usage_note', 'tr', 'CRM hizmeti firma hesabında açılır; şube kullanıcıları kendi şubesindeki müşterileri, hediye kartlarını ve takip işlerini görür. SMS, WhatsApp ve yoğun e-posta kullanımları ayrıca izlenebilir.'),
                        ('salon.modules.package.salon_crm.usage_note', 'en', 'The CRM service is enabled for the company account; branch users see clients, gift cards, and follow-up work in their own branch. SMS, WhatsApp, and high-volume email usage can still be tracked separately.')
                ),
                expanded_translations AS (
                    SELECT "Key", "LanguageCode", "Value" FROM desired_translations
                    UNION ALL
                    SELECT "Key", 'de', "Value" FROM desired_translations WHERE "LanguageCode" = 'en'
                    UNION ALL
                    SELECT "Key", 'ar', "Value" FROM desired_translations WHERE "LanguageCode" = 'en'
                    UNION ALL
                    SELECT "Key", 'ru', "Value" FROM desired_translations WHERE "LanguageCode" = 'en'
                ),
                resolved AS (
                    SELECT tk."Id" AS "TranslationKeyId", et."LanguageCode", et."Value"
                    FROM expanded_translations et
                    INNER JOIN "TranslationKeys" tk ON tk."Key" = et."Key"
                )
                INSERT INTO "Translations" ("TranslationKeyId", "LanguageCode", "Value", "UpdatedAt", "UpdatedBy")
                SELECT r."TranslationKeyId", r."LanguageCode", r."Value", NOW(), '20260527153000_AddSlnGiftCardBranchScope'
                FROM resolved r
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Translations" t
                    WHERE t."TranslationKeyId" = r."TranslationKeyId"
                      AND t."LanguageCode" = r."LanguageCode"
                );

                WITH desired_translations("Key", "LanguageCode", "Value") AS (
                    VALUES
                        ('salon.modules.package.salon_crm.usage_note', 'tr', 'CRM hizmeti firma hesabında açılır; şube kullanıcıları kendi şubesindeki müşterileri, hediye kartlarını ve takip işlerini görür. SMS, WhatsApp ve yoğun e-posta kullanımları ayrıca izlenebilir.'),
                        ('salon.modules.package.salon_crm.usage_note', 'en', 'The CRM service is enabled for the company account; branch users see clients, gift cards, and follow-up work in their own branch. SMS, WhatsApp, and high-volume email usage can still be tracked separately.')
                ),
                expanded_translations AS (
                    SELECT "Key", "LanguageCode", "Value" FROM desired_translations
                    UNION ALL
                    SELECT "Key", 'de', "Value" FROM desired_translations WHERE "LanguageCode" = 'en'
                    UNION ALL
                    SELECT "Key", 'ar', "Value" FROM desired_translations WHERE "LanguageCode" = 'en'
                    UNION ALL
                    SELECT "Key", 'ru', "Value" FROM desired_translations WHERE "LanguageCode" = 'en'
                ),
                resolved AS (
                    SELECT tk."Id" AS "TranslationKeyId", et."LanguageCode", et."Value"
                    FROM expanded_translations et
                    INNER JOIN "TranslationKeys" tk ON tk."Key" = et."Key"
                )
                UPDATE "Translations" t
                SET "Value" = r."Value",
                    "UpdatedAt" = NOW(),
                    "UpdatedBy" = '20260527153000_AddSlnGiftCardBranchScope'
                FROM resolved r
                WHERE t."TranslationKeyId" = r."TranslationKeyId"
                  AND t."LanguageCode" = r."LanguageCode";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SlnGiftCards_BranchId",
                table: "SlnGiftCards",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SlnGiftCards_CustomerId_BranchId",
                table: "SlnGiftCards",
                columns: new[] { "CustomerId", "BranchId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SlnGiftCards_SlnBranches_BranchId",
                table: "SlnGiftCards",
                column: "BranchId",
                principalTable: "SlnBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SlnGiftCards_SlnBranches_BranchId",
                table: "SlnGiftCards");

            migrationBuilder.DropIndex(
                name: "IX_SlnGiftCards_BranchId",
                table: "SlnGiftCards");

            migrationBuilder.DropIndex(
                name: "IX_SlnGiftCards_CustomerId_BranchId",
                table: "SlnGiftCards");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SlnGiftCards");
        }
    }
}
