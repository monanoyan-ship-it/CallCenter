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
                // ONCE upload (CloudFileId set edilsin), SONRA sync (CloudFileId backend'e gitsin)
                await _recordingUpload.UploadPendingRecordingsAsync(ct);
                await SyncUnsyncedRecordsAsync(ct);
                await SyncUnsyncedContactsAsync(ct);
                await SyncUnsyncedSipAccountsAsync(ct);
                await CleanupSyncedRecordsAsync();
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
        // Oncelikle: 1 saatten eski InProgress/Ringing kayitlari "Completed" yap
        // (EndCallAsync calismadiysa bu kayitlar sonsuza dek takilir)
        await RecoverStaleRecordsAsync();

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
        if (recording != null)
        {
            if (recording is { IsUploadedToCloud: true, CloudFileId: not null })
            {
                syncDto.CloudFileId = recording.CloudFileId;
                syncDto.CloudFileName = System.IO.Path.GetFileName(recording.FilePath);
                syncDto.CloudUploadedAt = recording.LastCloudUploadAttempt;
            }
            if (recording is { IsUploadedToPlatform: true, PlatformFileId: not null })
            {
                syncDto.PlatformFileId = recording.PlatformFileId;
                syncDto.PlatformUploadedAt = recording.LastPlatformUploadAttempt;
            }
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
    // ═══════════════════════════════════════
    // CONTACT BUFFER SYNC
    // ═══════════════════════════════════════

    private async Task SyncUnsyncedContactsAsync(CancellationToken ct)
    {
        var unsyncedContacts = await _localRepo.GetUnsyncedContactsAsync(BatchSize);
        if (unsyncedContacts.Count == 0) return;

        _logger.LogInformation("{Count} senkronlanmamis contact buffer kaydi bulundu", unsyncedContacts.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var contact in unsyncedContacts)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                bool success = contact.Operation switch
                {
                    "Create" => await PushContactCreateAsync(contact),
                    "Update" => await PushContactUpdateAsync(contact),
                    "Delete" => await PushContactDeleteAsync(contact),
                    _ => false
                };

                if (success)
                {
                    await _localRepo.DeleteContactAsync(contact.Uid);
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
                _logger.LogWarning(ex, "Contact sync basarisiz: {Uid} ({Op})", contact.Uid, contact.Operation);
            }
        }

        if (successCount > 0 || failCount > 0)
        {
            _logger.LogInformation("Contact sync: {Success} basarili, {Fail} basarisiz", successCount, failCount);
        }
    }

    private async Task<bool> PushContactCreateAsync(LocalContact contact)
    {
        var request = new CreateContactRequest
        {
            FullName = contact.FullName,
            PhoneNumber = contact.PhoneNumber,
            Company = contact.Company,
            Email = contact.Email
        };
        var response = await _http.PostAsJsonAsync("api/contact", request);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> PushContactUpdateAsync(LocalContact contact)
    {
        if (!contact.BackendContactId.HasValue) return false;
        var request = new UpdateContactRequest
        {
            FullName = contact.FullName,
            PhoneNumber = contact.PhoneNumber,
            Company = contact.Company,
            Email = contact.Email,
            IsFavorite = contact.IsFavorite
        };
        var response = await _http.PutAsJsonAsync($"api/contact/{contact.BackendContactId.Value}", request);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> PushContactDeleteAsync(LocalContact contact)
    {
        if (!contact.BackendContactId.HasValue) return false;
        var response = await _http.DeleteAsync($"api/contact/{contact.BackendContactId.Value}");
        return response.IsSuccessStatusCode;
    }

    // ═══════════════════════════════════════
    // SIP ACCOUNT BUFFER SYNC
    // ═══════════════════════════════════════

    private async Task SyncUnsyncedSipAccountsAsync(CancellationToken ct)
    {
        var unsyncedAccounts = await _localRepo.GetUnsyncedSipAccountsAsync(BatchSize);
        if (unsyncedAccounts.Count == 0) return;

        _logger.LogInformation("{Count} senkronlanmamis SIP hesabi bulundu", unsyncedAccounts.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var account in unsyncedAccounts)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (account.BackendSipAccountId == null)
                {
                    // Yeni — POST (Username/Password artik SipLine'da, gateway DTO'sunda yok)
                    var createDto = new SipAccountCreateDto
                    {
                        Name = account.Name,
                        Server = account.Server,
                        Port = account.Port,
                        Domain = account.Domain,
                        Transport = account.Transport,
                        WsUri = account.WsUri,
                        UseSrtp = account.UseSrtp,
                        StunServer = account.StunServer,
                        TurnServer = account.TurnServer,
                        TurnUsername = account.TurnUsername,
                        TurnPassword = account.TurnPassword,
                        PreferredCodecs = account.PreferredCodecs,
                        JitterBufferMinMs = account.JitterBufferMinMs,
                        JitterBufferMaxMs = account.JitterBufferMaxMs,
                        IsDefault = account.IsDefault
                    };

                    var response = await _http.PostAsJsonAsync("api/sipaccounts", createDto);
                    if (response.IsSuccessStatusCode)
                    {
                        await _localRepo.DeleteSipAccountAsync(account.Id);
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                else
                {
                    // Mevcut — PUT (Username/Password artik SipLine'da, gateway DTO'sunda yok)
                    var updateDto = new SipAccountUpdateDto
                    {
                        Name = account.Name,
                        Server = account.Server,
                        Port = account.Port,
                        Domain = account.Domain,
                        Transport = account.Transport,
                        WsUri = account.WsUri,
                        UseSrtp = account.UseSrtp,
                        StunServer = account.StunServer,
                        TurnServer = account.TurnServer,
                        TurnUsername = account.TurnUsername,
                        TurnPassword = account.TurnPassword,
                        PreferredCodecs = account.PreferredCodecs,
                        JitterBufferMinMs = account.JitterBufferMinMs,
                        JitterBufferMaxMs = account.JitterBufferMaxMs,
                        IsDefault = account.IsDefault,
                        IsActive = account.IsActive
                    };

                    var response = await _http.PutAsJsonAsync($"api/sipaccounts/{account.BackendSipAccountId}", updateDto);
                    if (response.IsSuccessStatusCode)
                    {
                        await _localRepo.DeleteSipAccountAsync(account.Id);
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogWarning(ex, "SIP account sync basarisiz: {Uid}", account.Uid);
            }
        }

        if (successCount > 0 || failCount > 0)
        {
            _logger.LogInformation("SIP account sync: {Success} basarili, {Fail} basarisiz", successCount, failCount);
        }
    }

    /// <summary>
    /// 1 saatten eski "InProgress" veya "Ringing" kayitlari kurtarir.
    /// EndCallAsync calismadiysa bu kayitlar sonsuza dek takilir.
    /// Status -> Completed, IsSyncedToBackend -> false (tekrar sync edilecek).
    /// </summary>
    private async Task RecoverStaleRecordsAsync()
    {
        try
        {
            var allRecords = await _localRepo.GetCallRecordsAsync(null, null, 1, 500);
            var staleRecords = allRecords
                .Where(r => r.Status is "Ringing" or "InProgress"
                         && r.StartedAt < DateTime.UtcNow.AddHours(-1))
                .ToList();

            foreach (var record in staleRecords)
            {
                record.Status = "Completed";
                record.EndedAt ??= record.AnsweredAt?.AddMinutes(30) ?? record.StartedAt.AddMinutes(1);
                if (record.AnsweredAt.HasValue)
                    record.DurationSeconds = (int)(record.EndedAt.Value - record.AnsweredAt.Value).TotalSeconds;
                record.IsSyncedToBackend = false; // Tekrar sync et
                await _localRepo.UpdateCallRecordAsync(record);
            }

            if (staleRecords.Count > 0)
                _logger.LogWarning("{Count} eski InProgress kayit Completed olarak kurtarildi", staleRecords.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stale record recovery hatasi");
        }
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
