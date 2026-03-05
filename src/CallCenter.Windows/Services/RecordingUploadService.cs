using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Windows.LocalData;
using CallCenter.Windows.LocalData.Entities;
using CallCenter.Windows.Services.CloudStorage;
using Microsoft.Extensions.Logging;

namespace CallCenter.Windows.Services;

/// <summary>
/// Lokal ses kayitlarini (.enc) dogrudan bulut depolamaya yukler.
/// Cift hedef destekler: Platform deposu + Musteri deposu.
/// Musterinin tercihleri API'den alinir (upload-targets endpoint).
///
/// Akis:
///   1. Upload hedeflerini API'den cek veya cache'ten oku (30 dk)
///   2. Yuklenmemis kayitlari lokal DB'den al (max 5 deneme/hedef)
///   3. Her kayit icin: aktif hedeflere yukle
///   4. Tum aktif hedefler tamamlaninca lokal .enc sil
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
    /// Bekleyen ses kayitlarini bulut hedeflerine yukle.
    /// Hedef yoksa sessizce cikar.
    /// </summary>
    public async Task UploadPendingRecordingsAsync(CancellationToken ct = default)
    {
        var targets = await GetUploadTargetsAsync(ct);
        if (targets == null || (!targets.UploadToPlatform && !targets.UploadToCustomerStorage))
        {
            _logger.LogDebug("Hic bir upload hedefi aktif degil — upload atlaniyor");
            return;
        }

        var recordings = await _localRepo.GetUnuploadedRecordingsAsync(10);
        if (recordings.Count == 0) return;

        _logger.LogInformation("{Count} ses kaydi yuklenecek (platform={Platform}, musteri={Customer})",
            recordings.Count, targets.UploadToPlatform, targets.UploadToCustomerStorage);

        foreach (var recording in recordings)
        {
            ct.ThrowIfCancellationRequested();

            if (!File.Exists(recording.FilePath))
            {
                _logger.LogWarning("Ses kaydi dosyasi bulunamadi: {Path}", recording.FilePath);
                // Her iki hedef icin de attempt artir
                await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
                await _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recording.Uid);
                continue;
            }

            // Platform hedefi
            if (targets.UploadToPlatform && targets.PlatformConfig != null
                && !recording.IsUploadedToPlatform && recording.PlatformUploadAttemptCount < MaxRetries)
            {
                await UploadToTargetAsync(targets.PlatformConfig, recording, "platform", ct);
            }

            // Musteri hedefi
            if (targets.UploadToCustomerStorage && targets.CustomerConfig != null
                && !recording.IsUploadedToCloud && recording.CloudUploadAttemptCount < MaxRetries)
            {
                await UploadToTargetAsync(targets.CustomerConfig, recording, "customer", ct);
            }

            // Tum aktif hedefler tamamlandiysa lokal dosyayi sil
            var allDone = IsAllTargetsComplete(recording, targets);
            if (allDone)
            {
                try
                {
                    File.Delete(recording.FilePath);
                    _logger.LogInformation("Tum hedeflere yuklendi, lokal silindi: {Path}", recording.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Dosya silme hatasi: {Path}", recording.FilePath);
                }
            }
        }
    }

    private async Task UploadToTargetAsync(
        CloudConfigForClientDto config,
        LocalRecording recording,
        string targetName,
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
                _logger.LogInformation("{Target} deposuna yuklendi: {FileName} → {FileId}",
                    targetName, fileName, fileId);

                if (targetName == "platform")
                    await _localRepo.MarkRecordingAsUploadedToPlatformAsync(recording.Uid, fileId);
                else
                    await _localRepo.MarkRecordingAsUploadedAsync(recording.Uid, fileId);

                // In-memory nesneyi de guncelle (ayni dogu icinde kontrol icin)
                if (targetName == "platform")
                {
                    recording.IsUploadedToPlatform = true;
                    recording.PlatformFileId = fileId;
                }
                else
                {
                    recording.IsUploadedToCloud = true;
                    recording.CloudFileId = fileId;
                }
            }
            else
            {
                _logger.LogWarning("{Target} upload basarisiz: {Error}", targetName, error);
                if (targetName == "platform")
                    await _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recording.Uid);
                else
                    await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Target} upload hatasi: {Uid}", targetName, recording.Uid);
            if (targetName == "platform")
                await _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recording.Uid);
            else
                await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
        }
    }

    private static bool IsAllTargetsComplete(LocalRecording recording, RecordingUploadTargetsDto targets)
    {
        if (targets.UploadToPlatform && !recording.IsUploadedToPlatform) return false;
        if (targets.UploadToCustomerStorage && !recording.IsUploadedToCloud) return false;
        return true;
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
