namespace CallCenter.Shared.Entities;

/// <summary>
/// SIP Gateway (GoIP, Asterisk, FreeSWITCH vb.)
/// Sunucu baglanti bilgilerini tutar. Hatlar (SipLine) alt tablodadir.
/// </summary>
public class SipAccount
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 5060;
    public string? Domain { get; set; }
    public string? Transport { get; set; } = "UDP";
    /// <summary>
    /// Ozel WebSocket URI. Doluysa dogrudan kullanilir.
    /// Bossa otomatik olusturulur: wss://{Server}:{Port}/ws
    /// Ornek: wss://edge.sip.onsip.com, wss://webrtc.sipthor.net
    /// </summary>
    public string? WsUri { get; set; }
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

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Hangi musteriye ait (her gateway bir firmaya baglidir)
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Opsiyonel organizasyon birimi baglantisi
    public int? OrganizationUnitId { get; set; }
    public CustomerOrganizationUnit? OrganizationUnit { get; set; }

    // ─── Hatlar (alt tablo) ───
    public ICollection<SipLine> Lines { get; set; } = new List<SipLine>();
}
