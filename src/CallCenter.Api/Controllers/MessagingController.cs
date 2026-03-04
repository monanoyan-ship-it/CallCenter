using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagingController : ControllerBase
{
    private readonly IMessagingFactory _messagingFactory;

    public MessagingController(IMessagingFactory messagingFactory) => _messagingFactory = messagingFactory;

    /// <summary>Mesaj gonder</summary>
    [HttpPost("send")]
    public async Task<ActionResult<InstantMessageDto>> SendMessage([FromBody] SendMessageRequest req)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var customerId = GetCustomerId();
        var result = await _messagingFactory.SendMessageAsync(req, userId.Value, customerId);
        return Ok(result);
    }

    /// <summary>Belirli bir kullaniciyla konusma gecmisi</summary>
    [HttpGet("conversation/{otherUserId}")]
    public async Task<ActionResult<List<InstantMessageDto>>> GetConversation(int otherUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var messages = await _messagingFactory.GetConversationAsync(userId.Value, otherUserId, page, pageSize);
        return Ok(messages);
    }

    /// <summary>Tum konusmalarin ozeti</summary>
    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationSummaryDto>>> GetConversations()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var conversations = await _messagingFactory.GetConversationsAsync(userId.Value);
        return Ok(conversations);
    }

    /// <summary>Mesaji okundu olarak isaretle</summary>
    [HttpPost("{messageId}/read")]
    public async Task<ActionResult> MarkAsRead(int messageId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var result = await _messagingFactory.MarkAsReadAsync(messageId, userId.Value);
        return result ? Ok() : NotFound();
    }

    /// <summary>Konusmadaki tum mesajlari okundu olarak isaretle</summary>
    [HttpPost("conversation/{otherUserId}/read-all")]
    public async Task<ActionResult<int>> MarkConversationAsRead(int otherUserId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var count = await _messagingFactory.MarkConversationAsReadAsync(userId.Value, otherUserId);
        return Ok(count);
    }

    /// <summary>Okunmamis mesaj sayisi</summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var count = await _messagingFactory.GetUnreadCountAsync(userId.Value);
        return Ok(count);
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null ? int.Parse(claim) : null;
    }

    private int? GetCustomerId()
    {
        var claim = User.FindFirst("CustomerId")?.Value;
        return claim != null ? int.Parse(claim) : null;
    }
}
