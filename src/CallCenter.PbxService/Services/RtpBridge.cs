using System.Net;
using System.Net.Sockets;

namespace CallCenter.PbxService.Services;

/// <summary>
/// RTP Bridge: iki taraf arasinda ses paketlerini ileten kopru.
/// GoIP RTP <-> PbxService <-> Agent RTP
/// Her iki yonlu UDP paketleri relay eder.
/// </summary>
public class RtpBridge : IDisposable
{
    private readonly string _callerEndpoint;
    private readonly string _agentEndpoint;
    private readonly string _callId;

    private UdpClient? _callerSocket;
    private UdpClient? _agentSocket;
    private CancellationTokenSource? _cts;

    private long _callerToAgentPackets;
    private long _agentToCallerPackets;
    private bool _disposed;

    public bool IsActive => _cts != null && !_cts.IsCancellationRequested;
    public long CallerToAgentPackets => _callerToAgentPackets;
    public long AgentToCallerPackets => _agentToCallerPackets;

    public RtpBridge(string callerEndpoint, string agentEndpoint, string callId)
    {
        _callerEndpoint = callerEndpoint;
        _agentEndpoint = agentEndpoint;
        _callId = callId;
    }

    /// <summary>Bridge'i baslat - iki yonlu RTP relay</summary>
    public void Start(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var callerEp = ParseEndpoint(_callerEndpoint);
        var agentEp = ParseEndpoint(_agentEndpoint);

        // Rastgele portlarda dinle
        _callerSocket = new UdpClient(0);
        _agentSocket = new UdpClient(0);

        // Caller -> Agent yonu
        _ = RelayAsync(_callerSocket, agentEp, isCallerToAgent: true, _cts.Token);

        // Agent -> Caller yonu
        _ = RelayAsync(_agentSocket, callerEp, isCallerToAgent: false, _cts.Token);
    }

    /// <summary>Bridge'i durdur</summary>
    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task RelayAsync(
        UdpClient source,
        IPEndPoint target,
        bool isCallerToAgent,
        CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await source.ReceiveAsync(ct);

                // RTP paketini hedefe ilet
                await source.SendAsync(result.Buffer, result.Buffer.Length, target);

                if (isCallerToAgent)
                    Interlocked.Increment(ref _callerToAgentPackets);
                else
                    Interlocked.Increment(ref _agentToCallerPackets);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal kapanis
        }
        catch (SocketException)
        {
            // Socket kapandi - normal kapanis durumu
        }
    }

    private static IPEndPoint ParseEndpoint(string endpoint)
    {
        // Format: "IP:Port" veya "IP"
        var parts = endpoint.Split(':');
        var ip = IPAddress.Parse(parts[0]);
        var port = parts.Length > 1 ? int.Parse(parts[1]) : 0;
        return new IPEndPoint(ip, port);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _callerSocket?.Dispose();
        _agentSocket?.Dispose();
        _cts?.Dispose();
    }
}
