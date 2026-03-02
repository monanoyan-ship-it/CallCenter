namespace CallCenter.Shared.Entities;

/// <summary>
/// Sesli karsilama mesaji (Auto-Attendant).
/// Gelen aramada calan karsilama sesi. Musteri bazli, tipe gore (mesai ici/disi/tatil).
/// </summary>
public class GreetingMessage
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public int CustomerId { get; set; }

    /// <summary>Tanitici isim: "Mesai Ici Karsilama", "Bayram Mesaji" vb.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>business_hours | after_hours | holiday | ivr_prompt | queue_announcement</summary>
    public string Type { get; set; } = "business_hours";

    /// <summary>Ses dosyasi yolu (sunucu uzerinde veya cloud storage)</summary>
    public string AudioFilePath { get; set; } = string.Empty;

    /// <summary>Orijinal dosya adi</summary>
    public string? AudioFileName { get; set; }

    /// <summary>Dosya suresi (saniye)</summary>
    public int? DurationSeconds { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Customer Customer { get; set; } = null!;
}
