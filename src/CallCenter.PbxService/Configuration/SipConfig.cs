namespace CallCenter.PbxService.Configuration;

public class SipConfig
{
    public int UdpPort { get; set; } = 5060;
    public int TcpPort { get; set; } = 5060;
    public int TlsPort { get; set; } = 5061;
    public bool EnableTls { get; set; }
    public int RtpPortStart { get; set; } = 10000;
    public int RtpPortEnd { get; set; } = 20000;
}
