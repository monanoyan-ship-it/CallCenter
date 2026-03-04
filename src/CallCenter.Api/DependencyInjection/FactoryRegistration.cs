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
        services.AddScoped<IConferenceFactory, ConferenceFactory>();
        services.AddScoped<IMonitoringFactory, MonitoringFactory>();
        services.AddScoped<IMessagingFactory, MessagingFactory>();
        services.AddScoped<IProvisioningFactory, ProvisioningFactory>();
        services.AddScoped<ICloudStorageFactory, CloudStorageFactory>();
        services.AddScoped<IIvrFactory, IvrFactory>();

        return services;
    }
}
