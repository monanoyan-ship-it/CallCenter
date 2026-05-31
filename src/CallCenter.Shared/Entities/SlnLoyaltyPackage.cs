namespace CallCenter.Shared.Entities;

// Sadakat Paketi (Prepaid Bundle) — "10 ode 12 al" tarzi sadakat paketi.
// Cok seansli hizmet (SlnService.SessionCount + SlnTreatmentRecord.SessionIndex) ile karistirilmaz.

public class SlnLoyaltyPackageOffer
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public int TotalSessions { get; set; }
    public int BonusSessions { get; set; }
    public decimal Price { get; set; }
    public int ValidDays { get; set; } = 365;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SlnLoyaltyPackagePurchase
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int OfferId { get; set; }
    public SlnLoyaltyPackageOffer? Offer { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public int TotalSessions { get; set; }
    public int UsedSessions { get; set; }
    public int RemainingSessions { get; set; }
    public decimal SaleAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public int? SourceInvoiceId { get; set; }
    public SlnInvoice? SourceInvoice { get; set; }

    public int? SourceInvoiceItemId { get; set; }
    public SlnInvoiceItem? SourceInvoiceItem { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public int? SoldByPersonnelId { get; set; }
    public CustomerPersonnel? SoldByPersonnel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SlnLoyaltyPackageRedemption> Redemptions { get; set; } = [];
}

public class SlnLoyaltyPackageRedemption
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public SlnLoyaltyPackagePurchase? Purchase { get; set; }

    public int? PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    public int? InvoiceId { get; set; }
    public SlnInvoice? Invoice { get; set; }

    public int? InvoiceItemId { get; set; }
    public SlnInvoiceItem? InvoiceItem { get; set; }

    public int? ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public int? SlnAppointmentId { get; set; }
    public SlnAppointment? SlnAppointment { get; set; }

    public string? Notes { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}
