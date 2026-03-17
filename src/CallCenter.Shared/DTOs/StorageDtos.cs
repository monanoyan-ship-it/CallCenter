using System.ComponentModel.DataAnnotations;

namespace CallCenter.Shared.DTOs;

// ═══════════════════════════════════════════════════════════════
// CLOUD STORAGE CONFIG DTO'LAR
// ═══════════════════════════════════════════════════════════════

/// <summary>Storage config listesi icin ozet bilgi</summary>
public class StorageConfigListDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ProviderTypeId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderIcon { get; set; } = string.Empty;
    public string ProviderCss { get; set; } = string.Empty;
    public string? BasePath { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSuccess { get; set; }
    public string? LastTestError { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Storage config detayi (credential'lar maskelenmis)</summary>
public class StorageConfigDetailDto : StorageConfigListDto
{
    // Maskelenmis credential bilgileri (ornek: "AKIA****XXXX")
    public Dictionary<string, string> MaskedCredentials { get; set; } = new();
}

/// <summary>Yeni storage config olusturma</summary>
public class StorageConfigCreateDto
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int ProviderTypeId { get; set; }

    public string? BasePath { get; set; }
    public bool IsDefault { get; set; }

    // ─── S3 / MinIO ───
    public string? Endpoint { get; set; }      // MinIO icin zorunlu
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? BucketName { get; set; }
    public string? Region { get; set; }         // S3 icin (ornek: eu-central-1)
    public string? Prefix { get; set; }         // S3 key prefix (ornek: recordings/)
    public bool UseSSL { get; set; } = true;    // MinIO icin

    // ─── Google Drive ───
    public string? GoogleClientId { get; set; }
    public string? GoogleClientSecret { get; set; }
    public string? GoogleRefreshToken { get; set; }
    public string? GoogleFolderId { get; set; }

    // ─── OneDrive (Microsoft 365) ───
    /// <summary>"ClientCredential" (gelismis) veya "Delegated" (kolay/orta)</summary>
    public string? MsAuthMode { get; set; }
    public string? MsClientId { get; set; }
    public string? MsClientSecret { get; set; }
    public string? MsRefreshToken { get; set; }
    public string? MsTenantId { get; set; }
    public string? MsDriveId { get; set; }
    public string? MsFolderId { get; set; }

    // ─── Yandex Disk ───
    public string? YandexOAuthToken { get; set; }
}

/// <summary>Storage config guncelleme (null olan alanlar degistirilmez)</summary>
public class StorageConfigUpdateDto
{
    public int? ProviderTypeId { get; set; }
    public string? BasePath { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDefault { get; set; }

    // S3 / MinIO
    public string? Endpoint { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? BucketName { get; set; }
    public string? Region { get; set; }
    public string? Prefix { get; set; }
    public bool? UseSSL { get; set; }

    // Google Drive
    public string? GoogleClientId { get; set; }
    public string? GoogleClientSecret { get; set; }
    public string? GoogleRefreshToken { get; set; }
    public string? GoogleFolderId { get; set; }

    // OneDrive
    public string? MsAuthMode { get; set; }
    public string? MsClientId { get; set; }
    public string? MsClientSecret { get; set; }
    public string? MsRefreshToken { get; set; }
    public string? MsTenantId { get; set; }
    public string? MsDriveId { get; set; }
    public string? MsFolderId { get; set; }

    // Yandex Disk
    public string? YandexOAuthToken { get; set; }
}

/// <summary>Baglanti testi sonucu</summary>
public class StorageTestResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime TestedAt { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// RECORDING UPLOAD/DOWNLOAD DTO'LAR
// ═══════════════════════════════════════════════════════════════

/// <summary>Ses kaydi bulut'a yukleme sonucu</summary>
public class RecordingUploadResultDto
{
    public bool Success { get; set; }
    public string? CloudFileId { get; set; }
    public string? Error { get; set; }
}

/// <summary>Ses kaydi gecici indirme URL'i</summary>
public class RecordingDownloadUrlDto
{
    public string Url { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>Windows app icin musteri cloud config (credential'lar decrypted)</summary>
public class CloudConfigForClientDto
{
    public int ProviderTypeId { get; set; }
    public string? BasePath { get; set; }
    public Dictionary<string, string> Credentials { get; set; } = new();
}

/// <summary>OneDrive OAuth baslama yaniti</summary>
public class OneDriveAuthUrlDto
{
    public string AuthUrl { get; set; } = "";
    public string State { get; set; } = "";
}

/// <summary>OneDrive OAuth code exchange istegi</summary>
public class OneDriveExchangeCodeDto
{
    public string Code { get; set; } = "";
    public string? TenantId { get; set; }
}

/// <summary>OneDrive drive bilgisi</summary>
public class OneDriveDriveDto
{
    public string DriveId { get; set; } = "";
    public string Name { get; set; } = "";
    public string DriveType { get; set; } = "";
    public string? OwnerName { get; set; }
    public long TotalSpace { get; set; }
    public long UsedSpace { get; set; }
}

/// <summary>OneDrive OAuth tamamlanma yaniti (token + drive listesi)</summary>
public class OneDriveAuthResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? RefreshToken { get; set; }
    public string? TenantId { get; set; }
    public List<OneDriveDriveDto> Drives { get; set; } = new();
}

/// <summary>Desteklenen provider listesi icin</summary>
public class StorageProviderInfoDto
{
    public int Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? CssClass { get; set; }
    public bool RequiresOAuth { get; set; }     // Google, OneDrive
    public List<string> RequiredFields { get; set; } = new();
}
