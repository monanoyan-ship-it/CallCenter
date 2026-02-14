namespace CallCenter.Shared.DTOs;

/// <summary>Mesaj gonderme istegi</summary>
public class SendMessageRequest
{
    /// <summary>Alici kullanici ID (null ise broadcast)</summary>
    public int? ReceiverUserId { get; set; }

    /// <summary>Mesaj icerigi</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Mesaj tipi (varsayilan: Text)</summary>
    public int MessageTypeId { get; set; } = 1;
}

/// <summary>Mesaj DTO (liste/detay gorunumu)</summary>
public class InstantMessageDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public int? ReceiverUserId { get; set; }
    public string? ReceiverName { get; set; }
    public string Content { get; set; } = string.Empty;
    public int MessageTypeId { get; set; }
    public string MessageTypeName { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime SentAt { get; set; }
}

/// <summary>Konusma ozeti (son mesaj + okunmamis sayisi)</summary>
public class ConversationSummaryDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserExtension { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

/// <summary>Mesaj okundu bildirimi (SignalR event)</summary>
public class MessageReadNotification
{
    public int MessageId { get; set; }
    public int ReadByUserId { get; set; }
    public DateTime ReadAt { get; set; }
}
