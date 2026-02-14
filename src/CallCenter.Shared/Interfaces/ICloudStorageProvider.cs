namespace CallCenter.Shared.Interfaces;

/// <summary>
/// Bulut depolama provider sozlesmesi.
/// Her provider (Google Drive, OneDrive, Yandex, S3, MinIO) bu arayuzu uygular.
/// Strategy Pattern: Factory, musterinin config'ine gore dogru provider'i ureretir.
/// </summary>
public interface ICloudStorageProvider
{
    /// <summary>Provider tipi ID'si (StorageProviders.Ids)</summary>
    int ProviderTypeId { get; }

    /// <summary>Dosya yukle. Donus: provider'daki dosya ID/yolu</summary>
    Task<StorageUploadResult> UploadAsync(Stream fileStream, string fileName, string? folder = null,
        CancellationToken ct = default);

    /// <summary>Dosya indir</summary>
    Task<Stream> DownloadAsync(string fileId, CancellationToken ct = default);

    /// <summary>Dosya sil</summary>
    Task<bool> DeleteAsync(string fileId, CancellationToken ct = default);

    /// <summary>Dosya varligini kontrol et</summary>
    Task<bool> ExistsAsync(string fileId, CancellationToken ct = default);

    /// <summary>Gecici indirme URL'i olustur (destekleyen provider'lar icin)</summary>
    Task<string?> GetDownloadUrlAsync(string fileId, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>Baglanti testi — credential'larin gecerli oldugunu dogrular</summary>
    Task<(bool Success, string? Error)> TestConnectionAsync(CancellationToken ct = default);
}

/// <summary>Upload isleminin sonucu</summary>
public class StorageUploadResult
{
    public bool Success { get; set; }
    public string? FileId { get; set; }      // Provider'daki benzersiz dosya ID/path
    public string? FileUrl { get; set; }      // Direkt erisim URL (varsa)
    public long FileSize { get; set; }
    public string? Error { get; set; }
}
