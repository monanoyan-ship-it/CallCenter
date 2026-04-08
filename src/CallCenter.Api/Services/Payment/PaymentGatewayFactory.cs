using System.Text.Json;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;

namespace CallCenter.Api.Services.Payment;

/// <summary>
/// Aktif PlatformPaymentConfig'e gore dogru IPaymentGateway'i ureten fabrika.
/// Credential'lari AES-256 ile cozumler ve gateway'e gecirir.
/// CloudStorageFactory pattern'i temel alinmistir.
/// </summary>
public class PaymentGatewayFactory
{
    private readonly AesEncryptionService _encryption;

    public PaymentGatewayFactory(AesEncryptionService encryption)
    {
        _encryption = encryption;
    }

    /// <summary>Config'ten uygun gateway olusturur</summary>
    public IPaymentGateway Create(PlatformPaymentConfig config)
    {
        var credentialsJson = _encryption.Decrypt(config.EncryptedCredentials);

        return config.ProviderTypeId switch
        {
            PaymentProviders.Ids.Iyzico => CreateIyzicoGateway(credentialsJson, config.IsSandbox),
            PaymentProviders.Ids.PayTR => throw new NotSupportedException(
                "PayTR entegrasyonu henuz tamamlanmadi. Lutfen Iyzico kullanin."),
            PaymentProviders.Ids.Param => throw new NotSupportedException(
                "Param entegrasyonu henuz tamamlanmadi. Lutfen Iyzico kullanin."),
            _ => throw new NotSupportedException(
                $"PaymentProvider {config.ProviderTypeId} desteklenmiyor. " +
                $"Desteklenen: Iyzico(1), PayTR(2), Param(3)")
        };
    }

    /// <summary>Config'in banka bilgilerini cozumler</summary>
    public BankAccountInfo? GetBankInfo(PlatformPaymentConfig config)
    {
        if (string.IsNullOrEmpty(config.EncryptedBankInfo))
            return null;

        var json = _encryption.Decrypt(config.EncryptedBankInfo);
        return JsonSerializer.Deserialize<BankAccountInfo>(json, JsonOpts);
    }

    /// <summary>Credential'lari sifreler</summary>
    public string EncryptCredentials(object credentials)
    {
        var json = JsonSerializer.Serialize(credentials, JsonOpts);
        return _encryption.Encrypt(json);
    }

    /// <summary>Banka bilgilerini sifreler</summary>
    public string EncryptBankInfo(BankAccountInfo bankInfo)
    {
        var json = JsonSerializer.Serialize(bankInfo, JsonOpts);
        return _encryption.Encrypt(json);
    }

    /// <summary>Credential'lari cozumler ve maskeler (UI gosterim icin)</summary>
    public Dictionary<string, string> GetMaskedCredentials(PlatformPaymentConfig config)
    {
        var json = _encryption.Decrypt(config.EncryptedCredentials);
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOpts) ?? new();

        var masked = new Dictionary<string, string>();
        foreach (var (key, value) in dict)
        {
            if (string.IsNullOrEmpty(value))
            {
                masked[key] = "";
                continue;
            }

            // URL'leri maskeleme
            if (key.Contains("Url", StringComparison.OrdinalIgnoreCase) || key.Contains("BaseUrl", StringComparison.OrdinalIgnoreCase))
            {
                masked[key] = value;
                continue;
            }

            // Diger alanlari maskele: ilk 4 + **** + son 4
            masked[key] = value.Length <= 8
                ? new string('*', value.Length)
                : $"{value[..4]}****{value[^4..]}";
        }

        return masked;
    }

    // ─── Private factory methods ───

    private static IyzicoGateway CreateIyzicoGateway(string json, bool isSandbox)
    {
        var creds = JsonSerializer.Deserialize<IyzicoCredentials>(json, JsonOpts)
            ?? throw new InvalidOperationException("Iyzico credential bilgileri okunamadi");

        // Sandbox/prod URL'ini config'e gore ayarla
        if (isSandbox && !creds.BaseUrl.Contains("sandbox"))
            creds.BaseUrl = "https://sandbox-api.iyzipay.com";
        else if (!isSandbox && creds.BaseUrl.Contains("sandbox"))
            creds.BaseUrl = "https://api.iyzipay.com";

        return new IyzicoGateway(creds);
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
