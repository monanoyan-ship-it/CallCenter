using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Services;
using CallCenter.Windows.LocalData;
using CallCenter.Windows.LocalData.Entities;
using CallCenter.Windows.Services.CloudStorage;
using Microsoft.Extensions.Logging;

namespace CallCenter.Windows.Services;

/// <summary>
/// Lokal ses kayitlarini aktif upload hedeflerine yukler.
/// CustomerStorage -> musteri deposu (CloudFileId), PlatformStorage -> platform deposu (PlatformFileId).
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
    private static readonly TimeSpan RetryCooldown = TimeSpan.FromMinutes(30);

    private enum UploadTargetKind
    {
        CustomerStorage,
        PlatformStorage
    }

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
    /// Eksik metadata'yi onarir, sonra bekleyen kayitlari platform ve/veya musteri deposuna yollar.
    /// </summary>
    public async Task UploadPendingRecordingsAsync(CancellationToken ct = default)
    {
        await RecoverOrphanRecordingFilesAsync(ct);
        await RepairMissingRecordingMetadataAsync(ct);

        var targets = await GetUploadTargetsAsync(ct);
        if (targets == null)
        {
            UploadLog("Upload hedefleri alinamadi; kayitlar korunacak ve sonra tekrar denenecek.");
            return;
        }

        var needsCustomerUpload = targets.UploadToCustomerStorage && targets.CustomerConfig != null;
        var needsPlatformUpload = targets.UploadToPlatform && targets.PlatformConfig != null;

        if (!needsCustomerUpload && !needsPlatformUpload)
        {
            UploadLog($"Upload hedefi yok. platform={targets.UploadToPlatform}, customer={targets.UploadToCustomerStorage}");
            return;
        }

        var recordings = await _localRepo.GetUnuploadedRecordingsAsync(10);
        UploadLog($"Bekleyen kayit: {recordings.Count} (platform={needsPlatformUpload}, customer={needsCustomerUpload})");
        if (recordings.Count == 0) return;

        _logger.LogInformation("{Count} ses kaydi isleniyor (platform={Platform}, customer={Customer})",
            recordings.Count, needsPlatformUpload, needsCustomerUpload);

        foreach (var recording in recordings)
        {
            ct.ThrowIfCancellationRequested();
            UploadLog($"Isleniyor: {recording.FilePath} (customer={recording.IsUploadedToCloud}, platform={recording.IsUploadedToPlatform})");

            if (!File.Exists(recording.FilePath))
            {
                UploadLog($"Dosya bulunamadi: {recording.FilePath}");
                if (needsCustomerUpload &&
                    !recording.IsUploadedToCloud &&
                    ShouldAttemptUpload(recording.CloudUploadAttemptCount, recording.LastCloudUploadAttempt))
                    await _localRepo.UpdateRecordingUploadAttemptAsync(recording.Uid);
                if (needsPlatformUpload &&
                    !recording.IsUploadedToPlatform &&
                    ShouldAttemptUpload(recording.PlatformUploadAttemptCount, recording.LastPlatformUploadAttempt))
                    await _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recording.Uid);
                continue;
            }

            if (needsCustomerUpload &&
                !recording.IsUploadedToCloud &&
                ShouldAttemptUpload(recording.CloudUploadAttemptCount, recording.LastCloudUploadAttempt))
                await UploadToTargetAsync(targets.CustomerConfig!, recording, UploadTargetKind.CustomerStorage, ct);

            if (needsPlatformUpload &&
                !recording.IsUploadedToPlatform &&
                ShouldAttemptUpload(recording.PlatformUploadAttemptCount, recording.LastPlatformUploadAttempt))
                await UploadToTargetAsync(targets.PlatformConfig!, recording, UploadTargetKind.PlatformStorage, ct);

            if (IsCompleteForTargets(recording, targets))
            {
                // Orphan (CallRecordUid bos): bir CallRecord'a bagli degil. Buluta yuklendi ama
                // backend'in haberi yok. Once backend'e orphan olarak kaydet; ancak basariliysa sil
                // (aksi halde bulutta referanssiz kalir, dinle/esle ekrani goremez).
                var isOrphan = recording.CallRecordUid == Guid.Empty;
                if (isOrphan && !await RegisterOrphanAsync(recording, ct))
                {
                    UploadLog($"Orphan backend kaydi basarisiz; dosya korunuyor, sonra tekrar denenecek: {recording.FilePath}");
                    continue;
                }

                try
                {
                    if (File.Exists(recording.FilePath))
                        File.Delete(recording.FilePath);

                    // Normal kayit: metadata silinmez; BackgroundSync CloudFileId/PlatformFileId'yi CallRecord'a tasir.
                    // Orphan: backend'e kaydedildi, lokal metadata artik gereksiz — sil.
                    if (isOrphan)
                        await _localRepo.DeleteRecordingAsync(recording.Uid);

                    UploadLog($"Tamamlandi; orijinal dosya silindi ({(isOrphan ? "orphan" : "normal")}): {recording.FilePath}");
                }
                catch (Exception ex)
                {
                    UploadLog($"Temizlik hatasi: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Bir CallRecord'a baglanamayan (Uid'siz) kaydi backend'e orphan olarak kaydeder.
    /// Bulut referanslari (CloudFileId/PlatformFileId) zaten yuklenmis durumdadir.
    /// </summary>
    private async Task<bool> RegisterOrphanAsync(LocalRecording recording, CancellationToken ct)
    {
        try
        {
            var fileName = Path.GetFileName(recording.FilePath);
            var req = new RegisterOrphanRecordingRequest
            {
                FileName = fileName,
                RecordedAt = ParseTimestampFromFileName(fileName) ?? recording.CreatedAt,
                FileSize = recording.FileSize,
                FileHash = recording.FileHash,
                IsEncrypted = recording.IsEncrypted,
                CloudFileId = recording.CloudFileId,
                CloudFileName = fileName,
                PlatformFileId = recording.PlatformFileId,
                MachineId = Environment.MachineName,
                RetentionDate = recording.RetentionDate
            };

            var resp = await _http.PostAsJsonAsync("api/recordings/orphan", req, ct);
            if (resp.IsSuccessStatusCode)
            {
                UploadLog($"Orphan backend'e kaydedildi: {fileName}");
                return true;
            }

            UploadLog($"Orphan register HTTP {(int)resp.StatusCode}: {fileName}");
            return false;
        }
        catch (Exception ex)
        {
            UploadLog($"Orphan register hata: {ex.Message}");
            return false;
        }
    }

    /// <summary>call_{yyyyMMdd_HHmmss}_... dosya adindan kayit zamanini (UTC) cozer.</summary>
    private static DateTime? ParseTimestampFromFileName(string fileName)
    {
        var m = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d{8})_(\d{6})");
        if (m.Success && DateTime.TryParseExact(
                m.Groups[1].Value + m.Groups[2].Value, "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeLocal, out var dt))
            return dt.ToUniversalTime();
        return null;
    }

    /// <summary>
    /// Lokal cagri kaydinda dosya yolu oldugu halde recordings.json metadata'si yoksa yeniden olusturur.
    /// </summary>
    public async Task RepairMissingRecordingMetadataAsync(CancellationToken ct = default)
    {
        if (!_localRepo.IsConfigured) return;

        try
        {
            var callRecords = await _localRepo.GetCallRecordsAsync(null, null, 1, 1000);
            var recordings = await _localRepo.GetRecordingsAsync(null, 1, 5000);
            var knownPaths = recordings
                .Where(r => !string.IsNullOrWhiteSpace(r.FilePath))
                .Select(r => Path.GetFullPath(r.FilePath))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var repaired = 0;
            foreach (var call in callRecords)
            {
                ct.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(call.RecordingFilePath))
                    continue;

                var fullPath = Path.GetFullPath(call.RecordingFilePath);
                if (knownPaths.Contains(fullPath) || !File.Exists(fullPath))
                    continue;

                var fileInfo = new FileInfo(fullPath);
                string? fileHash = null;
                try
                {
                    fileHash = await FileEncryptionService.ComputeFileHashAsync(fullPath);
                }
                catch { }

                await _localRepo.SaveRecordingMetadataAsync(new LocalRecording
                {
                    CallRecordUid = call.Uid,
                    FilePath = fullPath,
                    FileSize = fileInfo.Length,
                    Format = fileInfo.Extension.TrimStart('.').ToLowerInvariant(),
                    DurationSeconds = call.DurationSeconds,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    FileHash = fileHash,
                    IsEncrypted = fileInfo.Extension.Equals(".enc", StringComparison.OrdinalIgnoreCase),
                    RetentionDate = DateTime.UtcNow.AddYears(10)
                });

                knownPaths.Add(fullPath);
                repaired++;
            }

            if (repaired > 0)
                UploadLog($"Eksik recording metadata onarildi: {repaired}");
        }
        catch (Exception ex)
        {
            UploadLog($"Recording metadata onarim hatasi: {ex.Message}");
            _logger.LogWarning(ex, "Recording metadata onarim hatasi");
        }
    }

    /// <summary>
    /// Recordings klasorunu tarar; recordings.json metadata'sinda olmayan ses dosyalarini bulur.
    /// Dosya adindaki callUid (32 hex segment) parse edilebiliyorsa o CallRecordUid ile metadata
    /// olusturulur ve normal upload+sync pipeline'i dosyayi buluta yukleyip ilgili CallRecord'a baglar.
    ///
    /// Uid parse edilemeyen (eski timestamp-only format) dosyalar GERCEK orphan'dir: bunlar
    /// diskte korunur, otomatik upload edilmez (backend orphan kaydi/eslestirme akisi gelmeden
    /// buluta yukleyip referanssiz birakmamak icin). Sadece sayilir ve loglanir.
    /// </summary>
    public async Task RecoverOrphanRecordingFilesAsync(CancellationToken ct = default)
    {
        if (!_localRepo.IsConfigured) return;

        try
        {
            var recordingsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CorpLynk", "Recordings");
            if (!Directory.Exists(recordingsDir)) return;

            var files = Directory
                .EnumerateFiles(recordingsDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".enc", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (files.Count == 0) return;

            var existing = await _localRepo.GetRecordingsAsync(null, 1, 5000);
            var knownPaths = existing
                .Where(r => !string.IsNullOrWhiteSpace(r.FilePath))
                .Select(r => Path.GetFullPath(r.FilePath))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var recovered = 0;
            var orphans = 0;
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                var full = Path.GetFullPath(file);
                if (knownPaths.Contains(full)) continue;

                // Yarim kalmis WAV: ayni isimde sifreli .enc varsa WAV'i atla (enc finalize edilmis surum).
                if (full.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(Path.ChangeExtension(full, ".enc")))
                    continue;

                var fi = new FileInfo(full);
                if (!fi.Exists || fi.Length == 0) continue;

                var callUid = TryParseCallUidFromFileName(Path.GetFileName(full));

                string? fileHash = null;
                try { fileHash = await FileEncryptionService.ComputeFileHashAsync(full); }
                catch { }

                await _localRepo.SaveRecordingMetadataAsync(new LocalRecording
                {
                    // Uid cozulduyse o CallRecord'a baglanir (otomatik eslesir).
                    // Cozulemediyse Guid.Empty -> orphan: pipeline buluta yukler ve
                    // backend'e orphan olarak kaydeder (dinle/esle ekrani goruntuler).
                    CallRecordUid = callUid ?? Guid.Empty,
                    FilePath = full,
                    FileSize = fi.Length,
                    Format = fi.Extension.TrimStart('.').ToLowerInvariant(),
                    CreatedAt = fi.CreationTimeUtc,
                    FileHash = fileHash,
                    IsEncrypted = fi.Extension.Equals(".enc", StringComparison.OrdinalIgnoreCase),
                    RetentionDate = DateTime.UtcNow.AddYears(10) // TTK md. 82
                });

                knownPaths.Add(full);
                if (callUid == null) orphans++;
                else recovered++;
            }

            if (recovered > 0 || orphans > 0)
                UploadLog($"Recovery sweep: {recovered} dosya kurtarildi (Uid eslesti), {orphans} orphan (Uid yok, buluta yuklenip backend'e orphan kaydedilecek).");
        }
        catch (Exception ex)
        {
            UploadLog($"Recovery sweep hatasi: {ex.Message}");
            _logger.LogWarning(ex, "Recording recovery sweep hatasi");
        }
    }

    /// <summary>
    /// Kayit dosya adindan callUid'i cozer. Format: call_{timestamp}_{numara}_{uidN}.wav
    /// uid 32 karakter hex (Guid "N" format) segmenttir. Bulunamazsa null (eski format / orphan).
    /// </summary>
    private static Guid? TryParseCallUidFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        // Sondan basa: ilk gecerli 32-hex segment uid'dir.
        for (var i = parts.Length - 1; i >= 0; i--)
        {
            var seg = parts[i];
            if (seg.Length == 32 && Guid.TryParseExact(seg, "N", out var g) && g != Guid.Empty)
                return g;
        }
        return null;
    }

    /// <summary>
    /// Bu cagriya ait dosya varsa ve gerekli upload hedefleri tamamlanmamissa true doner.
    /// Hedefler gecici olarak alinamazsa dosya kaybolmasin diye pending kabul edilir.
    /// </summary>
    public async Task<bool> HasPendingRequiredUploadsAsync(Guid callRecordUid, CancellationToken ct = default)
    {
        var recordings = await _localRepo.GetRecordingsAsync(callRecordUid, 1, 100);
        if (recordings.Count == 0) return false;

        var targets = await GetUploadTargetsAsync(ct);
        foreach (var recording in recordings)
        {
            if (!File.Exists(recording.FilePath))
                continue;

            if (targets == null || !IsCompleteForTargets(recording, targets))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Herhangi bir cagri icin tamamlanmasi gereken upload isi varsa true doner.
    /// Logout/exit guard bu metodu kullanir; hedefler alinamazsa dosya kaybolmasin diye pending kabul edilir.
    /// </summary>
    public async Task<bool> HasAnyPendingRequiredUploadsAsync(CancellationToken ct = default)
    {
        await RepairMissingRecordingMetadataAsync(ct);

        var recordings = await _localRepo.GetUnuploadedRecordingsAsync(100);
        if (recordings.Count == 0) return false;

        var targets = await GetUploadTargetsAsync(ct);
        foreach (var recording in recordings)
        {
            if (!File.Exists(recording.FilePath))
                continue;

            if (targets == null || !IsCompleteForTargets(recording, targets))
                return true;
        }

        return false;
    }

    private async Task UploadToTargetAsync(
        CloudConfigForClientDto config,
        LocalRecording recording,
        UploadTargetKind target,
        CancellationToken ct)
    {
        if (config.ProviderTypeId == StorageProviders.Ids.LocalDisk)
        {
            await CopyToLocalPathAsync(config.BasePath, recording, target);
            return;
        }

        await UploadToCloudAsync(config, recording, target, ct);
    }

    private async Task UploadToCloudAsync(
        CloudConfigForClientDto config,
        LocalRecording recording,
        UploadTargetKind target,
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
                UploadLog($"{TargetLabel(target)} cloud BASARILI: {fileName} -> {fileId}");
                await MarkUploadedAsync(recording, target, fileId);
                await MarkCallRecordForResyncAsync(recording.CallRecordUid);
            }
            else
            {
                UploadLog($"{TargetLabel(target)} cloud BASARISIZ: {error}");
                await UpdateAttemptAsync(recording.Uid, target);
            }
        }
        catch (Exception ex)
        {
            UploadLog($"{TargetLabel(target)} cloud HATA: {ex.Message}");
            await UpdateAttemptAsync(recording.Uid, target);
        }
    }

    private async Task CopyToLocalPathAsync(string? targetDir, LocalRecording recording, UploadTargetKind target)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetDir))
            {
                UploadLog($"{TargetLabel(target)} LocalDisk hedef klasoru bos.");
                await UpdateAttemptAsync(recording.Uid, target);
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
                    _logger.LogWarning(ex, "Lokal hedef klasor olusturulamadi: {Path}", targetDir);
                    await UpdateAttemptAsync(recording.Uid, target);
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
            UploadLog($"{TargetLabel(target)} LocalDisk KOPYALANDI: {recording.FilePath} -> {targetPath}");

            await MarkUploadedAsync(recording, target, targetPath);
            await MarkCallRecordForResyncAsync(recording.CallRecordUid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lokal kopyalama hatasi: {Uid}", recording.Uid);
            await UpdateAttemptAsync(recording.Uid, target);
        }
    }

    private async Task MarkUploadedAsync(LocalRecording recording, UploadTargetKind target, string fileId)
    {
        if (target == UploadTargetKind.CustomerStorage)
        {
            await _localRepo.MarkRecordingAsUploadedAsync(recording.Uid, fileId);
            recording.IsUploadedToCloud = true;
            recording.CloudFileId = fileId;
            recording.LastCloudUploadAttempt = DateTime.UtcNow;
        }
        else
        {
            await _localRepo.MarkRecordingAsUploadedToPlatformAsync(recording.Uid, fileId);
            recording.IsUploadedToPlatform = true;
            recording.PlatformFileId = fileId;
            recording.LastPlatformUploadAttempt = DateTime.UtcNow;
        }
    }

    private Task UpdateAttemptAsync(Guid recordingUid, UploadTargetKind target)
    {
        return target == UploadTargetKind.CustomerStorage
            ? _localRepo.UpdateRecordingUploadAttemptAsync(recordingUid)
            : _localRepo.UpdateRecordingPlatformUploadAttemptAsync(recordingUid);
    }

    private async Task MarkCallRecordForResyncAsync(Guid callRecordUid)
    {
        try
        {
            var record = await _localRepo.GetCallRecordByUidAsync(callRecordUid);
            if (record == null) return;

            record.IsSyncedToBackend = false;
            await _localRepo.UpdateCallRecordAsync(record);
        }
        catch (Exception ex)
        {
            UploadLog($"Call record re-sync isaretleme hatasi: {ex.Message}");
        }
    }

    private static bool IsCompleteForTargets(LocalRecording recording, RecordingUploadTargetsDto targets)
    {
        var needsCustomer = targets.UploadToCustomerStorage && targets.CustomerConfig != null;
        var needsPlatform = targets.UploadToPlatform && targets.PlatformConfig != null;

        // Hic hedef yoksa dosyayi ve metadata'yi koru; ileride hedef acilinca upload edilebilir.
        if (!needsCustomer && !needsPlatform)
            return false;

        return (!needsCustomer || recording.IsUploadedToCloud)
            && (!needsPlatform || recording.IsUploadedToPlatform);
    }

    private static bool ShouldAttemptUpload(int attemptCount, DateTime? lastAttempt)
    {
        if (attemptCount < MaxRetries || lastAttempt == null)
            return true;

        var lastUtc = lastAttempt.Value.Kind == DateTimeKind.Local
            ? lastAttempt.Value.ToUniversalTime()
            : lastAttempt.Value;

        return DateTime.UtcNow - lastUtc >= RetryCooldown;
    }

    private static string TargetLabel(UploadTargetKind target)
        => target == UploadTargetKind.CustomerStorage ? "CustomerStorage" : "PlatformStorage";

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

    /// <summary>
    /// Upload hedeflerini once bellekten, sonra SecureStorage'dan, en son API'den al.
    /// 30 dakikada bir API'den taze hedefler cekilir.
    /// </summary>
    private async Task<RecordingUploadTargetsDto?> GetUploadTargetsAsync(CancellationToken ct)
    {
        if (_cachedTargets != null && DateTime.UtcNow - _lastTargetsFetch < TargetsCacheExpiry)
            return _cachedTargets;

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
            _logger.LogDebug(ex, "Upload targets API'den alinamadi; cache'e bakilacak");
        }

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
