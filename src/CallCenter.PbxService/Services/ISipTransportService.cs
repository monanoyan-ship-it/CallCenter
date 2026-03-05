using SIPSorcery.SIP;

namespace CallCenter.PbxService.Services;

public interface ISipTransportService
{
    SIPTransport Transport { get; }
    bool IsRunning { get; }
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync();
    Task SendResponseAsync(SIPResponse response);
    Task SendRequestAsync(SIPRequest request);
}
