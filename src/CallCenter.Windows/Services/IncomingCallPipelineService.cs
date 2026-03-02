using System.Net.Http;
using System.Net.Http.Json;
using CallCenter.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace CallCenter.Windows.Services;

/// <summary>
/// Gelen arama pipeline servisi.
/// API'den IVR yapilandirmasini alir, DTMF girisi toplar ve yonlendirme yapar.
///
/// Akis:
///   1. Gelen arama → API'den IncomingCallConfigDto al
///   2. IVR varsa: auto-answer → DTMF bekle → secenege gore yonlendir
///   3. IVR yoksa: mevcut akis (bildirim popup)
/// </summary>
public class IncomingCallPipelineService
{
    private readonly ISipService _sipService;
    private readonly HttpClient _http;
    private readonly ILogger<IncomingCallPipelineService> _logger;

    // IVR state
    private IncomingCallConfigDto? _activeConfig;
    private bool _ivrActive;
    private int _dtmfRetryCount;
    private readonly System.Timers.Timer _dtmfTimeoutTimer = new();

    /// <summary>IVR tamamlandiginda tetiklenir: (queueId, extension, holdMusicPath) — biri null olabilir</summary>
    public event Func<int?, string?, string?, Task>? OnIvrRouteSelected;

    /// <summary>IVR timeout/max retry asildiysa tetiklenir (varsayilan kuyruğa yonlendirilmeli)</summary>
    public event Func<Task>? OnIvrFallback;

    /// <summary>IVR aktif degil, normal popup gosterilmeli</summary>
    public event Func<string, string, Task>? OnShowIncomingCallPopup;

    public IncomingCallPipelineService(
        ISipService sipService,
        HttpClient http,
        ILogger<IncomingCallPipelineService> logger)
    {
        _sipService = sipService;
        _http = http;
        _logger = logger;

        // DTMF alma event'ine subscribe
        _sipService.OnDtmfReceived += OnDtmfReceivedAsync;

        // Timeout timer
        _dtmfTimeoutTimer.AutoReset = false;
        _dtmfTimeoutTimer.Elapsed += (_, _) => _ = HandleDtmfTimeoutAsync();
    }

    /// <summary>
    /// Gelen arama basladiginda cagirilir.
    /// API'den config cekilir ve IVR akisi baslatilir veya normal popup gosterilir.
    /// </summary>
    public async Task HandleIncomingCallAsync(string callerUri, string callerDisplay)
    {
        _logger.LogInformation("[Pipeline] Gelen arama: {CallerUri} ({CallerDisplay})", callerUri, callerDisplay);

        // Config cek
        _activeConfig = await FetchIncomingCallConfigAsync();

        if (_activeConfig == null)
        {
            _logger.LogWarning("[Pipeline] IVR config alinamadi, normal akis");
            await (OnShowIncomingCallPopup?.Invoke(callerUri, callerDisplay) ?? Task.CompletedTask);
            return;
        }

        _logger.LogInformation("[Pipeline] Config: BusinessHours={Bh}, Holiday={H}, HasIvr={Ivr}",
            _activeConfig.IsWithinBusinessHours, _activeConfig.IsHoliday, _activeConfig.HasIvrMenu);

        // Tatil veya mesai disi + IVR yok → normal popup
        if (!_activeConfig.HasIvrMenu)
        {
            _logger.LogInformation("[Pipeline] IVR yok, normal popup gosteriliyor");
            await (OnShowIncomingCallPopup?.Invoke(callerUri, callerDisplay) ?? Task.CompletedTask);
            return;
        }

        // IVR aktif — otomatik cevapla
        _logger.LogInformation("[Pipeline] IVR aktif, otomatik cevaplaniyor...");
        _ivrActive = true;
        _dtmfRetryCount = 0;

        var answered = await _sipService.AnswerCallAsync();
        if (!answered)
        {
            _logger.LogError("[Pipeline] Otomatik cevaplama basarisiz!");
            _ivrActive = false;
            return;
        }

        // TODO: Karsilama sesini cal (AudioFilePath mevcutsa)
        // Simdilik sadece DTMF bekliyoruz
        _logger.LogInformation("[Pipeline] Cevaplandi, DTMF bekleniyor. Timeout={T}sn, MaxRetry={R}",
            _activeConfig.IvrOptions?.Count > 0 ? 10 : 5,
            3);

        StartDtmfTimeout(_activeConfig.IvrOptions?.Count > 0 ? 10_000 : 5_000);
    }

    /// <summary>IVR pipeline aktif mi?</summary>
    public bool IsIvrActive => _ivrActive;

