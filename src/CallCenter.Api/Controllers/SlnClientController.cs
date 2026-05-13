using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-clients")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnClients)]
public class SlnClientController : ControllerBase
{
    private readonly ISlnClientFactory _clientFactory;

    public SlnClientController(ISlnClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetClients([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var result = await _clientFactory.GetClientsAsync(customerId, search, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnClientDetailDto>> GetClient(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var client = await _clientFactory.GetClientDetailAsync(id, customerId);
        return client != null ? Ok(client) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnClientDto>> CreateClient([FromBody] SlnClientCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var client = await _clientFactory.CreateClientAsync(dto, customerId);
        return Ok(client);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateClient(int id, [FromBody] SlnClientUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.UpdateClientAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("{id}/health")]
    public async Task<ActionResult> UpdateHealthInfo(int id, [FromBody] SlnClientHealthUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.UpdateHealthInfoAsync(
            id, dto, customerId, requiresReview: false, reviewedByPersonnelId: GetUserId());
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("{id}/health/review")]
    public async Task<ActionResult> ReviewHealthInfo(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.ReviewHealthInfoAsync(id, customerId, GetUserId());
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteClient(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.DeleteClientAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult> GetSuggestions()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var suggestions = await _clientFactory.GetSuggestionsAsync(customerId);
        return Ok(suggestions);
    }

    [HttpPost("formulas")]
    public async Task<ActionResult<SlnFormulaDto>> AddFormula([FromBody] SlnFormulaCreateDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        try
        {
            var formula = await _clientFactory.AddFormulaAsync(dto, userId, customerId);
            return Ok(formula);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("formulas/{id}")]
    public async Task<ActionResult> DeleteFormula(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.DeleteFormulaAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("treatment-records")]
    public async Task<ActionResult<SlnTreatmentRecordDto>> AddTreatmentRecord([FromBody] SlnTreatmentRecordCreateDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        try
        {
            var record = await _clientFactory.AddTreatmentRecordAsync(dto, userId, customerId);
            return Ok(record);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("treatment-records/{id}")]
    public async Task<ActionResult> DeleteTreatmentRecord(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.DeleteTreatmentRecordAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("{id}/unblock")]
    public async Task<ActionResult> UnblockClient(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.UnblockClientAsync(id, customerId);
        return success ? Ok() : NotFound(error);
    }

    private int GetUserId()
        => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
