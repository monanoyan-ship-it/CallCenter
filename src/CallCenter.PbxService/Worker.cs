using CallCenter.PbxService.Configuration;
using Microsoft.Extensions.Options;
using SIPSorcery.SIP;

namespace CallCenter.PbxService;

public class Worker(
    ILogger<Worker> logger,
    IOptions<SipConfig> sipConfig) : BackgroundService
{
    private SIPTransport? _sipTransport;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = sipConfig.Value;

        _sipTransport = new SIPTransport();
        _sipTransport.AddSIPChannel(new SIPUDPChannel(System.Net.IPAddress.Any, config.UdpPort));

        _sipTransport.SIPTransportRequestReceived += OnSipRequestReceived;

        logger.LogInformation("PBX SIP Transport baslatildi - UDP port {Port}", config.UdpPort);

        // Graceful shutdown bekle
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("PBX Service durduruluyor...");
        }
    }

    private Task OnSipRequestReceived(SIPEndPoint localSipEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
    {
        switch (sipRequest.Method)
        {
            case SIPMethodsEnum.INVITE:
                logger.LogInformation("Gelen INVITE: {From} -> {To}", sipRequest.Header.From.FromURI, sipRequest.Header.To.ToURI);
                // TODO 11.4: Gelen cagri isleme
                break;

            case SIPMethodsEnum.REGISTER:
                logger.LogInformation("REGISTER: {Contact}", sipRequest.Header.Contact?.FirstOrDefault());
                // TODO 11.3: Trunk / agent registration
                break;

            case SIPMethodsEnum.BYE:
                logger.LogInformation("BYE: CallId={CallId}", sipRequest.Header.CallId);
                // TODO 11.9: Cagri sonlandirma
                break;

            case SIPMethodsEnum.OPTIONS:
                // Health check - 200 OK dondur
                var optionsResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null);
                _sipTransport?.SendResponseAsync(optionsResponse);
                break;

            case SIPMethodsEnum.CANCEL:
                logger.LogInformation("CANCEL: CallId={CallId}", sipRequest.Header.CallId);
                break;

            default:
                logger.LogDebug("SIP {Method}: {From}", sipRequest.Method, sipRequest.Header.From.FromURI);
                break;
        }

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("SIP Transport kapatiliyor...");
        _sipTransport?.Shutdown();
        await base.StopAsync(cancellationToken);
    }
}
