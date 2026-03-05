using System.Net;
using CallCenter.PbxService.Configuration;
using Microsoft.Extensions.Options;
using SIPSorcery.SIP;

namespace CallCenter.PbxService.Services;

public class SipTransportService : ISipTransportService, IDisposable
{
    private readonly ILogger<SipTransportService> _logger;
    private readonly SipConfig _config;
    private SIPTransport? _transport;
    private bool _disposed;

    public SIPTransport Transport => _transport
        ?? throw new InvalidOperationException("SIP Transport henuz baslatilmadi");

    public bool IsRunning => _transport != null;

    // Dis bagimliliklar icin event'ler
    public event Func<SIPEndPoint, SIPEndPoint, SIPRequest, Task>? OnRequestReceived;
    public event Func<SIPEndPoint, SIPEndPoint, SIPResponse, Task>? OnResponseReceived;

    public SipTransportService(
        ILogger<SipTransportService> logger,
        IOptions<SipConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _transport = new SIPTransport();

        // UDP channel
        var udpChannel = new SIPUDPChannel(IPAddress.Any, _config.UdpPort);
        _transport.AddSIPChannel(udpChannel);
        _logger.LogInformation("SIP UDP dinleniyor: port {Port}", _config.UdpPort);

        // TCP channel
        var tcpChannel = new SIPTCPChannel(IPAddress.Any, _config.TcpPort);
        _transport.AddSIPChannel(tcpChannel);
        _logger.LogInformation("SIP TCP dinleniyor: port {Port}", _config.TcpPort);

        // TLS channel (opsiyonel)
        if (_config.EnableTls)
        {
            // TLS icin sertifika gerekli - production deploy sirasinda eklenecek
            _logger.LogWarning("TLS aktif ama sertifika yapilandirmasi gerekli (port {Port})", _config.TlsPort);
        }

        // SIP mesaj handler'lari
        _transport.SIPTransportRequestReceived += HandleRequestReceived;
        _transport.SIPTransportResponseReceived += HandleResponseReceived;

        _logger.LogInformation("SIP Transport baslatildi");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (_transport != null)
        {
            _logger.LogInformation("SIP Transport kapatiliyor...");
            _transport.Shutdown();
            _transport = null;
        }
        return Task.CompletedTask;
    }

    public async Task SendResponseAsync(SIPResponse response)
    {
        if (_transport == null) return;
        await _transport.SendResponseAsync(response);
    }

    public async Task SendRequestAsync(SIPRequest request)
    {
        if (_transport == null) return;
        await _transport.SendRequestAsync(request);
    }

    private async Task HandleRequestReceived(
        SIPEndPoint localEndPoint, SIPEndPoint remoteEndPoint, SIPRequest request)
    {
        _logger.LogDebug("SIP {Method} alindi: {From} -> {To} [{Remote}]",
            request.Method, request.Header.From.FromURI, request.Header.To.ToURI, remoteEndPoint);

        // OPTIONS -> hemen 200 OK (health check / keep-alive)
        if (request.Method == SIPMethodsEnum.OPTIONS)
        {
            var response = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.Ok, null);
            await SendResponseAsync(response);
            return;
        }

