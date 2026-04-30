using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<CallRecord> CallRecords => Set<CallRecord>();
    public DbSet<Queue> Queues => Set<Queue>();
    public DbSet<QueueAgent> QueueAgents => Set<QueueAgent>();
    public DbSet<SipAccount> SipAccounts => Set<SipAccount>();
    public DbSet<SipLine> SipLines => Set<SipLine>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerPersonnel> CustomerPersonnel => Set<CustomerPersonnel>();
    public DbSet<CustomerPortalModule> CustomerPortalModules => Set<CustomerPortalModule>();
    public DbSet<TranslationKey> TranslationKeys => Set<TranslationKey>();
    public DbSet<Translation> Translations => Set<Translation>();
    public DbSet<CustomerOrganizationUnit> CustomerOrganizationUnits => Set<CustomerOrganizationUnit>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<CallForwardingRule> CallForwardingRules => Set<CallForwardingRule>();

    public DbSet<CustomerStorageConfig> CustomerStorageConfigs => Set<CustomerStorageConfig>();
    public DbSet<InstantMessage> InstantMessages => Set<InstantMessage>();
    public DbSet<CrmContact> CrmContacts => Set<CrmContact>();

    // ─── Customer Products ───
    public DbSet<CustomerProduct> CustomerProducts => Set<CustomerProduct>();

    // ─── Billing ───
    public DbSet<CustomerBillingPeriod> CustomerBillingPeriods => Set<CustomerBillingPeriod>();
    public DbSet<CustomerBillingPeriodModuleLine> CustomerBillingPeriodModuleLines => Set<CustomerBillingPeriodModuleLine>();

    // ─── Recording Access Log ───
    public DbSet<RecordingAccessLog> RecordingAccessLogs => Set<RecordingAccessLog>();

    // ─── Personel-Org Junction ───
    public DbSet<CustomerPersonnelOrganizationUnit> CustomerPersonnelOrganizationUnits => Set<CustomerPersonnelOrganizationUnit>();

    // ─── KVKK Uyumluluk ───
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();
    public DbSet<DataSubjectRequest> DataSubjectRequests => Set<DataSubjectRequest>();
    public DbSet<DataBreach> DataBreaches => Set<DataBreach>();
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
    public DbSet<DataDestructionLog> DataDestructionLogs => Set<DataDestructionLog>();
    public DbSet<DataInventoryItem> DataInventoryItems => Set<DataInventoryItem>();
    public DbSet<PrivacyNotice> PrivacyNotices => Set<PrivacyNotice>();
    public DbSet<CrossBorderTransfer> CrossBorderTransfers => Set<CrossBorderTransfer>();

    // ─── Service Subscription ───
    public DbSet<CustomerServiceSubscription> CustomerServiceSubscriptions => Set<CustomerServiceSubscription>();
    public DbSet<ServiceBillingItem> ServiceBillingItems => Set<ServiceBillingItem>();

    // ─── Campaign (Gunluk Arama Listesi) ───
    public DbSet<CallCampaign> CallCampaigns => Set<CallCampaign>();
    public DbSet<CampaignContact> CampaignContacts => Set<CampaignContact>();

    // ─── CRM ───
    public DbSet<CrmTicket> CrmTickets => Set<CrmTicket>();
    public DbSet<CrmTicketCategory> CrmTicketCategories => Set<CrmTicketCategory>();
    public DbSet<CrmTicketComment> CrmTicketComments => Set<CrmTicketComment>();
    public DbSet<CrmDeal> CrmDeals => Set<CrmDeal>();
    public DbSet<CrmActivity> CrmActivities => Set<CrmActivity>();
    public DbSet<CrmTask> CrmTasks => Set<CrmTask>();
    public DbSet<CrmSurvey> CrmSurveys => Set<CrmSurvey>();
    public DbSet<CrmSurveyQuestion> CrmSurveyQuestions => Set<CrmSurveyQuestion>();
    public DbSet<CrmSurveyResponse> CrmSurveyResponses => Set<CrmSurveyResponse>();
    public DbSet<CrmSurveyAnswer> CrmSurveyAnswers => Set<CrmSurveyAnswer>();
    public DbSet<CrmContactTag> CrmContactTags => Set<CrmContactTag>();
    public DbSet<CrmContactTagLink> CrmContactTagLinks => Set<CrmContactTagLink>();

    // ─── IVR & Auto-Attendant ───
    public DbSet<GreetingMessage> GreetingMessages => Set<GreetingMessage>();
    public DbSet<IvrMenu> IvrMenus => Set<IvrMenu>();
    public DbSet<IvrMenuOption> IvrMenuOptions => Set<IvrMenuOption>();
    public DbSet<HoldMusic> HoldMusics => Set<HoldMusic>();
    public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
    public DbSet<Holiday> Holidays => Set<Holiday>();

    // ─── CrmQuality Management (SecretCustomer Adaptasyonu) ───
    public DbSet<CrmQualityChecklist> CrmQualityChecklists => Set<CrmQualityChecklist>();
    public DbSet<CrmQualityQuestion> CrmQualityQuestions => Set<CrmQualityQuestion>();
    public DbSet<CrmQualityQuestionSubCriteria> CrmQualityQuestionSubCriteria => Set<CrmQualityQuestionSubCriteria>();
    public DbSet<CrmQualityEvaluation> CrmQualityEvaluations => Set<CrmQualityEvaluation>();
    public DbSet<CrmQualityAnswer> CrmQualityAnswers => Set<CrmQualityAnswer>();
    public DbSet<CrmQualityAnswerSubCriteriaSelection> CrmQualityAnswerSubCriteriaSelections => Set<CrmQualityAnswerSubCriteriaSelection>();
    public DbSet<CrmQualityScoreThreshold> CrmQualityScoreThresholds => Set<CrmQualityScoreThreshold>();

    // ─── Salon (Sln) ───
    public DbSet<SlnClient> SlnClients => Set<SlnClient>();
    public DbSet<SlnFormula> SlnFormulas => Set<SlnFormula>();
    public DbSet<SlnClientPhoto> SlnClientPhotos => Set<SlnClientPhoto>();
    public DbSet<SlnServiceCategory> SlnServiceCategories => Set<SlnServiceCategory>();
    public DbSet<SlnService> SlnServices => Set<SlnService>();
    public DbSet<SlnPersonnelSkill> SlnPersonnelSkills => Set<SlnPersonnelSkill>();
    public DbSet<SlnPersonnelCommission> SlnPersonnelCommissions => Set<SlnPersonnelCommission>();
    public DbSet<SlnPayroll> SlnPayrolls => Set<SlnPayroll>();
    public DbSet<SlnAdvance> SlnAdvances => Set<SlnAdvance>();
    public DbSet<SlnAppointment> SlnAppointments => Set<SlnAppointment>();
    public DbSet<SlnAppointmentService> SlnAppointmentServices => Set<SlnAppointmentService>();
    public DbSet<SlnProductCategory> SlnProductCategories => Set<SlnProductCategory>();
    public DbSet<SlnProductBrand> SlnProductBrands => Set<SlnProductBrand>();
    public DbSet<SlnProduct> SlnProducts => Set<SlnProduct>();
    public DbSet<SlnSupplier> SlnSuppliers => Set<SlnSupplier>();
    public DbSet<SlnStockMovement> SlnStockMovements => Set<SlnStockMovement>();
    public DbSet<SlnInvoice> SlnInvoices => Set<SlnInvoice>();
    public DbSet<SlnInvoiceItem> SlnInvoiceItems => Set<SlnInvoiceItem>();
    public DbSet<SlnCashRegister> SlnCashRegisters => Set<SlnCashRegister>();
    public DbSet<SlnCashTransaction> SlnCashTransactions => Set<SlnCashTransaction>();
    public DbSet<SlnSupplierTransaction> SlnSupplierTransactions => Set<SlnSupplierTransaction>();
    public DbSet<SlnExpenseCategory> SlnExpenseCategories => Set<SlnExpenseCategory>();
    public DbSet<SlnExpense> SlnExpenses => Set<SlnExpense>();
    public DbSet<SlnBankAccount> SlnBankAccounts => Set<SlnBankAccount>();
    public DbSet<SlnPosDevice> SlnPosDevices => Set<SlnPosDevice>();
    public DbSet<SlnCampaign> SlnCampaigns => Set<SlnCampaign>();
    public DbSet<SlnAutoReminder> SlnAutoReminders => Set<SlnAutoReminder>();
    public DbSet<SlnBranch> SlnBranches => Set<SlnBranch>();
    public DbSet<SlnRecipe> SlnRecipes => Set<SlnRecipe>();
    public DbSet<SlnRecipeItem> SlnRecipeItems => Set<SlnRecipeItem>();
    public DbSet<SlnCashClosing> SlnCashClosings => Set<SlnCashClosing>();
    public DbSet<SlnGiftCard> SlnGiftCards => Set<SlnGiftCard>();
    public DbSet<SlnGiftCardTransaction> SlnGiftCardTransactions => Set<SlnGiftCardTransaction>();
    public DbSet<SlnSalonProfile> SlnSalonProfiles => Set<SlnSalonProfile>();
    public DbSet<SlnWaitlistEntry> SlnWaitlistEntries => Set<SlnWaitlistEntry>();
    public DbSet<SlnWhatsAppConfig> SlnWhatsAppConfigs => Set<SlnWhatsAppConfig>();
    public DbSet<SlnWhatsAppMessage> SlnWhatsAppMessages => Set<SlnWhatsAppMessage>();
    public DbSet<SlnEmailCampaign> SlnEmailCampaigns => Set<SlnEmailCampaign>();
    public DbSet<SlnReview> SlnReviews => Set<SlnReview>();
    public DbSet<SlnReviewRequest> SlnReviewRequests => Set<SlnReviewRequest>();
    public DbSet<SlnConsentForm> SlnConsentForms => Set<SlnConsentForm>();
    public DbSet<SlnClientConsent> SlnClientConsents => Set<SlnClientConsent>();
    public DbSet<SlnBeforeAfterPhoto> SlnBeforeAfterPhotos => Set<SlnBeforeAfterPhoto>();
    public DbSet<SlnWinbackRule> SlnWinbackRules => Set<SlnWinbackRule>();
    public DbSet<SlnPersonnelServicePrice> SlnPersonnelServicePrices => Set<SlnPersonnelServicePrice>();
    public DbSet<SlnRevenueShare> SlnRevenueShares => Set<SlnRevenueShare>();
    public DbSet<SlnNoShowPolicy> SlnNoShowPolicies => Set<SlnNoShowPolicy>();
    public DbSet<SlnMembershipPlan> SlnMembershipPlans => Set<SlnMembershipPlan>();
    public DbSet<SlnClientMembership> SlnClientMemberships => Set<SlnClientMembership>();
    public DbSet<SlnLoyaltyConfig> SlnLoyaltyConfigs => Set<SlnLoyaltyConfig>();
    public DbSet<SlnClientLoyalty> SlnClientLoyalties => Set<SlnClientLoyalty>();
    public DbSet<SlnLoyaltyTransaction> SlnLoyaltyTransactions => Set<SlnLoyaltyTransaction>();
    public DbSet<SlnPackageDefinition> SlnPackageDefinitions => Set<SlnPackageDefinition>();
    public DbSet<SlnClientPackage> SlnClientPackages => Set<SlnClientPackage>();
    public DbSet<SlnPackageUsage> SlnPackageUsages => Set<SlnPackageUsage>();

    public DbSet<SlnMembershipPlanService> SlnMembershipPlanServices => Set<SlnMembershipPlanService>();

    public DbSet<SlnMembershipUsage> SlnMembershipUsages => Set<SlnMembershipUsage>();

    // ─── Salon Finance (ek) ───
    public DbSet<SlnInvoicePayment> SlnInvoicePayments => Set<SlnInvoicePayment>();
    public DbSet<SlnInvoiceRefund> SlnInvoiceRefunds => Set<SlnInvoiceRefund>();
    public DbSet<SlnCashOpening> SlnCashOpenings => Set<SlnCashOpening>();
    public DbSet<SlnClientLedger> SlnClientLedgers => Set<SlnClientLedger>();

    // ─── Email Integration ───
    public DbSet<CustomerEmailIntegration> CustomerEmailIntegrations => Set<CustomerEmailIntegration>();

    // ─── Platform Email ───
    public DbSet<PlatformEmailEvent> PlatformEmailEvents => Set<PlatformEmailEvent>();
    public DbSet<PlatformEmailTemplate> PlatformEmailTemplates => Set<PlatformEmailTemplate>();

    // ─── Service Pricing Periods ───
    public DbSet<ServicePricingPeriod> ServicePricingPeriods => Set<ServicePricingPeriod>();
    public DbSet<ServicePricingItem> ServicePricingItems => Set<ServicePricingItem>();
    public DbSet<ServicePricingBranchDiscountTier> ServicePricingBranchDiscountTiers => Set<ServicePricingBranchDiscountTier>();

    // ─── Subscription ───
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<CustomerSubscription> CustomerSubscriptions => Set<CustomerSubscription>();

    // ─── Payment ───
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PlatformPaymentConfig> PlatformPaymentConfigs => Set<PlatformPaymentConfig>();

    // ─── Platform User ───
    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<PlatformUserSalon> PlatformUserSalons => Set<PlatformUserSalon>();

    // ─── Salon Role Permissions ───
    public DbSet<SalonRolePermission> SalonRolePermissions => Set<SalonRolePermission>();

    // ─── Module Licensing ───
    public DbSet<ModulePricing> ModulePricings => Set<ModulePricing>();
    public DbSet<ModuleRequest> ModuleRequests => Set<ModuleRequest>();

    // ─── Integration & Webhook ───
    public DbSet<IntegrationConnection> IntegrationConnections => Set<IntegrationConnection>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<IntegrationApiKey> IntegrationApiKeys => Set<IntegrationApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Uid).IsUnique();
            e.HasIndex(u => u.UserName).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.UserName).HasMaxLength(50).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(u => u.Extension).HasMaxLength(10);
            e.Property(u => u.PreferredLanguage).HasMaxLength(5);
            e.HasOne(u => u.CustomerPersonnel)
             .WithOne(cp => cp.User)
             .HasForeignKey<CustomerPersonnel>(cp => cp.UserId);
        });

        // Customer
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Uid).IsUnique();
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(c => c.Name);
            e.Property(c => c.TaxNumber).HasMaxLength(20);
            e.Property(c => c.Address).HasMaxLength(500);
            e.Property(c => c.Phone).HasMaxLength(20);
            e.Property(c => c.Email).HasMaxLength(150);
            e.Property(c => c.MaxUsers).HasDefaultValue(1);
            e.Property(c => c.SaveRecordingToPlatform).HasDefaultValue(true);
            e.Property(c => c.SaveRecordingToOwnStorage).HasDefaultValue(false);
        });

        // CustomerProduct
        modelBuilder.Entity<CustomerProduct>(e =>
        {
            e.HasKey(cp => cp.Id);
            e.HasIndex(cp => new { cp.CustomerId, cp.ProductTypeId }).IsUnique();
            e.Property(cp => cp.MonthlyPrice).HasPrecision(18, 2).HasDefaultValue(0m);
            e.HasOne(cp => cp.Customer)
             .WithMany(c => c.Products)
             .HasForeignKey(cp => cp.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CustomerPortalModule (musteriye acik moduller)
        modelBuilder.Entity<CustomerPortalModule>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.CustomerId, m.ModuleId }).IsUnique();
            e.Property(m => m.Notes).HasMaxLength(500);
            e.Property(m => m.MonthlyPrice).HasPrecision(18, 2);
            e.HasOne(m => m.Customer)
             .WithMany(c => c.PortalModules)
             .HasForeignKey(m => m.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ServicePricingPeriod
        modelBuilder.Entity<ServicePricingPeriod>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.ExtraBranchMonthlyPrice).HasPrecision(18, 2);
        });
        modelBuilder.Entity<ServicePricingItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasIndex(i => new { i.PeriodId, i.ProductTypeId, i.ServiceId, i.PackageGroupId }).IsUnique();
            e.Property(i => i.MonthlyPrice).HasPrecision(18, 2);
            e.Property(i => i.PreviousPrice).HasPrecision(18, 2);
            e.Property(i => i.ServiceName).HasMaxLength(200);
            e.HasOne(i => i.Period).WithMany(p => p.Items).HasForeignKey(i => i.PeriodId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ServicePricingBranchDiscountTier>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.DiscountPercent).HasPrecision(5, 2);
            e.HasIndex(t => new { t.PeriodId, t.SortOrder });
            e.HasOne(t => t.Period)
                .WithMany(p => p.BranchDiscountTiers)
                .HasForeignKey(t => t.PeriodId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SubscriptionPlan
        modelBuilder.Entity<SubscriptionPlan>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(100);
            e.Property(p => p.DiscountPercent).HasPrecision(5, 2);
        });

        // CustomerSubscription
        modelBuilder.Entity<CustomerSubscription>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.CustomerId, s.StatusId });
            e.HasIndex(s => s.BranchId);
            e.Property(s => s.MonthlyPrice).HasPrecision(18, 2);
            e.Property(s => s.PeriodPrice).HasPrecision(18, 2);
            e.Property(s => s.DiscountPercentOverride).HasPrecision(5, 2);
            e.HasOne(s => s.Customer).WithMany(c => c.Subscriptions).HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Plan).WithMany().HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.SetNull);
        });

        // PaymentTransaction
        modelBuilder.Entity<PaymentTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Uid).IsUnique();
            e.HasIndex(t => t.ProviderTransactionId);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.Currency).HasMaxLength(5);
            e.Property(t => t.Provider).HasMaxLength(50);
            e.Property(t => t.ProviderTransactionId).HasMaxLength(200);
            e.Property(t => t.ProviderPaymentId).HasMaxLength(200);
            e.Property(t => t.CardLastFour).HasMaxLength(4);
            e.Property(t => t.ErrorMessage).HasMaxLength(1000);
            e.HasOne(t => t.Customer).WithMany().HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.PlatformUser).WithMany().HasForeignKey(t => t.PlatformUserId).OnDelete(DeleteBehavior.SetNull);
        });

        // PlatformPaymentConfig
        modelBuilder.Entity<PlatformPaymentConfig>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Uid).IsUnique();
            e.HasIndex(c => new { c.ProviderTypeId, c.IsActive });
            e.Property(c => c.EncryptedCredentials).HasMaxLength(2000);
            e.Property(c => c.EncryptedBankInfo).HasMaxLength(2000);
            e.Property(c => c.LastTestError).HasMaxLength(1000);
        });

        // SlnRecipe (malzeme bazli recete)
        modelBuilder.Entity<SlnRecipe>(e =>
        {
            e.Property(r => r.EstimatedCost).HasPrecision(18, 2);
        });
        modelBuilder.Entity<SlnRecipeItem>(e =>
        {
            e.Property(i => i.Quantity).HasPrecision(10, 3);
            e.Property(i => i.Cost).HasPrecision(18, 2);
            e.Property(i => i.Unit).HasMaxLength(20);
            e.Property(i => i.Notes).HasMaxLength(500);
            e.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SlnFormula>(e =>
        {
            e.Property(f => f.MaterialCost).HasPrecision(18, 2);
        });

        // SlnMembershipPlan
        modelBuilder.Entity<SlnMembershipPlan>(e =>
        {
            e.Property(p => p.Price).HasPrecision(18, 2);
        });
        modelBuilder.Entity<SlnClientMembership>(e =>
        {
            e.Property(m => m.PaidAmount).HasPrecision(18, 2);
        });

        // SlnMembershipUsage (hizmet bazli kullanim takibi)
        modelBuilder.Entity<SlnMembershipUsage>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => new { u.MembershipId, u.ServiceId, u.PeriodStart }).IsUnique();
        });

        // SlnAppointment prepaid
        modelBuilder.Entity<SlnAppointment>(e =>
        {
            e.Property(a => a.PrepaidAmount).HasPrecision(18, 2);
        });

        // SlnClient (ayni salonda ayni telefon mukerrer olamaz)
        modelBuilder.Entity<SlnClient>(e =>
        {
            e.HasIndex(c => new { c.CustomerId, c.Phone }).IsUnique().HasFilter("\"Phone\" IS NOT NULL");
            e.Property(c => c.Phone).HasMaxLength(20);
            e.Property(c => c.Phone2).HasMaxLength(20);
        });

        // SlnMembershipPlanService (plan-hizmet iliskisi)
        modelBuilder.Entity<SlnMembershipPlanService>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.PlanId, s.ServiceId }).IsUnique();
            e.HasOne(s => s.Plan).WithMany(p => p.Services).HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Service).WithMany().HasForeignKey(s => s.ServiceId).OnDelete(DeleteBehavior.Cascade);
        });

        // SlnInvoicePayment (karma odeme)
        modelBuilder.Entity<SlnInvoicePayment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.HasOne(p => p.Invoice).WithMany(i => i.Payments).HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        // SlnInvoiceRefund (iade)
        modelBuilder.Entity<SlnInvoiceRefund>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.RefundAmount).HasPrecision(18, 2);
            e.HasOne(r => r.Invoice).WithMany().HasForeignKey(r => r.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        // SlnCashOpening (kasa acilis)
        modelBuilder.Entity<SlnCashOpening>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.OpeningBalance).HasPrecision(18, 2);
            e.HasIndex(o => new { o.RegisterId, o.OpeningDate }).IsUnique();
        });

        // SlnClientLedger (cari hesap)
        modelBuilder.Entity<SlnClientLedger>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Amount).HasPrecision(18, 2);
            e.Property(l => l.RunningBalance).HasPrecision(18, 2);
            e.HasIndex(l => new { l.CustomerId, l.SlnClientId });
        });

        // SlnInvoice ek decimal precision
        modelBuilder.Entity<SlnInvoice>(e =>
        {
            e.Property(i => i.TaxAmount).HasPrecision(18, 2);
            e.Property(i => i.GrandTotal).HasPrecision(18, 2);
        });

        // SlnInvoiceItem ek decimal precision
        modelBuilder.Entity<SlnInvoiceItem>(e =>
        {
            e.Property(i => i.TaxRate).HasPrecision(5, 2);
            e.Property(i => i.TaxAmount).HasPrecision(18, 2);
        });

        // SlnExpense ek alanlar
        modelBuilder.Entity<SlnExpense>(e =>
        {
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.DocumentRef).HasMaxLength(200);
        });

        // SlnService/Product tax rate
        modelBuilder.Entity<SlnService>(e =>
        {
            e.Property(s => s.TaxRate).HasPrecision(5, 2);
        });
        modelBuilder.Entity<SlnProduct>(e =>
        {
            e.Property(p => p.TaxRate).HasPrecision(5, 2);
        });

        // PlatformUser
        modelBuilder.Entity<PlatformUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Uid).IsUnique();
            e.HasIndex(u => u.Phone).IsUnique();
            e.HasIndex(u => u.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
            e.Property(u => u.FullName).HasMaxLength(200);
            e.Property(u => u.Phone).HasMaxLength(20);
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.PreferredLanguage).HasMaxLength(5);
        });

        // PlatformUserSalon
        modelBuilder.Entity<PlatformUserSalon>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.PlatformUserId, s.CustomerId }).IsUnique();
            e.HasOne(s => s.PlatformUser)
             .WithMany(u => u.Salons)
             .HasForeignKey(s => s.PlatformUserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Customer)
             .WithMany()
             .HasForeignKey(s => s.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.SlnClient)
             .WithMany()
             .HasForeignKey(s => s.SlnClientId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // SalonRolePermission (merkezi rol-sayfa izinleri)
        modelBuilder.Entity<SalonRolePermission>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.RoleId, p.PageName }).IsUnique();
            e.Property(p => p.PageName).HasMaxLength(100);
        });

        // ModulePricing (modul katalog fiyatlari)
        modelBuilder.Entity<ModulePricing>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.ModuleId).IsUnique();
            e.Property(p => p.MonthlyPrice).HasPrecision(18, 2);
        });

        // ModuleRequest (firma modul talepleri)
        modelBuilder.Entity<ModuleRequest>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Uid).IsUnique();
            e.HasIndex(r => new { r.CustomerId, r.ModuleId, r.StatusId });
            e.Property(r => r.RequestNotes).HasMaxLength(1000);
            e.Property(r => r.AdminNotes).HasMaxLength(1000);
            e.HasOne(r => r.Customer)
             .WithMany(c => c.ModuleRequests)
             .HasForeignKey(r => r.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.RequestedByPersonnel)
             .WithMany()
             .HasForeignKey(r => r.RequestedByPersonnelId)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(r => r.ReviewedByUser)
             .WithMany()
             .HasForeignKey(r => r.ReviewedByUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // CustomerOrganizationUnit (organizasyon hiyerarsisi)
        modelBuilder.Entity<CustomerOrganizationUnit>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.Uid).IsUnique();
            e.Property(o => o.Name).HasMaxLength(200).IsRequired();
            e.Property(o => o.Code).HasMaxLength(50);
            e.Property(o => o.Address).HasMaxLength(500);
            e.Property(o => o.Phone).HasMaxLength(20);
            e.Property(o => o.Email).HasMaxLength(150);
            e.HasIndex(o => new { o.CustomerId, o.Name, o.ParentId }).IsUnique();
            e.HasOne(o => o.Customer)
             .WithMany(c => c.OrganizationUnits)
             .HasForeignKey(o => o.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            // Self-reference: ust birim
            e.HasOne(o => o.Parent)
             .WithMany(o => o.Children)
             .HasForeignKey(o => o.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // CustomerPersonnelOrganizationUnit (junction: personel-org coka-cok)
        modelBuilder.Entity<CustomerPersonnelOrganizationUnit>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PersonnelId, x.OrganizationUnitId }).IsUnique();
            e.HasOne(x => x.Personnel)
             .WithMany(p => p.OrganizationUnits)
             .HasForeignKey(x => x.PersonnelId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.OrganizationUnit)
             .WithMany(o => o.PersonnelAssignments)
             .HasForeignKey(x => x.OrganizationUnitId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CustomerPersonnel
        modelBuilder.Entity<CustomerPersonnel>(e =>
        {
            e.HasKey(cp => cp.Id);
            e.HasIndex(cp => cp.Uid).IsUnique();
            e.Property(cp => cp.Title).HasMaxLength(100).IsRequired();
            e.HasOne(cp => cp.Customer)
             .WithMany(c => c.Personnel)
             .HasForeignKey(cp => cp.CustomerId);
            // Organizasyon birimi (opsiyonel)
            e.HasOne(cp => cp.OrganizationUnit)
             .WithMany(o => o.Personnel)
             .HasForeignKey(cp => cp.OrganizationUnitId)
             .OnDelete(DeleteBehavior.SetNull);
            // Ust yonetici (self-reference)
            e.HasOne(cp => cp.ReportsToPersonnel)
             .WithMany(cp => cp.Subordinates)
             .HasForeignKey(cp => cp.ReportsToPersonnelId)
             .OnDelete(DeleteBehavior.SetNull);
            // Şube (opsiyonel)
            e.HasOne(cp => cp.Branch)
             .WithMany(b => b.Personnel)
             .HasForeignKey(cp => cp.BranchId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // CallRecord
        modelBuilder.Entity<CallRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Uid).IsUnique();
            e.Property(c => c.CallerNumber).HasMaxLength(50).IsRequired();
            e.Property(c => c.CalleeNumber).HasMaxLength(50).IsRequired();
            e.HasOne(c => c.Agent).WithMany(u => u.CallRecords).HasForeignKey(c => c.AgentId);
            e.HasOne(c => c.Queue).WithMany(q => q.CallRecords).HasForeignKey(c => c.QueueId);
            e.HasOne(c => c.Customer).WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.ConsentRecord).WithMany().HasForeignKey(c => c.ConsentRecordId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.PrivacyNotice).WithMany().HasForeignKey(c => c.PrivacyNoticeId).OnDelete(DeleteBehavior.SetNull);

            // Callback Yonetimi
            e.HasOne(c => c.CallbackAssignedTo).WithMany().HasForeignKey(c => c.CallbackAssignedToId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.CallbackResultCall).WithMany().HasForeignKey(c => c.CallbackResultCallId).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(c => c.StartedAt);
            e.HasIndex(c => new { c.CallbackAssignedToId, c.CallbackStatusId }).HasFilter("\"CallbackStatusId\" IS NOT NULL");
            });

        // Queue
        modelBuilder.Entity<Queue>(e =>
        {
            e.HasKey(q => q.Id);
            e.HasIndex(q => q.Uid).IsUnique();
            e.Property(q => q.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(q => new { q.CustomerId, q.Name }).IsUnique();
            e.HasOne(q => q.Customer)
             .WithMany(c => c.Queues)
             .HasForeignKey(q => q.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(q => q.OrganizationUnit)
             .WithMany(o => o.Queues)
             .HasForeignKey(q => q.OrganizationUnitId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // QueueAgent (many-to-many)
        modelBuilder.Entity<QueueAgent>(e =>
        {
            e.HasKey(qa => new { qa.QueueId, qa.AgentId });
            e.HasOne(qa => qa.Queue).WithMany(q => q.QueueAgents).HasForeignKey(qa => qa.QueueId);
            e.HasOne(qa => qa.Agent).WithMany().HasForeignKey(qa => qa.AgentId);
        });

        // SipAccount (Gateway)
        modelBuilder.Entity<SipAccount>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Uid).IsUnique();
            e.Property(s => s.Name).HasMaxLength(100).IsRequired();
            e.Property(s => s.Server).HasMaxLength(200).IsRequired();
            e.Property(s => s.Domain).HasMaxLength(200);
            e.Property(s => s.Transport).HasMaxLength(10);
            e.Property(s => s.StunServer).HasMaxLength(200);
            e.Property(s => s.TurnServer).HasMaxLength(200);
            e.Property(s => s.TurnUsername).HasMaxLength(100);
            e.Property(s => s.TurnPassword).HasMaxLength(512);
            e.HasOne(s => s.Customer)
             .WithMany(c => c.SipAccounts)
             .HasForeignKey(s => s.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.OrganizationUnit)
             .WithMany(o => o.SipAccounts)
             .HasForeignKey(s => s.OrganizationUnitId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // SipLine (Hat)
        modelBuilder.Entity<SipLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Username).HasMaxLength(100).IsRequired();
            e.Property(l => l.Password).HasMaxLength(512).IsRequired();
            e.Property(l => l.Description).HasMaxLength(200);
            e.HasOne(l => l.SipAccount)
             .WithMany(s => s.Lines)
             .HasForeignKey(l => l.SipAccountId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // TranslationKey
        modelBuilder.Entity<TranslationKey>(e =>
        {
            e.HasKey(tk => tk.Id);
            e.Property(tk => tk.Key).HasMaxLength(200).IsRequired();
            e.HasIndex(tk => tk.Key).IsUnique();
            e.Property(tk => tk.Module).HasMaxLength(50).IsRequired();
            e.Property(tk => tk.Description).HasMaxLength(500);
        });

        // Translation
        modelBuilder.Entity<Translation>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Value).IsRequired();
            e.Property(t => t.LanguageCode).HasMaxLength(5).IsRequired();
            e.Property(t => t.UpdatedBy).HasMaxLength(100);
            e.HasOne(t => t.TranslationKey).WithMany(tk => tk.Translations).HasForeignKey(t => t.TranslationKeyId);
            e.HasIndex(t => new { t.TranslationKeyId, t.LanguageCode }).IsUnique();
        });

        // SystemSetting
        modelBuilder.Entity<SystemSetting>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Key).HasMaxLength(100).IsRequired();
            e.HasIndex(s => s.Key).IsUnique();
            e.Property(s => s.Value).IsRequired();
            e.Property(s => s.Group).HasMaxLength(50).IsRequired();
            e.Property(s => s.ValueType).HasMaxLength(20).IsRequired();
            e.Property(s => s.Description).HasMaxLength(500);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.Property(rt => rt.Token).HasMaxLength(256).IsRequired();
            e.HasIndex(rt => rt.Token).IsUnique();
            e.HasIndex(rt => rt.UserId);
            e.HasOne(rt => rt.User)
             .WithMany()
             .HasForeignKey(rt => rt.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            // Computed kolonlar EF'e bildirilir (DB'de kolon yok)
            e.Ignore(rt => rt.IsExpired);
            e.Ignore(rt => rt.IsRevoked);
            e.Ignore(rt => rt.IsActive);
        });

        // PasswordHistory (sifre tekrar kullanim engelleme)
        modelBuilder.Entity<PasswordHistory>(e =>
        {
            e.HasKey(ph => ph.Id);
            e.Property(ph => ph.PasswordHash).HasMaxLength(256).IsRequired();
            e.HasIndex(ph => new { ph.UserId, ph.CreatedAt });
            e.HasOne(ph => ph.User)
             .WithMany(u => u.PasswordHistories)
             .HasForeignKey(ph => ph.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog (KVKK / BDDK uyumlu denetim kaydi)
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Category).HasMaxLength(50).IsRequired();
            e.Property(a => a.Action).HasMaxLength(50).IsRequired();
            e.Property(a => a.UserName).HasMaxLength(100);
            e.Property(a => a.EntityType).HasMaxLength(100);
            e.Property(a => a.EntityId).HasMaxLength(50);
            e.Property(a => a.Description).HasMaxLength(1000).IsRequired();
            e.Property(a => a.IpAddress).HasMaxLength(50);
            e.Property(a => a.UserAgent).HasMaxLength(500);
            // Performans: sik sorgulanan kolonlara index
            e.HasIndex(a => a.CreatedAt);
            e.HasIndex(a => a.Category);
            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.CustomerId);
            e.HasIndex(a => new { a.EntityType, a.EntityId });
            // FK YOK — PostgreSQL partitioned tablolarda FK desteklenmiyor
            // UserId ve CustomerId sadece bilgi amacli (snapshot), referential integrity gerekmiyor
            e.Ignore(a => a.User);
            e.Ignore(a => a.Customer);
        });

        // CallForwardingRule (arama yonlendirme kurali)
        modelBuilder.Entity<CallForwardingRule>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => f.Uid).IsUnique();
            e.Property(f => f.Destination).HasMaxLength(200).IsRequired();
            e.Property(f => f.Description).HasMaxLength(500);
            e.HasIndex(f => new { f.UserId, f.ForwardType, f.IsActive });
            e.HasOne(f => f.User)
             .WithMany(u => u.CallForwardingRules)
             .HasForeignKey(f => f.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Customer)
             .WithMany()
             .HasForeignKey(f => f.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);
        });


        // CustomerStorageConfig (bulut depolama yapilandirmasi)
        modelBuilder.Entity<CustomerStorageConfig>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Uid).IsUnique();
            e.Property(c => c.EncryptedCredentials).IsRequired();
            e.Property(c => c.BasePath).HasMaxLength(500);
            e.Property(c => c.LastTestError).HasMaxLength(2000);
            // Musteri basina tek default config
            e.HasIndex(c => new { c.CustomerId, c.IsDefault })
             .HasFilter("\"IsDefault\" = true")
             .IsUnique();
            e.HasOne(c => c.Customer)
             .WithMany(c => c.StorageConfigs)
             .HasForeignKey(c => c.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // InstantMessage (anlik mesajlasma)
        modelBuilder.Entity<InstantMessage>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.Uid).IsUnique();
            e.Property(m => m.Content).IsRequired();
            e.HasIndex(m => new { m.SenderUserId, m.ReceiverUserId, m.SentAt });
            e.HasIndex(m => new { m.ReceiverUserId, m.IsRead }); // Okunmamis mesajlar icin
            e.HasOne(m => m.SenderUser)
             .WithMany()
             .HasForeignKey(m => m.SenderUserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.ReceiverUser)
             .WithMany()
             .HasForeignKey(m => m.ReceiverUserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Customer)
             .WithMany()
             .HasForeignKey(m => m.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // CrmContact (rehber)
        modelBuilder.Entity<CrmContact>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Uid).IsUnique();
            e.Property(c => c.FullName).IsRequired().HasMaxLength(200);
            e.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(50);
            e.HasIndex(c => new { c.OwnerUserId, c.FullName });
            e.HasIndex(c => c.PhoneNumber);
            e.HasOne(c => c.OwnerUser)
             .WithMany()
             .HasForeignKey(c => c.OwnerUserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Customer)
             .WithMany()
             .HasForeignKey(c => c.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // CallCampaign (gunluk arama listesi)
        modelBuilder.Entity<CallCampaign>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Uid).IsUnique();
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(c => new { c.CustomerId, c.StatusId });
            e.HasOne(c => c.Customer)
             .WithMany()
             .HasForeignKey(c => c.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.CreatedByPersonnel)
             .WithMany()
             .HasForeignKey(c => c.CreatedByPersonnelId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // CampaignContact (kampanya kisi kaydi)
        modelBuilder.Entity<CampaignContact>(e =>
        {
            e.HasKey(cc => cc.Id);
            e.HasIndex(cc => new { cc.CampaignId, cc.CrmContactId }).IsUnique();
            e.HasIndex(cc => new { cc.AssignedPersonnelId, cc.StatusId });
            e.HasOne(cc => cc.Campaign)
             .WithMany(c => c.CampaignContacts)
             .HasForeignKey(cc => cc.CampaignId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cc => cc.CrmContact)
             .WithMany()
             .HasForeignKey(cc => cc.CrmContactId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cc => cc.AssignedPersonnel)
             .WithMany()
             .HasForeignKey(cc => cc.AssignedPersonnelId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(cc => cc.CallRecord)
             .WithMany()
             .HasForeignKey(cc => cc.CallRecordId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // GreetingMessage (sesli karsilama)
        modelBuilder.Entity<GreetingMessage>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => g.Uid).IsUnique();
            e.Property(g => g.Name).IsRequired().HasMaxLength(200);
            e.Property(g => g.Type).IsRequired().HasMaxLength(50);
            e.Property(g => g.AudioFilePath).IsRequired().HasMaxLength(500);
            e.Property(g => g.AudioFileName).HasMaxLength(200);
            e.HasIndex(g => new { g.CustomerId, g.Type });
            e.HasOne(g => g.Customer)
             .WithMany()
             .HasForeignKey(g => g.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // IvrMenu
        modelBuilder.Entity<IvrMenu>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.Uid).IsUnique();
            e.Property(m => m.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(m => new { m.CustomerId, m.Name }).IsUnique();
            e.HasOne(m => m.Customer)
             .WithMany()
             .HasForeignKey(m => m.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.GreetingMessage)
             .WithMany()
             .HasForeignKey(m => m.GreetingMessageId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // IvrMenuOption
        modelBuilder.Entity<IvrMenuOption>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Digit).IsRequired().HasMaxLength(2);
            e.Property(o => o.ActionType).IsRequired().HasMaxLength(50);
            e.Property(o => o.TargetExtension).HasMaxLength(50);
            e.Property(o => o.Label).HasMaxLength(100);
            e.HasIndex(o => new { o.IvrMenuId, o.Digit }).IsUnique();
            e.HasOne(o => o.IvrMenu)
             .WithMany(m => m.Options)
             .HasForeignKey(o => o.IvrMenuId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(o => o.TargetQueue)
             .WithMany()
             .HasForeignKey(o => o.TargetQueueId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(o => o.TargetIvrMenu)
             .WithMany()
             .HasForeignKey(o => o.TargetIvrMenuId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(o => o.TargetGreetingMessage)
             .WithMany()
             .HasForeignKey(o => o.TargetGreetingMessageId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // HoldMusic (bekleme muzigi)
        modelBuilder.Entity<HoldMusic>(e =>
        {
            e.HasKey(h => h.Id);
            e.HasIndex(h => h.Uid).IsUnique();
            e.Property(h => h.Name).IsRequired().HasMaxLength(200);
            e.Property(h => h.AudioFilePath).IsRequired().HasMaxLength(500);
            e.Property(h => h.AudioFileName).HasMaxLength(200);
            e.HasIndex(h => new { h.CustomerId, h.QueueId });
            e.HasOne(h => h.Customer)
             .WithMany()
             .HasForeignKey(h => h.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(h => h.Queue)
             .WithMany()
             .HasForeignKey(h => h.QueueId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // BusinessHours (mesai saatleri)
        modelBuilder.Entity<BusinessHours>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.CustomerId, b.DayOfWeek }).IsUnique();
            e.HasOne(b => b.Customer)
             .WithMany()
             .HasForeignKey(b => b.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Holiday (tatil takvimi)
        modelBuilder.Entity<Holiday>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(h => new { h.CustomerId, h.Date });
            e.HasOne(h => h.Customer)
             .WithMany()
             .HasForeignKey(h => h.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(h => h.GreetingMessage)
             .WithMany()
             .HasForeignKey(h => h.GreetingMessageId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // RecordingAccessLog (KVKK/BTK kayit dinleme denetim logu)
        modelBuilder.Entity<RecordingAccessLog>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.AccessedByUserName).HasMaxLength(100);
            e.Property(r => r.IpAddress).HasMaxLength(50);
            e.Property(r => r.UserAgent).HasMaxLength(500);
            e.Property(r => r.FailureReason).HasMaxLength(500);
            e.HasIndex(r => r.CallRecordId);
            e.HasIndex(r => r.AccessedByUserId);
            e.HasIndex(r => r.AccessedAt);
            e.HasIndex(r => r.CustomerId);
            e.HasOne(r => r.CallRecord)
             .WithMany()
             .HasForeignKey(r => r.CallRecordId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CustomerBillingPeriod (faturalama donemi)
        modelBuilder.Entity<CustomerBillingPeriod>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.CustomerId, b.Year, b.Month, b.BillingKindId }).IsUnique();
            e.Property(b => b.UnitPrice).HasPrecision(18, 2);
            e.Property(b => b.Amount).HasPrecision(18, 2);
            e.Property(b => b.ServiceAmount).HasPrecision(18, 2);
            e.Property(b => b.Notes).HasMaxLength(1000);
            e.HasOne(b => b.Customer)
             .WithMany(c => c.BillingPeriods)
             .HasForeignKey(b => b.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerBillingPeriodModuleLine>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ModuleDisplayName).HasMaxLength(200).IsRequired();
            e.Property(x => x.MonthlyUnitPrice).HasPrecision(18, 2);
            e.Property(x => x.LineAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.CustomerBillingPeriodId);
            e.HasIndex(x => x.PackageGroupId);
            e.HasOne(x => x.CustomerBillingPeriod)
                .WithMany(b => b.ModuleLines)
                .HasForeignKey(x => x.CustomerBillingPeriodId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CustomerPortalModule)
                .WithMany()
                .HasForeignKey(x => x.CustomerPortalModuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ═══════════════════════════════════════════════════════════════
        // KVKK UYUMLULUK ENTITY'LERİ
        // ═══════════════════════════════════════════════════════════════

        // ConsentRecord (KVKK riza kaydi)
        modelBuilder.Entity<ConsentRecord>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Uid).IsUnique();
            e.Property(c => c.SubjectIdentifier).IsRequired().HasMaxLength(200);
            e.Property(c => c.SubjectName).IsRequired().HasMaxLength(200);
            e.Property(c => c.ConsentMethod).IsRequired().HasMaxLength(50);
            e.Property(c => c.LegalBasis).HasMaxLength(500);
            e.Property(c => c.PrivacyNoticeVersion).HasMaxLength(50);
            e.Property(c => c.RevokedBy).HasMaxLength(200);
            e.Property(c => c.Notes).HasMaxLength(1000);
            e.HasIndex(c => new { c.CustomerId, c.ConsentTypeId });
            e.HasIndex(c => c.SubjectIdentifier);
            e.HasOne(c => c.Customer)
             .WithMany()
             .HasForeignKey(c => c.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PrivacyNotice (KVKK aydinlatma metni)
        modelBuilder.Entity<PrivacyNotice>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Uid).IsUnique();
            e.Property(p => p.Title).IsRequired().HasMaxLength(300);
            e.Property(p => p.Content).IsRequired();
            e.Property(p => p.Version).IsRequired().HasMaxLength(50);
            e.Property(p => p.ApprovedBy).HasMaxLength(200);
            e.HasIndex(p => new { p.CustomerId, p.TypeId, p.IsActive });
            e.HasOne(p => p.Customer)
             .WithMany()
             .HasForeignKey(p => p.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.GreetingMessage)
             .WithMany()
             .HasForeignKey(p => p.GreetingMessageId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // CrossBorderTransfer (KVKK yurt disi aktarim)
        modelBuilder.Entity<CrossBorderTransfer>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Uid).IsUnique();
            e.Property(t => t.RecipientName).IsRequired().HasMaxLength(300);
            e.Property(t => t.RecipientCountry).IsRequired().HasMaxLength(100);
            e.Property(t => t.DataCategories).IsRequired().HasMaxLength(1000);
            e.Property(t => t.Purpose).IsRequired().HasMaxLength(500);
            e.Property(t => t.LegalBasis).IsRequired().HasMaxLength(500);
            e.Property(t => t.Notes).HasMaxLength(2000);
            e.Property(t => t.CreatedByUserName).HasMaxLength(200);
            e.HasIndex(t => new { t.CustomerId, t.IsActive });
            e.HasOne(t => t.Customer)
             .WithMany()
             .HasForeignKey(t => t.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // DataSubjectRequest (KVKK ilgili kisi basvurusu)
        modelBuilder.Entity<DataSubjectRequest>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.Uid).IsUnique();
            e.Property(d => d.RequesterName).IsRequired().HasMaxLength(200);
            e.Property(d => d.RequesterIdentifier).IsRequired().HasMaxLength(200);
            e.Property(d => d.RequesterContact).IsRequired().HasMaxLength(500);
            e.Property(d => d.RequestDescription).IsRequired().HasMaxLength(2000);
            e.Property(d => d.ResponseDescription).HasMaxLength(2000);
            e.Property(d => d.AssignedToUserName).HasMaxLength(200);
            e.Property(d => d.RejectionReason).HasMaxLength(1000);
            e.HasIndex(d => new { d.CustomerId, d.StatusId });
            e.HasIndex(d => d.Deadline);
            e.HasOne(d => d.Customer)
             .WithMany()
             .HasForeignKey(d => d.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // DataBreach (KVKK veri ihlali)
        modelBuilder.Entity<DataBreach>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => b.Uid).IsUnique();
            e.Property(b => b.Title).IsRequired().HasMaxLength(300);
            e.Property(b => b.Description).IsRequired().HasMaxLength(5000);
            e.Property(b => b.AffectedDataCategories).HasMaxLength(1000);
            e.Property(b => b.CauseSummary).HasMaxLength(2000);
            e.Property(b => b.MeasuresTaken).HasMaxLength(2000);
            e.Property(b => b.ReportedByUserName).HasMaxLength(200);
            e.HasIndex(b => b.StatusId);
            e.HasIndex(b => b.NotificationDeadline);
            e.HasOne(b => b.Customer)
             .WithMany()
             .HasForeignKey(b => b.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // RetentionPolicy (KVKK saklama politikasi)
        modelBuilder.Entity<RetentionPolicy>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.CategoryName).IsRequired().HasMaxLength(200);
            e.Property(r => r.LegalBasis).IsRequired().HasMaxLength(500);
            e.Property(r => r.Description).HasMaxLength(1000);
            e.HasIndex(r => new { r.CustomerId, r.CategoryId }).IsUnique();
            e.HasOne(r => r.Customer)
             .WithMany()
             .HasForeignKey(r => r.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // DataDestructionLog (KVKK veri imha kaydi)
        modelBuilder.Entity<DataDestructionLog>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.DataCategory).IsRequired().HasMaxLength(200);
            e.Property(d => d.Description).IsRequired().HasMaxLength(2000);
            e.Property(d => d.LegalBasis).HasMaxLength(500);
            e.Property(d => d.ApprovedByUserName).IsRequired().HasMaxLength(200);
            e.HasIndex(d => d.CustomerId);
            e.HasIndex(d => d.DestroyedAt);
            e.HasOne(d => d.Customer)
             .WithMany()
             .HasForeignKey(d => d.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // DataInventoryItem (KVKK veri envanteri)
        modelBuilder.Entity<DataInventoryItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.DataCategory).IsRequired().HasMaxLength(200);
            e.Property(i => i.Purpose).IsRequired().HasMaxLength(500);
            e.Property(i => i.LegalBasis).IsRequired().HasMaxLength(500);
            e.Property(i => i.DataSubjectGroup).IsRequired().HasMaxLength(200);
            e.Property(i => i.RecipientGroup).HasMaxLength(200);
            e.Property(i => i.TransferCountry).HasMaxLength(100);
            e.Property(i => i.SecurityMeasures).IsRequired().HasMaxLength(1000);
            e.Property(i => i.VerbisRegistrationNo).HasMaxLength(50);
            e.HasIndex(i => i.CustomerId);
            e.HasOne(i => i.Customer)
             .WithMany()
             .HasForeignKey(i => i.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ═══════════════════════════════════════════════════════════════
        // CRM ENTITY'LERİ
        // ═══════════════════════════════════════════════════════════════

        // CrmTicket (destek talebi)
        modelBuilder.Entity<CrmTicket>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Uid).IsUnique();
            e.Property(t => t.Subject).IsRequired().HasMaxLength(300);
            e.Property(t => t.Description).HasMaxLength(5000);
            e.HasIndex(t => new { t.CustomerId, t.StatusId });
            e.HasIndex(t => t.AssignedToPersonnelId);
            e.HasOne(t => t.CrmContact)
             .WithMany()
             .HasForeignKey(t => t.CrmContactId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.AssignedToPersonnel)
             .WithMany()
             .HasForeignKey(t => t.AssignedToPersonnelId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.CreatedByPersonnel)
             .WithMany()
             .HasForeignKey(t => t.CreatedByPersonnelId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Customer)
             .WithMany()
             .HasForeignKey(t => t.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CrmDeal (satis firsati)
        modelBuilder.Entity<CrmDeal>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.Uid).IsUnique();
            e.Property(d => d.Title).IsRequired().HasMaxLength(300);
            e.Property(d => d.Value).HasPrecision(18, 2);
            e.Property(d => d.Notes).HasMaxLength(5000);
            e.HasIndex(d => new { d.CustomerId, d.StageId });
            e.HasOne(d => d.CrmContact)
             .WithMany()
             .HasForeignKey(d => d.CrmContactId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.OwnerPersonnel)
             .WithMany()
             .HasForeignKey(d => d.OwnerPersonnelId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.CreatedByPersonnel)
             .WithMany()
             .HasForeignKey(d => d.CreatedByPersonnelId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Customer)
             .WithMany()
             .HasForeignKey(d => d.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CrmActivity (etkilesim kaydi)
        modelBuilder.Entity<CrmActivity>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Summary).HasMaxLength(500);
            e.Property(a => a.Detail).HasMaxLength(5000);
            e.HasIndex(a => new { a.CustomerId, a.CreatedAt });
            e.HasIndex(a => a.CrmContactId);
            e.HasOne(a => a.CrmContact)
             .WithMany()
             .HasForeignKey(a => a.CrmContactId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.Ticket)
             .WithMany(t => t.Activities)
             .HasForeignKey(a => a.TicketId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.Deal)
             .WithMany(d => d.Activities)
             .HasForeignKey(a => a.DealId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.CallRecord)
             .WithMany()
             .HasForeignKey(a => a.CallRecordId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.Personnel)
             .WithMany()
             .HasForeignKey(a => a.PersonnelId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Customer)
             .WithMany()
             .HasForeignKey(a => a.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CrmTask (gorev)
        modelBuilder.Entity<CrmTask>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).IsRequired().HasMaxLength(300);
            e.Property(t => t.Description).HasMaxLength(5000);
            e.HasIndex(t => new { t.CustomerId, t.StatusId });
            e.HasIndex(t => new { t.AssignedToPersonnelId, t.DueDate });
            e.HasOne(t => t.CrmContact)
             .WithMany()
             .HasForeignKey(t => t.CrmContactId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.Ticket)
             .WithMany()
             .HasForeignKey(t => t.TicketId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.Deal)
             .WithMany()
             .HasForeignKey(t => t.DealId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.AssignedToPersonnel)
             .WithMany()
             .HasForeignKey(t => t.AssignedToPersonnelId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.CreatedByPersonnel)
             .WithMany()
             .HasForeignKey(t => t.CreatedByPersonnelId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Customer)
             .WithMany()
             .HasForeignKey(t => t.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CrmTicketCategory
        modelBuilder.Entity<CrmTicketCategory>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(e2 => new { e2.CustomerId, e2.Name }).IsUnique();
            e.HasOne(c => c.Customer).WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        // CrmTicket -> Category FK
        modelBuilder.Entity<CrmTicket>(e =>
        {
            e.HasOne(t => t.Category)
             .WithMany(c => c.Tickets)
             .HasForeignKey(t => t.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // CrmTicketComment
        modelBuilder.Entity<CrmTicketComment>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Content).IsRequired().HasMaxLength(5000);
            e.HasIndex(c => c.TicketId);
            e.HasOne(c => c.Ticket).WithMany(t => t.Comments).HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.CreatedByPersonnel).WithMany().HasForeignKey(c => c.CreatedByPersonnelId).OnDelete(DeleteBehavior.Restrict);
        });

        // CrmContactTag
        modelBuilder.Entity<CrmContactTag>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(50);
            e.Property(t => t.Color).HasMaxLength(10);
            e.HasIndex(e2 => new { e2.CustomerId, e2.Name }).IsUnique();
            e.HasOne(t => t.Customer).WithMany().HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        // CrmContactTagLink
        modelBuilder.Entity<CrmContactTagLink>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(e2 => new { e2.CrmContactId, e2.TagId }).IsUnique();
            e.HasOne(l => l.CrmContact).WithMany().HasForeignKey(l => l.CrmContactId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Tag).WithMany(t => t.CrmContactLinks).HasForeignKey(l => l.TagId).OnDelete(DeleteBehavior.Cascade);
        });

        // CrmSurvey
        modelBuilder.Entity<CrmSurvey>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Uid).IsUnique();
            e.Property(s => s.Title).IsRequired().HasMaxLength(300);
            e.Property(s => s.Description).HasMaxLength(2000);
            e.HasIndex(e2 => new { e2.CustomerId, e2.IsActive });
            e.HasOne(s => s.Customer).WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.CreatedByPersonnel).WithMany().HasForeignKey(s => s.CreatedByPersonnelId).OnDelete(DeleteBehavior.Restrict);
        });

        // CrmSurveyQuestion
        modelBuilder.Entity<CrmSurveyQuestion>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.Text).IsRequired().HasMaxLength(500);
            e.Property(q => q.Options).HasMaxLength(2000);
            e.HasIndex(q => new { q.SurveyId, q.SortOrder });
            e.HasOne(q => q.Survey).WithMany(s => s.Questions).HasForeignKey(q => q.SurveyId).OnDelete(DeleteBehavior.Cascade);
        });

        // CrmSurveyResponse
        modelBuilder.Entity<CrmSurveyResponse>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Uid).IsUnique();
            e.Property(r => r.RespondentPhone).HasMaxLength(50);
            e.Property(r => r.RespondentName).HasMaxLength(200);
            e.Property(r => r.OverallScore).HasPrecision(5, 2);
            e.HasIndex(e2 => new { e2.SurveyId, e2.CreatedAt });
            e.HasOne(r => r.Survey).WithMany(s => s.Responses).HasForeignKey(r => r.SurveyId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.CrmContact).WithMany().HasForeignKey(r => r.CrmContactId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.CallRecord).WithMany().HasForeignKey(r => r.CallRecordId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.CreatedByPersonnel).WithMany().HasForeignKey(r => r.CreatedByPersonnelId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        // CrmSurveyAnswer
        modelBuilder.Entity<CrmSurveyAnswer>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.AnswerText).HasMaxLength(2000);
            e.HasIndex(e2 => new { e2.ResponseId, e2.QuestionId }).IsUnique();
            e.HasOne(a => a.Response).WithMany(r => r.Answers).HasForeignKey(a => a.ResponseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Question).WithMany().HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Restrict);
        });

        // ═══════════════════════════════════════════════════════════════
        // HİZMET ABONELİK YÖNETİMİ (Faz 14)
        // ═══════════════════════════════════════════════════════════════

        // CustomerServiceSubscription (musteri hizmet aboneligi)
        modelBuilder.Entity<CustomerServiceSubscription>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Uid).IsUnique();
            e.HasIndex(s => new { s.CustomerId, s.ServiceTypeId }).IsUnique();
            e.Property(s => s.MonthlyPrice).HasPrecision(18, 2);
            e.Property(s => s.Notes).HasMaxLength(1000);
            e.HasOne(s => s.Customer)
             .WithMany(c => c.ServiceSubscriptions)
             .HasForeignKey(s => s.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ServiceBillingItem (hizmet fatura kalemi)
        modelBuilder.Entity<ServiceBillingItem>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.CustomerServiceSubscriptionId, b.Year, b.Month }).IsUnique();
            e.Property(b => b.Amount).HasPrecision(18, 2);
            e.Property(b => b.Notes).HasMaxLength(1000);
            e.HasOne(b => b.Customer)
             .WithMany()
             .HasForeignKey(b => b.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.CustomerServiceSubscription)
             .WithMany(s => s.BillingItems)
             .HasForeignKey(b => b.CustomerServiceSubscriptionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ═══════════════════════════════════════════════════════════════
        // KALİTE YÖNETİMİ (Faz 18 - SecretCustomer Adaptasyonu)
        // ═══════════════════════════════════════════════════════════════

        // CrmQualityChecklist (kalite değerlendirme kontrol listesi şablonu)
        modelBuilder.Entity<CrmQualityChecklist>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Uid).IsUnique();
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.Property(c => c.Description).HasMaxLength(1000);
            e.Property(c => c.Code).HasMaxLength(50);
            e.Property(c => c.MaxTotalPoints).HasPrecision(18, 2);
            e.HasIndex(c => new { c.CustomerId, c.Name }).IsUnique();
            e.HasOne(c => c.Customer)
             .WithMany()
             .HasForeignKey(c => c.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CrmQualityQuestion (kalite kriteri/sorusu)
        modelBuilder.Entity<CrmQualityQuestion>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.Text).IsRequired().HasMaxLength(500);
            e.Property(q => q.WeightPoints).HasPrecision(18, 2);
            e.Property(q => q.HelpText).HasMaxLength(1000);
            e.Property(q => q.GroupName).HasMaxLength(100);
            e.HasIndex(q => new { q.ChecklistId, q.Order });
            e.HasOne(q => q.Checklist)
             .WithMany(c => c.Questions)
             .HasForeignKey(q => q.ChecklistId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CrmQualityQuestionSubCriteria (puan kırılma nedeni)
        modelBuilder.Entity<CrmQualityQuestionSubCriteria>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Description).IsRequired().HasMaxLength(500);
            e.Property(s => s.WeightPoints).HasPrecision(18, 2);
            e.HasIndex(s => new { s.QuestionId, s.Order });
            e.HasOne(s => s.Question)
             .WithMany(q => q.SubCriteria)
             .HasForeignKey(s => s.QuestionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CrmQualityEvaluation (çağrı değerlendirmesi)
        modelBuilder.Entity<CrmQualityEvaluation>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.HasIndex(ev => ev.Uid).IsUnique();
            e.Property(ev => ev.TotalScore).HasPrecision(18, 2);
            e.Property(ev => ev.MaxScore).HasPrecision(18, 2);
            e.Property(ev => ev.ScorePercentage).HasPrecision(18, 2);
            e.Property(ev => ev.EvaluationComment).HasMaxLength(2000);
            e.Property(ev => ev.Notes).HasMaxLength(2000);
            e.HasIndex(ev => new { ev.CustomerId, ev.StatusId });
            e.HasIndex(ev => ev.CallRecordId);
            e.HasIndex(ev => ev.EvaluatedPersonnelId);
            e.HasOne(ev => ev.Checklist)
             .WithMany(c => c.Evaluations)
             .HasForeignKey(ev => ev.ChecklistId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ev => ev.CallRecord)
             .WithMany()
             .HasForeignKey(ev => ev.CallRecordId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ev => ev.EvaluatorPersonnel)
             .WithMany()
             .HasForeignKey(ev => ev.EvaluatorPersonnelId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ev => ev.EvaluatedPersonnel)
             .WithMany()
             .HasForeignKey(ev => ev.EvaluatedPersonnelId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ev => ev.Customer)
             .WithMany()
             .HasForeignKey(ev => ev.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CrmQualityAnswer (değerlendirme cevabı/puanı)
        modelBuilder.Entity<CrmQualityAnswer>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.GivenPoints).HasPrecision(18, 2);
            e.Property(a => a.EarnedPoints).HasPrecision(18, 2);
            e.Property(a => a.AnswerText).HasMaxLength(1000);
            e.Property(a => a.Notes).HasMaxLength(1000);
            e.Property(a => a.RecommendationNotes).HasMaxLength(1000);
            e.HasIndex(a => new { a.EvaluationId, a.QuestionId }).IsUnique();
            e.HasOne(a => a.Evaluation)
             .WithMany(ev => ev.Answers)
             .HasForeignKey(a => a.EvaluationId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Question)
             .WithMany(q => q.Answers)
             .HasForeignKey(a => a.QuestionId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // CrmQualityAnswerSubCriteriaSelection (seçilen puan kırılma nedeni)
        modelBuilder.Entity<CrmQualityAnswerSubCriteriaSelection>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Notes).HasMaxLength(500);
            e.HasIndex(s => new { s.AnswerId, s.SubCriteriaId }).IsUnique();
            e.HasOne(s => s.Answer)
             .WithMany(a => a.SubCriteriaSelections)
             .HasForeignKey(s => s.AnswerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.SubCriteria)
             .WithMany(sc => sc.Selections)
             .HasForeignKey(s => s.SubCriteriaId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // CrmQualityScoreThreshold (müşteri bazlı puan eşik değerleri)
        modelBuilder.Entity<CrmQualityScoreThreshold>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.SuccessThreshold).HasPrecision(18, 2);
            e.Property(t => t.WarningThreshold).HasPrecision(18, 2);
            e.HasIndex(t => new { t.CustomerId, t.ChecklistId }).IsUnique();
            e.HasOne(t => t.Customer)
             .WithMany()
             .HasForeignKey(t => t.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Checklist)
             .WithMany()
             .HasForeignKey(t => t.ChecklistId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ─── IntegrationConnection ───
        modelBuilder.Entity<IntegrationConnection>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => new { x.CustomerId, x.PlatformTypeId });
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.EncryptedCredentials).HasMaxLength(4000);
            e.Property(x => x.LastError).HasMaxLength(2000);
            e.HasOne(x => x.Customer)
             .WithMany()
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── WebhookSubscription ───
        modelBuilder.Entity<WebhookSubscription>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.TargetUrl).HasMaxLength(2000);
            e.Property(x => x.Secret).HasMaxLength(500);
            e.Property(x => x.EventFilter).HasMaxLength(2000);
            e.HasOne(x => x.Customer)
             .WithMany()
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.IntegrationConnection)
             .WithMany(c => c.WebhookSubscriptions)
             .HasForeignKey(x => x.IntegrationConnectionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ─── WebhookDelivery (bigint PK — yuksek hacim) ───
        modelBuilder.Entity<WebhookDelivery>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EventId);
            e.HasIndex(x => x.NextRetryAt).HasFilter("\"NextRetryAt\" IS NOT NULL");
            e.HasIndex(x => x.CreatedAt);
            e.Property(x => x.EventType).HasMaxLength(100);
            e.Property(x => x.EventId).HasMaxLength(100);
            e.Property(x => x.ResponseBody).HasMaxLength(4000);
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.HasOne(x => x.WebhookSubscription)
             .WithMany(s => s.Deliveries)
             .HasForeignKey(x => x.WebhookSubscriptionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── IntegrationApiKey ───
        modelBuilder.Entity<IntegrationApiKey>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => x.ApiKeyHash).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.ApiKeyHash).HasMaxLength(128);
            e.Property(x => x.ApiKeyPrefix).HasMaxLength(20);
            e.Property(x => x.Scopes).HasMaxLength(1000);
            e.HasOne(x => x.Customer)
             .WithMany()
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.IntegrationConnection)
             .WithMany(c => c.ApiKeys)
             .HasForeignKey(x => x.IntegrationConnectionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Platform Email Events
        modelBuilder.Entity<PlatformEmailEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EventKey).IsUnique();
            e.Property(x => x.EventKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.ProductType).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(500);
        });

        // Platform Email Templates (dil bazli)
        modelBuilder.Entity<PlatformEmailTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EventId, x.Language }).IsUnique();
            e.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            e.Property(x => x.Language).HasMaxLength(5).HasDefaultValue("tr");
            e.HasOne(x => x.Event)
             .WithMany(x => x.Templates)
             .HasForeignKey(x => x.EventId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // =============================================
        // SEED DATA
        // =============================================

        // Varsayılan admin kullanıcısı
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Uid = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            UserName = "admin",
            FullName = "System Admin",
            Email = "admin@callcenter.local",
            // Sifre: 1123Azs+-  (BCrypt hash sabitlesti - migration uyumlulugu icin)
            PasswordHash = "$2a$11$4NK5QRHYyKGuXY/Wr41bGOgqCOD1PDK.c1473NdyCowy2.HJswS72",
            RoleId = UserRoles.Ids.Admin,
            StatusId = AgentStatuses.Ids.Offline,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Varsayılan çeviri key'leri ve çeviriler
        SeedTranslations(modelBuilder);

        // Varsayılan sistem ayarları
        SeedSystemSettings(modelBuilder);

    }

    private static void SeedTranslations(ModelBuilder modelBuilder)
    {
        var keys = new (int id, string key, string module, string desc, string tr, string en)[]
        {
            // Auth
            (1, "auth.login", "auth", "Login butonu", "Giriş Yap", "Sign In"),
            (2, "auth.logout", "auth", "Çıkış butonu", "Çıkış Yap", "Sign Out"),
            (3, "auth.username", "auth", "Kullanıcı adı", "Kullanıcı Adı", "Username"),
            (4, "auth.password", "auth", "Şifre", "Şifre", "Password"),
            (5, "auth.login_failed", "auth", "Hatalı giriş", "Kullanıcı adı veya şifre hatalı.", "Invalid username or password."),

            // Agent Status
            (10, "agent.status.available", "agent", "Müsait durumu", "Müsait", "Available"),
            (11, "agent.status.busy", "agent", "Meşgul durumu", "Meşgul", "Busy"),
            (12, "agent.status.on_break", "agent", "Mola durumu", "Molada", "On Break"),
            (13, "agent.status.in_call", "agent", "Çağrıda durumu", "Çağrıda", "In Call"),
            (14, "agent.status.offline", "agent", "Çevrimdışı durumu", "Çevrimdışı", "Offline"),
            (15, "agent.status.acw", "agent", "Çağrı sonrası iş", "Çağrı Sonrası", "After Call Work"),

            // Common
            (20, "common.save", "common", "Kaydet butonu", "Kaydet", "Save"),
            (21, "common.cancel", "common", "İptal butonu", "İptal", "Cancel"),
            (22, "common.delete", "common", "Sil butonu", "Sil", "Delete"),
            (23, "common.edit", "common", "Düzenle butonu", "Düzenle", "Edit"),
            (24, "common.search", "common", "Arama", "Ara", "Search"),
            (25, "common.loading", "common", "Yükleniyor", "Yükleniyor...", "Loading..."),
            (26, "common.yes", "common", "Evet", "Evet", "Yes"),
            (27, "common.no", "common", "Hayır", "Hayır", "No"),

            // Call
            (30, "call.incoming", "call", "Gelen çağrı", "Gelen Çağrı", "Incoming Call"),
            (31, "call.outgoing", "call", "Giden çağrı", "Giden Çağrı", "Outgoing Call"),
            (32, "call.hold", "call", "Beklet", "Beklet", "Hold"),
            (33, "call.transfer", "call", "Transfer", "Transfer", "Transfer"),
            (34, "call.hangup", "call", "Kapat", "Kapat", "Hang Up"),
            (35, "call.answer", "call", "Cevapla", "Cevapla", "Answer"),
            (36, "call.reject", "call", "Reddet", "Reddet", "Reject"),

            // Dashboard
            (40, "dashboard.title", "dashboard", "Dashboard başlığı", "Gösterge Paneli", "Dashboard"),
            (41, "dashboard.active_calls", "dashboard", "Aktif çağrı", "Aktif Çağrılar", "Active Calls"),
            (42, "dashboard.agents_online", "dashboard", "Online agent", "Çevrimiçi Temsilciler", "Agents Online"),
            (43, "dashboard.queue_waiting", "dashboard", "Kuyrukta bekleyen", "Kuyrukta Bekleyen", "Waiting in Queue"),
        };

        int translationId = 1;

        foreach (var (id, key, module, desc, tr, en) in keys)
        {
            modelBuilder.Entity<TranslationKey>().HasData(new TranslationKey
            {
                Id = id,
                Key = key,
                Module = module,
                Description = desc
            });

            modelBuilder.Entity<Translation>().HasData(
                new Translation
                {
                    Id = translationId++,
                    TranslationKeyId = id,
                    LanguageCode = "tr",
                    Value = tr,
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedBy = "system"
                },
                new Translation
                {
                    Id = translationId++,
                    TranslationKeyId = id,
                    LanguageCode = "en",
                    Value = en,
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedBy = "system"
                }
            );
        }
    }

    private static void SeedSystemSettings(ModelBuilder modelBuilder)
    {
        var settings = new (int id, string key, string value, string group, string valueType, string desc, bool isSystem)[]
        {
            // Genel
            (1, "app.name", "Call Center", "general", "string", "Uygulama adi", true),
            (2, "app.language", "tr", "general", "string", "Varsayilan dil", true),
            (3, "app.timezone", "Europe/Istanbul", "general", "string", "Zaman dilimi", true),
            (4, "app.date_format", "dd.MM.yyyy", "general", "string", "Tarih formati", true),

            // Guvenlik
            (10, "security.max_login_attempts", "5", "security", "int", "Maks hatali giris denemesi", true),
            (11, "security.lockout_minutes", "15", "security", "int", "Hesap kilitleme suresi (dk)", true),
            (12, "security.token_expire_minutes", "480", "security", "int", "JWT token suresi (dk)", true),
            (13, "security.password_min_length", "8", "security", "int", "Minimum sifre uzunlugu", true),
            (14, "security.password_history_count", "5", "security", "int", "Son kac sifre tekrar kullanilamaz", true),
            (15, "security.recording_retention_years", "10", "security", "int", "Ses kaydi saklama suresi (yil) — TTK md. 82", true),

            // SIP
            (20, "sip.default_transport", "UDP", "sip", "string", "Varsayilan SIP transport", true),
            (21, "sip.registration_timeout", "3600", "sip", "int", "SIP kayit suresi (sn)", true),
            (22, "sip.keep_alive_interval", "30", "sip", "int", "Keep-alive araligi (sn)", true),

            // Bildirim
            (30, "notification.sound_enabled", "true", "notification", "bool", "Bildirim sesi", false),
            (31, "notification.desktop_enabled", "true", "notification", "bool", "Masaustu bildirimi", false),
            (32, "notification.ring_duration", "30", "notification", "int", "Zil calma suresi (sn)", false),

            // Platform Depolama (varsayilan: devre disi)
            (40, "storage.platform_enabled", "false", "storage", "bool", "Platform depolamasi aktif mi", true),
            (41, "storage.platform_provider_type_id", "0", "storage", "int", "StorageProviders ID", true),
            (42, "storage.platform_credentials", "", "storage", "encrypted_json", "Sifrelenmis kimlik bilgileri (JSON)", true),
            (43, "storage.platform_base_path", "/recordings/", "storage", "string", "Temel yol", true),
        };

        foreach (var (id, key, value, group, valueType, desc, isSystem) in settings)
        {
            modelBuilder.Entity<SystemSetting>().HasData(new SystemSetting
            {
                Id = id,
                Key = key,
                Value = value,
                Group = group,
                ValueType = valueType,
                Description = desc,
                IsSystem = isSystem
            });
        }
    }

}
