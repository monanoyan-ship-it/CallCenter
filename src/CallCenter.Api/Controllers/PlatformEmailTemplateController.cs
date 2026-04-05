using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/platform-email-templates")]
[Authorize(Roles = "Admin")]
public class PlatformEmailTemplateController : ControllerBase
{
    private readonly IPlatformEmailTemplateFactory _factory;

    public PlatformEmailTemplateController(IPlatformEmailTemplateFactory factory) => _factory = factory;

    // ─── Event CRUD ───

    [HttpGet]
    public async Task<ActionResult<List<PlatformEmailEventDto>>> GetAllEvents()
        => Ok(await _factory.GetAllEventsAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<PlatformEmailEventDto>> GetEvent(int id)
    {
        var ev = await _factory.GetEventByIdAsync(id);
        return ev != null ? Ok(ev) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<PlatformEmailEventDto>> CreateEvent([FromBody] PlatformEmailEventCreateDto dto)
        => Ok(await _factory.CreateEventAsync(dto));

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateEvent(int id, [FromBody] PlatformEmailEventUpdateDto dto)
    {
        var (success, error) = await _factory.UpdateEventAsync(id, dto);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEvent(int id)
    {
        var (success, error) = await _factory.DeleteEventAsync(id);
        return success ? Ok() : BadRequest(error);
    }

    // ─── Template CRUD (dil bazli) ───

    [HttpPost("{eventId}/templates")]
    public async Task<ActionResult<PlatformEmailTemplateDto>> AddTemplate(int eventId, [FromBody] PlatformEmailTemplateCreateDto dto)
        => Ok(await _factory.AddTemplateAsync(eventId, dto));

    [HttpPut("templates/{templateId}")]
    public async Task<ActionResult> UpdateTemplate(int templateId, [FromBody] PlatformEmailTemplateUpdateDto dto)
    {
        var (success, error) = await _factory.UpdateTemplateAsync(templateId, dto);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("templates/{templateId}")]
    public async Task<ActionResult> DeleteTemplate(int templateId)
    {
        var (success, error) = await _factory.DeleteTemplateAsync(templateId);
        return success ? Ok() : BadRequest(error);
    }
}
