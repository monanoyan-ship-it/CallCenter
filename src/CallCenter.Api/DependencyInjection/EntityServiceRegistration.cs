using CallCenter.Api.EntityServices;
using CallCenter.Api.EntityServices.Interfaces;

namespace CallCenter.Api.DependencyInjection;

public static class EntityServiceRegistration
{
    public static IServiceCollection AddEntityServices(this IServiceCollection services)
    {
        // Faz 1
        services.AddScoped<ISettingEntityService, SettingEntityService>();
        services.AddScoped<IContactEntityService, ContactEntityService>();

        // Faz 2
        services.AddScoped<IUserEntityService, UserEntityService>();
        services.AddScoped<IRefreshTokenEntityService, RefreshTokenEntityService>();
        services.AddScoped<IPasswordHistoryEntityService, PasswordHistoryEntityService>();
        services.AddScoped<ICallRecordEntityService, CallRecordEntityService>();
        services.AddScoped<IQueueEntityService, QueueEntityService>();
        services.AddScoped<ICustomerEntityService, CustomerEntityService>();
        services.AddScoped<ICustomerPersonnelEntityService, CustomerPersonnelEntityService>();

        // Faz 3
        services.AddScoped<ISipAccountEntityService, SipAccountEntityService>();
        services.AddScoped<IOrganizationEntityService, OrganizationEntityService>();
        services.AddScoped<ICustomerPortalModuleEntityService, CustomerPortalModuleEntityService>();
        services.AddScoped<ICustomerPersonnelPermissionEntityService, CustomerPersonnelPermissionEntityService>();

        // Faz 4
        services.AddScoped<IAuditLogEntityService, AuditLogEntityService>();
        services.AddScoped<ITranslationKeyEntityService, TranslationKeyEntityService>();
        services.AddScoped<ICallForwardingRuleEntityService, CallForwardingRuleEntityService>();
        services.AddScoped<IConferenceEntityService, ConferenceEntityService>();
        services.AddScoped<IMonitoringSessionEntityService, MonitoringSessionEntityService>();
        services.AddScoped<IInstantMessageEntityService, InstantMessageEntityService>();
        services.AddScoped<IStorageConfigEntityService, StorageConfigEntityService>();
        services.AddScoped<IIvrEntityService, IvrEntityService>();

        // Billing
        services.AddScoped<IBillingPeriodEntityService, BillingPeriodEntityService>();

        return services;
    }
}
