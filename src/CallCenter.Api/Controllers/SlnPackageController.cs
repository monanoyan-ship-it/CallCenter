using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-packages")]
[Authorize]
public class SlnPackageController : ControllerBase
{
    private readonly ISlnPackageFactory _packageFactory;

    public SlnPackageController(ISlnPackageFactory packageFactory) => _packageFactory = packageFactory;

    [HttpGet("definitions")]
    public async Task<ActionResult<List<SlnPackageDefinitionDto>>> GetDefinitions()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _packageFactory.GetDefinitionsAsync(customerId));
    }

    [HttpPost("definitions")]
    public async Task<ActionResult<SlnPackageDefinitionDto>> CreateDefinition([FromBody] SlnPackageDefinitionCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _packageFactory.CreateDefinitionAsync(dto, customerId));
    }

    [HttpPut("definitions/{id}")]
    public async Task<ActionResult> UpdateDefinition(int id, [FromBody] SlnPackageDefinitionCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _packageFactory.UpdateDefinitionAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("definitions/{id}")]
    public async Task<ActionResult> DeleteDefinition(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _packageFactory.DeleteDefinitionAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpGet("client-packages")]
    public async Task<ActionResult<List<SlnClientPackageDto>>> GetClientPackages([FromQuery] int? clientId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _packageFactory.GetClientPackagesAsync(customerId, clientId));
    }

    [HttpPost("sell")]
    public async Task<ActionResult<SlnClientPackageDto>> SellPackage([FromBody] SlnClientPackageSellDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (pkg, error) = await _packageFactory.SellPackageAsync(dto, userId, customerId);
        return pkg != null ? Ok(pkg) : BadRequest(error);
    }

    [HttpPost("use")]
    public async Task<ActionResult> UseSession([FromBody] SlnPackageUseDto dto)
    {
        var userId = GetUserId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _packageFactory.UseSessionAsync(dto, userId, customerId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetUserId()
        => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
