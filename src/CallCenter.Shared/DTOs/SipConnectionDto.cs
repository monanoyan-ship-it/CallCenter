namespace CallCenter.Shared.DTOs;

/// <summary>
/// Operatorun secebilecegi gateway listesi icin ozet bilgi.
/// GET /api/sipaccounts/my/gateways endpoint'inden doner.
/// </summary>
public class SipGatewaySummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Transport { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public int AvailableLineCount { get; set; }
}

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

    /// <summary>Tahsis edilen hat ID'si (release icin)</summary>
    public int SipLineId { get; set; }

    /// <summary>Gateway kanal numarasi (GoIP line/channel)</summary>
    public int ChannelNumber { get; set; }

    /// <summary>Ayni SIP kullanicisiyle coklu GoIP kanal secimi icin numaranin basina eklenecek routing prefix.</summary>
    public string? OutboundDialPrefix { get; set; }

    /// <summary>OutboundDialPrefix SIP'e giden hedefte kullanilsin mi?</summary>
    public bool UseChannelRoutingPrefix { get; set; }

    /// <summary>Gateway ID'si</summary>
    public int SipAccountId { get; set; }

    /// <summary>Gateway adi (Dialer'da gosterim icin)</summary>
    public string GatewayName { get; set; } = string.Empty;
}
