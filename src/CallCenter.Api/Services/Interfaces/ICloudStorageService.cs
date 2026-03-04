using CallCenter.Shared.DTOs;
using CallCenter.Shared.Interfaces;

namespace CallCenter.Api.Services.Interfaces;

/// <summary>
/// Bulut depolama is mantigi servisi.
/// Musterinin config'ini bulur, factory ile provider olusturur, islemi yapar.
/// </summary>
public interface ICloudStorageService
{
    // ─── Dosya Islemleri ───

    /// <summary>Kayit yukle (musterinin varsayilan storage'ina)</summary>
    Task<StorageUploadResult> UploadRecordingAsync(int customerId, Stream fileStream,
        string fileName, CancellationToken ct = default);

    /// <summary>Kayit indir</summary>
    Task<Stream?> DownloadRecordingAsync(int customerId, string fileId, CancellationToken ct = default);

    /// <summary>Kayit sil</summary>
    Task<bool> DeleteRecordingAsync(int customerId, string fileId, CancellationToken ct = default);

    /// <summary>Gecici indirme URL'i olustur (dinleme icin)</summary>
    Task<string?> GetRecordingUrlAsync(int customerId, string fileId, CancellationToken ct = default);

    // ─── Cagri Kaydi Upload/Download ───

    /// <summary>Ses kaydini bulut'a yukle ve CallRecord'u guncelle</summary>
    Task<RecordingUploadResultDto> UploadCallRecordingAsync(int customerId, Guid callUid,
        Stream fileStream, string fileName, CancellationToken ct = default);

    /// <summary>Ses kaydinin gecici indirme URL'ini olustur (30dk)</summary>
    Task<RecordingDownloadUrlDto?> GetCallRecordingUrlAsync(int customerId, Guid callUid,
        CancellationToken ct = default);

    /// <summary>Musterinin aktif cloud storage config'i var mi?</summary>
    Task<bool> HasActiveConfigAsync(int customerId);

    /// <summary>Musterinin default cloud config'ini decrypted olarak dondur (Windows app icin)</summary>
    Task<CloudConfigForClientDto?> GetConfigForClientAsync(int customerId);

    // ─── Config CRUD ───

    /// <summary>Musterinin storage config'lerini listele</summary>
    Task<List<StorageConfigListDto>> GetConfigsAsync(int? customerId = null);

    /// <summary>Config detayini getir (credential'lar maskelenmis)</summary>
    Task<StorageConfigDetailDto?> GetConfigByIdAsync(int configId);

    /// <summary>Yeni config olustur</summary>
    Task<(bool Success, int? Id, string? Error)> CreateConfigAsync(StorageConfigCreateDto dto);

    /// <summary>Config guncelle</summary>
    Task<(bool Success, string? Error)> UpdateConfigAsync(int configId, StorageConfigUpdateDto dto);

    /// <summary>Config sil</summary>
    Task<(bool Success, string? Error)> DeleteConfigAsync(int configId);

    // ─── Baglanti Testi ───

    /// <summary>Belirli bir config'in baglanti testini yapar</summary>
    Task<StorageTestResultDto> TestConnectionAsync(int configId, CancellationToken ct = default);

    // ─── Provider Bilgisi ───

    /// <summary>Desteklenen provider listesi</summary>
    List<StorageProviderInfoDto> GetAvailableProviders();
}
