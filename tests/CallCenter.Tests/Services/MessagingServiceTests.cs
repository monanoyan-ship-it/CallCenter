using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Tests.Helpers;

namespace CallCenter.Tests.Services;

public class MessagingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly MessagingService _sut;

    public MessagingServiceTests()
    {
        _db = TestDbContextFactory.CreateWithSeedData();
        _sut = new MessagingService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ═══════════════════════════════════════
    // SendMessageAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task SendMessage_ValidRequest_CreatesMessage()
    {
        var req = new SendMessageRequest
        {
            ReceiverUserId = 102,
            Content = "Merhaba, test mesaji",
            MessageTypeId = MessageTypes.Ids.Text
        };

        var result = await _sut.SendMessageAsync(req, senderUserId: 101, customerId: 200);

        result.Should().NotBeNull();
        result.Content.Should().Be("Merhaba, test mesaji");
        result.SenderUserId.Should().Be(101);
        result.ReceiverUserId.Should().Be(102);
        result.SenderName.Should().Be("Agent Bir");
        result.IsRead.Should().BeFalse();

        // DB'de kaydedildi mi?
        var dbMessage = await _db.InstantMessages.FindAsync(result.Id);
        dbMessage.Should().NotBeNull();
        dbMessage!.Content.Should().Be("Merhaba, test mesaji");
    }

    [Fact]
    public async Task SendMessage_BroadcastMessage_ReceiverIsNull()
    {
        var req = new SendMessageRequest
        {
            ReceiverUserId = null,
            Content = "Broadcast mesaj",
            MessageTypeId = MessageTypes.Ids.System
        };

        var result = await _sut.SendMessageAsync(req, senderUserId: 100, customerId: 200);

        result.Should().NotBeNull();
        result.ReceiverUserId.Should().BeNull();
        result.ReceiverName.Should().BeNull();
    }

    // ═══════════════════════════════════════
    // GetConversationAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task GetConversation_ReturnsMessagesBetweenTwoUsers()
    {
        // Arrange: 3 mesaj ekle (2 aralarinda, 1 baska biriyle)
        _db.InstantMessages.AddRange(
            new InstantMessage { SenderUserId = 101, ReceiverUserId = 102, Content = "Mesaj 1", SentAt = DateTime.UtcNow.AddMinutes(-3) },
            new InstantMessage { SenderUserId = 102, ReceiverUserId = 101, Content = "Mesaj 2", SentAt = DateTime.UtcNow.AddMinutes(-2) },
            new InstantMessage { SenderUserId = 101, ReceiverUserId = 100, Content = "Baska mesaj", SentAt = DateTime.UtcNow.AddMinutes(-1) }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetConversationAsync(101, 102);

        result.Should().HaveCount(2);
        result[0].Content.Should().Be("Mesaj 1"); // kronolojik sira
        result[1].Content.Should().Be("Mesaj 2");
    }

    [Fact]
    public async Task GetConversation_EmptyConversation_ReturnsEmptyList()
    {
        var result = await _sut.GetConversationAsync(101, 102);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConversation_Pagination_RespectsPageSize()
    {
        // 5 mesaj ekle
        for (int i = 1; i <= 5; i++)
        {
            _db.InstantMessages.Add(new InstantMessage
            {
                SenderUserId = 101,
                ReceiverUserId = 102,
                Content = $"Mesaj {i}",
                SentAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _db.SaveChangesAsync();

        var page1 = await _sut.GetConversationAsync(101, 102, page: 1, pageSize: 2);
        var page2 = await _sut.GetConversationAsync(101, 102, page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
    }

    // ═══════════════════════════════════════
    // MarkAsReadAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task MarkAsRead_OwnMessage_ReturnsFalse()
    {
        // Kendi gonderdigi mesaji okundu yapamazsin
        var msg = new InstantMessage { SenderUserId = 101, ReceiverUserId = 102, Content = "test" };
        _db.InstantMessages.Add(msg);
        await _db.SaveChangesAsync();

        var result = await _sut.MarkAsReadAsync(msg.Id, userId: 101);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsRead_ReceivedMessage_ReturnsTrue()
    {
        var msg = new InstantMessage { SenderUserId = 101, ReceiverUserId = 102, Content = "test" };
        _db.InstantMessages.Add(msg);
        await _db.SaveChangesAsync();

        var result = await _sut.MarkAsReadAsync(msg.Id, userId: 102);

        result.Should().BeTrue();
        var dbMsg = await _db.InstantMessages.FindAsync(msg.Id);
        dbMsg!.IsRead.Should().BeTrue();
        dbMsg.ReadAt.Should().NotBeNull();
    }

    // ═══════════════════════════════════════
    // GetUnreadCountAsync
    // ═══════════════════════════════════════

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        _db.InstantMessages.AddRange(
            new InstantMessage { SenderUserId = 101, ReceiverUserId = 102, Content = "a", IsRead = false },
            new InstantMessage { SenderUserId = 101, ReceiverUserId = 102, Content = "b", IsRead = false },
            new InstantMessage { SenderUserId = 101, ReceiverUserId = 102, Content = "c", IsRead = true },
            new InstantMessage { SenderUserId = 102, ReceiverUserId = 101, Content = "d", IsRead = false } // agent1'e gelen
        );
        await _db.SaveChangesAsync();

        var unreadFor102 = await _sut.GetUnreadCountAsync(102);
        var unreadFor101 = await _sut.GetUnreadCountAsync(101);

        unreadFor102.Should().Be(2); // 101'den gelen 2 okunmamis
        unreadFor101.Should().Be(1); // 102'den gelen 1 okunmamis
    }
}
