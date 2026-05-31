namespace CallCenter.Shared.Entities;

public class SlnServiceSessionPlan
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public int? SourceInvoiceId { get; set; }
    public SlnInvoice? SourceInvoice { get; set; }

    public int? SourceInvoiceItemId { get; set; }
    public SlnInvoiceItem? SourceInvoiceItem { get; set; }

    public int TotalSessions { get; set; }
    public int UsedSessions { get; set; }
    public int RemainingSessions { get; set; }

    public decimal SaleAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public int? SoldByPersonnelId { get; set; }
    public CustomerPersonnel? SoldByPersonnel { get; set; }

    public DateTime SoldAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SlnServiceSessionRecord> Records { get; set; } = [];
}

public class SlnServiceSessionRecord
{
    public int Id { get; set; }

    public int PlanId { get; set; }
    public SlnServiceSessionPlan? Plan { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public int ServiceId { get; set; }
    public SlnService? Service { get; set; }

    public int SessionNumber { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    public int? PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    public int? InvoiceId { get; set; }
    public SlnInvoice? Invoice { get; set; }

    public int? InvoiceItemId { get; set; }
    public SlnInvoiceItem? InvoiceItem { get; set; }

    public int? SlnAppointmentId { get; set; }
    public SlnAppointment? SlnAppointment { get; set; }

    public int? TreatmentRecordId { get; set; }
    public SlnTreatmentRecord? TreatmentRecord { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedByPersonnelId { get; set; }
    public CustomerPersonnel? CreatedByPersonnel { get; set; }
}
