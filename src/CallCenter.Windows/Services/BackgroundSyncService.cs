using System.Net.Http;
using System.Net.Http.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Windows.LocalData;
using CallCenter.Windows.LocalData.Entities;
using Microsoft.Extensions.Logging;

namespace CallCenter.Windows.Services;

/// <summary>
/// Arka planda calisan senkronizasyon servisi.
///
/// Her 60 saniyede bir:
///   1. Lokal DB'den "senkronlanmamis" kayitlari alir (IsSyncedToBackend = false)
///   2. Her birini backend API'ye push eder
///   3. Basariliysa kaydi "senkronlandi" olarak isaretler
///
/// Amac: CallSyncService'in backend push'u basarisiz oldugunda
/// verilerin kaybolmamasini saglamak. Internet kesilse bile
/// lokal kayitlar korunur ve baglanti geldiginde otomatik senkronlanir.
///
/// Yasam dongusu:
///   StartAsync() → uygulama basladiginda cagirilir
///   StopAsync()  → uygulama kapanirken cagirilir
/// </summary>
public class BackgroundSyncService
{
    private readonly ILocalRepository _localRepo;
    private readonly HttpClient _http;
    private readonly ILogger<BackgroundSyncService> _logger;
    private readonly RecordingUploadService _recordingUpload;

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    /// <summary>Senkronizasyon araligi (varsayilan: 60 saniye)</summary>
    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(60);

    /// <summary>Tek seferde en fazla kac kayit push edilir</summary>
    private const int BatchSize = 50;

    public BackgroundSyncService(
        ILocalRepository localRepo,
        HttpClient http,
        ILogger<BackgroundSyncService> logger,
        RecordingUploadService recordingUpload)
    {
        _localRepo = localRepo;
        _http = http;
        _logger = logger;
        _recordingUpload = recordingUpload;
    }

    // ═══════════════════════════════════════
    // BASLAT / DURDUR
    // ═══════════════════════════════════════

    /// <summary>
    /// Arka plan senkronizasyonunu baslatir.
    /// Lokal DB yapilandirilmadiysa sessizce cikar (NullLocalRepository).
    /// </summary>
    public void StartAsync()
    {
        if (!_localRepo.IsConfigured)
        {
            _logger.LogInformation("Lokal DB yapilandirilmamis — senkronizasyon devre disi");
            return;
        }

        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(SyncInterval);
        _runningTask = RunSyncLoopAsync(_cts.Token);

        _logger.LogInformation("BackgroundSyncService baslatildi (aralik: {Interval}s)", SyncInterval.TotalSeconds);
    }

    /// <summary>
    /// Arka plan senkronizasyonunu durdurur. Uygulama kapanirken cagirilir.
    /// </summary>
    public async Task StopAsync()
    {
        if (_cts == null) return;

        _cts.Cancel();
        _timer?.Dispose();

        if (_runningTask != null)
        {
            try { await _runningTask; }
            catch (OperationCanceledException) { } // beklenen
        }

        _logger.LogInformation("BackgroundSyncService durduruldu");
    }

    // ═══════════════════════════════════════
    // ANA DONGU
    // ═══════════════════════════════════════

    private async Task RunSyncLoopAsync(CancellationToken ct)
    {
        // Ilk calistirmada 10 saniye bekle (uygulamanin tam acilmasini bekle)
        try { await Task.Delay(TimeSpan.FromSeconds(10), ct); }
        catch (OperationCanceledException) { return; }

        while (await _timer!.WaitForNextTickAsync(ct))
        {
            try
            {
                await SyncUnsyncedRecordsAsync(ct);
                await CleanupSyncedRecordsAsync();
                await _recordingUpload.UploadPendingRecordingsAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Hic bir hata donguyu kirmamali — bir sonraki turda tekrar dene
                _logger.LogError(ex, "Senkronizasyon dongusunde hata");
            }
        }
    }

    // ═══════════════════════════════════════
    // SENKRONIZASYON MANTIGI
    // ═══════════════════════════════════════

