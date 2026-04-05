namespace CallCenter.Shared.Entities;

/// <summary>
/// Platform email olayi. Her olay bir veya daha fazla dilde taslaga sahip olabilir.
/// Management panelinden yonetilir.
/// </summary>
public class PlatformEmailEvent
{
    public int Id { get; set; }

    /// <summary>Benzersiz olay anahtari: "salon_register_confirm", "password_reset" vb.</summary>
    public string EventKey { get; set; } = string.Empty;

    /// <summary>Hangi urun icin: "Salon", "CallCenter", "CRM" veya null=tumu</summary>
    public string? ProductType { get; set; }

    /// <summary>Yonetici aciklamasi</summary>
    public string? Description { get; set; }

    /// <summary>Kullanilabilir placeholder listesi JSON: ["SalonAdi","KullaniciAdi","OnayLinki"]</summary>
    public string? AvailablePlaceholders { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public List<PlatformEmailTemplate> Templates { get; set; } = new();
}
