namespace CallCenter.Shared.Entities;

/// <summary>
/// Dis sistemlerin bizim API'mize erismesi icin API anahtari.
/// ApiKey SHA-256 hash olarak saklanir (plain text ASLA DB'de tutulmaz).
/// Scope bazli yetkilendirme: ["calls:read","calls:write","contacts:read"]
/// </summary>
public class IntegrationApiKey
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    // Hangi musteriye ait
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Hangi entegrasyon baglantisina ait
    public int IntegrationConnectionId { get; set; }
    public IntegrationConnection IntegrationConnection { get; set; } = null!;

    /// <summary>Tanimlayici isim: "Salesforce API Key", "Custom Integration"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>API key'in SHA-256 hash'i (dogrulama icin)</summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>API key'in ilk 8 karakteri (UI'da goruntuleme icin: "sk_live_abc...")</summary>
    public string ApiKeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Yetki kapsamlari (JSON array).
    /// Ornek: ["calls:read","calls:write","contacts:read","contacts:write","agents:read","tickets:write"]
    /// </summary>
    public string Scopes { get; set; } = "[]";

    /// <summary>Son kullanim tarihi (null = suresiz)</summary>
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Son kullanildigi zaman</summary>
    public DateTime? LastUsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
