namespace CallCenter.Shared.Entities;

/// <summary>
/// KVKK aydinlatma metni. Her musteri kendi metnini versiyonlayarak yonetir.
/// Ses kaydi oncesi arayan kisiye okunur veya IVR ile dinletilir.
/// </summary>
public class PrivacyNotice
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>PrivacyNoticeTypes (CallRecording=1, DataProcessing=2, General=3)</summary>
    public int TypeId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Aydinlatma metninin tam icerigi (HTML/duz metin)</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Versiyon etiketi: "v1.0", "2026-03" vb.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Ayni Customer+Type icin sadece bir aktif versiyon olabilir</summary>
    public bool IsActive { get; set; }

    /// <summary>IVR'da dinletilecek ses dosyasi (opsiyonel)</summary>
    public int? GreetingMessageId { get; set; }
    public GreetingMessage? GreetingMessage { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
