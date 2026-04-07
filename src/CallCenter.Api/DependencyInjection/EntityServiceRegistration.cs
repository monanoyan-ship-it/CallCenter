using CallCenter.Api.EntityServices;
using CallCenter.Api.EntityServices.Interfaces;

namespace CallCenter.Api.DependencyInjection;

public static class EntityServiceRegistration
{
    public static IServiceCollection AddEntityServices(this IServiceCollection services)
    {
        // Faz 1
        services.AddScoped<ISettingEntityService, SettingEntityService>();
        services.AddScoped<ICrmContactEntityService, CrmContactEntityService>();

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
        services.AddScoped<IModulePricingEntityService, ModulePricingEntityService>();
        services.AddScoped<IModuleRequestEntityService, ModuleRequestEntityService>();
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

        // Customer Products
        services.AddScoped<ICustomerProductEntityService, CustomerProductEntityService>();

        // Faz 14 — Hizmet Abonelik Yönetimi
        services.AddScoped<ICustomerServiceSubscriptionEntityService, CustomerServiceSubscriptionEntityService>();
        services.AddScoped<IServiceBillingItemEntityService, ServiceBillingItemEntityService>();

        // Kalite Yonetimi (CrmQuality Management)
        services.AddScoped<ICrmQualityChecklistEntityService, CrmQualityChecklistEntityService>();
        services.AddScoped<ICrmQualityQuestionEntityService, CrmQualityQuestionEntityService>();
        services.AddScoped<ICrmQualitySubCriteriaEntityService, CrmQualitySubCriteriaEntityService>();
        services.AddScoped<ICrmQualityEvaluationEntityService, CrmQualityEvaluationEntityService>();
        services.AddScoped<ICrmQualityAnswerEntityService, CrmQualityAnswerEntityService>();
        services.AddScoped<ICrmQualityAnswerSubCriteriaSelectionEntityService, CrmQualityAnswerSubCriteriaSelectionEntityService>();
        services.AddScoped<ICrmQualityScoreThresholdEntityService, CrmQualityScoreThresholdEntityService>();

        // Email Integration
        services.AddScoped<ICustomerEmailIntegrationEntityService, CustomerEmailIntegrationEntityService>();
        services.AddScoped<IPlatformEmailTemplateEntityService, PlatformEmailTemplateEntityService>();

        // Integration & Webhook
        services.AddScoped<IIntegrationConnectionEntityService, IntegrationConnectionEntityService>();
        services.AddScoped<IWebhookSubscriptionEntityService, WebhookSubscriptionEntityService>();
        services.AddScoped<IWebhookDeliveryEntityService, WebhookDeliveryEntityService>();
        services.AddScoped<IIntegrationApiKeyEntityService, IntegrationApiKeyEntityService>();

        // Salon Modulu
        services.AddScoped<ISlnClientEntityService, SlnClientEntityService>();
        services.AddScoped<ISlnFormulaEntityService, SlnFormulaEntityService>();
        services.AddScoped<ISlnClientPhotoEntityService, SlnClientPhotoEntityService>();
        services.AddScoped<ISlnServiceCategoryEntityService, SlnServiceCategoryEntityService>();
        services.AddScoped<ISlnServiceEntityService, SlnServiceEntityService>();
        services.AddScoped<ISlnAppointmentEntityService, SlnAppointmentEntityService>();
        services.AddScoped<ISlnProductEntityService, SlnProductEntityService>();
        services.AddScoped<ISlnProductCategoryEntityService, SlnProductCategoryEntityService>();
        services.AddScoped<ISlnProductBrandEntityService, SlnProductBrandEntityService>();
        services.AddScoped<ISlnStockMovementEntityService, SlnStockMovementEntityService>();
        services.AddScoped<ISlnSupplierEntityService, SlnSupplierEntityService>();
        services.AddScoped<ISlnInvoiceEntityService, SlnInvoiceEntityService>();
        services.AddScoped<ISlnInvoiceItemEntityService, SlnInvoiceItemEntityService>();
        services.AddScoped<ISlnCashRegisterEntityService, SlnCashRegisterEntityService>();
        services.AddScoped<ISlnCashTransactionEntityService, SlnCashTransactionEntityService>();
        services.AddScoped<ISlnExpenseCategoryEntityService, SlnExpenseCategoryEntityService>();
        services.AddScoped<ISlnExpenseEntityService, SlnExpenseEntityService>();

        services.AddScoped<ISlnPersonnelCommissionEntityService, SlnPersonnelCommissionEntityService>();

        // Salon S7 — Pazarlama
        services.AddScoped<ISlnCampaignEntityService, SlnCampaignEntityService>();
        services.AddScoped<ISlnAutoReminderEntityService, SlnAutoReminderEntityService>();

        // Salon — Hediye Karti
        services.AddScoped<ISlnGiftCardEntityService, SlnGiftCardEntityService>();
        services.AddScoped<ISlnGiftCardTransactionEntityService, SlnGiftCardTransactionEntityService>();

        // Salon — Receteler
        services.AddScoped<ISlnRecipeEntityService, SlnRecipeEntityService>();
        services.AddScoped<ISlnRecipeItemEntityService, SlnRecipeItemEntityService>();

        // Salon S9 — Cok Subeli Yonetim
        services.AddScoped<ISlnBranchEntityService, SlnBranchEntityService>();

        return services;
    }
}
