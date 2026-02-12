using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Windows.LocalData;
using CallCenter.Windows.LocalData.Entities;
using Microsoft.Extensions.Logging;

namespace CallCenter.Windows.Services;

/// <summary>
/// Cagri verisi akisini yoneten "cift yazim" servisi.
///
/// Her cagri isleminde (baslat, cevapla, bitir) su siralamayla calisir:
///   1. Lokal DB'ye yaz  (LocalCallRecord — aninda, guvenilir)
///   2. Backend API'ye push et  (async, arka planda — basarisiz olursa retry)
///
/// Lokal DB yoksa (NullLocalRepository) sadece backend'e yazar (eski davranis).
/// Backend erisim yoksa sadece lokale yazar — BackgroundSyncService sonra push eder.
///
/// Kullanim (Dialer.razor'dan):
///   var uid = await CallSync.StartCallAsync("me", "05551234567");
///   await CallSync.AnswerCallAsync(uid);
///   await CallSync.EndCallAsync(uid);
/// </summary>
public class CallSyncService
{
    private readonly ILocalRepository _localRepo;
    private readonly HttpClient _http;
    private readonly ILogger<CallSyncService> _logger;
    private readonly WindowsAuthService _authService;

    public CallSyncService(
        ILocalRepository localRepo,
        HttpClient http,
        ILogger<CallSyncService> logger,
        WindowsAuthService authService)
    {
        _localRepo = localRepo;
        _http = http;
        _logger = logger;
        _authService = authService;
    }

    // ═══════════════════════════════════════
    // ARAMA BASLAT
    // ═══════════════════════════════════════

