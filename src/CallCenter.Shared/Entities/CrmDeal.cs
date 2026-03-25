namespace CallCenter.Shared.Entities;

/// <summary>
/// CRM satis firsati. Pipeline yonetimi icin.
/// </summary>
public class CrmDeal
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    /// <summary>Firsat degeri (TL)</summary>
    public decimal Value { get; set; }

    /// <summary>DealStages TypeItem referansi</summary>
    public int StageId { get; set; } = 1; // Lead

    /// <summary>Kazanma olasiligi (%)</summary>
    public int Probability { get; set; }

    /// <summary>Beklenen kapanma tarihi</summary>
    public DateTime? ExpectedCloseDate { get; set; }

    /// <summary>Gercek kapanma tarihi</summary>
    public DateTime? ActualCloseDate { get; set; }

    public string? Notes { get; set; }

    /// <summary>Ilgili kisi</summary>
    public int? CrmContactId { get; set; }
    public CrmContact? CrmContact { get; set; }

    /// <summary>Sorumlu personel</summary>
    public int? OwnerPersonnelId { get; set; }
    public CustomerPersonnel? OwnerPersonnel { get; set; }

    /// <summary>Olusturan personel</summary>
    public int CreatedByPersonnelId { get; set; }
    public CustomerPersonnel CreatedByPersonnel { get; set; } = null!;

    /// <summary>Musteri bazli izolasyon</summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CrmActivity> Activities { get; set; } = new List<CrmActivity>();
}
