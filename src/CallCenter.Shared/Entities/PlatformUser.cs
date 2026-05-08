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

    /// <summary>Kullanici timezone (IANA format: Europe/Istanbul vb.)</summary>
    public string TimeZone { get; set; } = "Europe/Istanbul";

    public bool IsActive { get; set; } = true;
    public bool IsPhoneVerified { get; set; }
    public bool IsEmailVerified { get; set; }

    // Email dogrulama
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationSentAt { get; set; }

    // Sifre sifirlama
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetSentAt { get; set; }

    // ─── Fatura Bilgileri ───
    /// <summary>Fatura tipi: 1=Bireysel, 2=Kurumsal</summary>
    public int BillingType { get; set; } = 1;
    public string? BillingFullName { get; set; }
    public string? BillingCompanyName { get; set; }
    public string? BillingTaxOffice { get; set; }
    public string? BillingTaxNumber { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingDistrict { get; set; }
    public string? BillingPostalCode { get; set; }

    /// <summary>Son giris tarihi</summary>
    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Uye oldugu salonlar
    public ICollection<PlatformUserSalon> Salons { get; set; } = new List<PlatformUserSalon>();
}
