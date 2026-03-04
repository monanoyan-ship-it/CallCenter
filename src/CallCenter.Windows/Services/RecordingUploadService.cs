using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Windows.LocalData;
using CallCenter.Windows.Services.CloudStorage;
using Microsoft.Extensions.Logging;

namespace CallCenter.Windows.Services;

/// <summary>
/// Lokal ses kayitlarini (.enc) dogrudan bulut depolamaya yukler.
/// API'ye proxy olarak gondermez — Windows app bagimsiz olarak yukler.
///
/// Akis:
///   1. Cloud config'i API'den cek veya cache'ten oku
///   2. Yuklenmemis kayitlari lokal DB'den al (max 5 deneme)
///   3. Her kayit icin: dosya var mi → CloudUploadHelper ile yukle → basarili → lokal sil
///   4. Upload sonrasi CloudFileId, LocalRecording'e kaydedilir
///      ve BackgroundSync ile API'ye push edilir
/// </summary>
public class RecordingUploadService
{
    private readonly ILocalRepository _localRepo;
    private readonly HttpClient _http;
    private readonly SecureStorage _secureStorage;
    private readonly ILogger<RecordingUploadService> _logger;

    private const int MaxRetries = 5;
    private const string CloudConfigCacheKey = "cloud_storage_config";

    // Bellekte tutulan config (her seferinde SecureStorage'a gitmemek icin)
    private CloudConfigForClientDto? _cachedConfig;
    private DateTime _lastConfigFetch = DateTime.MinValue;
    private static readonly TimeSpan ConfigCacheExpiry = TimeSpan.FromMinutes(30);

    public RecordingUploadService(
        ILocalRepository localRepo,
        HttpClient http,
        SecureStorage secureStorage,
        ILogger<RecordingUploadService> logger)
    {
        _localRepo = localRepo;
        _http = http;
        _secureStorage = secureStorage;
        _logger = logger;
    }

    /// <summary>
    /// Bekleyen ses kayitlarini bulut'a yukle.
    /// Cloud config yoksa sessizce cikar.
    /// </summary>
    public async Task UploadPendingRecordingsAsync(CancellationToken ct = default)
    {
        // Cloud config'i al (API'den veya cache'ten)
        var config = await GetCloudConfigAsync(ct);
        if (config == null)
        {
            _logger.LogDebug("Cloud storage yapilandirilmamis — upload atlaniyor");
            return;
        }

        // Yuklenmemis kayitlari al
        var recordings = await _localRepo.GetUnuploadedRecordingsAsync(10);
        if (recordings.Count == 0) return;

        _logger.LogInformation("{Count} ses kaydi bulut'a yuklenecek", recordings.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var recording in recordings)
        {
            ct.ThrowIfCancellationRequested();

            if (recording.CloudUploadAttemptCount >= MaxRetries)
            {
                _logger.LogWarning("Kayit {Uid} max deneme sayisina ulasti ({Max}), atlaniyor",
                    recording.Uid, MaxRetries);
                continue;
            }

            if (!File.Exists(recording.FilePath))
            {
                _logger.LogWarning("Ses kaydi dosyasi bulunamadi: {Path}", recording.FilePath);
                await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
                failCount++;
                continue;
            }

            try
            {
                var uploaded = await UploadSingleRecordingAsync(config, recording, ct);
                if (uploaded)
                {
                    // Basarili: lokal .enc dosyasini sil
                    await _localRepo.MarkRecordingAsUploadedAsync(recording.Uid, recording.CloudFileId);
                    try
                    {
                        File.Delete(recording.FilePath);
                        _logger.LogInformation("Yuklendi ve silindi: {Path}", recording.FilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Dosya silme hatasi: {Path}", recording.FilePath);
                    }
                    successCount++;
                }
                else
                {
                    await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kayit yukleme hatasi: {Uid}", recording.Uid);
                await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
                failCount++;
            }
        }

        if (successCount > 0 || failCount > 0)
        {
            _logger.LogInformation("Bulut yukleme tamamlandi: {Success} basarili, {Fail} basarisiz",
                successCount, failCount);
        }
    }

    /// <summary>
    /// Tek bir ses kaydini CloudUploadHelper ile dogrudan bulut'a yukle.
    /// API'ye proxy gondermez — Windows bagimsiz yukler.
    /// </summary>
    private async Task<bool> UploadSingleRecordingAsync(
        CloudConfigForClientDto config,
        LocalData.Entities.LocalRecording recording,
        CancellationToken ct)
    {
        await using var fileStream = new FileStream(
            recording.FilePath, FileMode.Open, FileAccess.Read);

        var fileName = Path.GetFileName(recording.FilePath);

        var (success, fileId, error) = await CloudUploadHelper.UploadAsync(
            config, fileStream, fileName, ct);

        if (success && fileId != null)
        {
            _logger.LogInformation("Bulut'a yuklendi: {FileName} → {FileId}", fileName, fileId);
            recording.CloudFileId = fileId;
            return true;
        }

        _logger.LogWarning("Bulut upload basarisiz: {Error}", error);
        return false;
    }

    // ═══════════════════════════════════════
    // CLOUD CONFIG CACHE
    // ═══════════════════════════════════════

    /// <summary>
    /// Cloud config'i once bellekten, sonra SecureStorage'dan, en son API'den al.
    /// 30 dakikada bir API'den taze config cekilir.
    /// </summary>
    private async Task<CloudConfigForClientDto?> GetCloudConfigAsync(CancellationToken ct)
    {
        // 1. Bellek cache'i kontrol
        if (_cachedConfig != null && DateTime.UtcNow - _lastConfigFetch < ConfigCacheExpiry)
            return _cachedConfig;

        // 2. API'den cek (online ise)
        try
        {
            var response = await _http.GetAsync("api/recordings/cloud-config", ct);
            if (response.IsSuccessStatusCode)
            {
                var config = await response.Content.ReadFromJsonAsync<CloudConfigForClientDto>(
                    cancellationToken: ct);
                if (config != null)
                {
                    // SecureStorage'a kaydet (offline icin)
                    var json = JsonSerializer.Serialize(config);
                    await _secureStorage.SetAsync(CloudConfigCacheKey, json);

                    _cachedConfig = config;
                    _lastConfigFetch = DateTime.UtcNow;
                    return config;
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Cloud config yok — cache temizle
                await _secureStorage.RemoveAsync(CloudConfigCacheKey);
                _cachedConfig = null;
                _lastConfigFetch = DateTime.UtcNow;
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Cloud config API'den alinamadi — cache'e bakilacak");
        }

        // 3. SecureStorage'dan oku (offline mod)
        try
        {
            var cached = await _secureStorage.GetAsync(CloudConfigCacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                _cachedConfig = JsonSerializer.Deserialize<CloudConfigForClientDto>(cached);
                _lastConfigFetch = DateTime.UtcNow;
                return _cachedConfig;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SecureStorage'dan cloud config okunamadi");
        }

        return null;
    }
}
