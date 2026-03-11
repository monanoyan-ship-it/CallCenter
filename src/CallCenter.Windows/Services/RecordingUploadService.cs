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
            UploadLog($"Upload hedefi yok/inaktif. targets={targets != null}, platform={targets?.UploadToPlatform}, customer={targets?.UploadToCustomerStorage}");
            return;
        }

        var recordings = await _localRepo.GetUnuploadedRecordingsAsync(10);
        UploadLog($"Bekleyen kayit sayisi: {recordings.Count} (platform={targets.UploadToPlatform}, customer={targets.UploadToCustomerStorage})");
        if (recordings.Count == 0) return;

        _logger.LogInformation("{Count} ses kaydi yuklenecek (platform={Platform}, musteri={Customer})",
            recordings.Count, targets.UploadToPlatform, targets.UploadToCustomerStorage);

        foreach (var recording in recordings)
        {
            ct.ThrowIfCancellationRequested();
            UploadLog($"Isleniyor: {recording.FilePath} (cloud={recording.IsUploadedToCloud}, platform={recording.IsUploadedToPlatform})");

            if (!File.Exists(recording.FilePath))
            {
                UploadLog($"Dosya bulunamadi: {recording.FilePath}");
                await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
                await _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recording.Uid);
                continue;
            }

            // Platform hedefi
            if (targets.UploadToPlatform && targets.PlatformConfig != null
                && !recording.IsUploadedToPlatform && recording.PlatformUploadAttemptCount < MaxRetries)
            {
                UploadLog($"Platform upload basliyor: provider={targets.PlatformConfig.ProviderTypeId}");
                await UploadToTargetAsync(targets.PlatformConfig, recording, "platform", ct);
            }

            // Musteri hedefi
            if (targets.UploadToCustomerStorage && targets.CustomerConfig != null
                && !recording.IsUploadedToCloud && recording.CloudUploadAttemptCount < MaxRetries)
            {
                UploadLog($"Customer upload basliyor: provider={targets.CustomerConfig.ProviderTypeId}");
                await UploadToTargetAsync(targets.CustomerConfig, recording, "customer", ct);
            }

            // Tum aktif hedefler tamamlandiysa lokal dosyayi sil
            var allDone = IsAllTargetsComplete(recording, targets);
            UploadLog($"AllDone={allDone} (cloud={recording.IsUploadedToCloud}, platform={recording.IsUploadedToPlatform})");
            if (allDone)
            {
                try
                {
                    File.Delete(recording.FilePath);
                    UploadLog($"Lokal dosya silindi: {recording.FilePath}");
                }
                catch (Exception ex)
                {
                    UploadLog($"Dosya silme hatasi: {ex.Message}");
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
        // LocalDisk: dosyayi yerel klasore kopyala (cloud upload degil)
        if (config.ProviderTypeId == StorageProviders.Ids.LocalDisk)
        {
            await CopyToLocalDiskAsync(config, recording, targetName);
            return;
        }

        try
        {
            await using var fileStream = new FileStream(
                recording.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var fileName = Path.GetFileName(recording.FilePath);
            var (success, fileId, error) = await CloudUploadHelper.UploadAsync(config, fileStream, fileName, ct);

            if (success && fileId != null)
            {
                UploadLog($"{targetName} BASARILI: {fileName} → {fileId}");

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
                UploadLog($"{targetName} BASARISIZ: {error}");
                if (targetName == "platform")
                    await _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recording.Uid);
                else
                    await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
            }
        }
        catch (Exception ex)
        {
            UploadLog($"{targetName} HATA: {ex.Message}");
            if (targetName == "platform")
                await _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recording.Uid);
            else
                await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
        }
    }

    /// <summary>
    /// LocalDisk hedefi: .enc dosyasini kullanicinin ayarladigi klasore kopyalar.
    /// Kopyalama tamamlaninca kayit "uploaded" olarak isaretlenir.
    /// Tum hedefler tamamlaninca orijinal dosya silinir (UploadPendingRecordingsAsync'teki cleanup).
    /// </summary>
    private async Task CopyToLocalDiskAsync(
        CloudConfigForClientDto config,
        LocalRecording recording,
        string targetName)
    {
        try
        {
            var targetDir = config.BasePath;
            if (string.IsNullOrWhiteSpace(targetDir))
            {
                _logger.LogWarning("LocalDisk hedefi icin klasor yolu tanimlanmamis — atlaniyor");
                return;
            }

            if (!Directory.Exists(targetDir))
            {
                try
                {
                    Directory.CreateDirectory(targetDir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LocalDisk hedef klasoru olusturulamadi: {Path}", targetDir);
                    if (targetName == "platform")
                        await _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recording.Uid);
                    else
                        await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
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
            UploadLog($"{targetName} LocalDisk KOPYALANDI: {recording.FilePath} → {targetPath}");

            if (targetName == "platform")
                await _localRepo.MarkRecordingAsUploadedToPlatformAsync(recording.Uid, targetPath);
            else
                await _localRepo.MarkRecordingAsUploadedAsync(recording.Uid, targetPath);

            if (targetName == "platform")
            {
                recording.IsUploadedToPlatform = true;
                recording.PlatformFileId = targetPath;
            }
            else
            {
                recording.IsUploadedToCloud = true;
                recording.CloudFileId = targetPath;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Target} LocalDisk kopyalama hatasi: {Uid}", targetName, recording.Uid);
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
