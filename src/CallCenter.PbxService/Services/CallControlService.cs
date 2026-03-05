namespace CallCenter.PbxService.Services;

/// <summary>
/// Cagri kontrol islemleri: Hold/Unhold, Transfer, BYE.
/// Aktif cagri uzerinde mid-call islemleri yonetir.
/// </summary>
public class CallControlService
{
    private readonly ILogger<CallControlService> _logger;
    private readonly ICallSessionManager _sessionManager;
    private readonly ISipTransportService _transport;
    private readonly IAudioPlayer _audioPlayer;
    private readonly AudioFileCache _audioCache;
    private readonly IApiClient _apiClient;

    public CallControlService(
        ILogger<CallControlService> logger,
        ICallSessionManager sessionManager,
        ISipTransportService transport,
        IAudioPlayer audioPlayer,
        AudioFileCache audioCache,
        IApiClient apiClient)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _transport = transport;
        _audioPlayer = audioPlayer;
        _audioCache = audioCache;
        _apiClient = apiClient;
    }

    /// <summary>Cagriyi beklet (Hold)</summary>
    public async Task<bool> HoldAsync(string callId, CancellationToken ct)
    {
        var session = _sessionManager.GetSession(callId);
        if (session == null || session.State != CallSessionState.Connected)
        {
            _logger.LogWarning("Hold basarisiz - session yok veya connected degil: {CallId}", callId);
            return false;
        }

        // RTP Bridge'i durdur (ses iletimini kes)
        session.Bridge?.Stop();
        session.State = CallSessionState.OnHold;

        _logger.LogInformation("Cagri beklemede: {CallId}", callId);

        // Arayan tarafa bekleme muzigi cal
        if (session.CallerRtpEndpoint != null)
        {
            // Bekleme muzigini arka planda cal
            session.HoldMusicCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ = PlayHoldMusicToCallerAsync(session, session.HoldMusicCts.Token);
        }

        // API bilgilendir
        if (session.CallRecordId.HasValue)
        {
            await _apiClient.UpdateCallRecordAsync(session.CallRecordId.Value, new CallRecordUpdate
            {
                StatusId = 5 // OnHold
            });
        }

        return true;
    }

    /// <summary>Beklemeden cikar (Unhold/Resume)</summary>
    public async Task<bool> UnholdAsync(string callId, CancellationToken ct)
    {
        var session = _sessionManager.GetSession(callId);
        if (session == null || session.State != CallSessionState.OnHold)
        {
            _logger.LogWarning("Unhold basarisiz - session yok veya hold'da degil: {CallId}", callId);
            return false;
        }

        // Bekleme muzigini durdur
        session.HoldMusicCts?.Cancel();
        session.HoldMusicCts = null;

        // RTP Bridge'i yeniden baslat
        if (session.CallerRtpEndpoint != null && session.AgentRtpEndpoint != null)
        {
            session.Bridge?.Dispose();
            session.Bridge = new RtpBridge(
                session.CallerRtpEndpoint,
                session.AgentRtpEndpoint,
                session.CallId);
            session.Bridge.Start(ct);
        }

        session.State = CallSessionState.Connected;

        _logger.LogInformation("Cagri devam ediyor: {CallId}", callId);

        // API bilgilendir
        if (session.CallRecordId.HasValue)
        {
            await _apiClient.UpdateCallRecordAsync(session.CallRecordId.Value, new CallRecordUpdate
            {
                StatusId = 3 // Connected
            });
        }

        return true;
    }

    /// <summary>Blind Transfer: arayani baska bir hedefe yonlendir</summary>
    public async Task<bool> BlindTransferAsync(
        string callId, string targetExtension, CancellationToken ct)
    {
        var session = _sessionManager.GetSession(callId);
        if (session == null || (session.State != CallSessionState.Connected && session.State != CallSessionState.OnHold))
        {
            _logger.LogWarning("Transfer basarisiz - session uygun degil: {CallId}", callId);
            return false;
        }

        session.State = CallSessionState.Transferring;

        // Mevcut bridge'i durdur
        session.Bridge?.Stop();
        session.HoldMusicCts?.Cancel();

        _logger.LogInformation("Blind transfer baslatiliyor: {CallId} -> {Target}",
            callId, targetExtension);

        // Yeni hedefe INVITE gonder
        var targetUri = $"sip:{targetExtension}@{GetLocalSipAddress()}";
        var inviteResult = await _transport.SendInviteToAgentAsync(
            targetUri,
            session.CallerNumber ?? "unknown",
            session.CallId,
            ct);

        if (inviteResult.Success)
        {
            // Eski agent'la baglantiyi kes (BYE)
            // Yeni agent ile bridge kur
            session.AgentRtpEndpoint = inviteResult.AgentRtpEndpoint;

            if (session.CallerRtpEndpoint != null && session.AgentRtpEndpoint != null)
            {
                session.Bridge?.Dispose();
                session.Bridge = new RtpBridge(
                    session.CallerRtpEndpoint,
                    session.AgentRtpEndpoint,
                    session.CallId);
                session.Bridge.Start(ct);
            }

            session.State = CallSessionState.Connected;
            _logger.LogInformation("Blind transfer basarili: {CallId} -> {Target}",
                callId, targetExtension);
            return true;
        }

        // Transfer basarisiz - eski duruma don
        _logger.LogWarning("Blind transfer basarisiz: {CallId} -> {Reason}",
            callId, inviteResult.ErrorReason);

        // Eski bridge'i geri kur
        if (session.CallerRtpEndpoint != null && session.AgentRtpEndpoint != null)
        {
            session.Bridge = new RtpBridge(
                session.CallerRtpEndpoint,
                session.AgentRtpEndpoint,
                session.CallId);
            session.Bridge.Start(ct);
        }

        session.State = CallSessionState.Connected;
        return false;
    }

    /// <summary>Cagriyi kapat (BYE)</summary>
    public async Task EndCallAsync(string callId)
    {
        var session = _sessionManager.GetSession(callId);
        if (session == null)
        {
            _logger.LogWarning("EndCall: session bulunamadi: {CallId}", callId);
            return;
        }

        // Bekleme muzigi varsa durdur
        session.HoldMusicCts?.Cancel();
        session.HoldMusicCts = null;

        // RTP Bridge durdur
        session.Bridge?.Stop();
        session.Bridge?.Dispose();
        session.Bridge = null;

        session.State = CallSessionState.Completed;
        session.EndedAt = DateTime.UtcNow;

        var durationSeconds = session.AnsweredAt.HasValue
            ? (int)(session.EndedAt.Value - session.AnsweredAt.Value).TotalSeconds
            : 0;

        _logger.LogInformation("Cagri sonlandi: {CallId}, Sure={Duration}s", callId, durationSeconds);

        // API bilgilendir
        if (session.CallRecordId.HasValue)
        {
            await _apiClient.UpdateCallRecordAsync(session.CallRecordId.Value, new CallRecordUpdate
            {
                EndedAt = session.EndedAt,
                DurationSeconds = durationSeconds,
                StatusId = 6 // Completed
            });
        }

        _sessionManager.RemoveSession(callId);
    }

    private async Task PlayHoldMusicToCallerAsync(CallSession session, CancellationToken ct)
    {
        try
        {
            // TODO: Kuyruk/musteri bazli bekleme muzigi ID'si
            // Simdilik genel bekleme muzigi
            var holdMusicPath = await _audioCache.GetAudioFileAsync(1); // Default hold music ID

            if (holdMusicPath == null)
            {
                _logger.LogDebug("Bekleme muzigi dosyasi bulunamadi");
                return;
            }

            // Loop olarak cal
            while (!ct.IsCancellationRequested)
            {
                // CallerRtpEndpoint'e ses gondermek icin IRtpSender gerekli
                // Bu noktada session'dan RtpSender'a erisim lazim
                // Simdilik log
                _logger.LogDebug("Bekleme muzigi caliyor: {CallId}", session.CallId);
                await Task.Delay(5000, ct); // Placeholder
            }
        }
        catch (OperationCanceledException)
        {
            // Normal: unhold veya cagri kapandi
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hold muzik hatasi: {CallId}", session.CallId);
        }
    }

    private string GetLocalSipAddress()
    {
        try
        {
            var ep = _transport.Transport.GetSIPChannels().FirstOrDefault()?.ListeningSIPEndPoint;
            return ep?.GetIPEndPoint().Address.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}