    /// <summary>
    /// Senkronlanmamis kayitlari backend'e push eder.
    ///
    /// Her kayit icin:
    ///   - "Completed" veya "Missed" ise → tam kayit push (POST /api/calls/sync)
    ///   - Diger durumlarda (devam eden arama) → atla
    /// </summary>
    private async Task SyncUnsyncedRecordsAsync(CancellationToken ct)
    {
        var unsyncedRecords = await _localRepo.GetUnsyncedRecordsAsync(BatchSize);

        if (unsyncedRecords.Count == 0) return;

        _logger.LogInformation("{Count} senkronlanmamis kayit bulundu", unsyncedRecords.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var record in unsyncedRecords)
        {
            ct.ThrowIfCancellationRequested();

            // Devam eden aramalari atla — henuz tamamlanmamis
            if (record.Status is "Ringing" or "InProgress")
                continue;

            try
            {
                var backendCallId = await PushRecordToBackendAsync(record);

                if (backendCallId.HasValue)
                {
                    // Sync basarili → lokalden sil (write-ahead buffer pattern)
                    await _localRepo.DeleteCallRecordAsync(record.Uid);
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogWarning(ex, "Kayit push basarisiz: {Uid}", record.Uid);

                // Lokal DB'de son deneme zamanini guncelle
                try
                {
                    record.LastSyncAttempt = DateTime.UtcNow;
                    await _localRepo.UpdateCallRecordAsync(record);
                }
                catch { }
            }
        }

        if (successCount > 0 || failCount > 0)
        {
            _logger.LogInformation("Senkronizasyon tamamlandi: {Success} basarili, {Fail} basarisiz",
                successCount, failCount);
        }
    }

    /// <summary>
    /// Tek bir kaydi backend'e push eder.
    /// Backend tarafinda Uid bazli duplicate kontrolu var (idempotent).
    /// Ayni Uid ile birden fazla push yapilsa bile tek kayit olusur.
    /// </summary>
    /// <returns>Backend CallRecord ID'si (basariliysa), null (basarisizsa)</returns>
    private async Task<int?> PushRecordToBackendAsync(LocalCallRecord record)
    {
        // Shared DTO kullan — backend'teki CallSyncPushRequest ile ayni
        var syncDto = new CallSyncPushRequest
        {
            Uid = record.Uid,
            CallerNumber = record.CallerNumber,
            CalleeNumber = record.CalleeNumber,
            Direction = record.Direction,
            Status = record.Status,
            StartedAt = record.StartedAt,
            AnsweredAt = record.AnsweredAt,
            EndedAt = record.EndedAt,
            DurationSeconds = record.DurationSeconds,
            AgentName = record.AgentName,
            QueueName = record.QueueName,
            Notes = record.Notes,
            RecordingFilePath = record.RecordingFilePath,
            IsRecordingEncrypted = record.RecordingFilePath?.EndsWith(".enc", StringComparison.OrdinalIgnoreCase) ?? false,
            RecordingRetentionDate = DateTime.UtcNow.AddYears(10) // TTK md. 82
        };

        // Bulut depolama bilgilerini ekle (Windows app direkt yuklediyse)
        var recordings = await _localRepo.GetRecordingsAsync(record.Uid, 1, 1);
        var recording = recordings.FirstOrDefault();
        if (recording is { IsUploadedToCloud: true, CloudFileId: not null })
        {
            syncDto.CloudFileId = recording.CloudFileId;
            syncDto.CloudFileName = System.IO.Path.GetFileName(recording.FilePath);
            syncDto.CloudUploadedAt = recording.LastCloudUploadAttempt;
        }

        var response = await _http.PostAsJsonAsync("api/calls/sync", syncDto);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<CallSyncPushResponse>();
            return result?.Id;
        }

        _logger.LogWarning("Backend sync push basarisiz: {Status} — {Uid}",
            response.StatusCode, record.Uid);
        return null;
    }
    /// <summary>
    /// CallSyncService tarafindan realtime push ile sync edilmis
    /// tamamlanmis/cevapsiz kayitlari lokalden temizler.
    /// (StartCallAsync basarili push yapar → IsSyncedToBackend=true,
    ///  ama kayit devam eder. Arama bittiginde burada temizlenir.)
    /// </summary>
    private async Task CleanupSyncedRecordsAsync()
    {
        try
        {
            // Sync edilmis + tamamlanmis/cevapsiz kayitlari bul
            var syncedRecords = await _localRepo.GetCallRecordsAsync(null, null, 1, 500);
            var toDelete = syncedRecords
                .Where(r => r.IsSyncedToBackend && r.Status is "Completed" or "Missed" or "Failed")
                .ToList();

            foreach (var record in toDelete)
            {
                await _localRepo.DeleteCallRecordAsync(record.Uid);
            }

            if (toDelete.Count > 0)
            {
                _logger.LogInformation("{Count} sync edilmis kayit lokalden temizlendi", toDelete.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sync edilmis kayit temizleme hatasi");
        }
    }
}
