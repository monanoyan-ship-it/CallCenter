using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using CallCenter.Windows.LocalData;
using CallCenter.Windows.LocalData.Entities;
using CallCenter.Windows.Services.CloudStorage;
using Microsoft.Extensions.Logging;

namespace CallCenter.Windows.Services;

/// <summary>
/// Lokal ses kayitlarini (.enc) musteri config'ine gore yukler/kopyalar.
/// Musteri config'inde:
///   - Cloud provider (GoogleDrive, S3, vb.) varsa → cloud'a yukle
///   - BasePath (klasor yolu) doluysa → o klasore kopyala
///   - Ikisi de varsa → ikisini de yap
///   - Ikisi de yoksa → dosya AppData'da kalir
///
/// Takip:
///   IsUploadedToCloud = cloud upload tamamlandi
///   IsUploadedToPlatform = lokal klasor kopyasi tamamlandi (BasePath)
/// </summary>
public class RecordingUploadService
{
    private readonly ILocalRepository _localRepo;
    private readonly HttpClient _http;
    private readonly SecureStorage _secureStorage;
    private readonly ILogger<RecordingUploadService> _logger;

    private const int MaxRetries = 5;
    private const string UploadTargetsCacheKey = "upload_targets_config";

    private RecordingUploadTargetsDto? _cachedTargets;
    private DateTime _lastTargetsFetch = DateTime.MinValue;
    private static readonly TimeSpan TargetsCacheExpiry = TimeSpan.FromMinutes(30);

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
    /// Bekleyen ses kayitlarini musteri config'ine gore yukle/kopyala.
    /// </summary>
    public async Task UploadPendingRecordingsAsync(CancellationToken ct = default)
    {
        var targets = await GetUploadTargetsAsync(ct);
        var config = targets?.CustomerConfig;

        if (targets == null || !targets.UploadToCustomerStorage || config == null)
        {
            UploadLog($"Musteri storage config yok/inaktif. targets={targets != null}, customer={targets?.UploadToCustomerStorage}");
            return;
        }

        // Cloud upload gerekli mi? (provider LocalDisk degilse)
        bool needsCloudUpload = config.ProviderTypeId != StorageProviders.Ids.LocalDisk;
        // Lokal klasor kopyasi gerekli mi? (BasePath doluysa)
        bool needsLocalCopy = !string.IsNullOrWhiteSpace(config.BasePath);

        if (!needsCloudUpload && !needsLocalCopy)
        {
            UploadLog("Ne cloud ne lokal hedef tanimli — atlaniyor.");
            return;
        }

        var recordings = await _localRepo.GetUnuploadedRecordingsAsync(10);
        UploadLog($"Bekleyen kayit: {recordings.Count} (cloud={needsCloudUpload}, localCopy={needsLocalCopy}, basePath={config.BasePath})");
        if (recordings.Count == 0) return;

        _logger.LogInformation("{Count} ses kaydi isleniyor (cloud={Cloud}, localCopy={Local})",
            recordings.Count, needsCloudUpload, needsLocalCopy);

        foreach (var recording in recordings)
        {
            ct.ThrowIfCancellationRequested();
            UploadLog($"Isleniyor: {recording.FilePath} (cloud={recording.IsUploadedToCloud}, localCopy={recording.IsUploadedToPlatform})");

            if (!File.Exists(recording.FilePath))
            {
                UploadLog($"Dosya bulunamadi: {recording.FilePath}");
                if (needsCloudUpload) await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
                if (needsLocalCopy) await _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recording.Uid);
                continue;
            }

            // 1. Cloud upload (provider LocalDisk degilse)
            if (needsCloudUpload && !recording.IsUploadedToCloud && recording.CloudUploadAttemptCount < MaxRetries)
            {
                UploadLog($"Cloud upload basliyor: provider={config.ProviderTypeId}");
                await UploadToCloudAsync(config, recording, ct);
            }

            // 2. Lokal klasor kopyasi (BasePath doluysa)
            if (needsLocalCopy && !recording.IsUploadedToPlatform && recording.PlatformUploadAttemptCount < MaxRetries)
            {
                UploadLog($"Lokal kopya basliyor: basePath={config.BasePath}");
                await CopyToLocalPathAsync(config.BasePath!, recording);
            }

            // Tum hedefler tamamlandiysa orijinal dosyayi sil
            bool cloudDone = !needsCloudUpload || recording.IsUploadedToCloud;
            bool localDone = !needsLocalCopy || recording.IsUploadedToPlatform;
            UploadLog($"cloudDone={cloudDone}, localDone={localDone}");

            if (cloudDone && localDone)
            {
                try
                {
                    if (File.Exists(recording.FilePath))
                        File.Delete(recording.FilePath);
                    await _localRepo.DeleteRecordingAsync(recording.Uid);
                    UploadLog($"Tamamlandi — orijinal dosya ve metadata silindi: {recording.FilePath}");
                }
                catch (Exception ex)
                {
                    UploadLog($"Temizlik hatasi: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Cloud provider'a yukle (GoogleDrive, S3, OneDrive, vb.)
    /// IsUploadedToCloud ile takip edilir.
    /// </summary>
    private async Task UploadToCloudAsync(
        CloudConfigForClientDto config,
        LocalRecording recording,
        CancellationToken ct)
    {
        try
        {
            await using var fileStream = new FileStream(
                recording.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var fileName = Path.GetFileName(recording.FilePath);
            var (success, fileId, error) = await CloudUploadHelper.UploadAsync(config, fileStream, fileName, ct);

            if (success && fileId != null)
            {
                UploadLog($"Cloud BASARILI: {fileName} → {fileId}");
                await _localRepo.MarkRecordingAsUploadedAsync(recording.Uid, fileId);
                recording.IsUploadedToCloud = true;
                recording.CloudFileId = fileId;
            }
            else
            {
                UploadLog($"Cloud BASARISIZ: {error}");
                await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
            }
        }
        catch (Exception ex)
        {
            UploadLog($"Cloud HATA: {ex.Message}");
            await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
        }
    }

    /// <summary>
    /// Dosyayi BasePath klasorune kopyala.
    /// IsUploadedToPlatform ile takip edilir (lokal kopya = "platform" alani).
    /// </summary>
    private async Task CopyToLocalPathAsync(string targetDir, LocalRecording recording)
    {
        try
        {
            if (!Directory.Exists(targetDir))
            {
                try
                {
                    Directory.CreateDirectory(targetDir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lokal hedef klasor olusturulamadi: {Path}", targetDir);
                    await _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recording.Uid);
                    return;
                }
            }

            var fileName = Path.GetFileName(recording.FilePath);
            var targetPath = Path.Combine(targetDir, fileName);

            if (File.Exists(targetPath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);
                targetPath = Path.Combine(targetDir, $"{nameWithoutExt}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}");
            }

            File.Copy(recording.FilePath, targetPath);
            UploadLog($"Lokal KOPYALANDI: {recording.FilePath} → {targetPath}");

            await _localRepo.MarkRecordingAsUploadedToPlatformAsync(recording.Uid, targetPath);
            recording.IsUploadedToPlatform = true;
            recording.PlatformFileId = targetPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lokal kopyalama hatasi: {Uid}", recording.Uid);
            await _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recording.Uid);
        }
    }

    private static void UploadLog(string msg)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CorpLynk", "upload-debug.log");
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}";
            File.AppendAllText(path, line);
        }
        catch { }
    }

    // ═══════════════════════════════════════
    // UPLOAD TARGETS CACHE
    // ═══════════════════════════════════════

    /// <summary>
    /// Upload hedeflerini once bellekten, sonra SecureStorage'dan, en son API'den al.
    /// 30 dakikada bir API'den taze hedefler cekilir.
    /// </summary>
    private async Task<RecordingUploadTargetsDto?> GetUploadTargetsAsync(CancellationToken ct)
    {
        // 1. Bellek cache'i kontrol
        if (_cachedTargets != null && DateTime.UtcNow - _lastTargetsFetch < TargetsCacheExpiry)
            return _cachedTargets;

        // 2. API'den cek (online ise)
        try
        {
            var response = await _http.GetAsync("api/recordings/upload-targets", ct);
            if (response.IsSuccessStatusCode)
            {
                var targets = await response.Content.ReadFromJsonAsync<RecordingUploadTargetsDto>(
                    cancellationToken: ct);
                if (targets != null)
                {
                    var json = JsonSerializer.Serialize(targets);
                    await _secureStorage.SetAsync(UploadTargetsCacheKey, json);

                    _cachedTargets = targets;
                    _lastTargetsFetch = DateTime.UtcNow;
                    return targets;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Upload targets API'den alinamadi — cache'e bakilacak");
        }

        // 3. SecureStorage'dan oku (offline mod)
        try
        {
            var cached = await _secureStorage.GetAsync(UploadTargetsCacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                _cachedTargets = JsonSerializer.Deserialize<RecordingUploadTargetsDto>(cached);
                _lastTargetsFetch = DateTime.UtcNow;
                return _cachedTargets;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SecureStorage'dan upload targets okunamadi");
        }

        return null;
    }
}
