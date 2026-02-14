namespace CallCenter.Shared.Entities;

/// <summary>
/// Agent-to-agent ve supervisor-to-agent anlık mesajlaşma.
/// SignalR ile real-time delivery, SIP MESSAGE ile PBX entegrasyonu (opsiyonel).
/// </summary>
public class InstantMessage
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();

    /// <summary>Gönderen kullanıcı</summary>
    public int SenderUserId { get; set; }
    public User? SenderUser { get; set; }

    /// <summary>Alıcı kullanıcı (null ise broadcast/grup mesajı)</summary>
    public int? ReceiverUserId { get; set; }
    public User? ReceiverUser { get; set; }

    /// <summary>Mesaj içeriği (plain text)</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Mesaj tipi: Text, System, File (ileride genisletilebilir)</summary>
    public int MessageTypeId { get; set; } = 1; // MessageTypes.Ids.Text

    /// <summary>Okundu mu?</summary>
    public bool IsRead { get; set; }

    /// <summary>Okunma zamani</summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>Gönderim zamanı</summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>Hangi müşteriye ait (firma izolasyonu)</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}
