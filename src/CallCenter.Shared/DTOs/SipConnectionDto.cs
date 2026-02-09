namespace CallCenter.Shared.DTOs;

/// <summary>
/// Agent'ın SIP bağlantı bilgilerini taşır.
/// GET /api/sipaccounts/my/connection endpoint'inden döner.
/// </summary>
public class SipConnectionInfoDto
{
    /// <summary>WebSocket URI (örnek: wss://sip.example.com:8089/ws)</summary>
    public string WsUri { get; set; } = string.Empty;

    /// <summary>SIP URI (örnek: sip:1001@sip.example.com)</summary>
    public string SipUri { get; set; } = string.Empty;

    /// <summary>SIP kullanıcı adı</summary>
    public string AuthUsername { get; set; } = string.Empty;

    /// <summary>SIP şifresi</summary>
    public string AuthPassword { get; set; } = string.Empty;

    /// <summary>Ekranda gösterilecek ad</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Transport türü (WSS/UDP/TCP/TLS)</summary>
    public string Transport { get; set; } = "WSS";

    /// <summary>SRTP kullanılsın mı</summary>
    public bool UseSrtp { get; set; }
}
