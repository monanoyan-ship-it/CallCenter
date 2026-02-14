namespace CallCenter.Shared.Entities;

/// <summary>
/// Musterinin bulut depolama yapilandirmasi.
/// Her musteri kendi tercih ettigi cloud storage provider'ini secer.
/// Credential bilgileri AES-256 ile sifrelenerek saklanir.
/// </summary>
public class CustomerStorageConfig
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    // Hangi musteriye ait
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Provider tipi (StorageProviders.Ids)
    public int ProviderTypeId { get; set; }

    /// <summary>
    /// Provider-specific credential bilgileri (AES-256 sifreli JSON).
    /// Google: { "ClientId", "ClientSecret", "RefreshToken", "FolderId" }
    /// OneDrive: { "ClientId", "ClientSecret", "RefreshToken", "TenantId", "DriveId", "FolderId" }
    /// Yandex: { "OAuthToken", "BasePath" }
    /// S3: { "AccessKey", "SecretKey", "BucketName", "Region", "Prefix" }
    /// MinIO: { "Endpoint", "AccessKey", "SecretKey", "BucketName", "Prefix", "UseSSL" }
    /// </summary>
    public string EncryptedCredentials { get; set; } = string.Empty;

    // Ortak ayarlar
    public string? BasePath { get; set; }      // Provider icindeki klasor yolu (ornek: /recordings/)
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }        // Musterinin varsayilan depolama alani

    // Baglanti testi
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSuccess { get; set; }
    public string? LastTestError { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
