using CallCenter.Shared.Enums;

namespace CallCenter.Shared.Entities;

/// <summary>
/// Gunluk arama listesi / kampanya.
/// FirmaAdmin veya EkipLideri olusturur, Operator'lere atar.
/// </summary>
public class CallCampaign
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusId { get; set; } = CampaignStatuses.Ids.Draft;

    /// <summary>Planlanan arama tarihi</summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>Kampanyayi olusturan personel</summary>
    public int CreatedByPersonnelId { get; set; }
    public CustomerPersonnel? CreatedByPersonnel { get; set; }

    /// <summary>Firma bazli izolasyon</summary>
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<CampaignContact> CampaignContacts { get; set; } = new List<CampaignContact>();
}
