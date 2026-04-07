namespace CallCenter.Shared.Entities;

/// <summary>
/// Platform son kullanici. Salon musterisi olarak kayit olur.
/// Birden fazla salona uye olabilir, randevu alabilir, sadakat puani biriktirebilir.
/// User tablosundan bagimsiz — ayri auth, ayri JWT.
/// </summary>
public class PlatformUser
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }

    /// <summary>Profil fotografı URL</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Tercih edilen dil (orn: "tr", "en")</summary>
    public string PreferredLanguage { get; set; } = "tr";

    public bool IsActive { get; set; } = true;
    public bool IsPhoneVerified { get; set; }
    public bool IsEmailVerified { get; set; }

    /// <summary>Son giris tarihi</summary>
    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Uye oldugu salonlar
    public ICollection<PlatformUserSalon> Salons { get; set; } = new List<PlatformUserSalon>();
}