    /// <summary>
    /// Yeni bir cagri baslatir:
    ///   1. Lokal DB'ye LocalCallRecord yazar
    ///   2. Backend'e POST /api/calls/start gonderir
    ///   3. Backend'ten donen ID'yi lokal kayda yazar
    ///
    /// Backend basarisiz olursa lokal kayit "unsynced" kalir,
    /// BackgroundSyncService sonra push eder.
    /// </summary>
    /// <returns>Cagri Uid'si (hem lokal hem backend icin ortak referans)</returns>
    public async Task<CallSyncResult> StartCallAsync(string callerNumber, string calleeNumber, int? queueId = null)
    {
        // ── 1. Lokal DB'ye yaz ──
        var localRecord = new LocalCallRecord
        {
            CallerNumber = callerNumber,
            CalleeNumber = calleeNumber,
            Direction = "Outbound",
            Status = "Ringing",
            StartedAt = DateTime.UtcNow,
            AgentName = await _authService.GetFullNameAsync()
        };

        if (_localRepo.IsConfigured)
        {
            try
            {
                await _localRepo.SaveCallRecordAsync(localRecord);
                _logger.LogInformation("Lokal kayit olusturuldu: {Uid}", localRecord.Uid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lokal DB yazim hatasi (StartCall)");
            }
        }

        // ── 2. Backend'e push et ──
        int? backendCallId = null;
        try
        {
            var request = new StartCallRequest
            {
                CallerNumber = callerNumber,
                CalleeNumber = calleeNumber,
                QueueId = queueId
            };

            var response = await _http.PostAsJsonAsync("api/calls/start", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CallStartResult>();
                backendCallId = result?.Id;

                // Backend push basarili → lokal kaydi senkronlandi olarak isaretle
                if (_localRepo.IsConfigured && backendCallId.HasValue)
                {
                    await _localRepo.MarkAsSyncedAsync(localRecord.Uid, backendCallId);
                }

                _logger.LogInformation("Backend push basarili: CallId={CallId}", backendCallId);
            }
            else
            {
                _logger.LogWarning("Backend push basarisiz: {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Backend'e erisilemiyor (StartCall) — sonra retry edilecek");
        }

        return new CallSyncResult
        {
            Uid = localRecord.Uid,
            BackendCallId = backendCallId
        };
    }

    // ═══════════════════════════════════════
    // ARAMA CEVAPLANDI
    // ═══════════════════════════════════════

    /// <summary>
    /// Cagri cevaplandiginda:
    ///   1. Lokal kaydi guncelle (Status: InProgress, AnsweredAt)
    ///   2. Backend'e PUT /api/calls/{id}/answer gonder
    /// </summary>
    public async Task AnswerCallAsync(Guid uid, int? backendCallId)
    {
        // ── 1. Lokal kaydi guncelle ──
        if (_localRepo.IsConfigured)
        {
            try
            {
                var record = await _localRepo.GetCallRecordByUidAsync(uid);
                if (record != null)
                {
                    record.Status = "InProgress";
                    record.AnsweredAt = DateTime.UtcNow;
                    await _localRepo.UpdateCallRecordAsync(record);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lokal DB guncelleme hatasi (AnswerCall)");
            }
        }

        // ── 2. Backend'e bildir ──
        if (backendCallId.HasValue)
        {
            try
            {
                await _http.PutAsync($"api/calls/{backendCallId.Value}/answer", null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backend answer bildirimi basarisiz");
            }
        }
    }

    // ═══════════════════════════════════════
    // ARAMA BITTI
    // ═══════════════════════════════════════

    /// <summary>
    /// Cagri bittiginde:
    ///   1. Lokal kaydi guncelle (Status: Completed, EndedAt, DurationSeconds)
    ///   2. Backend'e PUT /api/calls/{id}/end gonder
    ///   3. Ses kaydi varsa metadata'yi lokal DB'ye yaz
    /// </summary>
    public async Task EndCallAsync(Guid uid, int? backendCallId, string? recordingFilePath = null)
    {
        // ── 1. Lokal kaydi guncelle ──
        if (_localRepo.IsConfigured)
        {
            try
            {
                var record = await _localRepo.GetCallRecordByUidAsync(uid);
                if (record != null)
                {
                    record.Status = "Completed";
                    record.EndedAt = DateTime.UtcNow;
                    if (record.AnsweredAt.HasValue)
                    {
                        record.DurationSeconds = (int)(record.EndedAt.Value - record.AnsweredAt.Value).TotalSeconds;
                    }
                    record.RecordingFilePath = recordingFilePath;
                    await _localRepo.UpdateCallRecordAsync(record);
                    _logger.LogInformation("Lokal kayit guncellendi: {Uid}, Sure: {Duration}s",
                        uid, record.DurationSeconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lokal DB guncelleme hatasi (EndCall)");
            }

            // ── 3. Ses kaydi metadata'si ──
            if (!string.IsNullOrEmpty(recordingFilePath))
            {
                try
                {
                    var fileInfo = new FileInfo(recordingFilePath);
                    if (fileInfo.Exists)
                    {
                        var recording = new LocalRecording
                        {
                            CallRecordUid = uid,
                            FilePath = recordingFilePath,
                            FileSize = fileInfo.Length,
                            Format = fileInfo.Extension.TrimStart('.').ToLowerInvariant()
                        };
                        await _localRepo.SaveRecordingMetadataAsync(recording);
                        _logger.LogInformation("Ses kaydi metadata kaydedildi: {Path}", recordingFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ses kaydi metadata yazim hatasi");
                }
            }
        }

        // ── 2. Backend'e bildir ──
        if (backendCallId.HasValue)
        {
            try
            {
                await _http.PutAsync($"api/calls/{backendCallId.Value}/end", null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backend end bildirimi basarisiz");
            }
        }
    }

    // ═══════════════════════════════════════
    // CEVAPSIZ ARAMA
    // ═══════════════════════════════════════

    /// <summary>
    /// Arama basarisiz oldu veya cevaplanmadan kesildi.
    /// </summary>
    public async Task MissCallAsync(Guid uid, int? backendCallId)
    {
        if (_localRepo.IsConfigured)
        {
            try
            {
                var record = await _localRepo.GetCallRecordByUidAsync(uid);
                if (record != null)
                {
                    record.Status = "Missed";
                    record.EndedAt = DateTime.UtcNow;
                    await _localRepo.UpdateCallRecordAsync(record);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lokal DB guncelleme hatasi (MissCall)");
            }
        }

        if (backendCallId.HasValue)
        {
            try
            {
                await _http.PutAsync($"api/calls/{backendCallId.Value}/end", null);
            }
            catch { }
        }
    }
}

/// <summary>
/// StartCallAsync donusu — Dialer'in hem Uid hem BackendCallId'yi takip edebilmesi icin.
/// </summary>
public class CallSyncResult
{
    /// <summary>Lokal cagri Uid'si (tum islemler bu Uid ile yapilir)</summary>
    public Guid Uid { get; set; }

    /// <summary>Backend'teki CallRecord ID'si (backend push basariliysa dolu)</summary>
    public int? BackendCallId { get; set; }
}
