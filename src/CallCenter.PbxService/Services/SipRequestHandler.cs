using SIPSorcery.SIP;

namespace CallCenter.PbxService.Services;

public class SipRequestHandler
{
    private readonly ILogger<SipRequestHandler> _logger;
    private readonly SipTransportService _transport;
    private readonly ICallSessionManager _sessionManager;
    private readonly InboundCallHandler _inboundHandler;

    public SipRequestHandler(
        ILogger<SipRequestHandler> logger,
        SipTransportService transport,
        ICallSessionManager sessionManager,
        InboundCallHandler inboundHandler)
    {
        _logger = logger;
        _transport = transport;
        _sessionManager = sessionManager;
        _inboundHandler = inboundHandler;
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
                await _inboundHandler.HandleInviteAsync(localEp, remoteEp, request);
                break;

            case SIPMethodsEnum.ACK:
                _logger.LogDebug("ACK: CallId={CallId}", request.Header.CallId);
                break;

            case SIPMethodsEnum.BYE:
                await _inboundHandler.HandleByeAsync(request);
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
        _logger.LogInformation("REGISTER: {Contact} [{Remote}]",
            request.Header.Contact?.FirstOrDefault()?.ContactURI, remoteEp);

        // Agent register isleme - simdilik 200 OK
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
