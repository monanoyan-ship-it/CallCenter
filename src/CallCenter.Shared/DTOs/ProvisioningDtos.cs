namespace CallCenter.Shared.DTOs;

/// <summary>
/// Provisioning konfigurasyonu — Client baslatildiginda sunucudan cekilir.
/// SIP credentials, codec preferences, STUN/TURN, UI settings icerir.
/// </summary>
public class ProvisioningConfigDto
{
    /// <summary>Config versiyonu (degisiklikleri tespit etmek icin)</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Kullanici ID</summary>
    public int UserId { get; set; }

    /// <summary>Kullanici adi (gorunur)</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>SIP baglanti bilgileri</summary>
    public SipConnectionInfoDto? SipConnection { get; set; }

    /// <summary>UI tercihleri</summary>
    public ProvisioningUiSettings Ui { get; set; } = new();

    /// <summary>Olusturulma zamani</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gecerlilik suresi (saat)</summary>
    public int ExpiresInHours { get; set; } = 24;
}

/// <summary>UI tercihleri (provisioning ile dagitilir)</summary>
public class ProvisioningUiSettings
{
    /// <summary>Varsayilan dil kodu</summary>
    public string Language { get; set; } = "tr";

    /// <summary>Tema (light/dark)</summary>
    public string Theme { get; set; } = "light";

    /// <summary>Otomatik cevap etkin mi</summary>
    public bool AutoAnswer { get; set; }

    /// <summary>DND (Rahatsiz Etmeyin) varsayilan durumu</summary>
    public bool DndEnabled { get; set; }

    /// <summary>Kayit etkin mi (arama kaydetme)</summary>
    public bool RecordingEnabled { get; set; }
}

/// <summary>Admin'in per-user provisioning config olusturma istegi</summary>
public class CreateProvisioningRequest
{
    /// <summary>Kullanici ID</summary>
    public int UserId { get; set; }

    /// <summary>Gecerlilik suresi (saat). 0 = sinirsiz.</summary>
    public int ExpiresInHours { get; set; } = 24;

    /// <summary>UI tercihleri (opsiyonel, bos ise varsayilan)</summary>
    public ProvisioningUiSettings? Ui { get; set; }
}

/// <summary>Provisioning URL response (admin icin)</summary>
public class ProvisioningUrlResponse
{
    /// <summary>Provisioning URL (one-time token iceren)</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Token</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Son kullanma zamani</summary>
    public DateTime ExpiresAt { get; set; }
}
