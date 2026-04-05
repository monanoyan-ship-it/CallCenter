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

    [HttpGet]
    public async Task<ActionResult<List<PlatformEmailTemplateDto>>> GetAll()
        => Ok(await _factory.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<PlatformEmailTemplateDto>> GetById(int id)
    {
        var template = await _factory.GetByIdAsync(id);
        return template != null ? Ok(template) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<PlatformEmailTemplateDto>> Create([FromBody] PlatformEmailTemplateCreateDto dto)
        => Ok(await _factory.CreateAsync(dto));

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] PlatformEmailTemplateUpdateDto dto)
    {
        var (success, error) = await _factory.UpdateAsync(id, dto);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var (success, error) = await _factory.DeleteAsync(id);
        return success ? Ok() : BadRequest(error);
    }
}
