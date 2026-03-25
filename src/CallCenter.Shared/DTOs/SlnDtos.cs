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
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public class SlnServiceCreateDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 30;
    public decimal Price { get; set; }
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
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int StatusId { get; set; }
    public string? Notes { get; set; }
}

public class SlnAppointmentCreateDto
{
    public int SlnClientId { get; set; }
    public int PersonnelId { get; set; }
    public int ServiceId { get; set; }
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
    public int? PosDeviceId { get; set; }
    public decimal DiscountAmount { get; set; }
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

// ═══ Dashboard ═══
public class SlnDashboardDto
{
    public int TotalClients { get; set; }
    public int TodayAppointments { get; set; }
    public decimal TodayRevenue { get; set; }
    public int ActiveStaff { get; set; }
    public List<SlnAppointmentDto> UpcomingAppointments { get; set; } = [];
}
