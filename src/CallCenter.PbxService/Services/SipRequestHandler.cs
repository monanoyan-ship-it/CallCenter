using System.Collections.Concurrent;
using SIPSorcery.SIP;

namespace CallCenter.PbxService.Services;

public class SipRequestHandler
{
    private readonly ILogger<SipRequestHandler> _logger;
    private readonly SipTransportService _transport;
    private readonly ICallSessionManager _sessionManager;
    private readonly InboundCallHandler _inboundHandler;
    private readonly OutboundCallHandler _outboundHandler;
    private readonly ITrunkManager _trunkManager;

    // Registered agent'lar: Extension -> RemoteEndPoint
    private readonly ConcurrentDictionary<string, AgentRegistration> _registeredAgents = new();

    public SipRequestHandler(
        ILogger<SipRequestHandler> logger,
        SipTransportService transport,
        ICallSessionManager sessionManager,
        InboundCallHandler inboundHandler,
        OutboundCallHandler outboundHandler,
        ITrunkManager trunkManager)
    {
        _logger = logger;
        _transport = transport;
        _sessionManager = sessionManager;
        _inboundHandler = inboundHandler;
        _outboundHandler = outboundHandler;
        _trunkManager = trunkManager;
    }

    public void Bind()
    {
        _transport.OnRequestReceived += HandleRequest;
        _transport.OnResponseReceived += HandleResponse;
    }

    /// <summary>Kayitli agent bilgisini getir</summary>
    public AgentRegistration? GetAgent(string extension)
    {
        return _registeredAgents.TryGetValue(extension, out var agent) ? agent : null;
    }

    /// <summary>Tum kayitli agent'lar</summary>
    public IReadOnlyCollection<AgentRegistration> GetRegisteredAgents()
    {
        return _registeredAgents.Values.ToList().AsReadOnly();
    }

    private async Task HandleRequest(SIPEndPoint localEp, SIPEndPoint remoteEp, SIPRequest request)
    {
        switch (request.Method)
        {
            case SIPMethodsEnum.INVITE:
                await HandleInvite(localEp, remoteEp, request);
                break;

            case SIPMethodsEnum.ACK:
                _logger.LogDebug("ACK: CallId={CallId}", request.Header.CallId);
                break;

            case SIPMethodsEnum.BYE:
                await HandleBye(request);
                break;

            case SIPMethodsEnum.CANCEL:
                await HandleCancel(request);
                break;

            case SIPMethodsEnum.REGISTER:
                await HandleRegister(remoteEp, request);
                break;

            default:
                _logger.LogDebug("Islenmemiyor: {Method}", request.Method);
                var response = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.MethodNotAllowed, null);
                await _transport.SendResponseAsync(response);
                break;
        }
    }

    /// <summary>
    /// INVITE kaynak tespiti:
    /// - Trunk'tan geliyorsa -> Gelen cagri (InboundCallHandler)
    /// - Agent'tan geliyorsa -> Giden cagri (OutboundCallHandler)
    /// </summary>
    private async Task HandleInvite(SIPEndPoint localEp, SIPEndPoint remoteEp, SIPRequest request)
    {
        var remoteIp = remoteEp.GetIPEndPoint().Address.ToString();

        // Trunk'tan mi geliyor?
        var isTrunk = _trunkManager.GetAllTrunkStates()
            .Any(t => t.Server == remoteIp || IsFromTrunk(request, t));

        if (isTrunk)
        {
            // Gelen cagri - trunk'tan
            await _inboundHandler.HandleInviteAsync(localEp, remoteEp, request);
        }
        else
        {
            // Agent'tan giden cagri istegi
            var fromUser = request.Header.From.FromURI.User;
            var toUser = request.Header.To.ToURI.User;

            _logger.LogInformation("Agent giden cagri: {From} -> {To}", fromUser, toUser);

            // 100 Trying
            var trying = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Trying, null);
            await _transport.SendResponseAsync(trying);

            // OutboundCallHandler'a ilet
            await _outboundHandler.HandleAgentInviteAsync(
                request.Header.CallId,
                fromUser,
                toUser,
                agentId: 0, // TODO: Agent ID tespiti (REGISTER bilgisinden)
                CancellationToken.None);
        }
    }

    private async Task HandleBye(SIPRequest request)
    {
        var callId = request.Header.CallId;

        // Oncelik: InboundCallHandler (gelen cagri)
        var session = _sessionManager.GetSession(callId);
        if (session != null)
        {
            await _inboundHandler.HandleByeAsync(request);
        }
        else
        {
            // Giden cagri BYE
            await _outboundHandler.EndOutboundCallAsync(callId);

            var ok = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, null);
            await _transport.SendResponseAsync(ok);
        }
    }

    private async Task HandleCancel(SIPRequest request)
    {
        var callId = request.Header.CallId;
        _logger.LogInformation("CANCEL: CallId={CallId}", callId);

        var session = _sessionManager.GetSession(callId);
        if (session != null)
        {
            session.State = CallSessionState.Failed;
            _sessionManager.RemoveSession(callId);
        }

        var ok = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, null);
        await _transport.SendResponseAsync(ok);
    }

    private async Task HandleRegister(SIPEndPoint remoteEp, SIPRequest request)
    {
        var contactUri = request.Header.Contact?.FirstOrDefault()?.ContactURI;
        var fromUser = request.Header.From.FromURI.User;

        _logger.LogInformation("REGISTER: {User} [{Remote}]", fromUser, remoteEp);

        // Agent kaydini tut
        if (!string.IsNullOrEmpty(fromUser))
        {
            _registeredAgents[fromUser] = new AgentRegistration
            {
                Extension = fromUser,
                RemoteEndPoint = remoteEp.GetIPEndPoint().ToString(),
                RegisteredAt = DateTime.UtcNow,
                CrmContactUri = contactUri?.ToString()
            };
        }

        var ok = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, null);
        await _transport.SendResponseAsync(ok);
    }

    private static bool IsFromTrunk(SIPRequest request, TrunkState trunk)
    {
        // Via header veya From domain trunk server'i mi kontrol et
        var fromHost = request.Header.From.FromURI.Host;
        return fromHost == trunk.Server;
    }

    private Task HandleResponse(SIPEndPoint localEp, SIPEndPoint remoteEp, SIPResponse response)
    {
        _logger.LogDebug("Response: {StatusCode} {Reason} [CallId={CallId}]",
            response.StatusCode, response.ReasonPhrase, response.Header.CallId);
        return Task.CompletedTask;
    }
}

/// <summary>PbxService'e register olan agent bilgisi</summary>
public class AgentRegistration
{
    public string Extension { get; set; } = string.Empty;
    public string RemoteEndPoint { get; set; } = string.Empty;
    public string? CrmContactUri { get; set; }
    public DateTime RegisteredAt { get; set; }
}
