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
    public async Task<ActionResult<List<SlnBeforeAfterPhotoDto>>> GetPhotos()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetPhotosAsync(customerId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnBeforeAfterPhotoDto>> GetPhoto(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var photo = await _factory.GetPhotoAsync(id, customerId);
        return photo != null ? Ok(photo) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnBeforeAfterPhotoDto>> CreatePhoto([FromBody] SlnBeforeAfterPhotoCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.CreatePhotoAsync(dto, customerId));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdatePhoto(int id, [FromBody] SlnBeforeAfterPhotoUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdatePhotoAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePhoto(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeletePhotoAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
