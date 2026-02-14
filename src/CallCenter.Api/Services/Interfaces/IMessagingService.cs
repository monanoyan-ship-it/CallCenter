using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface IMessagingService
{
    /// <summary>Mesaj gonder</summary>
    Task<InstantMessageDto> SendMessageAsync(SendMessageRequest req, int senderUserId, int? customerId);

    /// <summary>Iki kullanici arasindaki mesaj gecmisini getir</summary>
    Task<List<InstantMessageDto>> GetConversationAsync(int userId1, int userId2, int page = 1, int pageSize = 50);

    /// <summary>Kullanicinin tum konusmalarinin ozetini getir</summary>
    Task<List<ConversationSummaryDto>> GetConversationsAsync(int userId);

    /// <summary>Mesaji okundu olarak isaretle</summary>
    Task<bool> MarkAsReadAsync(int messageId, int userId);

    /// <summary>Bir konusmadaki tum mesajlari okundu olarak isaretle</summary>
    Task<int> MarkConversationAsReadAsync(int currentUserId, int otherUserId);

    /// <summary>Toplam okunmamis mesaj sayisi</summary>
    Task<int> GetUnreadCountAsync(int userId);
}
