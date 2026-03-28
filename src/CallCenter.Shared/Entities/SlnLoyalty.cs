namespace CallCenter.Shared.Entities;

/// <summary>
/// Salon bazli sadakat programi ayarlari
/// </summary>
public class SlnLoyaltyConfig
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Her 1 TL harcama icin kazanilan puan</summary>
    public decimal PointsPerTL { get; set; } = 1;

    /// <summary>1 puan kac TL deger? (orn: 0.1 = 10 puan = 1 TL)</summary>
    public decimal PointValue { get; set; } = 0.1m;

    /// <summary>Minimum kullanim puani</summary>
    public int MinRedeemPoints { get; set; } = 100;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Musteri puan bakiyesi
/// </summary>
public class SlnClientLoyalty
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    public int TotalEarned { get; set; }
    public int TotalSpent { get; set; }
    public int CurrentBalance { get; set; }

    public ICollection<SlnLoyaltyTransaction> Transactions { get; set; } = [];
}

/// <summary>
/// Puan hareketi
/// </summary>
public class SlnLoyaltyTransaction
{
    public int Id { get; set; }
    public int ClientLoyaltyId { get; set; }
    public SlnClientLoyalty? ClientLoyalty { get; set; }

    /// <summary>1=Kazanım, 2=Harcama, 3=İade, 4=Manuel Ekleme</summary>
    public int TransactionTypeId { get; set; }
    public int Points { get; set; }
    public string? Description { get; set; }

    public int? RelatedInvoiceId { get; set; }
    public SlnInvoice? RelatedInvoice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
