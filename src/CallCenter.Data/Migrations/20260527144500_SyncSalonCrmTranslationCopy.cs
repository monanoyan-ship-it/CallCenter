using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallCenter.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260527144500_SyncSalonCrmTranslationCopy")]
    public partial class SyncSalonCrmTranslationCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH desired_keys("Key", "Module", "Description") AS (
                    VALUES
                        ('salon.sidebar.modules', 'sidebar', 'Service packages sidebar link (legacy)'),
                        ('salon.sidebar.service_packages', 'sidebar', 'Service packages sidebar link'),
                        ('salon.sidebar.marketing', 'sidebar', 'Customer relations sidebar group (legacy)'),
                        ('salon.sidebar.customer_relations', 'sidebar', 'Customer relations sidebar group'),
                        ('salon.modules.title', 'modules', 'Service packages page title (legacy)'),
                        ('salon.modules.service_packages_title', 'modules', 'Service packages page title'),
                        ('salon.modules.active_extras', 'modules', 'Active services title (legacy)'),
                        ('salon.modules.active_title', 'modules', 'Active services title (legacy)'),
                        ('salon.modules.active_services_title', 'modules', 'Active services title'),
                        ('salon.modules.available', 'modules', 'Available services title (legacy)'),
                        ('salon.modules.available_title', 'modules', 'Available services title (legacy)'),
                        ('salon.modules.available_services_title', 'modules', 'Available services title'),
                        ('salon.modules.monthly_service_total', 'modules', 'Monthly service total'),
                        ('salon.modules.package.loyalty_marketing', 'modules', 'Salon CRM package name (legacy)'),
                        ('salon.modules.package.loyalty_marketing.summary', 'modules', 'Salon CRM package summary (legacy)'),
                        ('salon.modules.package.loyalty_marketing.outcome', 'modules', 'Salon CRM package outcome (legacy)'),
                        ('salon.modules.package.loyalty_marketing.flow_note', 'modules', 'Salon CRM package flow note (legacy)'),
                        ('salon.modules.package.loyalty_marketing.usage_note', 'modules', 'Salon CRM package usage note (legacy)'),
                        ('salon.modules.package.salon_crm', 'modules', 'Salon CRM package name'),
                        ('salon.modules.package.salon_crm.summary', 'modules', 'Salon CRM package summary'),
                        ('salon.modules.package.salon_crm.outcome', 'modules', 'Salon CRM package outcome'),
                        ('salon.modules.package.salon_crm.flow_note', 'modules', 'Salon CRM package flow note'),
                        ('salon.modules.package.salon_crm.usage_note', 'modules', 'Salon CRM package usage note'),
                        ('salon.modules.purchase_title', 'modules', 'Service purchase modal title (legacy)'),
                        ('salon.modules.service_purchase_modal_title', 'modules', 'Service purchase modal title'),
                        ('salon.modules.cancel_module_title', 'modules', 'Service cancellation title (legacy)'),
                        ('salon.modules.cancel_module_body', 'modules', 'Service cancellation body (legacy)'),
                        ('salon.modules.cancel_service_title', 'modules', 'Service cancellation title'),
                        ('salon.modules.cancel_service_body', 'modules', 'Service cancellation body'),
                        ('salon.modules.back_to_modules', 'modules', 'Back to service packages (legacy)'),
                        ('salon.modules.back_to_services', 'modules', 'Back to service packages'),
                        ('salon.modules.service_not_found', 'modules', 'Service not found'),
                        ('salon.modules.service_active_after_payment', 'modules', 'Service active after payment'),
                        ('salon.modules.service_activated', 'modules', 'Service activated'),
                        ('salon.modules.refresh_to_see_service', 'modules', 'Refresh to see service'),
                        ('salon.layout.payment_modules', 'common', 'Billing banner payment services button')
                )
                INSERT INTO "TranslationKeys" ("Key", "Module", "Description", "PlatformId")
                SELECT d."Key", d."Module", d."Description", 2
                FROM desired_keys d
                WHERE NOT EXISTS (
                    SELECT 1 FROM "TranslationKeys" tk WHERE tk."Key" = d."Key"
                );

                WITH desired_keys("Key", "Module", "Description") AS (
                    VALUES
                        ('salon.sidebar.modules', 'sidebar', 'Service packages sidebar link (legacy)'),
                        ('salon.sidebar.service_packages', 'sidebar', 'Service packages sidebar link'),
                        ('salon.sidebar.marketing', 'sidebar', 'Customer relations sidebar group (legacy)'),
                        ('salon.sidebar.customer_relations', 'sidebar', 'Customer relations sidebar group'),
                        ('salon.modules.title', 'modules', 'Service packages page title (legacy)'),
                        ('salon.modules.service_packages_title', 'modules', 'Service packages page title'),
                        ('salon.modules.active_extras', 'modules', 'Active services title (legacy)'),
                        ('salon.modules.active_title', 'modules', 'Active services title (legacy)'),
                        ('salon.modules.active_services_title', 'modules', 'Active services title'),
                        ('salon.modules.available', 'modules', 'Available services title (legacy)'),
                        ('salon.modules.available_title', 'modules', 'Available services title (legacy)'),
                        ('salon.modules.available_services_title', 'modules', 'Available services title'),
                        ('salon.modules.monthly_service_total', 'modules', 'Monthly service total'),
                        ('salon.modules.package.loyalty_marketing', 'modules', 'Salon CRM package name (legacy)'),
                        ('salon.modules.package.loyalty_marketing.summary', 'modules', 'Salon CRM package summary (legacy)'),
                        ('salon.modules.package.loyalty_marketing.outcome', 'modules', 'Salon CRM package outcome (legacy)'),
                        ('salon.modules.package.loyalty_marketing.flow_note', 'modules', 'Salon CRM package flow note (legacy)'),
                        ('salon.modules.package.loyalty_marketing.usage_note', 'modules', 'Salon CRM package usage note (legacy)'),
                        ('salon.modules.package.salon_crm', 'modules', 'Salon CRM package name'),
                        ('salon.modules.package.salon_crm.summary', 'modules', 'Salon CRM package summary'),
                        ('salon.modules.package.salon_crm.outcome', 'modules', 'Salon CRM package outcome'),
                        ('salon.modules.package.salon_crm.flow_note', 'modules', 'Salon CRM package flow note'),
                        ('salon.modules.package.salon_crm.usage_note', 'modules', 'Salon CRM package usage note'),
                        ('salon.modules.purchase_title', 'modules', 'Service purchase modal title (legacy)'),
                        ('salon.modules.service_purchase_modal_title', 'modules', 'Service purchase modal title'),
                        ('salon.modules.cancel_module_title', 'modules', 'Service cancellation title (legacy)'),
                        ('salon.modules.cancel_module_body', 'modules', 'Service cancellation body (legacy)'),
                        ('salon.modules.cancel_service_title', 'modules', 'Service cancellation title'),
                        ('salon.modules.cancel_service_body', 'modules', 'Service cancellation body'),
                        ('salon.modules.back_to_modules', 'modules', 'Back to service packages (legacy)'),
                        ('salon.modules.back_to_services', 'modules', 'Back to service packages'),
                        ('salon.modules.service_not_found', 'modules', 'Service not found'),
                        ('salon.modules.service_active_after_payment', 'modules', 'Service active after payment'),
                        ('salon.modules.service_activated', 'modules', 'Service activated'),
                        ('salon.modules.refresh_to_see_service', 'modules', 'Refresh to see service'),
                        ('salon.layout.payment_modules', 'common', 'Billing banner payment services button')
                )
                UPDATE "TranslationKeys" tk
                SET "Module" = d."Module",
                    "Description" = d."Description",
                    "PlatformId" = 2
                FROM desired_keys d
                WHERE tk."Key" = d."Key";

                WITH desired_translations("Key", "LanguageCode", "Value") AS (
                    VALUES
                        ('salon.sidebar.modules', 'tr', 'Hizmet Paketleri'),
                        ('salon.sidebar.service_packages', 'tr', 'Hizmet Paketleri'),
                        ('salon.sidebar.marketing', 'tr', 'Müşteri İlişkileri'),
                        ('salon.sidebar.customer_relations', 'tr', 'Müşteri İlişkileri'),
                        ('salon.modules.title', 'tr', 'Hizmet Paketleri'),
                        ('salon.modules.service_packages_title', 'tr', 'Hizmet Paketleri'),
                        ('salon.modules.active_extras', 'tr', 'Aktif Hizmetleriniz'),
                        ('salon.modules.active_title', 'tr', 'Aktif Hizmetleriniz'),
                        ('salon.modules.active_services_title', 'tr', 'Aktif Hizmetleriniz'),
                        ('salon.modules.available', 'tr', 'Satın Alınabilir Hizmetler'),
                        ('salon.modules.available_title', 'tr', 'Satın Alınabilir Hizmetler'),
                        ('salon.modules.available_services_title', 'tr', 'Satın Alınabilir Hizmetler'),
                        ('salon.modules.monthly_service_total', 'tr', 'Aylık Hizmet Tutarı'),
                        ('salon.modules.package.loyalty_marketing', 'tr', 'Salon CRM'),
                        ('salon.modules.package.salon_crm', 'tr', 'Salon CRM'),
                        ('salon.modules.package.loyalty_marketing.summary', 'tr', 'Müşteri kartını satış sonrası takip, üyelik, hediye kartı, sadakat, yorum ve geri kazanım işleriyle güçlendirir.'),
                        ('salon.modules.package.salon_crm.summary', 'tr', 'Müşteri kartını satış sonrası takip, üyelik, hediye kartı, sadakat, yorum ve geri kazanım işleriyle güçlendirir.'),
                        ('salon.modules.package.loyalty_marketing.outcome', 'tr', 'Müşteriyle ilişkiniz randevudan sonra da devam eder; tekrar geliş, düzenli gelir ve memnuniyet takibi aynı hizmette toplanır.'),
                        ('salon.modules.package.salon_crm.outcome', 'tr', 'Müşteriyle ilişkiniz randevudan sonra da devam eder; tekrar geliş, düzenli gelir ve memnuniyet takibi aynı hizmette toplanır.'),
                        ('salon.modules.package.loyalty_marketing.flow_note', 'tr', 'Müşteri kaydı, satış sonrası haklar ve geri dönüş işleri aynı CRM hizmetinde takip edilir.'),
                        ('salon.modules.package.salon_crm.flow_note', 'tr', 'Müşteri kaydı, satış sonrası haklar ve geri dönüş işleri aynı CRM hizmetinde takip edilir.'),
                        ('salon.modules.package.loyalty_marketing.usage_note', 'tr', 'SMS, WhatsApp ve yüksek hacimli e-posta gönderimleri CRM hizmetinden ayrı kredi/kullanım olarak izlenebilir.'),
                        ('salon.modules.package.salon_crm.usage_note', 'tr', 'SMS, WhatsApp ve yüksek hacimli e-posta gönderimleri CRM hizmetinden ayrı kredi/kullanım olarak izlenebilir.'),
                        ('salon.modules.purchase_title', 'tr', 'Hizmet Satın Al'),
                        ('salon.modules.service_purchase_modal_title', 'tr', 'Hizmet Satın Al'),
                        ('salon.modules.cancel_module_title', 'tr', 'Hizmet İptali'),
                        ('salon.modules.cancel_service_title', 'tr', 'Hizmet İptali'),
                        ('salon.modules.cancel_module_body', 'tr', '{name} hizmetini iptal etmek istediğinize emin misiniz? İptal talebi admin onayına gönderilecektir.'),
                        ('salon.modules.cancel_service_body', 'tr', '{name} hizmetini iptal etmek istediğinize emin misiniz? İptal talebi admin onayına gönderilecektir.'),
                        ('salon.modules.back_to_modules', 'tr', 'Hizmet Paketlerine Dön'),
                        ('salon.modules.back_to_services', 'tr', 'Hizmet Paketlerine Dön'),
                        ('salon.layout.payment_modules', 'tr', 'Ödeme / Hizmetler'),

                        ('salon.sidebar.modules', 'en', 'Service Packages'),
                        ('salon.sidebar.service_packages', 'en', 'Service Packages'),
                        ('salon.sidebar.marketing', 'en', 'Customer Relations'),
                        ('salon.sidebar.customer_relations', 'en', 'Customer Relations'),
                        ('salon.modules.title', 'en', 'Service Packages'),
                        ('salon.modules.service_packages_title', 'en', 'Service Packages'),
                        ('salon.modules.active_extras', 'en', 'Your Active Services'),
                        ('salon.modules.active_title', 'en', 'Your Active Services'),
                        ('salon.modules.active_services_title', 'en', 'Your Active Services'),
                        ('salon.modules.available', 'en', 'Available Services'),
                        ('salon.modules.available_title', 'en', 'Available Services'),
                        ('salon.modules.available_services_title', 'en', 'Available Services'),
                        ('salon.modules.monthly_service_total', 'en', 'Monthly Service Total'),
                        ('salon.modules.package.loyalty_marketing', 'en', 'Salon CRM'),
                        ('salon.modules.package.salon_crm', 'en', 'Salon CRM'),
                        ('salon.modules.package.loyalty_marketing.summary', 'en', 'Strengthens the client profile with after-sales follow-up, memberships, gift cards, loyalty, reviews, and winback work.'),
                        ('salon.modules.package.salon_crm.summary', 'en', 'Strengthens the client profile with after-sales follow-up, memberships, gift cards, loyalty, reviews, and winback work.'),
                        ('salon.modules.package.loyalty_marketing.outcome', 'en', 'The client relationship continues after the appointment; return visits, recurring revenue, and satisfaction tracking stay in one service.'),
                        ('salon.modules.package.salon_crm.outcome', 'en', 'The client relationship continues after the appointment; return visits, recurring revenue, and satisfaction tracking stay in one service.'),
                        ('salon.modules.package.loyalty_marketing.flow_note', 'en', 'Client records, after-sales entitlements, and return actions are tracked in the same CRM service.'),
                        ('salon.modules.package.salon_crm.flow_note', 'en', 'Client records, after-sales entitlements, and return actions are tracked in the same CRM service.'),
                        ('salon.modules.package.loyalty_marketing.usage_note', 'en', 'SMS, WhatsApp, and high-volume email sending can be tracked separately from the CRM service as credit/usage.'),
                        ('salon.modules.package.salon_crm.usage_note', 'en', 'SMS, WhatsApp, and high-volume email sending can be tracked separately from the CRM service as credit/usage.'),
                        ('salon.modules.purchase_title', 'en', 'Buy Service'),
                        ('salon.modules.service_purchase_modal_title', 'en', 'Buy Service'),
                        ('salon.modules.cancel_module_title', 'en', 'Service Cancellation'),
                        ('salon.modules.cancel_service_title', 'en', 'Service Cancellation'),
                        ('salon.modules.cancel_module_body', 'en', 'Are you sure you want to cancel the {name} service? The cancellation request will be sent for admin approval.'),
                        ('salon.modules.cancel_service_body', 'en', 'Are you sure you want to cancel the {name} service? The cancellation request will be sent for admin approval.'),
                        ('salon.modules.back_to_modules', 'en', 'Back to Service Packages'),
                        ('salon.modules.back_to_services', 'en', 'Back to Service Packages'),
                        ('salon.layout.payment_modules', 'en', 'Payment / Services')
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
                SELECT r."TranslationKeyId", r."LanguageCode", r."Value", NOW(), '20260527144500_SyncSalonCrmTranslationCopy'
                FROM resolved r
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Translations" t
                    WHERE t."TranslationKeyId" = r."TranslationKeyId"
                      AND t."LanguageCode" = r."LanguageCode"
                );

                WITH desired_translations("Key", "LanguageCode", "Value") AS (
                    VALUES
                        ('salon.sidebar.modules', 'tr', 'Hizmet Paketleri'),
                        ('salon.sidebar.service_packages', 'tr', 'Hizmet Paketleri'),
                        ('salon.sidebar.marketing', 'tr', 'Müşteri İlişkileri'),
                        ('salon.sidebar.customer_relations', 'tr', 'Müşteri İlişkileri'),
                        ('salon.modules.title', 'tr', 'Hizmet Paketleri'),
                        ('salon.modules.service_packages_title', 'tr', 'Hizmet Paketleri'),
                        ('salon.modules.active_extras', 'tr', 'Aktif Hizmetleriniz'),
                        ('salon.modules.active_title', 'tr', 'Aktif Hizmetleriniz'),
                        ('salon.modules.active_services_title', 'tr', 'Aktif Hizmetleriniz'),
                        ('salon.modules.available', 'tr', 'Satın Alınabilir Hizmetler'),
                        ('salon.modules.available_title', 'tr', 'Satın Alınabilir Hizmetler'),
                        ('salon.modules.available_services_title', 'tr', 'Satın Alınabilir Hizmetler'),
                        ('salon.modules.monthly_service_total', 'tr', 'Aylık Hizmet Tutarı'),
                        ('salon.modules.package.loyalty_marketing', 'tr', 'Salon CRM'),
                        ('salon.modules.package.salon_crm', 'tr', 'Salon CRM'),
                        ('salon.modules.package.loyalty_marketing.summary', 'tr', 'Müşteri kartını satış sonrası takip, üyelik, hediye kartı, sadakat, yorum ve geri kazanım işleriyle güçlendirir.'),
                        ('salon.modules.package.salon_crm.summary', 'tr', 'Müşteri kartını satış sonrası takip, üyelik, hediye kartı, sadakat, yorum ve geri kazanım işleriyle güçlendirir.'),
                        ('salon.modules.package.loyalty_marketing.outcome', 'tr', 'Müşteriyle ilişkiniz randevudan sonra da devam eder; tekrar geliş, düzenli gelir ve memnuniyet takibi aynı hizmette toplanır.'),
                        ('salon.modules.package.salon_crm.outcome', 'tr', 'Müşteriyle ilişkiniz randevudan sonra da devam eder; tekrar geliş, düzenli gelir ve memnuniyet takibi aynı hizmette toplanır.'),
                        ('salon.modules.package.loyalty_marketing.flow_note', 'tr', 'Müşteri kaydı, satış sonrası haklar ve geri dönüş işleri aynı CRM hizmetinde takip edilir.'),
                        ('salon.modules.package.salon_crm.flow_note', 'tr', 'Müşteri kaydı, satış sonrası haklar ve geri dönüş işleri aynı CRM hizmetinde takip edilir.'),
                        ('salon.modules.package.loyalty_marketing.usage_note', 'tr', 'SMS, WhatsApp ve yüksek hacimli e-posta gönderimleri CRM hizmetinden ayrı kredi/kullanım olarak izlenebilir.'),
                        ('salon.modules.package.salon_crm.usage_note', 'tr', 'SMS, WhatsApp ve yüksek hacimli e-posta gönderimleri CRM hizmetinden ayrı kredi/kullanım olarak izlenebilir.'),
                        ('salon.modules.purchase_title', 'tr', 'Hizmet Satın Al'),
                        ('salon.modules.service_purchase_modal_title', 'tr', 'Hizmet Satın Al'),
                        ('salon.modules.cancel_module_title', 'tr', 'Hizmet İptali'),
                        ('salon.modules.cancel_service_title', 'tr', 'Hizmet İptali'),
                        ('salon.modules.cancel_module_body', 'tr', '{name} hizmetini iptal etmek istediğinize emin misiniz? İptal talebi admin onayına gönderilecektir.'),
                        ('salon.modules.cancel_service_body', 'tr', '{name} hizmetini iptal etmek istediğinize emin misiniz? İptal talebi admin onayına gönderilecektir.'),
                        ('salon.modules.back_to_modules', 'tr', 'Hizmet Paketlerine Dön'),
                        ('salon.modules.back_to_services', 'tr', 'Hizmet Paketlerine Dön'),
                        ('salon.layout.payment_modules', 'tr', 'Ödeme / Hizmetler'),

                        ('salon.sidebar.modules', 'en', 'Service Packages'),
                        ('salon.sidebar.service_packages', 'en', 'Service Packages'),
                        ('salon.sidebar.marketing', 'en', 'Customer Relations'),
                        ('salon.sidebar.customer_relations', 'en', 'Customer Relations'),
                        ('salon.modules.title', 'en', 'Service Packages'),
                        ('salon.modules.service_packages_title', 'en', 'Service Packages'),
                        ('salon.modules.active_extras', 'en', 'Your Active Services'),
                        ('salon.modules.active_title', 'en', 'Your Active Services'),
                        ('salon.modules.active_services_title', 'en', 'Your Active Services'),
                        ('salon.modules.available', 'en', 'Available Services'),
                        ('salon.modules.available_title', 'en', 'Available Services'),
                        ('salon.modules.available_services_title', 'en', 'Available Services'),
                        ('salon.modules.monthly_service_total', 'en', 'Monthly Service Total'),
                        ('salon.modules.package.loyalty_marketing', 'en', 'Salon CRM'),
                        ('salon.modules.package.salon_crm', 'en', 'Salon CRM'),
                        ('salon.modules.package.loyalty_marketing.summary', 'en', 'Strengthens the client profile with after-sales follow-up, memberships, gift cards, loyalty, reviews, and winback work.'),
                        ('salon.modules.package.salon_crm.summary', 'en', 'Strengthens the client profile with after-sales follow-up, memberships, gift cards, loyalty, reviews, and winback work.'),
                        ('salon.modules.package.loyalty_marketing.outcome', 'en', 'The client relationship continues after the appointment; return visits, recurring revenue, and satisfaction tracking stay in one service.'),
                        ('salon.modules.package.salon_crm.outcome', 'en', 'The client relationship continues after the appointment; return visits, recurring revenue, and satisfaction tracking stay in one service.'),
                        ('salon.modules.package.loyalty_marketing.flow_note', 'en', 'Client records, after-sales entitlements, and return actions are tracked in the same CRM service.'),
                        ('salon.modules.package.salon_crm.flow_note', 'en', 'Client records, after-sales entitlements, and return actions are tracked in the same CRM service.'),
                        ('salon.modules.package.loyalty_marketing.usage_note', 'en', 'SMS, WhatsApp, and high-volume email sending can be tracked separately from the CRM service as credit/usage.'),
                        ('salon.modules.package.salon_crm.usage_note', 'en', 'SMS, WhatsApp, and high-volume email sending can be tracked separately from the CRM service as credit/usage.'),
                        ('salon.modules.purchase_title', 'en', 'Buy Service'),
                        ('salon.modules.service_purchase_modal_title', 'en', 'Buy Service'),
                        ('salon.modules.cancel_module_title', 'en', 'Service Cancellation'),
                        ('salon.modules.cancel_service_title', 'en', 'Service Cancellation'),
                        ('salon.modules.cancel_module_body', 'en', 'Are you sure you want to cancel the {name} service? The cancellation request will be sent for admin approval.'),
                        ('salon.modules.cancel_service_body', 'en', 'Are you sure you want to cancel the {name} service? The cancellation request will be sent for admin approval.'),
                        ('salon.modules.back_to_modules', 'en', 'Back to Service Packages'),
                        ('salon.modules.back_to_services', 'en', 'Back to Service Packages'),
                        ('salon.layout.payment_modules', 'en', 'Payment / Services')
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
                    "UpdatedBy" = '20260527144500_SyncSalonCrmTranslationCopy'
                FROM resolved r
                WHERE t."TranslationKeyId" = r."TranslationKeyId"
                  AND t."LanguageCode" = r."LanguageCode";
                """);

            migrationBuilder.Sql("""
                WITH desired_keys("Key", "Module", "Description") AS (
                    VALUES
                        ('salon.modules.auto.modul_bilgisi_bulunamadi', 'modules', 'Legacy service not found'),
                        ('salon.modules.service_not_found', 'modules', 'Service not found'),
                        ('salon.modules.auto.odeme_sonrasi_modul_hemen_aktif_olacaktir', 'modules', 'Legacy service active after payment'),
                        ('salon.modules.service_active_after_payment', 'modules', 'Service active after payment'),
                        ('salon.modules.activated', 'modules', 'Legacy service activated'),
                        ('salon.modules.service_activated', 'modules', 'Service activated'),
                        ('salon.modules.auto.yeni_modulu_menude_gormek_icin_oturumu_yenileyin', 'modules', 'Legacy refresh to see service'),
                        ('salon.modules.refresh_to_see_service', 'modules', 'Refresh to see service')
                )
                INSERT INTO "TranslationKeys" ("Key", "Module", "Description", "PlatformId")
                SELECT d."Key", d."Module", d."Description", 2
                FROM desired_keys d
                WHERE NOT EXISTS (
                    SELECT 1 FROM "TranslationKeys" tk WHERE tk."Key" = d."Key"
                );

                WITH desired_translations("Key", "LanguageCode", "Value") AS (
                    VALUES
                        ('salon.modules.auto.modul_bilgisi_bulunamadi', 'tr', 'Hizmet bilgisi bulunamadı.'),
                        ('salon.modules.service_not_found', 'tr', 'Hizmet bilgisi bulunamadı.'),
                        ('salon.modules.auto.odeme_sonrasi_modul_hemen_aktif_olacaktir', 'tr', 'Ödeme sonrası hizmet hemen aktif olacaktır.'),
                        ('salon.modules.service_active_after_payment', 'tr', 'Ödeme sonrası hizmet hemen aktif olacaktır.'),
                        ('salon.modules.activated', 'tr', 'Hizmetiniz aktif edildi.'),
                        ('salon.modules.service_activated', 'tr', 'Hizmetiniz aktif edildi.'),
                        ('salon.modules.auto.yeni_modulu_menude_gormek_icin_oturumu_yenileyin', 'tr', 'Yeni hizmeti menüde görmek için oturumu yenileyin.'),
                        ('salon.modules.refresh_to_see_service', 'tr', 'Yeni hizmeti menüde görmek için oturumu yenileyin.'),
                        ('salon.modules.package.loyalty_marketing.flow_note', 'tr', 'Müşteri kaydı, satış sonrası haklar ve geri dönüş işleri aynı CRM hizmetinde takip edilir.'),
                        ('salon.modules.package.salon_crm.flow_note', 'tr', 'Müşteri kaydı, satış sonrası haklar ve geri dönüş işleri aynı CRM hizmetinde takip edilir.'),

                        ('salon.modules.auto.modul_bilgisi_bulunamadi', 'en', 'Service information could not be found.'),
                        ('salon.modules.service_not_found', 'en', 'Service information could not be found.'),
                        ('salon.modules.auto.odeme_sonrasi_modul_hemen_aktif_olacaktir', 'en', 'The service will be active immediately after payment.'),
                        ('salon.modules.service_active_after_payment', 'en', 'The service will be active immediately after payment.'),
                        ('salon.modules.activated', 'en', 'Your service has been activated.'),
                        ('salon.modules.service_activated', 'en', 'Your service has been activated.'),
                        ('salon.modules.auto.yeni_modulu_menude_gormek_icin_oturumu_yenileyin', 'en', 'Refresh your session to see the new service in the menu.'),
                        ('salon.modules.refresh_to_see_service', 'en', 'Refresh your session to see the new service in the menu.'),
                        ('salon.modules.package.loyalty_marketing.flow_note', 'en', 'Client records, after-sales entitlements, and return actions are tracked in the same CRM service.'),
                        ('salon.modules.package.salon_crm.flow_note', 'en', 'Client records, after-sales entitlements, and return actions are tracked in the same CRM service.')
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
                SELECT r."TranslationKeyId", r."LanguageCode", r."Value", NOW(), '20260527144500_SyncSalonCrmTranslationCopy'
                FROM resolved r
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Translations" t
                    WHERE t."TranslationKeyId" = r."TranslationKeyId"
                      AND t."LanguageCode" = r."LanguageCode"
                );

                WITH desired_translations("Key", "LanguageCode", "Value") AS (
                    VALUES
                        ('salon.modules.auto.modul_bilgisi_bulunamadi', 'tr', 'Hizmet bilgisi bulunamadı.'),
                        ('salon.modules.service_not_found', 'tr', 'Hizmet bilgisi bulunamadı.'),
                        ('salon.modules.auto.odeme_sonrasi_modul_hemen_aktif_olacaktir', 'tr', 'Ödeme sonrası hizmet hemen aktif olacaktır.'),
                        ('salon.modules.service_active_after_payment', 'tr', 'Ödeme sonrası hizmet hemen aktif olacaktır.'),
                        ('salon.modules.activated', 'tr', 'Hizmetiniz aktif edildi.'),
                        ('salon.modules.service_activated', 'tr', 'Hizmetiniz aktif edildi.'),
                        ('salon.modules.auto.yeni_modulu_menude_gormek_icin_oturumu_yenileyin', 'tr', 'Yeni hizmeti menüde görmek için oturumu yenileyin.'),
                        ('salon.modules.refresh_to_see_service', 'tr', 'Yeni hizmeti menüde görmek için oturumu yenileyin.'),
                        ('salon.modules.package.loyalty_marketing.flow_note', 'tr', 'Müşteri kaydı, satış sonrası haklar ve geri dönüş işleri aynı CRM hizmetinde takip edilir.'),
                        ('salon.modules.package.salon_crm.flow_note', 'tr', 'Müşteri kaydı, satış sonrası haklar ve geri dönüş işleri aynı CRM hizmetinde takip edilir.'),

                        ('salon.modules.auto.modul_bilgisi_bulunamadi', 'en', 'Service information could not be found.'),
                        ('salon.modules.service_not_found', 'en', 'Service information could not be found.'),
                        ('salon.modules.auto.odeme_sonrasi_modul_hemen_aktif_olacaktir', 'en', 'The service will be active immediately after payment.'),
                        ('salon.modules.service_active_after_payment', 'en', 'The service will be active immediately after payment.'),
                        ('salon.modules.activated', 'en', 'Your service has been activated.'),
                        ('salon.modules.service_activated', 'en', 'Your service has been activated.'),
                        ('salon.modules.auto.yeni_modulu_menude_gormek_icin_oturumu_yenileyin', 'en', 'Refresh your session to see the new service in the menu.'),
                        ('salon.modules.refresh_to_see_service', 'en', 'Refresh your session to see the new service in the menu.'),
                        ('salon.modules.package.loyalty_marketing.flow_note', 'en', 'Client records, after-sales entitlements, and return actions are tracked in the same CRM service.'),
                        ('salon.modules.package.salon_crm.flow_note', 'en', 'Client records, after-sales entitlements, and return actions are tracked in the same CRM service.')
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
                    "UpdatedBy" = '20260527144500_SyncSalonCrmTranslationCopy'
                FROM resolved r
                WHERE t."TranslationKeyId" = r."TranslationKeyId"
                  AND t."LanguageCode" = r."LanguageCode";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only copy migration. We do not restore stale module wording on rollback.
        }
    }
}
