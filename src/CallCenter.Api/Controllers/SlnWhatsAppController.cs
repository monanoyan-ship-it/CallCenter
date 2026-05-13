using System.Security.Claims;
using System.Text.Json;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-whatsapp")]
[Authorize]
public class SlnWhatsAppController : ControllerBase
{
    private readonly ISlnWhatsAppFactory _factory;

    public SlnWhatsAppController(ISlnWhatsAppFactory factory)
    {
        _factory = factory;
    }

    [HttpGet("config")]
    public async Task<ActionResult> GetConfig()
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        return Ok(await _factory.GetConfigAsync(cid));
    }

    [HttpPost("config")]
    public async Task<ActionResult> SaveConfig([FromBody] SlnWhatsAppConfig dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        await _factory.SaveConfigAsync(cid, dto);
        return Ok();
    }

    [HttpGet("messages")]
    public async Task<ActionResult> GetMessages([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        return Ok(await _factory.GetMessagesAsync(cid, page, pageSize));
    }

    [HttpPost("send-test")]
    public async Task<ActionResult> SendTest([FromBody] SendTestDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var success = await _factory.SendTestAsync(cid, dto.Phone, dto.Message);
        return success ? Ok(new { success = true }) : BadRequest("Mesaj gonderilemedi");
    }

    [HttpPost("send-message")]
    public async Task<ActionResult> SendMessage([FromBody] SendTestDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var success = await _factory.SendMessageAsync(cid, dto.Phone, dto.Message);
        return success ? Ok(new { success = true }) : BadRequest("Mesaj gonderilemedi");
    }

    [AllowAnonymous]
    [HttpPost("webhook/{phoneNumberId}")]
    public async Task<ActionResult> Webhook(string phoneNumberId, [FromBody] JsonElement payload)
    {
        var incoming = ExtractIncomingMessage(payload);
        if (incoming == null) return Ok();

        var (success, error) = await _factory.RecordIncomingMessageAsync(
            phoneNumberId, incoming.Value.Phone, incoming.Value.Message, incoming.Value.MessageId);

        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private static (string Phone, string Message, string? MessageId)? ExtractIncomingMessage(JsonElement payload)
    {
        if (payload.TryGetProperty("phone", out var phone) && payload.TryGetProperty("message", out var message))
            return (phone.GetString() ?? "", message.GetString() ?? "", payload.TryGetProperty("messageId", out var id) ? id.GetString() : null);

        if (!payload.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array) continue;
            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value)) continue;
                if (!value.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array) continue;
                var first = messages.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Undefined) continue;
                var from = first.TryGetProperty("from", out var fromProp) ? fromProp.GetString() : null;
                var id = first.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                var body = first.TryGetProperty("text", out var text) && text.TryGetProperty("body", out var bodyProp)
                    ? bodyProp.GetString()
                    : null;
                if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(body))
                    return (from, body, id);
            }
        }

        return null;
    }
}

public class SendTestDto
{
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
