using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class MessagingService : IMessagingService
{
    private readonly AppDbContext _db;

    public MessagingService(AppDbContext db) => _db = db;

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

        _db.InstantMessages.Add(message);
        await _db.SaveChangesAsync();

        // Sender bilgisini doldurmak icin yeniden oku
        var sender = await _db.Users.FindAsync(senderUserId);
        var receiver = req.ReceiverUserId.HasValue
            ? await _db.Users.FindAsync(req.ReceiverUserId.Value)
            : null;

        return MapToDto(message, sender?.FullName ?? "", receiver?.FullName);
    }

    public async Task<List<InstantMessageDto>> GetConversationAsync(int userId1, int userId2, int page = 1, int pageSize = 50)
    {
        var messages = await _db.InstantMessages
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
            .OrderBy(m => m.SentAt) // Kronolojik sira
            .Select(m => MapToDto(m, m.SenderUser?.FullName ?? "", m.ReceiverUser?.FullName))
            .ToList();
    }

    public async Task<List<ConversationSummaryDto>> GetConversationsAsync(int userId)
    {
        // Kullanicinin dahil oldugu tum mesajlari grupla
        var messages = await _db.InstantMessages
            .Include(m => m.SenderUser)
            .Include(m => m.ReceiverUser)
            .Where(m => m.SenderUserId == userId || m.ReceiverUserId == userId)
            .ToListAsync();

        // Konusma partnerleri bazinda grupla
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
        var message = await _db.InstantMessages.FindAsync(messageId);
        if (message == null || message.ReceiverUserId != userId) return false;

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkConversationAsReadAsync(int currentUserId, int otherUserId)
    {
        var unread = await _db.InstantMessages
            .Where(m => m.SenderUserId == otherUserId &&
                        m.ReceiverUserId == currentUserId &&
                        !m.IsRead)
            .ToListAsync();

        foreach (var msg in unread)
        {
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return unread.Count;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.InstantMessages
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
