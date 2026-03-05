namespace CallCenter.PbxService.Services;

/// <summary>
/// ACD (Automatic Call Distribution) Servisi.
/// Kuyruktan cagri alir, API'den musait agent bulur, agent'a SIP INVITE gonderir.
/// Agent kabul ederse RTP bridge kurar (GoIP <-> PbxService <-> Agent).
/// </summary>
public class AcdService
{
    private readonly ILogger<AcdService> _logger;
    private readonly IApiClient _apiClient;
    private readonly QueueManager _queueManager;
    private readonly ICallSessionManager _sessionManager;
    private readonly ISipTransportService _transport;

    private CancellationTokenSource? _pollCts;

    public AcdService(
        ILogger<AcdService> logger,
        IApiClient apiClient,
        QueueManager queueManager,
        ICallSessionManager sessionManager,
        ISipTransportService transport)
    {
        _logger = logger;
        _apiClient = apiClient;
        _queueManager = queueManager;
        _sessionManager = sessionManager;
        _transport = transport;
    }

    /// <summary>ACD polling basla - periyodik olarak kuyruklari kontrol et</summary>
    public void Start(CancellationToken cancellationToken)
    {
        _pollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = PollQueuesAsync(_pollCts.Token);
        _logger.LogInformation("ACD servisi baslatildi");
    }

    public void Stop()
    {
        _pollCts?.Cancel();
        _logger.LogInformation("ACD servisi durduruldu");
    }

    private async Task PollQueuesAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var queueLengths = _queueManager.GetAllQueueLengths();

                foreach (var (queueId, length) in queueLengths)
                {
                    if (length == 0) continue;

                    // Musait agent var mi sor
                    var agent = await _apiClient.GetAvailableAgentAsync(queueId);
                    if (agent == null) continue;

                    // Kuyruktan ilk arayani al
                    var queuedCall = _queueManager.Dequeue(queueId);
                    if (queuedCall == null) continue;

                    _logger.LogInformation(
                        "ACD eslestirme: CallId={CallId} -> Agent={AgentName} (Queue={QueueId})",
                        queuedCall.CallId, agent.AgentName, queueId);

                    // Agent'a baglanma islemi (arka planda)
                    _ = BridgeToAgentAsync(queuedCall, agent, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ACD polling hatasi");
            }

            // 2 saniyede bir kontrol
            await Task.Delay(2000, ct);
        }
    }

    private async Task BridgeToAgentAsync(
        QueuedCall queuedCall,
        AvailableAgentInfo agent,
        CancellationToken ct)
    {
        var session = _sessionManager.GetSession(queuedCall.CallId);
        if (session == null)
        {
            _logger.LogWarning("ACD: Session bulunamadi: {CallId}", queuedCall.CallId);
            return;
        }

        try
        {
            session.State = CallSessionState.Ringing;
            session.AgentId = agent.AgentId;

            // API'ye agent bilgisini guncelle
            if (session.CallRecordId.HasValue)
            {
                await _apiClient.UpdateCallRecordAsync(session.CallRecordId.Value, new CallRecordUpdate
                {
                    AgentId = agent.AgentId,
                    StatusId = 1 // Ringing
                });
            }

            // Agent'a SIP INVITE gonder
            var agentUri = BuildAgentSipUri(agent);
            _logger.LogInformation("Agent'a INVITE gonderiliyor: {Uri}", agentUri);

            var inviteResult = await _transport.SendInviteToAgentAsync(
                agentUri,
                session.CallerNumber ?? "unknown",
                session.CallId,
                ct);

            if (inviteResult.Success)
            {
                _logger.LogInformation("Agent cagriyi kabul etti: {Agent}, CallId={CallId}",
                    agent.AgentName, queuedCall.CallId);

                session.State = CallSessionState.Connected;
                session.AnsweredAt = DateTime.UtcNow;
                session.AgentRtpEndpoint = inviteResult.AgentRtpEndpoint;

                // API'ye cagri cevaplandi bildir
                if (session.CallRecordId.HasValue)
                {
                    await _apiClient.UpdateCallRecordAsync(session.CallRecordId.Value, new CallRecordUpdate
                    {
                        AnsweredAt = session.AnsweredAt,
                        StatusId = 3 // Connected
                    });
                }

                // RTP Bridge baslat: GoIP RTP <-> Agent RTP
                if (session.CallerRtpEndpoint != null && session.AgentRtpEndpoint != null)
                {
                    var bridge = new RtpBridge(
                        session.CallerRtpEndpoint,
                        session.AgentRtpEndpoint,
                        session.CallId);
                    session.Bridge = bridge;
                    bridge.Start(ct);

                    _logger.LogInformation("RTP Bridge kuruldu: {Caller} <-> {Agent}",
                        session.CallerRtpEndpoint, session.AgentRtpEndpoint);
                }
            }
            else
            {
                _logger.LogWarning("Agent cagriyi reddetti/cevaplamadi: {Agent}, CallId={CallId}",
                    agent.AgentName, queuedCall.CallId);

                // Tekrar kuyruga al
                session.State = CallSessionState.Queued;
                await _queueManager.EnqueueAsync(
                    queuedCall.CallId, queuedCall.QueueId,
                    queuedCall.RtpSender, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent bridge hatasi: CallId={CallId}, Agent={Agent}",
                queuedCall.CallId, agent.AgentName);

            // Hata durumunda tekrar kuyruga al
            session.State = CallSessionState.Queued;
            try
            {
                await _queueManager.EnqueueAsync(
                    queuedCall.CallId, queuedCall.QueueId,
                    queuedCall.RtpSender, ct);
            }
            catch { /* suppress */ }
        }
    }

    private static string BuildAgentSipUri(AvailableAgentInfo agent)
    {
        var server = agent.SipServer ?? "127.0.0.1";
        var port = agent.SipPort != 5060 ? $":{agent.SipPort}" : "";
        return $"sip:{agent.SipExtension}@{server}{port}";
    }
}

/// <summary>Agent'a INVITE sonucu</summary>
public class InviteResult
{
    public bool Success { get; set; }
    public string? AgentRtpEndpoint { get; set; }
    public string? ErrorReason { get; set; }

    public static InviteResult Ok(string rtpEndpoint) =>
        new() { Success = true, AgentRtpEndpoint = rtpEndpoint };

    public static InviteResult Failed(string reason) =>
        new() { Success = false, ErrorReason = reason };
}
