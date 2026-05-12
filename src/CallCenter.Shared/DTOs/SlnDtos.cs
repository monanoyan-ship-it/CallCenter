namespace CallCenter.Shared.DTOs;

// ═══ SlnClient ═══
public class SlnClientDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int? GenderId { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? HairColor { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
    public int VisitCount { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastVisit { get; set; }
}

public class SlnClientDetailDto : SlnClientDto
{
    public string? Phone2 { get; set; }
    public DateTime? MarriageDate { get; set; }
    public string? Occupation { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public int? WhiteRatioPercent { get; set; }
    public string? SkinType { get; set; }
    public string? Notes { get; set; }
    public List<SlnFormulaDto> Formulas { get; set; } = [];
    public List<SlnClientPhotoDto> Photos { get; set; } = [];
}

public class SlnClientCreateDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Phone2 { get; set; }
    public string? Email { get; set; }
    public int? GenderId { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime? MarriageDate { get; set; }
    public string? Occupation { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? HairColor { get; set; }
    public int? WhiteRatioPercent { get; set; }
    public string? SkinType { get; set; }
    public string? Notes { get; set; }
}

public class SlnClientUpdateDto : SlnClientCreateDto
{
    public bool IsFavorite { get; set; }
}

public class SlnClientSuggestionsDto
{
    public List<string> HairColors { get; set; } = [];
    public List<string> SkinTypes { get; set; } = [];
}

// ═══ SlnFormula ═══
public class SlnFormulaDto
{
    public int Id { get; set; }
    public string FormulaText { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
    public string? OxidantRatio { get; set; }
    public string? ApplicationNotes { get; set; }
    public string? AppliedByName { get; set; }
    public DateTime AppliedAt { get; set; }
}

public class SlnFormulaCreateDto
{
    public int SlnClientId { get; set; }
    public string FormulaText { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
    public string? OxidantRatio { get; set; }
    public string? ApplicationNotes { get; set; }
}

// ═══ SlnClientPhoto ═══
public class SlnClientPhotoDto
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime TakenAt { get; set; }
}

// ═══ SlnService ═══
public class SlnServiceCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IconClass { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<SlnServiceDto> Services { get; set; } = [];
}

public class SlnServiceDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int BufferBeforeMinutes { get; set; }
    public int BufferAfterMinutes { get; set; }
    public int ProcessingMinutes { get; set; }
    public decimal Price { get; set; }
    public int? ParentServiceId { get; set; }
    public bool IsAddOn { get; set; }
    public bool RequiresConsultation { get; set; }
    public bool RequiresPatchTest { get; set; }
    public string? PrerequisiteNotes { get; set; }
    public bool IsActive { get; set; }
    public List<SlnServiceResourceRequirementDto> ResourceRequirements { get; set; } = [];
}

public class SlnServiceCreateDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 30;
    public int BufferBeforeMinutes { get; set; }
    public int BufferAfterMinutes { get; set; }
    public int ProcessingMinutes { get; set; }
    public decimal Price { get; set; }
    public int? ParentServiceId { get; set; }
    public bool IsAddOn { get; set; }
    public bool RequiresConsultation { get; set; }
    public bool RequiresPatchTest { get; set; }
    public string? PrerequisiteNotes { get; set; }
    public List<SlnServiceResourceRequirementCreateDto> ResourceRequirements { get; set; } = [];
}

public class SlnResourceDto
{
    public int Id { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ResourceKind { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
}

public class SlnResourceCreateDto
{
    public int? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ResourceKind { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
}

public class SlnServiceResourceRequirementDto
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public int QuantityRequired { get; set; }
}

public class SlnServiceResourceRequirementCreateDto
{
    public int ResourceId { get; set; }
    public int QuantityRequired { get; set; } = 1;
}

public class SlnServiceComboDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public List<SlnServiceComboItemDto> Items { get; set; } = [];
}

public class SlnServiceComboItemDto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int SortOrder { get; set; }
}

public class SlnServiceComboCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public List<SlnServiceComboItemCreateDto> Items { get; set; } = [];
}

public class SlnServiceComboItemCreateDto
{
    public int ServiceId { get; set; }
    public int SortOrder { get; set; }
}

// ═══ SlnAppointment ═══
public class SlnAppointmentDto
{
    public int Id { get; set; }
    public int SlnClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? ComboId { get; set; }
    public string? ComboName { get; set; }
    public List<int> ServiceIds { get; set; } = new();
    public List<string> ServiceNames { get; set; } = new();
    public int DurationMinutes { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int StatusId { get; set; }
    public string? Notes { get; set; }
    public bool IsPrepaid { get; set; }
    public decimal PrepaidAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public int ClientNoShowCount { get; set; }
    public bool ClientIsBlacklisted { get; set; }
}

public class SlnAppointmentCreateDto
{
    public int SlnClientId { get; set; }
    public int PersonnelId { get; set; }
    public int? ComboId { get; set; }
    public List<int> ServiceIds { get; set; } = new();
    public DateTime StartTime { get; set; }
    public string? Notes { get; set; }
}

// ═══ SlnProduct ═══
public class SlnProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal MinStockLevel { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class SlnProductCreateDto
{
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal MinStockLevel { get; set; }
    public string Unit { get; set; } = "Adet";
}

// ═══ SlnSupplier ═══
public class SlnSupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
}

public class SlnSupplierCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? Notes { get; set; }
}

// ═══ SlnInvoice ═══
public class SlnInvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string? ClientName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public int PaymentMethodId { get; set; }
    public string? PersonnelName { get; set; }
    public int StatusId { get; set; }
    public decimal TipAmount { get; set; }
    public List<SlnInvoiceItemDto> Items { get; set; } = [];
}

public class SlnInvoiceItemDto
{
    public int Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? PersonnelName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class SlnInvoiceCreateDto
{
    public int? SlnClientId { get; set; }
    public int PaymentMethodId { get; set; } = 1;
    public string? GiftCardCode { get; set; }
    public int? PosDeviceId { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TipAmount { get; set; }
    /// <summary>true: bahsis NetAmount'a dahil edilir (musteri toplama oder). false: bahsis ayri tutulur (personel hakki).</summary>
    public bool IncludeTipInTotal { get; set; }
    public string? Notes { get; set; }
    public List<SlnInvoiceItemCreateDto> Items { get; set; } = [];
}

public class SlnInvoiceItemCreateDto
{
    public int? ServiceId { get; set; }
    public int? ProductId { get; set; }
    public int? PersonnelId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public int? MembershipId { get; set; }
    public bool UseMembershipBenefit { get; set; }
    public int? ClientPackageId { get; set; }
    public bool UsePackageSession { get; set; }
}

// ═══ SlnExpense ═══
public class SlnExpenseDto
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public int PaymentMethodId { get; set; }
}

public class SlnExpenseCreateDto
{
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public int PaymentMethodId { get; set; } = 1;
}

// ═══ SlnCash ═══
public class SlnCashTransactionDto
{
    public int Id { get; set; }
    public string RegisterName { get; set; } = string.Empty;
    public int TransactionTypeId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int PaymentMethodId { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ═══ SlnOnlineBooking ═══
public class SlnOnlineBookingDto
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int ServiceId { get; set; }
    public int? ComboId { get; set; }
    public int? PersonnelId { get; set; }
    public DateTime StartTime { get; set; }
    public string? Notes { get; set; }

    /// <summary>On odeme/depozito icin kart bilgileri (politika gerektiriyorsa zorunlu)</summary>
    public SlnOnlineBookingCardDto? Card { get; set; }
}

public class SlnOnlineBookingCardDto
{
    public string? CardHolderName { get; set; }
    public string? CardNumber { get; set; }
    public string? ExpireMonth { get; set; }
    public string? ExpireYear { get; set; }
    public string? Cvc { get; set; }
}

public class SlnPublicWaitlistDto
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int ServiceId { get; set; }
    public int? PersonnelId { get; set; }
    public DateTime PreferredDate { get; set; }
    /// <summary>Sabah | Ogle | Aksam | Farketmez (serbest metin de olabilir)</summary>
    public string? PreferredTimeSlot { get; set; }
    public string? Notes { get; set; }
}

public class SlnMembershipSignupDto
{
    public int PlanId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
}

// ═══ SlnSalonProfile ═══
public class SlnSalonProfileDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string SalonName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public bool IsHeadquarter { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? InstagramHandle { get; set; }
    public string? FacebookUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? GalleryImagesJson { get; set; }
    public bool IsPublished { get; set; }
    public int BillingType { get; set; } = 1;
    // PageSettings (hala profil entity'sinde)
    public bool ShowServices { get; set; } = true;
    public bool ShowMemberships { get; set; } = true;
    public bool ShowBooking { get; set; } = true;
    public bool ShowHours { get; set; } = true;
    public bool ShowContact { get; set; } = true;
    public string? SectionOrderJson { get; set; }
    public bool ShowBanners { get; set; } = true;
    public bool ShowTeam { get; set; } = true;
    public bool ShowReviews { get; set; } = true;
    public bool ShowMap { get; set; } = true;
    public string? BannersJson { get; set; }
    public List<SlnServiceCategoryDto> ServiceCategories { get; set; } = [];
    public List<SlnServiceComboDto> ServiceCombos { get; set; } = [];
    // Geriye uyumluluk: Public sayfa slug'i merkez subeden alinir
    public string Slug { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public string? WorkingHoursJson { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class SlnSalonProfileUpdateDto
{
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? InstagramHandle { get; set; }
    public string? FacebookUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    public int BillingType { get; set; } = 1;
}

public class SlnPageSettingsDto
{
    public bool ShowServices { get; set; } = true;
    public bool ShowMemberships { get; set; } = true;
    public bool ShowBooking { get; set; } = true;
    public bool ShowHours { get; set; } = true;
    public bool ShowContact { get; set; } = true;
    public bool ShowBanners { get; set; } = true;
    public bool ShowTeam { get; set; } = true;
    public bool ShowReviews { get; set; } = true;
    public bool ShowMap { get; set; } = true;
    public string? SectionOrderJson { get; set; }
    public string? BannersJson { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? GalleryImagesJson { get; set; }
}

// ═══ SlnNoShowPolicy ═══
public class SlnNoShowPolicyDto
{
    public int Id { get; set; }
    public bool RequireDeposit { get; set; }
    public decimal DepositAmount { get; set; }
    public int FreeCancellationHours { get; set; }
    public decimal LateCancellationFee { get; set; }
    public decimal NoShowFee { get; set; }
    public int BlacklistThreshold { get; set; }
    public bool IsActive { get; set; }
}

public class SlnNoShowPolicyUpdateDto
{
    public bool RequireDeposit { get; set; }
    public decimal DepositAmount { get; set; }
    public int FreeCancellationHours { get; set; } = 24;
    public decimal LateCancellationFee { get; set; }
    public decimal NoShowFee { get; set; }
    public int BlacklistThreshold { get; set; } = 3;
    public bool IsActive { get; set; } = true;
}

// ═══ SlnMembership ═══
public class SlnMembershipPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public string? Color { get; set; }
    public int DurationType { get; set; }
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public int DiscountPercent { get; set; }
    public bool PriorityBooking { get; set; }
    public bool IsActive { get; set; }
    public int ActiveMembers { get; set; }
    public List<int> ServiceIds { get; set; } = new();
    public List<string> ServiceNames { get; set; } = new();
    public List<MembershipServiceDetailDto> ServiceDetails { get; set; } = new();
}

public class MembershipServiceDetailDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int FreeCount { get; set; }
    public int DiscountPercent { get; set; }
}

public class SlnMembershipPlanCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public string? Color { get; set; }
    public int DurationType { get; set; } = 1;
    public int DurationDays { get; set; } = 30;
    public decimal Price { get; set; }
    public int DiscountPercent { get; set; }
    public bool PriorityBooking { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int> ServiceIds { get; set; } = new();
    public List<MembershipServiceDetailDto> ServiceDetails { get; set; } = new();
}

public class SlnClientMembershipDto
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string? PlanColor { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public decimal PaidAmount { get; set; }
    public int StatusId { get; set; }
}

public class SlnClientMembershipCreateDto
{
    public int PlanId { get; set; }
    public int SlnClientId { get; set; }
}

// ═══ SlnLoyalty ═══
public class SlnLoyaltyConfigDto
{
    public int Id { get; set; }
    public decimal PointsPerTL { get; set; }
    public decimal PointValue { get; set; }
    public int MinRedeemPoints { get; set; }
    public bool IsActive { get; set; }
}

public class SlnLoyaltyConfigUpdateDto
{
    public decimal PointsPerTL { get; set; } = 1;
    public decimal PointValue { get; set; } = 0.1m;
    public int MinRedeemPoints { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

public class SlnClientLoyaltyDto
{
    public int Id { get; set; }
    public int SlnClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int TotalEarned { get; set; }
    public int TotalSpent { get; set; }
    public int CurrentBalance { get; set; }
    public decimal BalanceValue { get; set; }
}

public class SlnLoyaltyTransactionDto
{
    public int Id { get; set; }
    public int TransactionTypeId { get; set; }
    public int Points { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnLoyaltyRedeemDto
{
    public int SlnClientId { get; set; }
    public int Points { get; set; }
    public int? InvoiceId { get; set; }
}

// ═══ SlnPackage ═══
public class SlnPackageDefinitionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public decimal Price { get; set; }
    public decimal PricePerSession { get; set; }
    public int ValidDays { get; set; }
    public bool IsActive { get; set; }
}

public class SlnPackageDefinitionCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ServiceId { get; set; }
    public int TotalSessions { get; set; }
    public decimal Price { get; set; }
    public int ValidDays { get; set; } = 365;
    public bool IsActive { get; set; } = true;
}

public class SlnClientPackageDto
{
    public int Id { get; set; }
    public int PackageDefinitionId { get; set; }
    public int ServiceId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public int TotalSessions { get; set; }
    public int UsedSessions { get; set; }
    public int RemainingSessions { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnClientPackageSellDto
{
    public int PackageDefinitionId { get; set; }
    public int? SlnClientId { get; set; }
    public int PaymentMethodId { get; set; } = 1;
}

public class SlnPackageUseDto
{
    public int ClientPackageId { get; set; }
    public string? Notes { get; set; }
}

public class SlnPackageBenefitCheckDto
{
    public int SlnClientId { get; set; }
    public List<int> ServiceIds { get; set; } = [];
}

public class SlnPackageBenefitDto
{
    public int ClientPackageId { get; set; }
    public int PackageDefinitionId { get; set; }
    public int ServiceId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public int RemainingSessions { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

// ═══ SlnGiftCard ═══
public class SlnGiftCardDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? SenderName { get; set; }
    public string? Message { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? SoldByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SlnGiftCardTransactionDto> Transactions { get; set; } = [];
}

public class SlnGiftCardTransactionDto
{
    public int Id { get; set; }
    public int TransactionTypeId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnGiftCardCreateDto
{
    public decimal Amount { get; set; }
    public int PaymentMethodId { get; set; } = 1;
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? SenderName { get; set; }
    public string? Message { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class SlnGiftCardRedeemDto
{
    public string Code { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? InvoiceId { get; set; }
}

// ═══ SlnCashClosing ═══
public class SlnCashClosingDto
{
    public int Id { get; set; }
    public int RegisterId { get; set; }
    public string RegisterName { get; set; } = string.Empty;
    public DateTime ClosingDate { get; set; }
    public decimal SystemTotal { get; set; }
    public decimal CountedTotal { get; set; }
    public decimal Difference { get; set; }
    public string? Notes { get; set; }
    public string? ClosedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnCashClosingCreateDto
{
    public int RegisterId { get; set; }
    public decimal CountedTotal { get; set; }
    public string? Notes { get; set; }
}

// ═══ Dashboard ═══
public class SlnDashboardDto
{
    public int TotalClients { get; set; }
    public int TodayAppointments { get; set; }
    public decimal TodayRevenue { get; set; }
    public int ActiveStaff { get; set; }
    public List<SlnAppointmentDto> UpcomingAppointments { get; set; } = [];
}

// ═══ SlnCampaign (S7) ═══
public class SlnCampaignDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public string? SegmentFilter { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnCampaignCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public string? SegmentFilter { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class SlnCampaignUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public string? SegmentFilter { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class SlnSegmentPreviewDto
{
    public int MatchingClients { get; set; }
    public int SmsReachableClients { get; set; }
    public int EmailReachableClients { get; set; }
    public int MissingPhoneCount { get; set; }
    public int MissingEmailCount { get; set; }
    public int ExcludedByOptOutCount { get; set; }
    public decimal EstimatedSmsCost { get; set; }
    public decimal EstimatedEmailCost { get; set; }
}

public class SlnSegmentPresetDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? FilterJson { get; set; }
    public int MatchingClients { get; set; }
    public int SmsReachableClients { get; set; }
    public int EmailReachableClients { get; set; }
    public int MissingPhoneCount { get; set; }
    public int MissingEmailCount { get; set; }
    public int ExcludedByOptOutCount { get; set; }
    public decimal EstimatedSmsCost { get; set; }
    public decimal EstimatedEmailCost { get; set; }
}

// ═══ SlnAutoReminder (S7) ═══
public class SlnAutoReminderDto
{
    public int Id { get; set; }
    public int ReminderTypeId { get; set; }
    public string ReminderTypeName { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public int DaysBefore { get; set; }
    public int InactiveDaysThreshold { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnAutoReminderCreateDto
{
    public int ReminderTypeId { get; set; }
    public string MessageTemplate { get; set; } = string.Empty;
    public int DaysBefore { get; set; }
    public int InactiveDaysThreshold { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SlnAutoReminderUpdateDto : SlnAutoReminderCreateDto
{
}

// ═══ SlnReports (S8) ═══
public class SlnSalesReportDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalInvoices { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public decimal AverageTicket { get; set; }
    public List<SlnDailySalesDto> DailySales { get; set; } = [];
    public List<SlnPaymentMethodSalesDto> PaymentMethodBreakdown { get; set; } = [];
}

public class SlnKpiReportDto
{
    public decimal TotalRevenue { get; set; }
    public int InvoiceCount { get; set; }
    public decimal AverageTicket { get; set; }
    public decimal BookedHours { get; set; }
    public decimal CapacityHours { get; set; }
    public decimal OccupancyPercent { get; set; }
    public int AppointmentCount { get; set; }
    public int CompletedAppointmentCount { get; set; }
    public int ActiveClientCount { get; set; }
    public int RepeatClientCount { get; set; }
    public decimal RepeatVisitRatePercent { get; set; }
    public decimal AverageLifetimeValue { get; set; }
    public decimal PeriodSpendPerClient { get; set; }
    public int ActiveStaffCount { get; set; }
    public decimal RevenuePerActiveStaff { get; set; }
    public decimal RevenuePerBookedHour { get; set; }
    public List<SlnStaffEfficiencyDto> StaffEfficiency { get; set; } = [];
}

public class SlnStaffEfficiencyDto
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public int ServiceCount { get; set; }
    public int AppointmentCount { get; set; }
    public int CompletedAppointmentCount { get; set; }
    public decimal BookedHours { get; set; }
    public decimal Revenue { get; set; }
    public decimal RevenuePerBookedHour { get; set; }
    public decimal RevenuePerService { get; set; }
}

public class SlnBranchComparisonReportDto
{
    public List<SlnBranchComparisonRowDto> Branches { get; set; } = [];
    public List<SlnBranchDimensionRowDto> Services { get; set; } = [];
    public List<SlnBranchDimensionRowDto> Personnel { get; set; } = [];
    public List<SlnBranchDimensionRowDto> Products { get; set; } = [];
}

public class SlnBranchComparisonRowDto
{
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public int InvoiceCount { get; set; }
    public decimal AverageTicket { get; set; }
    public int AppointmentCount { get; set; }
    public int CompletedAppointmentCount { get; set; }
    public int ActiveClientCount { get; set; }
    public decimal RevenueSharePercent { get; set; }
}

public class SlnBranchDimensionRowDto
{
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int DimensionId { get; set; }
    public string DimensionName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class SlnDailySalesDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int InvoiceCount { get; set; }
}

public class SlnPaymentMethodSalesDto
{
    public int PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class SlnStaffReportDto
{
    public List<SlnStaffPerformanceDto> Staff { get; set; } = [];
}

public class SlnStaffPerformanceDto
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public int ServiceCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal Commission { get; set; }
}

public class SlnStockReportDto
{
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public decimal TotalStockValue { get; set; }
    public decimal TotalRetailValue { get; set; }
    public decimal PotentialGrossProfit { get; set; }
    public decimal AverageMarginPercent { get; set; }
    public decimal EstimatedVatTotal { get; set; }
    public decimal SupplierDebtTotal { get; set; }
    public List<SlnStockItemDto> Items { get; set; } = [];
    public List<SlnStockTaxBreakdownDto> TaxBreakdown { get; set; } = [];
    public List<SlnSupplierDebtBreakdownDto> SupplierDebtBreakdown { get; set; } = [];
}

public class SlnStockItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal StockQuantity { get; set; }
    public decimal MinStockLevel { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal StockValue { get; set; }
    public decimal RetailValue { get; set; }
    public decimal PotentialGrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal EstimatedVatAmount { get; set; }
    public bool IsLowStock { get; set; }
}

public class SlnStockTaxBreakdownDto
{
    public decimal TaxRate { get; set; }
    public int ProductCount { get; set; }
    public decimal StockValue { get; set; }
    public decimal RetailValue { get; set; }
    public decimal EstimatedVatAmount { get; set; }
}

public class SlnSupplierDebtBreakdownDto
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}

/// <summary>
/// PS.12 — Management sub-merchant listesi satiri. Tum salonlarin onboarding
/// durumu + iletisim + IBAN. KYC pending/active/rejected filtreleme icin.
/// </summary>
public class AdminSubMerchantDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? IyzicoSubMerchantKey { get; set; }
    public string? IyzicoSubMerchantType { get; set; }
    public string? ContactName { get; set; }
    public string? ContactSurname { get; set; }
    public string? Iban { get; set; }
    public string? GsmNumber { get; set; }
    /// <summary>0=NotStarted, 1=Pending, 2=Approved, 3=Rejected</summary>
    public int OnboardingStatusId { get; set; }
    public string OnboardingStatus { get; set; } = string.Empty;
    public DateTime? OnboardedAt { get; set; }
    public string? OnboardingError { get; set; }
    public decimal? CommissionPercentOverride { get; set; }
}

/// <summary>
/// PS.10 — iyzico Pazaryeri sub-merchant settlement breakdown.
/// 1 Ocak 2025 sonrasi 1% e-ticaret aracilik stopaji iyzico tarafindan
/// otomatik kesilir; bu DTO salon-a aylik hak edis raporunda gosterilir.
/// PS.13 hak edis raporu bu DTO'yu kullanir.
/// </summary>
public class SettlementBreakdownDto
{
    /// <summary>Brut tahsilat tutari (musteri odedigi)</summary>
    public decimal GrossAmount { get; set; }
    /// <summary>%1 e-ticaret aracilik stopaji (iyzico kesintisi, 1.1.2025 sonrasi)</summary>
    public decimal WithholdingTax { get; set; }
    /// <summary>Platform komisyonu (corplynk, 5% default)</summary>
    public decimal PlatformCommission { get; set; }
    /// <summary>Salon hak edisi: gross - stopaj - komisyon</summary>
    public decimal NetSettlement { get; set; }
    /// <summary>Stopaj orani uygulandi mi (1.1.2025 sonrasi tarihler icin true)</summary>
    public bool WithholdingApplied { get; set; }
    /// <summary>Komisyon orani (informational)</summary>
    public decimal CommissionPercent { get; set; }
}

/// <summary>
/// PS.13 — salon hak edis rapor satiri. Her basarili tx icin brut + komisyon
/// + stopaj + net dagilim. PaymentTransaction.Notes'tan turetilen ek bilgiler:
/// transferDate (settlement event-i loglandi ise), source (booking/uyelik/adisyon).
/// </summary>
public class SettlementEntryDto
{
    public Guid Uid { get; set; }
    public int TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    /// <summary>SalonAdisyon / RandevuOnOdemesi / UyelikOdemesi vs.</summary>
    public string Source { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SettlementBreakdownDto Breakdown { get; set; } = new();
    /// <summary>Sub-merchant settlement event'i loglandi mi (webhook'tan)</summary>
    public bool SettlementReceived { get; set; }
    public DateTime? SettlementDate { get; set; }
}

public class SettlementReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<SettlementEntryDto> Entries { get; set; } = [];
    /// <summary>Aggregate: tum entry-lerin toplami</summary>
    public SettlementBreakdownDto Total { get; set; } = new();
}

public class SlnFinanceReportDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
    public int InvoiceCount { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public decimal SalesVatTotal { get; set; }
    public decimal ExpenseVatTotal { get; set; }
    public decimal VatPayable { get; set; }
    public decimal StockValue { get; set; }
    public decimal RetailStockValue { get; set; }
    public decimal EstimatedStockVat { get; set; }
    public decimal CashIncome { get; set; }
    public decimal CashExpense { get; set; }
    public decimal CashNet { get; set; }
    public decimal CashBalance { get; set; }
    public List<SlnPaymentMethodSalesDto> PaymentMethodBreakdown { get; set; } = [];
    public List<SlnFinanceTaxBreakdownDto> TaxBreakdown { get; set; } = [];
    public List<SlnExpenseCategoryBreakdownDto> ExpenseBreakdown { get; set; } = [];
}

public class SlnFinanceTaxBreakdownDto
{
    public decimal TaxRate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public int LineCount { get; set; }
}

public class SlnExpenseCategoryBreakdownDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class SlnClientReportDto
{
    public int TotalClients { get; set; }
    public int NewClientsInPeriod { get; set; }
    public decimal AverageVisitFrequency { get; set; }
    public List<SlnTopClientDto> TopClients { get; set; } = [];
}

public class SlnTopClientDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastVisit { get; set; }
}

// ═══ SlnRecipe ═══
public class SlnRecipeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
    public List<SlnRecipeItemDto> Items { get; set; } = [];
}

public class SlnRecipeItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "gr";
    public decimal Cost { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

public class SlnRecipeCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int? ServiceId { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SlnRecipeItemCreateDto> Items { get; set; } = [];
}

public class SlnRecipeItemCreateDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "gr";
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

// ═══ SlnBranch (S9) ═══
public class SlnBranchDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public string? WorkingHoursJson { get; set; }
    public string? PhotoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? GalleryImagesJson { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? ManagerPersonnelId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsHeadquarter { get; set; }
    public bool IsActive { get; set; }
    // Fatura bilgileri
    public string? CompanyTitle { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? MersisNo { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnBranchCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public string? WorkingHoursJson { get; set; }
    public string? PhotoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? GalleryImagesJson { get; set; }
    public int? ManagerPersonnelId { get; set; }
    public bool IsHeadquarter { get; set; }
    public bool IsActive { get; set; } = true;
    // Fatura bilgileri
    public string? CompanyTitle { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? MersisNo { get; set; }
    // Konum (manuel girilebilir, yoksa backend Nominatim ile otomatik cozer)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class SlnBranchUpdateDto : SlnBranchCreateDto
{
}

// ═══ Salon Self-Registration ═══
public class SlnRegisterRequest
{
    public string SalonName { get; set; } = string.Empty;
    public string OwnerFullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int? SubscriptionPlanId { get; set; }
}

public class SlnRegisterResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public bool EmailVerificationRequired { get; set; }
    public string? Email { get; set; }
}

// ═══ SlnWaitlistEntry (C3) ═══
public class SlnWaitlistEntryDto
{
    public int Id { get; set; }
    public int SlnClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int? PreferredPersonnelId { get; set; }
    public string? PreferredPersonnelName { get; set; }
    public DateTime PreferredDate { get; set; }
    public string? PreferredTimeSlot { get; set; }
    public string? Notes { get; set; }
    public int StatusId { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnWaitlistEntryCreateDto
{
    public int SlnClientId { get; set; }
    public int ServiceId { get; set; }
    public int? PreferredPersonnelId { get; set; }
    public DateTime PreferredDate { get; set; }
    public string? PreferredTimeSlot { get; set; }
    public string? Notes { get; set; }
}

public class SlnWaitlistEntryUpdateDto : SlnWaitlistEntryCreateDto
{
}

// ═══ SlnEmailCampaign (C5) ═══
public class SlnEmailCampaignDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? SegmentFilter { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int OpenCount { get; set; }
    public int ClickCount { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnEmailCampaignCreateDto
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? SegmentFilter { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class SlnEmailCampaignUpdateDto : SlnEmailCampaignCreateDto
{
}

// ═══ SlnReview (C6) ═══
public class SlnReviewDto
{
    public int Id { get; set; }
    public int? SlnClientId { get; set; }
    public string? ClientName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int SourceId { get; set; }
    public string? ExternalUrl { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnReviewCreateDto
{
    public int? SlnClientId { get; set; }
    public string? ClientName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int SourceId { get; set; } = 1;
    public string? ExternalUrl { get; set; }
}

public class SlnReviewUpdateDto
{
    public int StatusId { get; set; }
}

public class SlnReviewStatsDto
{
    public int TotalReviews { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public double AverageRating { get; set; }
}

// ═══ SlnConsentForm (D1) ═══
public class SlnConsentFormDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public bool RequireSignature { get; set; }
    public bool IsActive { get; set; }
    public int SignedCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnConsentFormCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public bool RequireSignature { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SlnConsentFormUpdateDto : SlnConsentFormCreateDto
{
}

public class SlnClientConsentDto
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public string FormTitle { get; set; } = string.Empty;
    public int SlnClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime SignedAt { get; set; }
}

public class SlnClientConsentCreateDto
{
    public int FormId { get; set; }
    public int SlnClientId { get; set; }
    public string? SignatureData { get; set; }
    public string? IpAddress { get; set; }
}

// ═══ SlnBeforeAfterPhoto (D2) ═══
public class SlnBeforeAfterPhotoDto
{
    public int Id { get; set; }
    public int SlnClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public string? BeforePhotoUrl { get; set; }
    public string? AfterPhotoUrl { get; set; }
    public string? Notes { get; set; }
    public int? PersonnelId { get; set; }
    public string? PersonnelName { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnBeforeAfterPhotoCreateDto
{
    public int SlnClientId { get; set; }
    public int? ServiceId { get; set; }
    public string? BeforePhotoUrl { get; set; }
    public string? AfterPhotoUrl { get; set; }
    public string? Notes { get; set; }
    public int? PersonnelId { get; set; }
    public bool IsPublic { get; set; }
}

public class SlnBeforeAfterPhotoUpdateDto : SlnBeforeAfterPhotoCreateDto
{
}

// ═══ SlnWinbackRule (D3) ═══
public class SlnWinbackRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int InactiveDays { get; set; }
    public int ChannelId { get; set; }
    public string MessageTemplate { get; set; } = string.Empty;
    public int? DiscountPercent { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnWinbackRuleCreateDto
{
    public string Name { get; set; } = string.Empty;
    public int InactiveDays { get; set; } = 30;
    public int ChannelId { get; set; } = 1;
    public string MessageTemplate { get; set; } = string.Empty;
    public int? DiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SlnWinbackRuleUpdateDto : SlnWinbackRuleCreateDto
{
}

public class SlnWinbackPreviewDto
{
    public int RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int InactiveDays { get; set; }
    public int EligibleClients { get; set; }
    public int SmsReachableClients { get; set; }
    public int EmailReachableClients { get; set; }
    public int MissingContactCount { get; set; }
    public decimal DiscountPercent { get; set; }
    public string MessagePreview { get; set; } = string.Empty;
    public List<SlnWinbackCandidateDto> Candidates { get; set; } = [];
}

public class SlnWinbackCandidateDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime? LastVisitAt { get; set; }
    public int InactiveDays { get; set; }
}

// ═══ SlnPersonnelServicePrice (D4) ═══
public class SlnPersonnelServicePriceDto
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class SlnPersonnelServicePriceCreateDto
{
    public int PersonnelId { get; set; }
    public int ServiceId { get; set; }
    public decimal Price { get; set; }
}

public class SlnPersonnelServicePriceUpdateDto
{
    public decimal Price { get; set; }
}

// ═══ SlnRevenueShare (D5) ═══
public class SlnRevenueShareDto
{
    public int Id { get; set; }
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public int ModelTypeId { get; set; }
    public decimal PersonnelSharePercent { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal MinimumGuarantee { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SlnRevenueShareCreateDto
{
    public int PersonnelId { get; set; }
    public int ModelTypeId { get; set; } = 1;
    public decimal PersonnelSharePercent { get; set; } = 60;
    public decimal MonthlyRent { get; set; }
    public decimal MinimumGuarantee { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SlnRevenueShareUpdateDto : SlnRevenueShareCreateDto
{
}
