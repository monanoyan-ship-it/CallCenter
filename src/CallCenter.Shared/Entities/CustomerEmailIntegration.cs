namespace CallCenter.Shared.Entities;

/// <summary>
/// Musterinin e-posta gonderim yapilandirmasi.
/// Her musteri kendi email hesabini baglayarak sistem uzerinden mail gonderebilir.
/// Credential bilgileri AES-256 ile sifrelenerek saklanir.
///
/// EncryptedCredentials JSON yapisi (provider bazli):
/// Gmail:       { "RefreshToken", "AccessToken", "TokenExpiresAt" }
/// Office365:   { "TenantId", "RefreshToken", "AccessToken", "TokenExpiresAt" }
/// Yandex:      { "Email", "AppPassword" }
/// SmtpGeneric: { "Host", "Port", "Username", "Password", "UseSsl" }
/// </summary>
public class CustomerEmailIntegration
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Provider tipi (EmailProviders.Ids)</summary>
    public int ProviderTypeId { get; set; }

    /// <summary>Kullanicinin verdigi isim ("Salon Gmail" gibi)</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gonderici e-posta adresi (From)</summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>Gonderici gorunen adi (From display name)</summary>
    public string? SenderName { get; set; }

    /// <summary>Provider-specific credential bilgileri (AES-256 sifreli JSON)</summary>
    public string EncryptedCredentials { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }

    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSuccess { get; set; }
    public string? LastTestError { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
