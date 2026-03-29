using System.Security.Claims;
using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-whatsapp")]
[Authorize]
public class SlnWhatsAppController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWhatsAppService _whatsApp;

    public SlnWhatsAppController(AppDbContext db, IWhatsAppService whatsApp)
    {
        _db = db;
        _whatsApp = whatsApp;
    }

    [HttpGet("config")]
    public async Task<ActionResult> GetConfig()
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        var config = await _db.SlnWhatsAppConfigs.FirstOrDefaultAsync(c => c.CustomerId == cid);
        return Ok(config ?? new SlnWhatsAppConfig());
    }

    [HttpPost("config")]
    public async Task<ActionResult> SaveConfig([FromBody] SlnWhatsAppConfig dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var config = await _db.SlnWhatsAppConfigs.FirstOrDefaultAsync(c => c.CustomerId == cid);
        if (config == null)
        {
            config = new SlnWhatsAppConfig { CustomerId = cid };
            _db.SlnWhatsAppConfigs.Add(config);
        }

        config.BusinessAccountId = dto.BusinessAccountId;
        config.PhoneNumberId = dto.PhoneNumberId;
        config.AccessToken = dto.AccessToken;
        config.WebhookVerifyToken = dto.WebhookVerifyToken;
        config.IsActive = dto.IsActive;
        config.SendAppointmentReminder = dto.SendAppointmentReminder;
        config.SendAppointmentConfirmation = dto.SendAppointmentConfirmation;
        config.SendBirthdayGreeting = dto.SendBirthdayGreeting;
        config.ReminderHoursBefore = dto.ReminderHoursBefore;

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("messages")]
    public async Task<ActionResult> GetMessages([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var messages = await _db.SlnWhatsAppMessages
            .Where(m => m.CustomerId == cid)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new
            {
                m.Id, m.DirectionId, m.PhoneNumber, m.TemplateName, m.MessageBody,
                m.StatusId, m.ErrorMessage, m.CreatedAt,
                ClientName = m.SlnClient != null ? m.SlnClient.FullName : null
            }).ToListAsync();

        return Ok(messages);
    }

    [HttpPost("send-test")]
    public async Task<ActionResult> SendTest([FromBody] SendTestDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var success = await _whatsApp.SendTextMessageAsync(cid, dto.Phone, dto.Message);
        return success ? Ok(new { success = true }) : BadRequest("Mesaj gonderilemedi");
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}

public class SendTestDto
{
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
