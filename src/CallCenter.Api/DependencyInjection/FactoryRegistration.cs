using CallCenter.Api.Factories;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Services;

namespace CallCenter.Api.DependencyInjection;

public static class FactoryRegistration
{
    public static IServiceCollection AddFactories(this IServiceCollection services)
    {
        // Faz 1 Factory'ler
        services.AddScoped<ISettingFactory, SettingFactory>();
        services.AddScoped<ICrmContactFactory, CrmContactFactory>();

        // Faz 2 Factory'ler
        services.AddScoped<IAuthFactory, AuthFactory>();
        services.AddScoped<IUserFactory, UserFactory>();
        services.AddScoped<IAgentFactory, AgentFactory>();
        services.AddScoped<ICallFactory, CallFactory>();
        services.AddScoped<IQueueFactory, QueueFactory>();
        services.AddScoped<IPasswordPolicyFactory, PasswordPolicyFactory>();

        // Faz 3 Factory'ler
        services.AddScoped<ICustomerFactory, CustomerFactory>();
        services.AddScoped<IModuleRequestFactory, ModuleRequestFactory>();
        services.AddScoped<ISubscriptionFactory, SubscriptionFactory>();
        services.AddScoped<ServicePricingFactory>();
        services.AddScoped<IPortalFactory, PortalFactory>();
        services.AddScoped<ISipAccountFactory, SipAccountFactory>();
        services.AddScoped<IOrganizationFactory, OrganizationFactory>();
        services.AddScoped<ISupervisorFactory, SupervisorFactory>();

        // Faz 4 Factory'ler
        services.AddScoped<IReportFactory, ReportFactory>();
        services.AddScoped<IAuditFactory, AuditFactory>();
        services.AddScoped<IAuditLogFactory, AuditLogFactory>();
        services.AddScoped<ITranslationFactory, TranslationFactory>();
        services.AddScoped<ICallForwardingFactory, CallForwardingFactory>();

        services.AddScoped<IMessagingFactory, MessagingFactory>();
        services.AddScoped<IProvisioningFactory, ProvisioningFactory>();
        services.AddScoped<ICloudStorageFactory, CloudStorageFactory>();
        services.AddScoped<IIvrFactory, IvrFactory>();

        // Billing
        services.AddScoped<IBillingFactory, BillingFactory>();

        // PBX Service
        services.AddScoped<IPbxFactory, PbxFactory>();

        // Recording Playback
        services.AddScoped<IRecordingPlaybackFactory, RecordingPlaybackFactory>();

        // Campaign (Gunluk Arama Listesi)
        services.AddScoped<ICampaignFactory, CampaignFactory>();

        // Faz 13 — KVKK Uyumluluk
        services.AddScoped<IKvkkFactory, KvkkFactory>();

        // Faz 15 — CRM
        services.AddScoped<ICrmFactory, CrmFactory>();

        // Faz 14 — Hizmet Abonelik Yönetimi
        services.AddScoped<IServiceSubscriptionFactory, ServiceSubscriptionFactory>();

        // Kalite Yonetimi (CrmQuality Management)
        services.AddScoped<ICrmQualityFactory, CrmQualityFactory>();

        // Faz 8 — Entegrasyon (Integration & Webhook)
        services.AddScoped<IIntegrationFactory, IntegrationFactory>();

        // Salon Modulu
        services.AddScoped<ISlnClientFactory, SlnClientFactory>();
        services.AddScoped<ISlnDataImportFactory, SlnDataImportFactory>();
        services.AddScoped<ISlnServiceFactory, SlnServiceFactory>();
        services.AddScoped<ISlnAppointmentFactory, SlnAppointmentFactory>();
        services.AddScoped<ISlnProductFactory, SlnProductFactory>();
        services.AddScoped<ISlnScannerFactory, SlnScannerFactory>();
        services.AddScoped<ISlnFinanceFactory, SlnFinanceFactory>();
        services.AddScoped<ISlnDashboardFactory, SlnDashboardFactory>();

        // Salon S7 — Pazarlama
        services.AddScoped<ISlnMarketingFactory, SlnMarketingFactory>();

        // Salon S8 — Raporlama
        services.AddScoped<ISlnReportFactory, SlnReportFactory>();

        // Salon — WhatsApp
        services.AddScoped<IWhatsAppService, WhatsAppService>();

        // Salon — Uyelik + Sadakat + Hediye Karti + Paketler
        services.AddScoped<ISlnMembershipFactory, SlnMembershipFactory>();
        services.AddScoped<ISlnLoyaltyFactory, SlnLoyaltyFactory>();
        services.AddScoped<ISlnGiftCardFactory, SlnGiftCardFactory>();
        services.AddScoped<ISlnServiceSessionFactory, SlnServiceSessionFactory>();
        services.AddScoped<ISlnLoyaltyPackageFactory, SlnLoyaltyPackageFactory>();
        services.AddScoped<ISlnLoyaltyProgramFactory, SlnLoyaltyProgramFactory>();

        // Salon — Receteler
        services.AddScoped<ISlnRecipeFactory, SlnRecipeFactory>();

        // Salon S9 — Cok Subeli Yonetim
        services.AddScoped<ISlnBranchFactory, SlnBranchFactory>();

        // Salon — No-Show Policy (B5)
        services.AddScoped<ISlnNoShowPolicyFactory, SlnNoShowPolicyFactory>();

        // Salon — Waitlist (C3)
        services.AddScoped<ISlnWaitlistFactory, SlnWaitlistFactory>();

        // Salon — E-posta Entegrasyonu
        services.AddScoped<ISlnEmailIntegrationFactory, SlnEmailIntegrationFactory>();

        // Salon — E-posta Kampanyalari (C5)
        services.AddScoped<ISlnEmailCampaignFactory, SlnEmailCampaignFactory>();

        // Platform Email Taslak Yonetimi
        services.AddScoped<IPlatformEmailTemplateFactory, PlatformEmailTemplateFactory>();

        // Salon — Yorum/Review (C6)
        services.AddScoped<ISlnReviewFactory, SlnReviewFactory>();

        // Salon — Onay Formlari (D1)
        services.AddScoped<ISlnConsentFormFactory, SlnConsentFormFactory>();

        // Salon — Once/Sonra Fotograflari (D2)
        services.AddScoped<ISlnBeforeAfterFactory, SlnBeforeAfterFactory>();

        // Salon — Winback (D3)
        services.AddScoped<ISlnWinbackFactory, SlnWinbackFactory>();

        // Salon — Personel Fiyat + Hasilat Paylasimi (D4+D5)
        services.AddScoped<ISlnPersonnelPriceFactory, SlnPersonnelPriceFactory>();

        // Odeme Gateway Yapilandirma
        services.AddScoped<IPaymentConfigFactory, PaymentConfigFactory>();

        // Salon — Profil + Public
        services.AddScoped<ISlnProfileFactory, SlnProfileFactory>();
        services.AddScoped<ISlnPublicFactory, SlnPublicFactory>();

        // Salon — WhatsApp Factory
        services.AddScoped<ISlnWhatsAppFactory, SlnWhatsAppFactory>();

        // Management (Admin Dashboard + Module Pricing + Module Requests)
        services.AddScoped<IManagementFactory, ManagementFactory>();

        // Platform (Son kullanici: salon uyelik, randevu, sadakat, kesfet)
        services.AddScoped<IPlatformFactory, PlatformFactory>();

        // Platform Auth (kayit, giris, profil)
        services.AddScoped<IPlatformAuthFactory, PlatformAuthFactory>();
        services.AddScoped<IPlatformPushTokenFactory, PlatformPushTokenFactory>();

        return services;
    }
}
