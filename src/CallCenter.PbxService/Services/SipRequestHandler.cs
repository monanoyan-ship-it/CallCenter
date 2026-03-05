using SIPSorcery.SIP;

namespace CallCenter.PbxService.Services;

public class SipRequestHandler
{
    private readonly ILogger<SipRequestHandler> _logger;
    private readonly SipTransportService _transport;
    private readonly ICallSessionManager _sessionManager;

    public SipRequestHandler(
        ILogger<SipRequestHandler> logger,
        SipTransportService transport,
        ICallSessionManager sessionManager)
    {
        _logger = logger;
        _transport = transport;
        _sessionManager = sessionManager;
    }

    public void Bind()
    {
        _transport.OnRequestReceived += HandleRequest;
        _transport.OnResponseReceived += HandleResponse;
    }

    private async Task HandleRequest(SIPEndPoint localEp, SIPEndPoint remoteEp, SIPRequest request)
    {
        switch (request.Method)
        {
            case SIPMethodsEnum.INVITE:
                await HandleInvite(localEp, remoteEp, request);
                break;

            case SIPMethodsEnum.ACK:
                HandleAck(request);
                break;

            case SIPMethodsEnum.BYE:
                await HandleBye(localEp, remoteEp, request);
                break;

            case SIPMethodsEnum.CANCEL:
                await HandleCancel(localEp, remoteEp, request);
                break;

            case SIPMethodsEnum.REGISTER:
                await HandleRegister(localEp, remoteEp, request);
                break;

            default:
                _logger.LogDebug("Islenmemiyor: {Method}", request.Method);
                var response = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.MethodNotAllowed, null);
                await _transport.SendResponseAsync(response);
                break;
        }
    }

    private async Task HandleInvite(SIPEndPoint localEp, SIPEndPoint remoteEp, SIPRequest request)
    {
        var callId = request.Header.CallId;
        var from = request.Header.From.FromURI.ToString();
        var to = request.Header.To.ToURI.ToString();

        _logger.LogInformation("INVITE: {From} -> {To} [CallId={CallId}]", from, to, callId);

        // 100 Trying gonder
        var trying = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Trying, null);
        await _transport.SendResponseAsync(trying);

        // Oturum olustur
        var session = _sessionManager.CreateSession(callId, from, to);

        // TODO 11.4: Mesai kontrolu + IVR/kuyruk yonlendirme
        // Simdilik 180 Ringing + 486 Busy (henuz agent yonlendirme yok)
        var ringing = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ringing, null);
        await _transport.SendResponseAsync(ringing);

        _logger.LogWarning("INVITE islendi ama henuz agent yonlendirme yok (11.4/11.8 bekliyor). 486 Busy donuluyor.");
        var busy = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.BusyHere, "PBX henuz hazir degil");
        await _transport.SendResponseAsync(busy);
        _sessionManager.RemoveSession(callId);
    }

    private void HandleAck(SIPRequest request)
    {
        _logger.LogDebug("ACK: CallId={CallId}", request.Header.CallId);
    }

    private async Task HandleBye(SIPEndPoint localEp, SIPEndPoint remoteEp, SIPRequest request)
    {
        var callId = request.Header.CallId;
        _logger.LogInformation("BYE: CallId={CallId}", callId);

        // Oturumu sonlandir
        _sessionManager.RemoveSession(callId);

        // 200 OK
        var ok = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, null);
        await _transport.SendResponseAsync(ok);

        // TODO 11.9: CallRecord guncelle (API)
    }

    private async Task HandleCancel(SIPEndPoint localEp, SIPEndPoint remoteEp, SIPRequest request)
    {
        var callId = request.Header.CallId;
        _logger.LogInformation("CANCEL: CallId={CallId}", callId);

        var session = _sessionManager.GetSession(callId);
        if (session != null)
        {
            session.State = CallSessionState.Failed;
            _sessionManager.RemoveSession(callId);
        }

        // 200 OK (CANCEL icin)
        var ok = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, null);
        await _transport.SendResponseAsync(ok);
    }

    private async Task HandleRegister(SIPEndPoint localEp, SIPEndPoint remoteEp, SIPRequest request)
    {
        _logger.LogInformation("REGISTER: {Contact} [{Remote}]",
            request.Header.Contact?.FirstOrDefault()?.ContactURI, remoteEp);

        // TODO 11.3: Agent/Trunk registration isleme
        // Simdilik 200 OK
        var ok = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, null);
        await _transport.SendResponseAsync(ok);
    }

    private Task HandleResponse(SIPEndPoint localEp, SIPEndPoint remoteEp, SIPResponse response)
    {
        _logger.LogDebug("Response: {StatusCode} {Reason} [CallId={CallId}]",
            response.StatusCode, response.ReasonPhrase, response.Header.CallId);
        return Task.CompletedTask;
    }
}
