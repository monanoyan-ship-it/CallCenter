using CallCenter.Api.Factories;
using CallCenter.Api.Factories.Interfaces;

namespace CallCenter.Api.DependencyInjection;

public static class FactoryRegistration
{
    public static IServiceCollection AddFactories(this IServiceCollection services)
    {
        // Faz 1 Factory'ler
        services.AddScoped<ISettingFactory, SettingFactory>();
        services.AddScoped<IContactFactory, ContactFactory>();

        // Faz 2 Factory'ler
        services.AddScoped<IAuthFactory, AuthFactory>();
        services.AddScoped<IUserFactory, UserFactory>();
        services.AddScoped<IAgentFactory, AgentFactory>();
        services.AddScoped<ICallFactory, CallFactory>();
        services.AddScoped<IQueueFactory, QueueFactory>();
        services.AddScoped<IPasswordPolicyFactory, PasswordPolicyFactory>();

        // Faz 3 Factory'ler
        services.AddScoped<ICustomerFactory, CustomerFactory>();
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

        // Kalite Yonetimi (Quality Management)
        services.AddScoped<IQualityFactory, QualityFactory>();

        return services;
    }
}
