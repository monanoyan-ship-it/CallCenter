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

    // ─── TURN/ICE NAT Traversal ───

    /// <summary>STUN sunucu adresi (örnek: stun:stun.l.google.com:19302)</summary>
    public string? StunServer { get; set; }

    /// <summary>TURN sunucu adresi (örnek: turn:turn.example.com:3478)</summary>
    public string? TurnServer { get; set; }

    /// <summary>TURN kimlik doğrulama kullanıcı adı</summary>
    public string? TurnUsername { get; set; }

    /// <summary>TURN kimlik doğrulama şifresi</summary>
    public string? TurnPassword { get; set; }

    // ─── Codec Tercihleri ───

    /// <summary>Codec oncelik sirasi JSON array: ["opus","g722","pcmu","pcma"]. Bos ise varsayilan.</summary>
    public string? PreferredCodecs { get; set; }

    /// <summary>Jitter buffer minimum gecikme (ms). 0 = varsayilan.</summary>
    public int JitterBufferMinMs { get; set; }

    /// <summary>Jitter buffer maksimum gecikme (ms). 0 = varsayilan.</summary>
    public int JitterBufferMaxMs { get; set; }
}
