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
        services.AddScoped<IModuleRequestEntityService, ModuleRequestEntityService>();
        services.AddScoped<CallCenter.Api.Services.SalonRolePermissionService>();
        services.AddScoped<CallCenter.Api.Services.PaymentService>();
        services.AddScoped<CallCenter.Api.Services.NotificationService>();
        // Faz 4
        services.AddScoped<IAuditLogEntityService, AuditLogEntityService>();
        services.AddScoped<ITranslationKeyEntityService, TranslationKeyEntityService>();
        services.AddScoped<ICallForwardingRuleEntityService, CallForwardingRuleEntityService>();

        services.AddScoped<IInstantMessageEntityService, InstantMessageEntityService>();
        services.AddScoped<IStorageConfigEntityService, StorageConfigEntityService>();
        services.AddScoped<IIvrEntityService, IvrEntityService>();

        // Billing
        services.AddScoped<IBillingPeriodEntityService, BillingPeriodEntityService>();
        services.AddScoped<ICustomerBillingPeriodModuleLineEntityService, CustomerBillingPeriodModuleLineEntityService>();

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
        services.AddScoped<ISlnTreatmentRecordEntityService, SlnTreatmentRecordEntityService>();
        services.AddScoped<ISlnClientPhotoEntityService, SlnClientPhotoEntityService>();
        services.AddScoped<ISlnServiceCategoryEntityService, SlnServiceCategoryEntityService>();
        services.AddScoped<ISlnServiceEntityService, SlnServiceEntityService>();
        services.AddScoped<ISlnAppointmentEntityService, SlnAppointmentEntityService>();
        services.AddScoped<ISlnProductEntityService, SlnProductEntityService>();
        services.AddScoped<ISlnProductBranchStockEntityService, SlnProductBranchStockEntityService>();
        services.AddScoped<ISlnProductCategoryEntityService, SlnProductCategoryEntityService>();
        services.AddScoped<ISlnProductBrandEntityService, SlnProductBrandEntityService>();
        services.AddScoped<ISlnStockMovementEntityService, SlnStockMovementEntityService>();
        services.AddScoped<ISlnSupplierEntityService, SlnSupplierEntityService>();
        services.AddScoped<ISlnSupplierTransactionEntityService, SlnSupplierTransactionEntityService>();
        services.AddScoped<ISlnSupplierOrderEntityService, SlnSupplierOrderEntityService>();
        services.AddScoped<ISlnInvoiceEntityService, SlnInvoiceEntityService>();
        services.AddScoped<ISlnInvoiceItemEntityService, SlnInvoiceItemEntityService>();
        services.AddScoped<ISlnCashRegisterEntityService, SlnCashRegisterEntityService>();
        services.AddScoped<ISlnCashTransactionEntityService, SlnCashTransactionEntityService>();
        services.AddScoped<ISlnExpenseCategoryEntityService, SlnExpenseCategoryEntityService>();
        services.AddScoped<ISlnExpenseEntityService, SlnExpenseEntityService>();

        services.AddScoped<ISlnPersonnelCommissionEntityService, SlnPersonnelCommissionEntityService>();
        services.AddScoped<ISlnPayrollEntityService, SlnPayrollEntityService>();
        services.AddScoped<ISlnAdvanceEntityService, SlnAdvanceEntityService>();
        services.AddScoped<ISlnPersonnelShiftEntityService, SlnPersonnelShiftEntityService>();
        services.AddScoped<ISlnPersonnelLeaveEntityService, SlnPersonnelLeaveEntityService>();
        services.AddScoped<ISlnPersonnelTimesheetEntityService, SlnPersonnelTimesheetEntityService>();

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

        // Salon — Profil + Public + NoShowPolicy
        services.AddScoped<ISlnSalonProfileEntityService, SlnSalonProfileEntityService>();
        services.AddScoped<ISlnNoShowPolicyEntityService, SlnNoShowPolicyEntityService>();
        services.AddScoped<ISlnPersonnelSkillEntityService, SlnPersonnelSkillEntityService>();
        services.AddScoped<ISlnReviewEntityService, SlnReviewEntityService>();
        services.AddScoped<ISlnMembershipPlanEntityService, SlnMembershipPlanEntityService>();
        services.AddScoped<ISlnClientMembershipEntityService, SlnClientMembershipEntityService>();

        // Payment
        services.AddScoped<IPlatformPaymentConfigEntityService, PlatformPaymentConfigEntityService>();
        services.AddScoped<IPaymentTransactionEntityService, PaymentTransactionEntityService>();

        // Platform
        services.AddScoped<IPlatformUserEntityService, PlatformUserEntityService>();
        services.AddScoped<IPlatformUserSalonEntityService, PlatformUserSalonEntityService>();
        services.AddScoped<IPlatformPushTokenEntityService, PlatformPushTokenEntityService>();

        // Service Pricing
        services.AddScoped<IServicePricingPeriodEntityService, ServicePricingPeriodEntityService>();
        services.AddScoped<IServicePricingItemEntityService, ServicePricingItemEntityService>();

        // Salon — Before/After, Consent, Email Campaign, Waitlist
        services.AddScoped<ISlnBeforeAfterPhotoEntityService, SlnBeforeAfterPhotoEntityService>();
        services.AddScoped<ISlnConsentFormEntityService, SlnConsentFormEntityService>();
        services.AddScoped<ISlnClientConsentEntityService, SlnClientConsentEntityService>();
        services.AddScoped<ISlnEmailCampaignEntityService, SlnEmailCampaignEntityService>();
        services.AddScoped<ISlnWaitlistEntryEntityService, SlnWaitlistEntryEntityService>();

        // Salon — WhatsApp, Winback
        services.AddScoped<ISlnWhatsAppConfigEntityService, SlnWhatsAppConfigEntityService>();
        services.AddScoped<ISlnWhatsAppMessageEntityService, SlnWhatsAppMessageEntityService>();
        services.AddScoped<ISlnWinbackRuleEntityService, SlnWinbackRuleEntityService>();

        // Salon — Personnel Pricing, Revenue Share
        services.AddScoped<ISlnPersonnelServicePriceEntityService, SlnPersonnelServicePriceEntityService>();
        services.AddScoped<ISlnRevenueShareEntityService, SlnRevenueShareEntityService>();

        // Salon — Loyalty
        services.AddScoped<ISlnLoyaltyConfigEntityService, SlnLoyaltyConfigEntityService>();
        services.AddScoped<ISlnClientLoyaltyEntityService, SlnClientLoyaltyEntityService>();
        services.AddScoped<ISlnLoyaltyTransactionEntityService, SlnLoyaltyTransactionEntityService>();

        // Salon — Membership
        services.AddScoped<ISlnMembershipPlanServiceEntityService, SlnMembershipPlanServiceEntityService>();
        services.AddScoped<ISlnMembershipUsageEntityService, SlnMembershipUsageEntityService>();

        // Salon — Sadakat Paketi (Loyalty Package)
        services.AddScoped<ISlnLoyaltyPackageOfferEntityService, SlnLoyaltyPackageOfferEntityService>();
        services.AddScoped<ISlnLoyaltyPackagePurchaseEntityService, SlnLoyaltyPackagePurchaseEntityService>();
        services.AddScoped<ISlnLoyaltyPackageRedemptionEntityService, SlnLoyaltyPackageRedemptionEntityService>();

        // Subscription
        services.AddScoped<ISubscriptionPlanEntityService, SubscriptionPlanEntityService>();
        services.AddScoped<ICustomerSubscriptionEntityService, CustomerSubscriptionEntityService>();

        // Salon — Appointment Service
        services.AddScoped<ISlnAppointmentServiceEntityService, SlnAppointmentServiceEntityService>();
        services.AddScoped<ISlnResourceEntityService, SlnResourceEntityService>();
        services.AddScoped<ISlnServiceResourceRequirementEntityService, SlnServiceResourceRequirementEntityService>();
        services.AddScoped<ISlnServiceComboEntityService, SlnServiceComboEntityService>();
        services.AddScoped<ISlnServiceComboItemEntityService, SlnServiceComboItemEntityService>();

        // Salon — Finance (Cash Closing, Cash Opening, Client Ledger, Invoice Refund)
        services.AddScoped<ISlnCashClosingEntityService, SlnCashClosingEntityService>();
        services.AddScoped<ISlnCashOpeningEntityService, SlnCashOpeningEntityService>();
        services.AddScoped<ISlnClientLedgerEntityService, SlnClientLedgerEntityService>();
        services.AddScoped<ISlnInvoiceRefundEntityService, SlnInvoiceRefundEntityService>();

        return services;
    }
}
