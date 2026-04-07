using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-consent-forms")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnConsentForms)]
public class SlnConsentFormController : ControllerBase
{
    private readonly ISlnConsentFormFactory _factory;

    public SlnConsentFormController(ISlnConsentFormFactory factory) => _factory = factory;

    [HttpGet]
    public async Task<ActionResult<List<SlnConsentFormDto>>> GetForms()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetFormsAsync(customerId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnConsentFormDto>> GetForm(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var form = await _factory.GetFormAsync(id, customerId);
        return form != null ? Ok(form) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnConsentFormDto>> CreateForm([FromBody] SlnConsentFormCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.CreateFormAsync(dto, customerId));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateForm(int id, [FromBody] SlnConsentFormUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdateFormAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteForm(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeleteFormAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpGet("consents")]
    public async Task<ActionResult<List<SlnClientConsentDto>>> GetSignedConsents([FromQuery] int? formId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetSignedConsentsAsync(customerId, formId));
    }

    [HttpPost("consents")]
    public async Task<ActionResult<SlnClientConsentDto>> CreateConsent([FromBody] SlnClientConsentCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.CreateConsentAsync(dto));
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
