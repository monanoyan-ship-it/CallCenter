namespace CallCenter.Shared.Entities;

/// <summary>
/// Platform seviyesi odeme gateway yapilandirmasi.
/// Management panelinden admin tarafindan yonetilir.
/// Tek aktif config ile tum odemeler (modul satin alma, uyelik, abonelik) islenir.
/// CustomerStorageConfig pattern'i temel alinmistir — fark: CustomerId yok (platform geneli).
/// </summary>
public class PlatformPaymentConfig
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Odeme saglayici tipi (PaymentProviders.Ids: 1=Iyzico, 2=PayTR, 3=Param)</summary>
    public int ProviderTypeId { get; set; }

    /// <summary>
    /// Provider-specific credential bilgileri (AES-256 sifreli JSON).
    /// Iyzico: { "ApiKey", "SecretKey", "BaseUrl" }
    /// PayTR:  { "MerchantId", "MerchantKey", "MerchantSalt", "BaseUrl" }
    /// Param:  { "ClientCode", "ClientUsername", "ClientPassword", "Guid", "BaseUrl" }
    /// </summary>
    public string EncryptedCredentials { get; set; } = string.Empty;

    /// <summary>
    /// Havale/EFT banka bilgileri (AES-256 sifreli JSON).
    /// { "BankName", "IBAN", "AccountHolder", "Description" }
    /// </summary>
    public string? EncryptedBankInfo { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>true = sandbox/test modu, false = production</summary>
    public bool IsSandbox { get; set; } = true;

    // Baglanti testi
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSuccess { get; set; }
    public string? LastTestError { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
