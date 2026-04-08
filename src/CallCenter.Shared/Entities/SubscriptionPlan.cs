namespace CallCenter.Shared.Entities;

/// <summary>
/// Abonelik plan tanimlari (aylik, 3 aylik, 6 aylik, yillik vb.)
/// Management panelinden yonetilir. Tum musteriler icin gecerli.
/// </summary>
public class SubscriptionPlan
{
    public int Id { get; set; }

    /// <summary>Plan adi (ornegin: "Aylik", "3 Aylik", "Yillik")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Kac ayda bir tahakkuk olusur</summary>
    public int IntervalMonths { get; set; }

    /// <summary>Indirim orani (yuzde). Ornegin yillik %20 indirim</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Siralama</summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
