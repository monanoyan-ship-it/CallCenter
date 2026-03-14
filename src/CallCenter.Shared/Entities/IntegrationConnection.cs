namespace CallCenter.Shared.Entities;

/// <summary>
/// Musterinin dis sistem baglantisi (Salesforce, HubSpot, Zendesk vb.).
/// Credential bilgileri AES-256 ile sifrelenerek saklanir (CustomerStorageConfig pattern).
/// </summary>
public class IntegrationConnection
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    // Hangi musteriye ait
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Platform tipi (IntegrationPlatforms.Ids)
    public int PlatformTypeId { get; set; }

    /// <summary>Kullanici tarafindan verilen isim: "Salesforce Production", "HubSpot CRM"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Platform-specific credential bilgileri (AES-256 sifreli JSON).
    /// Salesforce: { "InstanceUrl", "AccessToken", "RefreshToken", "ClientId", "ClientSecret", "TokenExpiresAt" }
    /// HubSpot: { "AccessToken", "RefreshToken", "ClientId", "ClientSecret", "PortalId", "TokenExpiresAt" }
    /// Zendesk: { "Subdomain", "Email", "ApiToken", "AuthMethod" }
    /// GenericWebhook: { } (bos — webhook subscription uzerinden yonetilir)
    /// </summary>
    public string EncryptedCredentials { get; set; } = string.Empty;

    /// <summary>Platform ile baglanti kuruldu mu</summary>
    public bool IsConnected { get; set; }
    public bool IsActive { get; set; } = true;

    // Opsiyonel ayarlar
    /// <summary>Cagri bittiginde otomatik olarak dis sistemde aktivite olustur</summary>
    public bool AutoLogCalls { get; set; } = true;

    /// <summary>Rehber senkronizasyonu yonu: None, ToExternal, FromExternal, Bidirectional</summary>
    public int ContactSyncDirectionId { get; set; }

    // Durum takibi
    public DateTime? LastSyncAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSuccess { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<WebhookSubscription> WebhookSubscriptions { get; set; } = new List<WebhookSubscription>();
    public ICollection<IntegrationApiKey> ApiKeys { get; set; } = new List<IntegrationApiKey>();
}
