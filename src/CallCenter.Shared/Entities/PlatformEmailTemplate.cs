namespace CallCenter.Shared.Entities;

/// <summary>
/// Platform seviyesi email taslagi.
/// Management panelinden yonetilir, kayit onayi / sifre sifirlama / hosgeldin gibi sistem emailleri icin.
/// </summary>
public class PlatformEmailTemplate
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Benzersiz olay anahtari: "salon_register_confirm", "password_reset" vb.</summary>
    public string EventKey { get; set; } = string.Empty;

    /// <summary>Hangi urun icin: "Salon", "CallCenter", "CRM" veya null=tumu</summary>
    public string? ProductType { get; set; }

    public string Subject { get; set; } = string.Empty;

    /// <summary>HTML govde. Placeholder: {{SalonAdi}}, {{KullaniciAdi}}, {{OnayLinki}} vb.</summary>
    public string HtmlBody { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Yonetici aciklamasi</summary>
    public string? Description { get; set; }

    /// <summary>Kullanilabilir placeholder listesi JSON: ["SalonAdi","KullaniciAdi","OnayLinki"]</summary>
    public string? AvailablePlaceholders { get; set; }

    public string Language { get; set; } = "tr";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
