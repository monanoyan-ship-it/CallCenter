namespace CallCenter.Shared.Entities;

/// <summary>
/// Platform kullanici ↔ Salon baglantisi.
/// Bir kullanici birden fazla salona uye olabilir.
/// Her uyelik bir SlnClient kaydina baglanir (salonun kendi musteri karti).
/// </summary>
public class PlatformUserSalon
{
    public int Id { get; set; }

    /// <summary>Platform kullanicisi</summary>
    public int PlatformUserId { get; set; }
    public PlatformUser PlatformUser { get; set; } = null!;

    /// <summary>Hangi salon (Customer = salon firmasi)</summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Salonun kendi musteri karti (otomatik olusturulur)</summary>
    public int? SlnClientId { get; set; }
    public SlnClient? SlnClient { get; set; }

    /// <summary>Uyelik durumu: aktif/pasif</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Favori salon mi?</summary>
    public bool IsFavorite { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
