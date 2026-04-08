using System.ComponentModel.DataAnnotations;

namespace CallCenter.Shared.DTOs;

// ═══════════════════════════════════════════════════════════════
// PAYMENT CONFIG DTO'LAR
// ═══════════════════════════════════════════════════════════════

/// <summary>Payment config listesi icin ozet bilgi</summary>
public class PaymentConfigListDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int ProviderTypeId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderIcon { get; set; } = string.Empty;
    public string ProviderCss { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSandbox { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSuccess { get; set; }
    public string? LastTestError { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Payment config detayi (credential'lar maskelenmis)</summary>
public class PaymentConfigDetailDto : PaymentConfigListDto
{
    public Dictionary<string, string> MaskedCredentials { get; set; } = new();
    public PaymentBankInfoDto? BankInfo { get; set; }
}

/// <summary>Banka bilgileri (havale icin)</summary>
public class PaymentBankInfoDto
{
    public string BankName { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>Yeni config olusturma / guncelleme</summary>
public class PaymentConfigSaveDto
{
    [Required]
    public int ProviderTypeId { get; set; }

    public bool IsSandbox { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // ─── Iyzico ───
    public string? IyzicoApiKey { get; set; }
    public string? IyzicoSecretKey { get; set; }

    // ─── PayTR ───
    public string? PayTrMerchantId { get; set; }
    public string? PayTrMerchantKey { get; set; }
    public string? PayTrMerchantSalt { get; set; }

    // ─── Param ───
    public string? ParamClientCode { get; set; }
    public string? ParamClientUsername { get; set; }
    public string? ParamClientPassword { get; set; }
    public string? ParamGuid { get; set; }

    // ─── Banka Bilgileri (Havale) ───
    public string? BankName { get; set; }
    public string? BankIBAN { get; set; }
    public string? BankAccountHolder { get; set; }
    public string? BankDescription { get; set; }
}

/// <summary>Baglanti testi sonucu</summary>
public class PaymentConfigTestResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime TestedAt { get; set; }
}

/// <summary>Mevcut provider bilgisi</summary>
public class PaymentProviderInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Css { get; set; } = string.Empty;
    public List<string> RequiredFields { get; set; } = new();
}
