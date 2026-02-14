namespace CallCenter.Api.Services.CloudStorage;

/// <summary>Amazon S3 ve MinIO (S3-uyumlu) credential bilgileri</summary>
public class S3Credentials
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string? Region { get; set; }         // S3 icin (ornek: eu-central-1)
    public string? Endpoint { get; set; }       // MinIO icin (ornek: https://minio.example.com:9000)
    public string? Prefix { get; set; }         // Key prefix (ornek: recordings/)
    public bool UseSSL { get; set; } = true;    // MinIO icin
}

/// <summary>Google Drive OAuth2 credential bilgileri</summary>
public class GoogleDriveCredentials
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string? FolderId { get; set; }       // Yuklenecek Google Drive klasor ID'si
}

/// <summary>Microsoft OneDrive (Graph API) credential bilgileri</summary>
public class OneDriveCredentials
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string? TenantId { get; set; }       // Azure AD tenant (varsayilan: common)
    public string? DriveId { get; set; }        // OneDrive drive ID'si
    public string? FolderId { get; set; }       // Klasor ID'si
}

/// <summary>Yandex Disk credential bilgileri</summary>
public class YandexDiskCredentials
{
    public string OAuthToken { get; set; } = string.Empty;
    public string? BasePath { get; set; }       // Yandex Disk icindeki klasor yolu (ornek: /CallCenter/Recordings/)
}
