using System.Net;
using System.Net.Sockets;

namespace CallCenter.PbxService.Services;

/// <summary>
/// Arayana (GoIP) RTP paketleri gonderen sender.
/// Kuyruk bekleme muzigi bu sender uzerinden calar.
/// </summary>
public class CallerRtpSender : IRtpSender, IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _callerEndpoint;
    private ushort _sequenceNumber;
    private uint _timestamp;
    private readonly uint _ssrc;
    private bool _disposed;

    /// <summary>Lokal dinleme adresi (SDP answer icin)</summary>
    public string LocalEndpoint { get; }

    public CallerRtpSender(IPEndPoint callerEndpoint)
    {
        _callerEndpoint = callerEndpoint;
        _udpClient = new UdpClient(0); // rastgele port
        _ssrc = (uint)Random.Shared.Next();

        var localPort = ((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port;
        LocalEndpoint = $"0.0.0.0:{localPort}";
    }

    /// <summary>Lokal RTP port numarasi</summary>
    public int LocalPort => ((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port;

    public async Task SendAudioFrameAsync(byte[] encodedFrame, int durationMs)
    {
        if (_disposed) return;

        // RTP header (12 byte) + payload
        var rtpPacket = new byte[12 + encodedFrame.Length];

        // V=2, P=0, X=0, CC=0
        rtpPacket[0] = 0x80;
        // M=0, PT=0 (PCMU)
        rtpPacket[1] = 0x00;
        // Sequence number
        rtpPacket[2] = (byte)(_sequenceNumber >> 8);
        rtpPacket[3] = (byte)(_sequenceNumber & 0xFF);
        _sequenceNumber++;
        // Timestamp
        rtpPacket[4] = (byte)(_timestamp >> 24);
        rtpPacket[5] = (byte)(_timestamp >> 16);
        rtpPacket[6] = (byte)(_timestamp >> 8);
        rtpPacket[7] = (byte)(_timestamp & 0xFF);
        _timestamp += (uint)(durationMs * 8); // 8kHz sample rate
        // SSRC
        rtpPacket[8] = (byte)(_ssrc >> 24);
        rtpPacket[9] = (byte)(_ssrc >> 16);
        rtpPacket[10] = (byte)(_ssrc >> 8);
        rtpPacket[11] = (byte)(_ssrc & 0xFF);

        // Payload
        Buffer.BlockCopy(encodedFrame, 0, rtpPacket, 12, encodedFrame.Length);

        try
        {
            await _udpClient.SendAsync(rtpPacket, rtpPacket.Length, _callerEndpoint);
        }
        catch (ObjectDisposedException) { }
        catch (SocketException) { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _udpClient.Dispose();
    }
}
