using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Api.Services.CloudStorage;

using CallCenter.Shared.Services;

namespace CallCenter.Api.DependencyInjection;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Singleton utilities
        services.AddSingleton<AesEncryptionService>();
        services.AddSingleton<ITranslationService, TranslationService>();
        services.AddSingleton<CloudStorageFactory>();

        // Scoped utilities
        services.AddScoped<TokenService>();
        services.AddScoped<CallDistributionService>();


        // Background Services
        services.AddHostedService<AuditPartitionMaintenanceService>();
        services.AddHostedService<SipLineCleanupService>();

        return services;
    }
}
