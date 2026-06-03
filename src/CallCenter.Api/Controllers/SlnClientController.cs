using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Api.Security;
using CallCenter.Api.Services;
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
    private readonly GcsUploadService _gcs;

    public SlnClientController(ISlnClientFactory clientFactory, GcsUploadService gcs)
    {
        _clientFactory = clientFactory;
        _gcs = gcs;
    }

    [HttpGet]
    public async Task<IActionResult> GetClients([FromQuery] string? search, [FromQuery] int? branchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var result = await _clientFactory.GetClientsAsync(customerId, search, ResolveBranchId(branchId), page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnClientDetailDto>> GetClient(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var client = await _clientFactory.GetClientDetailAsync(id, customerId, ResolveBranchId(branchId));
        return client != null ? Ok(client) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnClientDto>> CreateClient([FromBody] SlnClientCreateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        try
        {
            var client = await _clientFactory.CreateClientAsync(dto, customerId, ResolveBranchId(branchId));
            return Ok(client);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateClient(int id, [FromBody] SlnClientUpdateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.UpdateClientAsync(id, dto, customerId, ResolveBranchId(branchId));
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("{id}/health")]
    public async Task<ActionResult> UpdateHealthInfo(int id, [FromBody] SlnClientHealthUpdateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.UpdateHealthInfoAsync(
            id, dto, customerId, requiresReview: false, reviewedByPersonnelId: GetUserId(), branchId: ResolveBranchId(branchId));
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("{id}/health/review")]
    public async Task<ActionResult> ReviewHealthInfo(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.ReviewHealthInfoAsync(id, customerId, GetUserId(), ResolveBranchId(branchId));
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteClient(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.DeleteClientAsync(id, customerId, ResolveBranchId(branchId));
        return success ? Ok() : BadRequest(error);
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult> GetSuggestions([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var suggestions = await _clientFactory.GetSuggestionsAsync(customerId, ResolveBranchId(branchId));
        return Ok(suggestions);
    }

    [HttpPost("formulas")]
    public async Task<ActionResult<SlnFormulaDto>> AddFormula([FromBody] SlnFormulaCreateDto dto, [FromQuery] int? branchId)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        try
        {
            var formula = await _clientFactory.AddFormulaAsync(dto, userId, customerId, ResolveBranchId(branchId));
            return Ok(formula);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("formulas/{id}")]
    public async Task<ActionResult> DeleteFormula(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.DeleteFormulaAsync(id, customerId, ResolveBranchId(branchId));
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("treatment-records")]
    public async Task<ActionResult<SlnTreatmentRecordDto>> AddTreatmentRecord([FromBody] SlnTreatmentRecordCreateDto dto, [FromQuery] int? branchId)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        try
        {
            var record = await _clientFactory.AddTreatmentRecordAsync(dto, userId, customerId, ResolveBranchId(branchId));
            return Ok(record);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("treatment-records/{id}")]
    public async Task<ActionResult> DeleteTreatmentRecord(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.DeleteTreatmentRecordAsync(id, customerId, ResolveBranchId(branchId));
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("{id}/photos")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    public async Task<ActionResult<SlnClientPhotoDto>> UploadPhoto(
        int id,
        IFormFile file,
        [FromForm] string? description,
        [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var validation = await FileUploadValidation.ValidateImageAsync(file);
        if (!validation.Success) return BadRequest(validation.Error);

        var fileName = $"client-photo-{Guid.NewGuid():N}{validation.Extension}";
        var path = $"salons/{customerId}/clients/{id}/{fileName}";

        using var stream = file.OpenReadStream();
        var (url, error) = await _gcs.UploadAsync(stream, path, validation.ContentType);
        if (url == null) return BadRequest(error ?? "Yukleme hatasi.");

        try
        {
            var photo = await _clientFactory.AddPhotoAsync(id, url, description, customerId, ResolveBranchId(branchId));
            return Ok(photo);
        }
        catch (InvalidOperationException ex)
        {
            await _gcs.DeleteAsync(path);
            return BadRequest(ex.Message);
        }
        catch
        {
            await _gcs.DeleteAsync(path);
            throw;
        }
    }

    [HttpDelete("photos/{id}")]
    public async Task<ActionResult> DeletePhoto(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error, filePath) = await _clientFactory.DeletePhotoAsync(id, customerId, ResolveBranchId(branchId));
        if (success)
        {
            var path = _gcs.TryGetObjectPath(filePath);
            if (!string.IsNullOrEmpty(path) && path.StartsWith($"salons/{customerId}/clients/", StringComparison.OrdinalIgnoreCase))
                await _gcs.DeleteAsync(path);
        }

        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("{id}/unblock")]
    public async Task<ActionResult> UnblockClient(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _clientFactory.UnblockClientAsync(id, customerId, ResolveBranchId(branchId));
        return success ? Ok() : NotFound(error);
    }

    private int GetUserId()
        => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return int.TryParse(claim, out var id) && id > 0 ? id : null;
    }

    private int GetCustomerRoleId()
    {
        var claim = User.FindFirst("CustomerRoleId")?.Value;
        return int.TryParse(claim, out var roleId) ? roleId : SalonRoles.Ids.SalonOwner;
    }

    private int? GetBranchScopeId()
        => GetCustomerRoleId() == SalonRoles.Ids.SalonOwner ? null : GetBranchId();

    private int? ResolveBranchId(int? requestedBranchId)
        => GetBranchScopeId() ?? requestedBranchId;
}