    /// <summary>Aktif IVR config</summary>
    public IncomingCallConfigDto? ActiveConfig => _activeConfig;

    // ─── Private ───

    private async Task<IncomingCallConfigDto?> FetchIncomingCallConfigAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/ivr/incoming-config");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IncomingCallConfigDto>();
            }
            _logger.LogWarning("[Pipeline] API yaniti: {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pipeline] IVR config API hatasi");
        }
        return null;
    }

    private async Task OnDtmfReceivedAsync(byte tone, int durationMs)
    {
        if (!_ivrActive || _activeConfig?.IvrOptions == null) return;

        _dtmfTimeoutTimer.Stop();

        var digit = DtmfToneToString(tone);
        _logger.LogInformation("[Pipeline] DTMF alindi: {Digit} (tone={Tone})", digit, tone);

        // Seceneklerde bu tus var mi?
        var option = _activeConfig.IvrOptions.FirstOrDefault(o =>
            o.Digit.Equals(digit, StringComparison.OrdinalIgnoreCase));

        if (option == null)
        {
            _logger.LogWarning("[Pipeline] Gecersiz tus: {Digit}", digit);
            _dtmfRetryCount++;
            if (_dtmfRetryCount >= 3)
            {
                _logger.LogWarning("[Pipeline] Max retry asildi, fallback");
                await CompletePipelineAsync(null, null);
                return;
            }
            // Tekrar bekle
            StartDtmfTimeout(10_000);
            return;
        }

        _logger.LogInformation("[Pipeline] Gecerli secenek: Digit={D}, Action={A}, Queue={Q}, Ext={E}",
            option.Digit, option.ActionType, option.TargetQueueId, option.TargetExtension);

        switch (option.ActionType.ToLowerInvariant())
        {
            case "transfer_queue":
                await CompletePipelineAsync(option.TargetQueueId, null);
                break;

            case "transfer_extension":
                await CompletePipelineAsync(null, option.TargetExtension);
                break;

            case "sub_menu":
                // Alt menu — yeni IVR config cekilecek
                _logger.LogInformation("[Pipeline] Alt menu: IvrMenuId={Id}", option.TargetIvrMenuId);
                // Simdilik fallback — alt menu destegi sonra eklenecek
                await CompletePipelineAsync(null, null);
                break;

            case "play_message":
                // Mesaj cal ve kapat
                _logger.LogInformation("[Pipeline] Mesaj cal: GreetingId={Id}", option.TargetGreetingMessageId);
                await CompletePipelineAsync(null, null);
                break;

            case "hangup":
                _logger.LogInformation("[Pipeline] Kapat secenegi secildi");
                _ivrActive = false;
                await _sipService.HangupAsync();
                break;

            default:
                _logger.LogWarning("[Pipeline] Bilinmeyen aksiyon: {A}", option.ActionType);
                await CompletePipelineAsync(null, null);
                break;
        }
    }

    private async Task CompletePipelineAsync(int? queueId, string? extension)
    {
        _ivrActive = false;
        _dtmfTimeoutTimer.Stop();

        if (queueId != null || extension != null)
        {
            var holdMusicPath = _activeConfig?.HoldMusicAudioPath;
            _logger.LogInformation("[Pipeline] Yonlendirme: QueueId={Q}, Extension={E}, HoldMusic={H}",
                queueId, extension, holdMusicPath);
            await (OnIvrRouteSelected?.Invoke(queueId, extension, holdMusicPath) ?? Task.CompletedTask);
        }
        else
        {
            _logger.LogInformation("[Pipeline] Fallback — varsayilan kuyruga yonlendir");
            await (OnIvrFallback?.Invoke() ?? Task.CompletedTask);
        }
    }

    private async Task HandleDtmfTimeoutAsync()
    {
        if (!_ivrActive) return;

        _dtmfRetryCount++;
        _logger.LogWarning("[Pipeline] DTMF timeout (retry {R}/3)", _dtmfRetryCount);

        if (_dtmfRetryCount >= 3)
        {
            await CompletePipelineAsync(null, null);
        }
        else
        {
            // Tekrar bekle
            StartDtmfTimeout(10_000);
        }
    }

    private void StartDtmfTimeout(double intervalMs)
    {
        _dtmfTimeoutTimer.Stop();
        _dtmfTimeoutTimer.Interval = intervalMs;
        _dtmfTimeoutTimer.Start();
    }

    private static string DtmfToneToString(byte tone) => tone switch
    {
        <= 9 => tone.ToString(),
        10 => "*",
        11 => "#",
        12 => "A",
        13 => "B",
        14 => "C",
        15 => "D",
        _ => "?"
    };
}
