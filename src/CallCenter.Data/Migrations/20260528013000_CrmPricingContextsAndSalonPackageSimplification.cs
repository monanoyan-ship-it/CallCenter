using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260528013000_CrmPricingContextsAndSalonPackageSimplification")]
    public partial class CrmPricingContextsAndSalonPackageSimplification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Stok/Tedarik ve Masraf artik temel Salon paketindedir.
                INSERT INTO "CustomerPortalModules" ("CustomerId", "ModuleId", "IsActive", "ActivatedAt", "Notes")
                SELECT DISTINCT cp."CustomerId", m."ModuleId", TRUE, NOW(), '20260528013000 core salon backfill'
                FROM "CustomerProducts" cp
                CROSS JOIN (VALUES (208), (210)) AS m("ModuleId")
                WHERE cp."ProductTypeId" = 2
                  AND cp."IsActive" = TRUE
                ON CONFLICT ("CustomerId", "ModuleId") DO UPDATE
                SET "IsActive" = TRUE,
                    "DeactivatedAt" = NULL,
                    "ActivatedAt" = COALESCE("CustomerPortalModules"."ActivatedAt", NOW());
                """);

            migrationBuilder.Sql("""
                -- Eski Salon CRM/LoyaltyMarketing modul kullanimindan CRM urununu ve Salon CRM modul baglamini olustur.
                WITH salon_crm_customers AS (
                    SELECT DISTINCT "CustomerId"
                    FROM "CustomerPortalModules"
                    WHERE "IsActive" = TRUE
                      AND "ModuleId" IN (216, 218, 219, 212, 222, 223, 227)
                )
                INSERT INTO "CustomerProducts" ("CustomerId", "ProductTypeId", "MonthlyPrice", "IsActive", "CreatedAt")
                SELECT "CustomerId", 3, 0, TRUE, NOW()
                FROM salon_crm_customers
                ON CONFLICT ("CustomerId", "ProductTypeId") DO UPDATE
                SET "IsActive" = TRUE;

                WITH salon_crm_map("SalonModuleId", "CrmModuleId") AS (
                    VALUES
                        (216, 401),
                        (218, 402),
                        (219, 403),
                        (212, 404),
                        (222, 405),
                        (223, 406),
                        (227, 407)
                ),
                active_salon_crm AS (
                    SELECT DISTINCT cpm."CustomerId", m."CrmModuleId"
                    FROM "CustomerPortalModules" cpm
                    INNER JOIN salon_crm_map m ON m."SalonModuleId" = cpm."ModuleId"
                    WHERE cpm."IsActive" = TRUE
                )
                INSERT INTO "CustomerPortalModules" ("CustomerId", "ModuleId", "IsActive", "ActivatedAt", "Notes")
                SELECT "CustomerId", "CrmModuleId", TRUE, NOW(), '20260528013000 salon crm backfill'
                FROM active_salon_crm
                ON CONFLICT ("CustomerId", "ModuleId") DO UPDATE
                SET "IsActive" = TRUE,
                    "DeactivatedAt" = NULL,
                    "ActivatedAt" = COALESCE("CustomerPortalModules"."ActivatedAt", NOW());
                """);

            migrationBuilder.Sql("""
                -- Mevcut CRM alan musterilerin eski genel CRM erisimini koru.
                WITH crm_customers AS (
                    SELECT DISTINCT "CustomerId"
                    FROM "CustomerProducts"
                    WHERE "ProductTypeId" = 3
                      AND "IsActive" = TRUE
                ),
                core_modules("ModuleId") AS (
                    VALUES (301), (302), (303), (304), (305), (306), (307), (308), (309), (310)
                )
                INSERT INTO "CustomerPortalModules" ("CustomerId", "ModuleId", "IsActive", "ActivatedAt", "Notes")
                SELECT c."CustomerId", m."ModuleId", TRUE, NOW(), '20260528013000 general crm backfill'
                FROM crm_customers c
                CROSS JOIN core_modules m
                ON CONFLICT ("CustomerId", "ModuleId") DO UPDATE
                SET "IsActive" = TRUE,
                    "DeactivatedAt" = NULL,
                    "ActivatedAt" = COALESCE("CustomerPortalModules"."ActivatedAt", NOW());

                -- CRM + CallCenter urunu birlikte olan musterilerin eski CC CRM kapsamini koru.
                WITH cc_crm_customers AS (
                    SELECT DISTINCT crm."CustomerId"
                    FROM "CustomerProducts" crm
                    INNER JOIN "CustomerProducts" cc
                        ON cc."CustomerId" = crm."CustomerId"
                       AND cc."ProductTypeId" = 1
                       AND cc."IsActive" = TRUE
                    WHERE crm."ProductTypeId" = 3
                      AND crm."IsActive" = TRUE
                ),
                cc_crm_modules("ModuleId") AS (
                    VALUES (501), (502), (503), (504), (505), (506)
                )
                INSERT INTO "CustomerPortalModules" ("CustomerId", "ModuleId", "IsActive", "ActivatedAt", "Notes")
                SELECT c."CustomerId", m."ModuleId", TRUE, NOW(), '20260528013000 callcenter crm backfill'
                FROM cc_crm_customers c
                CROSS JOIN cc_crm_modules m
                ON CONFLICT ("CustomerId", "ModuleId") DO UPDATE
                SET "IsActive" = TRUE,
                    "DeactivatedAt" = NULL,
                    "ActivatedAt" = COALESCE("CustomerPortalModules"."ActivatedAt", NOW());
                """);

            migrationBuilder.Sql("""
                -- Fiyat donemlerine CRM modul ve paket satirlarini ekle.
                WITH crm_modules("ModuleId", "Name") AS (
                    VALUES
                        (301, 'Dashboard'),
                        (302, 'Kisiler'),
                        (303, 'Talepler'),
                        (304, 'Firsatlar'),
                        (305, 'Etkilesimler'),
                        (306, 'Gorevler'),
                        (307, 'Anketler'),
                        (308, 'Kampanyalar'),
                        (309, 'Raporlar'),
                        (310, 'Entegrasyonlar'),
                        (401, 'Hediye Kartlari'),
                        (402, 'Uyelik Planlari'),
                        (403, 'Sadakat Programi'),
                        (404, 'Pazarlama ve SMS'),
                        (405, 'E-posta Kampanyalari'),
                        (406, 'Yorum Yonetimi'),
                        (407, 'Kayip Musteri Geri Kazanim'),
                        (501, 'CallCenter Dashboard'),
                        (502, 'Cagri Kisileri'),
                        (503, 'Destek Talepleri'),
                        (504, 'Cagri Etkilesimleri'),
                        (505, 'Arama Kampanyalari'),
                        (506, 'CallCenter CRM Raporlari')
                )
                INSERT INTO "ServicePricingItems" ("PeriodId", "ProductTypeId", "ServiceId", "PackageGroupId", "ServiceName", "MonthlyPrice", "PreviousPrice")
                SELECT p."Id", 3, m."ModuleId", NULL, m."Name", 0, NULL
                FROM "ServicePricingPeriods" p
                CROSS JOIN crm_modules m
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "ServicePricingItems" i
                    WHERE i."PeriodId" = p."Id"
                      AND i."ProductTypeId" = 3
                      AND i."ServiceId" = m."ModuleId"
                      AND i."PackageGroupId" IS NULL
                );

                WITH crm_packages("PackageGroupId", "Name", "Price") AS (
                    VALUES
                        (0, 'Genel CRM', 1500::numeric),
                        (1, 'Salon CRM', 1500::numeric),
                        (2, 'CallCenter CRM', 1500::numeric)
                )
                INSERT INTO "ServicePricingItems" ("PeriodId", "ProductTypeId", "ServiceId", "PackageGroupId", "ServiceName", "MonthlyPrice", "PreviousPrice")
                SELECT p."Id", 3, 0, pkg."PackageGroupId", pkg."Name", pkg."Price", pkg."Price"
                FROM "ServicePricingPeriods" p
                CROSS JOIN crm_packages pkg
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "ServicePricingItems" i
                    WHERE i."PeriodId" = p."Id"
                      AND i."ProductTypeId" = 3
                      AND i."ServiceId" = 0
                      AND i."PackageGroupId" = pkg."PackageGroupId"
                );
                """);

            migrationBuilder.Sql("""
                -- Islem gormemis salon tahakkuklerinde eski Stok/Finans paket satirini kaldir.
                WITH removed AS (
                    DELETE FROM "CustomerBillingPeriodModuleLines" l
                    USING "CustomerBillingPeriods" p
                    WHERE p."Id" = l."CustomerBillingPeriodId"
                      AND p."BillingKindId" = 2
                      AND p."IsPaid" = FALSE
                      AND p."StatusId" = 1
                      AND l."PackageGroupId" = 1
                    RETURNING p."Id" AS "PeriodId", l."LineAmount"
                ),
                sums AS (
                    SELECT "PeriodId", SUM("LineAmount") AS "Amount"
                    FROM removed
                    GROUP BY "PeriodId"
                )
                UPDATE "CustomerBillingPeriods" p
                SET "ServiceAmount" = GREATEST(0, p."ServiceAmount" - s."Amount")
                FROM sums s
                WHERE p."Id" = s."PeriodId";

                -- Eski Kurumsal/Raporlama paket satiri artik Raporlama ve Analiz grubuna baglanir.
                UPDATE "CustomerBillingPeriodModuleLines" l
                SET "PackageGroupId" = 5,
                    "ModuleDisplayName" = 'Raporlama ve Analiz'
                FROM "CustomerBillingPeriods" p
                WHERE p."Id" = l."CustomerBillingPeriodId"
                  AND p."BillingKindId" = 2
                  AND p."IsPaid" = FALSE
                  AND p."StatusId" = 1
                  AND l."PackageGroupId" = 6
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "CustomerBillingPeriodModuleLines" existing
                      WHERE existing."CustomerBillingPeriodId" = l."CustomerBillingPeriodId"
                        AND existing."PackageGroupId" = 5
                  );

                WITH removed AS (
                    DELETE FROM "CustomerBillingPeriodModuleLines" l
                    USING "CustomerBillingPeriods" p
                    WHERE p."Id" = l."CustomerBillingPeriodId"
                      AND p."BillingKindId" = 2
                      AND p."IsPaid" = FALSE
                      AND p."StatusId" = 1
                      AND l."PackageGroupId" = 6
                    RETURNING p."Id" AS "PeriodId", l."LineAmount"
                ),
                sums AS (
                    SELECT "PeriodId", SUM("LineAmount") AS "Amount"
                    FROM removed
                    GROUP BY "PeriodId"
                )
                UPDATE "CustomerBillingPeriods" p
                SET "ServiceAmount" = GREATEST(0, p."ServiceAmount" - s."Amount")
                FROM sums s
                WHERE p."Id" = s."PeriodId";
                """);

            migrationBuilder.Sql("""
                -- Canli DB cevirileri eski adlari gostermesin.
                WITH desired("Key", "Lang", "Value") AS (
                    VALUES
                        ('salon.modules.package.professional', 'tr', 'Raporlama ve Analiz'),
                        ('salon.modules.package.professional', 'en', 'Reporting and Analytics'),
                        ('salon.modules.package.professional.summary', 'tr', 'Raporlama, islem gorselleri ve karsilastirma ekranlarini tek pakette toplar.'),
                        ('salon.modules.package.professional.summary', 'en', 'Brings reporting, treatment visuals, and comparison views into one package.'),
                        ('salon.modules.package.professional.outcome', 'tr', 'Sube, personel, hizmet, urun ve islem sonuclarini daha rahat karsilastirirsiniz.'),
                        ('salon.modules.package.professional.outcome', 'en', 'You can compare branch, staff, service, product, and treatment results more comfortably.'),
                        ('salon.modules.package.professional.flow_note', 'tr', 'Raporlar ve islem gorselleri ayni analiz paketi altinda yonetilir.'),
                        ('salon.modules.package.professional.flow_note', 'en', 'Reports and treatment visuals are managed under the same analytics package.'),
                        ('salon.modules.package.professional.usage_note', 'tr', 'Bu paket rapor derinligi, disa aktarim ve islem gorseli ozelliklerini acar.'),
                        ('salon.modules.package.professional.usage_note', 'en', 'This package unlocks deeper reports, export, and treatment visual features.')
                ),
                distinct_keys AS (
                    SELECT DISTINCT "Key" FROM desired
                )
                INSERT INTO "TranslationKeys" ("Key", "Description", "Module", "PlatformId")
                SELECT dk."Key", 'Salon package pricing text', 'modules', 2
                FROM distinct_keys dk
                WHERE NOT EXISTS (SELECT 1 FROM "TranslationKeys" tk WHERE tk."Key" = dk."Key");
                """);

            migrationBuilder.Sql("""
                WITH desired("Key", "Lang", "Value") AS (
                    VALUES
                        ('salon.modules.package.professional', 'tr', 'Raporlama ve Analiz'),
                        ('salon.modules.package.professional', 'en', 'Reporting and Analytics'),
                        ('salon.modules.package.professional.summary', 'tr', 'Raporlama, islem gorselleri ve karsilastirma ekranlarini tek pakette toplar.'),
                        ('salon.modules.package.professional.summary', 'en', 'Brings reporting, treatment visuals, and comparison views into one package.'),
                        ('salon.modules.package.professional.outcome', 'tr', 'Sube, personel, hizmet, urun ve islem sonuclarini daha rahat karsilastirirsiniz.'),
                        ('salon.modules.package.professional.outcome', 'en', 'You can compare branch, staff, service, product, and treatment results more comfortably.'),
                        ('salon.modules.package.professional.flow_note', 'tr', 'Raporlar ve islem gorselleri ayni analiz paketi altinda yonetilir.'),
                        ('salon.modules.package.professional.flow_note', 'en', 'Reports and treatment visuals are managed under the same analytics package.'),
                        ('salon.modules.package.professional.usage_note', 'tr', 'Bu paket rapor derinligi, disa aktarim ve islem gorseli ozelliklerini acar.'),
                        ('salon.modules.package.professional.usage_note', 'en', 'This package unlocks deeper reports, export, and treatment visual features.')
                ),
                resolved AS (
                    SELECT tk."Id" AS "TranslationKeyId", d."Lang", d."Value"
                    FROM desired d
                    INNER JOIN "TranslationKeys" tk ON tk."Key" = d."Key"
                )
                INSERT INTO "Translations" ("TranslationKeyId", "LanguageCode", "Value", "UpdatedAt", "UpdatedBy")
                SELECT r."TranslationKeyId", r."Lang", r."Value", NOW(), '20260528013000_CrmPricingContextsAndSalonPackageSimplification'
                FROM resolved r
                ON CONFLICT ("TranslationKeyId", "LanguageCode") DO UPDATE
                SET "Value" = EXCLUDED."Value",
                    "UpdatedAt" = EXCLUDED."UpdatedAt",
                    "UpdatedBy" = EXCLUDED."UpdatedBy";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only migration. Down is intentionally left empty to avoid deleting customer entitlements.
        }
    }
}
