namespace CallCenter.PbxService.Services;

/// <summary>
/// Giden cagri yonetimi.
/// Agent Windows App'inden giden cagri istegi:
/// 1. Agent -> PbxService'e SIP INVITE (veya API uzerinden istek)
/// 2. PbxService -> GoIP/trunk uzerinden disari INVITE
/// 3. RTP bridge: Agent <-> PbxService <-> GoIP <-> GSM
/// Avantaj: Tek SIP trunk PBX'de, agent'lar trunk bilgisi bilmez.
/// </summary>
public class OutboundCallHandler
{
    private readonly ILogger<OutboundCallHandler> _logger;
    private readonly ISipTransportService _transport;
    private readonly ICallSessionManager _sessionManager;
    private readonly ITrunkManager _trunkManager;
    private readonly IApiClient _apiClient;
    private readonly CallRecordingService _recordingService;

    public OutboundCallHandler(
        ILogger<OutboundCallHandler> logger,
        ISipTransportService transport,
        ICallSessionManager sessionManager,
        ITrunkManager trunkManager,
        IApiClient apiClient,
        CallRecordingService recordingService)
    {
        _logger = logger;
        _transport = transport;
        _sessionManager = sessionManager;
        _trunkManager = trunkManager;
        _apiClient = apiClient;
        _recordingService = recordingService;
    }

    /// <summary>
    /// Agent'in Windows App'inden gelen INVITE'i isle.
    /// Agent PbxService'e INVITE gonderir, PbxService trunk uzerinden disari arar.
    /// </summary>
    public async Task HandleAgentInviteAsync(
        string agentCallId,
        string agentExtension,
        string targetNumber,
        int agentId,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Giden cagri istegi: Agent={AgentExt}, Hedef={Target}",
            agentExtension, targetNumber);

        // Session olustur
        var session = _sessionManager.CreateSession(
            agentCallId,
            $"sip:{agentExtension}@pbx",
            $"sip:{targetNumber}@trunk");

        session.AgentId = agentId;
        session.CallerNumber = targetNumber;
        session.State = CallSessionState.Ringing;

        // Aktif trunk bul
        var trunk = _trunkManager.GetActiveTrunk();
        if (trunk == null)
        {
            _logger.LogError("Giden cagri: Aktif trunk yok");
            session.State = CallSessionState.Failed;
            return;
        }

        try
        {
            // API'ye cagri kaydi olustur
            var callRecordId = await _apiClient.CreateIncomingCallRecordAsync(new IncomingCallRequest
            {
                CallerNumber = agentExtension,
                CalleeNumber = targetNumber,
                SipCallId = agentCallId
            });

            session.CallRecordId = callRecordId;

            // Trunk uzerinden disari INVITE gonder
            var trunkUri = $"sip:{targetNumber}@{trunk.Server}:{trunk.Port}";

            _logger.LogInformation("Trunk uzerinden arama: {Uri}", trunkUri);

            var result = await _transport.SendInviteToAgentAsync(
                trunkUri,
                agentExtension,
                agentCallId,
                ct);

            if (result.Success)
            {
                session.State = CallSessionState.Connected;
                session.AnsweredAt = DateTime.UtcNow;
                session.CallerRtpEndpoint = result.AgentRtpEndpoint;

                _logger.LogInformation("Giden cagri baglandi: {CallId} -> {Target}",
                    agentCallId, targetNumber);

                // API guncelle
                if (callRecordId.HasValue)
                {
                    await _apiClient.UpdateCallRecordAsync(callRecordId.Value, new CallRecordUpdate
                    {
                        AnsweredAt = session.AnsweredAt,
                        AgentId = agentId,
                        StatusId = 3 // Connected
                    });
                }

                // Kaydi baslat
                var recording = _recordingService.StartRecording(agentCallId);
                session.RecordingSession = recording;
            }
            else
            {
                _logger.LogWarning("Giden cagri basarisiz: {Reason}", result.ErrorReason);
                session.State = CallSessionState.Failed;

                if (callRecordId.HasValue)
                {
                    await _apiClient.UpdateCallRecordAsync(callRecordId.Value, new CallRecordUpdate
                    {
                        EndedAt = DateTime.UtcNow,
                        StatusId = 7 // Failed
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Giden cagri hatasi: {CallId}", agentCallId);
            session.State = CallSessionState.Failed;
        }
    }

    /// <summary>Giden cagriyi kapat</summary>
    public async Task EndOutboundCallAsync(string callId)
    {
        var session = _sessionManager.GetSession(callId);
        if (session == null) return;

        // Kaydi durdur
        if (session.RecordingSession != null)
        {
            await _recordingService.StopRecordingAsync(session.RecordingSession, session.CallRecordId);
        }

        // Bridge durdur
        session.Bridge?.Stop();
        session.Bridge?.Dispose();
        session.Bridge = null;

        session.State = CallSessionState.Completed;
        session.EndedAt = DateTime.UtcNow;

        var duration = session.AnsweredAt.HasValue
            ? (int)(session.EndedAt.Value - session.AnsweredAt.Value).TotalSeconds
            : 0;

        // API guncelle
        if (session.CallRecordId.HasValue)
        {
            await _apiClient.UpdateCallRecordAsync(session.CallRecordId.Value, new CallRecordUpdate
            {
                EndedAt = session.EndedAt,
                DurationSeconds = duration,
                StatusId = 6 // Completed
            });
        }

        _sessionManager.RemoveSession(callId);
        _logger.LogInformation("Giden cagri sonlandi: {CallId}, Sure={Duration}s", callId, duration);
    }
}
