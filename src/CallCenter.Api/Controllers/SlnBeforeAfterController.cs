using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-before-after")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnBeforeAfter)]
public class SlnBeforeAfterController : ControllerBase
{
    private readonly ISlnBeforeAfterFactory _factory;

    public SlnBeforeAfterController(ISlnBeforeAfterFactory factory) => _factory = factory;

    [HttpGet]
    public async Task<ActionResult<List<SlnBeforeAfterPhotoDto>>> GetPhotos([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetPhotosAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnBeforeAfterPhotoDto>> GetPhoto(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var photo = await _factory.GetPhotoAsync(id, customerId, GetBranchId() ?? branchId);
        return photo != null ? Ok(photo) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnBeforeAfterPhotoDto>> CreatePhoto([FromBody] SlnBeforeAfterPhotoCreateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.CreatePhotoAsync(dto, customerId, GetBranchId() ?? branchId));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdatePhoto(int id, [FromBody] SlnBeforeAfterPhotoUpdateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdatePhotoAsync(id, dto, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePhoto(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeletePhotoAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var value = User.FindFirst("BranchId")?.Value;
        return int.TryParse(value, out var branchId) && branchId > 0 ? branchId : null;
    }
}
