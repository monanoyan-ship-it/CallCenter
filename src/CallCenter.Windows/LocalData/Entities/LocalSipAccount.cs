namespace CallCenter.Windows.LocalData.Entities;

/// <summary>
/// Lokal veritabaninda saklanan SIP hesabi (gateway).
/// Backend'teki SipAccount'dan bagimsiz — FK iliskisi yok, sadece Uid referansi.
/// IsSyncedToBackend: Backend'e push edildi mi? (cift yazim takibi)
/// </summary>
public class LocalSipAccount
{
    /// <summary>Lokal veritabani birincil anahtari</summary>
    public int Id { get; set; }

    /// <summary>Benzersiz tanimlayici — backend ile eslesme icin</summary>
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Hesap adi (ornek: "Ofis Gateway", "VoIP Provider 1")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>SIP sunucu adresi (ornek: sip.example.com)</summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>SIP sunucu portu (varsayilan: 5060)</summary>
    public int Port { get; set; } = 5060;

    /// <summary>SIP domain (opsiyonel)</summary>
    public string? Domain { get; set; }

    /// <summary>SIP kullanici adi</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SIP sifresi (sifrelenmis olarak saklanir)</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Transport turu: UDP, TCP, TLS, WSS</summary>
    public string? Transport { get; set; } = "UDP";

    /// <summary>
    /// Ozel WebSocket URI. Doluysa dogrudan kullanilir.
    /// Bossa otomatik olusturulur: wss://{Server}:{Port}/ws
    /// Ornek: wss://edge.sip.onsip.com, wss://webrtc.sipthor.net
    /// </summary>
    public string? WsUri { get; set; }

    /// <summary>SRTP ile sifrelenmis arama mi?</summary>
    public bool UseSrtp { get; set; }

    // ─── TURN/ICE NAT Traversal ───
    /// <summary>STUN sunucu (ornek: stun:stun.l.google.com:19302)</summary>
    public string? StunServer { get; set; }

    /// <summary>TURN sunucu (ornek: turn:turn.example.com:3478)</summary>
    public string? TurnServer { get; set; }

    /// <summary>TURN kullanici adi</summary>
    public string? TurnUsername { get; set; }

    /// <summary>TURN sifresi (sifrelenmis olarak saklanir)</summary>
    public string? TurnPassword { get; set; }

    // ─── Codec Tercihleri ───
    /// <summary>Codec oncelik sirasi, JSON array: ["opus","g722","pcmu","pcma"]</summary>
    public string? PreferredCodecs { get; set; }

    /// <summary>Jitter buffer minimum gecikme (ms). 0 = varsayilan</summary>
    public int JitterBufferMinMs { get; set; }

    /// <summary>Jitter buffer maksimum gecikme (ms). 0 = varsayilan</summary>
    public int JitterBufferMaxMs { get; set; }

    /// <summary>Varsayilan SIP hesabi mi?</summary>
    public bool IsDefault { get; set; }

    /// <summary>Aktif mi?</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Olusturulma zamani</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Senkronizasyon ──

    /// <summary>Backend API'ye basariyla push edildi mi?</summary>
    public bool IsSyncedToBackend { get; set; }

    /// <summary>Son senkronizasyon denemesi zamani</summary>
    public DateTime? LastSyncAttempt { get; set; }

    /// <summary>Backend'teki SipAccount ID'si (push sonrasi doner)</summary>
    public int? BackendSipAccountId { get; set; }
}
