namespace CallCenter.Shared.Entities;

public class SlnInvoice
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Hangi sube (null = merkez)</summary>
    public int? BranchId { get; set; }
    public SlnBranch? Branch { get; set; }

    public int? SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }

    /// <summary>KDV toplam tutari</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Genel toplam (NetAmount + TaxAmount)</summary>
    public decimal GrandTotal { get; set; }

    /// <summary>1=Cash, 2=CreditCard, 3=Mixed</summary>
    public int PaymentMethodId { get; set; } = 1;

    public int? PosDeviceId { get; set; }
    public SlnPosDevice? PosDevice { get; set; }

    public int? PersonnelId { get; set; }
    public CustomerPersonnel? Personnel { get; set; }

    /// <summary>1=Open, 2=Paid, 3=Cancelled</summary>
    public int StatusId { get; set; } = 1;
    public decimal TipAmount { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SlnInvoiceItem> Items { get; set; } = [];
    public ICollection<SlnInvoicePayment> Payments { get; set; } = [];
}
