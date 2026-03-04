using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IMessagingFactory
{
    Task<InstantMessageDto> SendMessageAsync(SendMessageRequest req, int senderUserId, int? customerId);
    Task<List<InstantMessageDto>> GetConversationAsync(int userId1, int userId2, int page = 1, int pageSize = 50);
    Task<List<ConversationSummaryDto>> GetConversationsAsync(int userId);
    Task<bool> MarkAsReadAsync(int messageId, int userId);
    Task<int> MarkConversationAsReadAsync(int currentUserId, int otherUserId);
    Task<int> GetUnreadCountAsync(int userId);
}
