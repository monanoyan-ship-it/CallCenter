using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSalonPackageOfficialPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ServicePricingItems"
                SET
                    "MonthlyPrice" = CASE "PackageGroupId"
                        WHEN 0 THEN 1700.00
                        WHEN 1 THEN 400.00
                        WHEN 3 THEN 1500.00
                        WHEN 5 THEN 1500.00
                        WHEN 6 THEN 200.00
                        ELSE "MonthlyPrice"
                    END,
                    "ServiceName" = CASE "PackageGroupId"
                        WHEN 0 THEN 'Temel Paket'
                        WHEN 1 THEN 'Stok Tedarik / Finans'
                        WHEN 3 THEN 'Müşteri Sadakati / Pazarlama'
                        WHEN 5 THEN 'Profesyonel'
                        WHEN 6 THEN 'Kurumsal'
                        ELSE "ServiceName"
                    END
                WHERE "ProductTypeId" = 2
                  AND "PackageGroupId" IS NOT NULL
                  AND (
                      "MonthlyPrice" IN (0, 20)
                      OR "ServiceName" IS NULL
                      OR "ServiceName" = ''
                  );
                """);

            migrationBuilder.Sql("""
                UPDATE "SlnServiceCategories"
                SET "Name" = CASE "Name"
                    WHEN 'Sac' THEN 'Saç'
                    WHEN 'Cilt Bakimi' THEN 'Cilt Bakımı'
                    WHEN 'Tirnak' THEN 'Tırnak'
                    WHEN 'Agda / Epilasyon' THEN 'Ağda / Epilasyon'
                    WHEN 'Ozel Bakim' THEN 'Özel Bakım'
                    ELSE "Name"
                END
                WHERE "Name" IN ('Sac', 'Cilt Bakimi', 'Tirnak', 'Agda / Epilasyon', 'Ozel Bakim');

                UPDATE "SlnServices"
                SET "Name" = CASE "Name"
                    WHEN 'Sac Kesim' THEN 'Saç Kesim'
                    WHEN 'Sac Boyama' THEN 'Saç Boyama'
                    WHEN 'Keratin Bakimi' THEN 'Keratin Bakımı'
                    WHEN 'Sac Bakimi' THEN 'Saç Bakımı'
                    WHEN 'Sac Acma' THEN 'Saç Açma'
                    WHEN 'Gecici Duzlestirme' THEN 'Geçici Düzleştirme'
                    WHEN 'Cilt Bakimi' THEN 'Cilt Bakımı'
                    WHEN 'Maske Uygulamasi' THEN 'Maske Uygulaması'
                    WHEN 'Manikur' THEN 'Manikür'
                    WHEN 'Pedikur' THEN 'Pedikür'
                    WHEN 'Protez Tirnak' THEN 'Protez Tırnak'
                    WHEN 'Kalici Oje' THEN 'Kalıcı Oje'
                    WHEN 'Tirnak Bakimi' THEN 'Tırnak Bakımı'
                    WHEN 'Gunluk Makyaj' THEN 'Günlük Makyaj'
                    WHEN 'Gelin Makyaji' THEN 'Gelin Makyajı'
                    WHEN 'Ozel Gun Makyaji' THEN 'Özel Gün Makyajı'
                    WHEN 'Tum Vucut Agda' THEN 'Tüm Vücut Ağda'
                    WHEN 'Bacak Agda' THEN 'Bacak Ağda'
                    WHEN 'Kol Agda' THEN 'Kol Ağda'
                    WHEN 'Yuz Agda' THEN 'Yüz Ağda'
                    WHEN 'Bikini Agda' THEN 'Bikini Ağda'
                    WHEN 'Kas Alimi' THEN 'Kaş Alımı'
                    WHEN 'Kalici Kas' THEN 'Kalıcı Kaş'
                    ELSE "Name"
                END
                WHERE "Name" IN (
                    'Sac Kesim', 'Sac Boyama', 'Keratin Bakimi', 'Sac Bakimi', 'Sac Acma',
                    'Gecici Duzlestirme', 'Cilt Bakimi', 'Maske Uygulamasi', 'Manikur', 'Pedikur',
                    'Protez Tirnak', 'Kalici Oje', 'Tirnak Bakimi', 'Gunluk Makyaj', 'Gelin Makyaji',
                    'Ozel Gun Makyaji', 'Tum Vucut Agda', 'Bacak Agda', 'Kol Agda', 'Yuz Agda',
                    'Bikini Agda', 'Kas Alimi', 'Kalici Kas'
                );

                UPDATE "SlnExpenseCategories"
                SET "Name" = CASE "Name"
                    WHEN 'Malzeme / Urun Alimi' THEN 'Malzeme / Ürün Alımı'
                    WHEN 'Bakim / Onarim' THEN 'Bakım / Onarım'
                    WHEN 'Diger' THEN 'Diğer'
                    ELSE "Name"
                END
                WHERE "Name" IN ('Malzeme / Urun Alimi', 'Bakim / Onarim', 'Diger');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
