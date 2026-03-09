namespace CallCenter.Shared.Entities;

/// <summary>
/// Gateway'e ait bir hat (kanal). Her hat bir SIP kullanici hesabidir.
/// GoIP Config By Line modunda her kanal ayri extension'dir.
/// Trunk modunda tek hat yeterlidir (gateway dahili dagitim yapar).
/// </summary>
public class SipLine
{
    public int Id { get; set; }

    /// <summary>Kanal numarasi (1, 2, 3, 4 vb.)</summary>
    public int ChannelNumber { get; set; } = 1;

    /// <summary>SIP kullanici adi (extension)</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SIP sifresi (AES-256-CBC sifreli)</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Hat aciklamasi (opsiyonel)</summary>
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ─── Gateway baglantisi ───
    public int SipAccountId { get; set; }
    public SipAccount SipAccount { get; set; } = null!;

    // ─── Dinamik hat tahsisi (runtime) ───
    public int? AssignedPersonnelId { get; set; }
    public CustomerPersonnel? AssignedPersonnel { get; set; }
    public DateTime? AssignedAt { get; set; }
}
