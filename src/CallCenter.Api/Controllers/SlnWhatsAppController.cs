using System.Security.Claims;
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

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}

public class SendTestDto
{
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