        // Diger mesajlari event ile dis handler'lara ilet
        if (OnRequestReceived != null)
        {
            await OnRequestReceived.Invoke(localEndPoint, remoteEndPoint, request);
        }
        else
        {
            _logger.LogWarning("SIP {Method} icin handler yok, 501 donuluyor", request.Method);
            var notImpl = SIPResponse.GetResponse(request, SIPResponseStatusCodesEnum.NotImplemented, null);
            await SendResponseAsync(notImpl);
        }
    }

    private async Task HandleResponseReceived(
        SIPEndPoint localEndPoint, SIPEndPoint remoteEndPoint, SIPResponse response)
    {
        _logger.LogDebug("SIP Response: {StatusCode} {ReasonPhrase} [{Remote}]",
            response.StatusCode, response.ReasonPhrase, remoteEndPoint);

        if (OnResponseReceived != null)
        {
            await OnResponseReceived.Invoke(localEndPoint, remoteEndPoint, response);
        }
    }

    public async Task<InviteResult> SendInviteToAgentAsync(
        string agentSipUri, string callerNumber, string callId, CancellationToken ct)
    {
        if (_transport == null)
            return InviteResult.Failed("SIP Transport baslatilmamis");

        try
        {
            var agentUri = SIPURI.ParseSIPURI(agentSipUri);
            var fromUri = new SIPURI(callerNumber, _transport.GetSIPChannels().First().ListeningSIPEndPoint.GetIPEndPoint().Address.ToString(), null);

            var invite = SIPRequest.GetRequest(
                SIPMethodsEnum.INVITE,
                agentUri);

            invite.Header.From = new SIPFromHeader(callerNumber, fromUri, callId);
            invite.Header.To = new SIPToHeader(null, agentUri, null);
            invite.Header.CallId = $"acd-{callId}-{Guid.NewGuid():N}";
            invite.Header.CSeq = 1;
            invite.Header.CSeqMethod = SIPMethodsEnum.INVITE;

            // Basit SDP - agent'in RTP bilgisini almak icin
            // Gercek implementasyonda SIPSorcery'nin SDP negotiation kullanilacak
            invite.Header.ContentType = "application/sdp";

            var tcs = new TaskCompletionSource<InviteResult>();
            var agentCallId = invite.Header.CallId;

            // Response handler ekle
            Func<SIPEndPoint, SIPEndPoint, SIPResponse, Task>? responseHandler = null;
            responseHandler = async (local, remote, response) =>
            {
                if (response.Header.CallId != agentCallId) return;

                if (response.StatusCode >= 200 && response.StatusCode < 300)
                {
                    // 200 OK - agent kabul etti
                    // ACK gonder
                    var ack = SIPRequest.GetRequest(SIPMethodsEnum.ACK, agentUri);
                    ack.Header.CallId = agentCallId;
                    await SendRequestAsync(ack);

                    // Agent RTP endpoint cikart (SDP'den)
                    var rtpEndpoint = ExtractRtpEndpointFromSdp(response.Body);
                    tcs.TrySetResult(InviteResult.Ok(rtpEndpoint ?? remote.GetIPEndPoint().ToString()));

                    OnResponseReceived -= responseHandler;
                }
                else if (response.StatusCode >= 300)
                {
                    // Reddedildi
                    tcs.TrySetResult(InviteResult.Failed($"{response.StatusCode} {response.ReasonPhrase}"));
                    OnResponseReceived -= responseHandler;
                }
                // 1xx provisional response'lari ignored
            };

            OnResponseReceived += responseHandler;

            await _transport.SendRequestAsync(invite);

            // 30 saniye timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                return await tcs.Task.WaitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                OnResponseReceived -= responseHandler;

                // CANCEL gonder
                var cancel = SIPRequest.GetRequest(SIPMethodsEnum.CANCEL, agentUri);
                cancel.Header.CallId = agentCallId;
                try { await _transport.SendRequestAsync(cancel); } catch { /* suppress */ }

                return InviteResult.Failed("Agent cevap vermedi (timeout)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent INVITE hatasi: {Uri}", agentSipUri);
            return InviteResult.Failed(ex.Message);
        }
    }

    private static string? ExtractRtpEndpointFromSdp(string? sdp)
    {
        if (string.IsNullOrEmpty(sdp)) return null;

        // Basit SDP parser: c= ve m= satirlarindan IP:Port cikart
        string? ip = null;
        int? port = null;

        foreach (var line in sdp.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("c=IN IP4 "))
            {
                ip = trimmed["c=IN IP4 ".Length..].Trim();
            }
            else if (trimmed.StartsWith("m=audio "))
            {
                var parts = trimmed.Split(' ');
                if (parts.Length >= 2 && int.TryParse(parts[1], out var p))
                    port = p;
            }
        }

        if (ip != null && port != null)
            return $"{ip}:{port}";

        return null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _transport?.Shutdown();
            _disposed = true;
        }
    }
}
