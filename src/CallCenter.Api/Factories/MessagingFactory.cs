using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class MessagingFactory : IMessagingFactory
{
    private readonly IInstantMessageEntityService _messageEs;
    private readonly IUserEntityService _userEs;
    private readonly IUnitOfWork _uow;

    public MessagingFactory(IInstantMessageEntityService messageEs, IUserEntityService userEs, IUnitOfWork uow)
    {
        _messageEs = messageEs;
        _userEs = userEs;
        _uow = uow;
    }

    public async Task<InstantMessageDto> SendMessageAsync(SendMessageRequest req, int senderUserId, int? customerId)
    {
        var message = new InstantMessage
        {
            SenderUserId = senderUserId,
            ReceiverUserId = req.ReceiverUserId,
            Content = req.Content,
            MessageTypeId = req.MessageTypeId,
            CustomerId = customerId
        };

        _messageEs.Add(message);
        await _uow.SaveChangesAsync();

        var sender = await _userEs.GetByIdAsync(senderUserId);
        var receiver = req.ReceiverUserId.HasValue
            ? await _userEs.GetByIdAsync(req.ReceiverUserId.Value)
            : null;

        return MapToDto(message, sender?.FullName ?? "", receiver?.FullName);
    }

    public async Task<List<InstantMessageDto>> GetConversationAsync(int userId1, int userId2, int page = 1, int pageSize = 50)
    {
        var messages = await _messageEs.GetAllQueryable()
            .Include(m => m.SenderUser)
            .Include(m => m.ReceiverUser)
            .Where(m =>
                (m.SenderUserId == userId1 && m.ReceiverUserId == userId2) ||
                (m.SenderUserId == userId2 && m.ReceiverUserId == userId1))
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return messages
            .OrderBy(m => m.SentAt)
            .Select(m => MapToDto(m, m.SenderUser?.FullName ?? "", m.ReceiverUser?.FullName))
            .ToList();
    }

    public async Task<List<ConversationSummaryDto>> GetConversationsAsync(int userId)
    {
        var messages = await _messageEs.GetAllQueryable()
            .Include(m => m.SenderUser)
            .Include(m => m.ReceiverUser)
            .Where(m => m.SenderUserId == userId || m.ReceiverUserId == userId)
            .ToListAsync();

        var conversations = messages
            .GroupBy(m => m.SenderUserId == userId ? m.ReceiverUserId ?? 0 : m.SenderUserId)
            .Where(g => g.Key > 0)
            .Select(g =>
            {
                var lastMsg = g.OrderByDescending(m => m.SentAt).First();
                var partner = lastMsg.SenderUserId == userId ? lastMsg.ReceiverUser : lastMsg.SenderUser;
                var unread = g.Count(m => m.ReceiverUserId == userId && !m.IsRead);

                return new ConversationSummaryDto
                {
                    UserId = g.Key,
                    UserName = partner?.FullName ?? "",
                    UserExtension = partner?.Extension,
                    StatusId = partner?.StatusId ?? AgentStatuses.Ids.Offline,
                    StatusName = AgentStatuses.GetById(partner?.StatusId ?? 1)?.SystemName ?? "Offline",
                    LastMessageContent = lastMsg.Content.Length > 100
                        ? lastMsg.Content[..100] + "..."
                        : lastMsg.Content,
                    LastMessageAt = lastMsg.SentAt,
                    UnreadCount = unread
                };
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToList();

        return conversations;
    }

    public async Task<bool> MarkAsReadAsync(int messageId, int userId)
    {
        var message = await _messageEs.GetByIdAsync(messageId);
        if (message == null || message.ReceiverUserId != userId) return false;

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkConversationAsReadAsync(int currentUserId, int otherUserId)
    {
        var unread = await _messageEs.GetAllQueryable()
            .Where(m => m.SenderUserId == otherUserId &&
                        m.ReceiverUserId == currentUserId &&
                        !m.IsRead)
            .ToListAsync();

        foreach (var msg in unread)
        {
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
        }

        await _uow.SaveChangesAsync();
        return unread.Count;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _messageEs.GetAllQueryable()
            .CountAsync(m => m.ReceiverUserId == userId && !m.IsRead);
    }

    private static InstantMessageDto MapToDto(InstantMessage m, string senderName, string? receiverName) => new()
    {
        Id = m.Id,
        Uid = m.Uid,
        SenderUserId = m.SenderUserId,
        SenderName = senderName,
        ReceiverUserId = m.ReceiverUserId,
        ReceiverName = receiverName,
        Content = m.Content,
        MessageTypeId = m.MessageTypeId,
        MessageTypeName = MessageTypes.GetById(m.MessageTypeId)?.SystemName ?? "Text",
        IsRead = m.IsRead,
        ReadAt = m.ReadAt,
        SentAt = m.SentAt
    };
}
