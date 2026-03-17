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
        services.AddScoped<ISipLineEntityService, SipLineEntityService>();
        services.AddScoped<IOrganizationEntityService, OrganizationEntityService>();
        services.AddScoped<ICustomerPortalModuleEntityService, CustomerPortalModuleEntityService>();
        // Faz 4
        services.AddScoped<IAuditLogEntityService, AuditLogEntityService>();
        services.AddScoped<ITranslationKeyEntityService, TranslationKeyEntityService>();
        services.AddScoped<ICallForwardingRuleEntityService, CallForwardingRuleEntityService>();

        services.AddScoped<IInstantMessageEntityService, InstantMessageEntityService>();
        services.AddScoped<IStorageConfigEntityService, StorageConfigEntityService>();
        services.AddScoped<IIvrEntityService, IvrEntityService>();

        // Billing
        services.AddScoped<IBillingPeriodEntityService, BillingPeriodEntityService>();

        // Recording Access Log
        services.AddScoped<IRecordingAccessLogEntityService, RecordingAccessLogEntityService>();

        // Faz 13 — KVKK Uyumluluk
        services.AddScoped<IConsentRecordEntityService, ConsentRecordEntityService>();
        services.AddScoped<IDataSubjectRequestEntityService, DataSubjectRequestEntityService>();
        services.AddScoped<IDataBreachEntityService, DataBreachEntityService>();
        services.AddScoped<IRetentionPolicyEntityService, RetentionPolicyEntityService>();
        services.AddScoped<IDataDestructionLogEntityService, DataDestructionLogEntityService>();
        services.AddScoped<IDataInventoryEntityService, DataInventoryEntityService>();
        services.AddScoped<IPrivacyNoticeEntityService, PrivacyNoticeEntityService>();
        services.AddScoped<ICrossBorderTransferEntityService, CrossBorderTransferEntityService>();

        // Campaign (Gunluk Arama Listesi)
        services.AddScoped<ICampaignEntityService, CampaignEntityService>();
        services.AddScoped<ICampaignContactEntityService, CampaignContactEntityService>();

        // Faz 15 — CRM
        services.AddScoped<ICrmTicketEntityService, CrmTicketEntityService>();
        services.AddScoped<ICrmDealEntityService, CrmDealEntityService>();
        services.AddScoped<ICrmActivityEntityService, CrmActivityEntityService>();
        services.AddScoped<ICrmTaskEntityService, CrmTaskEntityService>();
        services.AddScoped<ICrmSurveyEntityService, CrmSurveyEntityService>();
        services.AddScoped<ICrmSurveyResponseEntityService, CrmSurveyResponseEntityService>();
        services.AddScoped<ICrmTicketCategoryEntityService, CrmTicketCategoryEntityService>();
        services.AddScoped<ICrmTicketCommentEntityService, CrmTicketCommentEntityService>();
        services.AddScoped<ICrmContactTagEntityService, CrmContactTagEntityService>();

        // Faz 14 — Hizmet Abonelik Yönetimi
        services.AddScoped<ICustomerServiceSubscriptionEntityService, CustomerServiceSubscriptionEntityService>();
        services.AddScoped<IServiceBillingItemEntityService, ServiceBillingItemEntityService>();

        // Kalite Yonetimi (Quality Management)
        services.AddScoped<IQualityChecklistEntityService, QualityChecklistEntityService>();
        services.AddScoped<IQualityQuestionEntityService, QualityQuestionEntityService>();
        services.AddScoped<IQualitySubCriteriaEntityService, QualitySubCriteriaEntityService>();
        services.AddScoped<IQualityEvaluationEntityService, QualityEvaluationEntityService>();
        services.AddScoped<IQualityAnswerEntityService, QualityAnswerEntityService>();
        services.AddScoped<IQualityAnswerSubCriteriaSelectionEntityService, QualityAnswerSubCriteriaSelectionEntityService>();
        services.AddScoped<IQualityScoreThresholdEntityService, QualityScoreThresholdEntityService>();

        // Integration & Webhook
        services.AddScoped<IIntegrationConnectionEntityService, IntegrationConnectionEntityService>();
        services.AddScoped<IWebhookSubscriptionEntityService, WebhookSubscriptionEntityService>();
        services.AddScoped<IWebhookDeliveryEntityService, WebhookDeliveryEntityService>();
        services.AddScoped<IIntegrationApiKeyEntityService, IntegrationApiKeyEntityService>();

        return services;
    }
}
