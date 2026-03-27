using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Api.Services.CloudStorage;
using CallCenter.Api.Services.Connectors;
using CallCenter.Shared.Services;
using Google.Cloud.TextToSpeech.V1;

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
        services.AddSingleton<OneDriveOAuthService>();
        services.AddSingleton<GoogleDriveOAuthService>();
        services.AddSingleton<YandexOAuthService>();

        // Scoped utilities
        services.AddScoped<TokenService>();
        services.AddScoped<CallDistributionService>();


        // Webhook Engine
        services.AddSingleton<WebhookEventPublisher>();
        services.AddHostedService<WebhookDispatchService>();

        // Connector Adapters (Platform-specific CRM integrations)
        services.AddScoped<SalesforceConnectorAdapter>();
        services.AddScoped<HubSpotConnectorAdapter>();
        services.AddScoped<ZendeskConnectorAdapter>();
        services.AddScoped<ConnectorFactory>();

        // Text-to-Speech
        var ttsCredPath = configuration["Tts:GoogleCredentialsPath"];
        if (!string.IsNullOrEmpty(ttsCredPath) && File.Exists(ttsCredPath))
        {
            var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(ttsCredPath);
            var ttsClient = new TextToSpeechClientBuilder { GoogleCredential = credential }.Build();
            services.AddSingleton(ttsClient);
        }
        else
        {
            try
            {
                // GCP ortaminda Application Default Credentials kullanilir
                services.AddSingleton(TextToSpeechClient.Create());
            }
            catch
            {
                // Lokal gelistirme: ADC yoksa TTS devre disi
                services.AddSingleton<TextToSpeechClient>(_ => null!);
            }
        }
        services.AddSingleton<ITtsService, GoogleTtsService>();

        // Background Services
        services.AddHostedService<AuditPartitionMaintenanceService>();
        services.AddHostedService<SipLineCleanupService>();

        return services;
    }
}
