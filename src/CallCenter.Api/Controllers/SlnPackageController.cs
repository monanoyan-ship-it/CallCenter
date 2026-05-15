using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-packages")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnPackages)]
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
    public async Task<ActionResult<List<SlnClientPackageDto>>> GetClientPackages([FromQuery] int? clientId, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _packageFactory.GetClientPackagesAsync(customerId, clientId, GetBranchId() ?? branchId));
    }

    [HttpPost("sell")]
    public async Task<ActionResult<SlnClientPackageDto>> SellPackage([FromBody] SlnClientPackageSellDto dto, [FromQuery] int? branchId)
    {
        var personnelId = GetPersonnelId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (pkg, error) = await _packageFactory.SellPackageAsync(dto, personnelId, customerId, GetBranchId() ?? branchId);
        return pkg != null ? Ok(pkg) : BadRequest(error);
    }

    [HttpPost("use")]
    public async Task<ActionResult> UseSession([FromBody] SlnPackageUseDto dto, [FromQuery] int? branchId)
    {
        var personnelId = GetPersonnelId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _packageFactory.UseSessionAsync(dto, personnelId, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("usable")]
    public async Task<ActionResult<List<SlnPackageBenefitDto>>> GetUsablePackages([FromBody] SlnPackageBenefitCheckDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _packageFactory.GetUsablePackagesAsync(customerId, dto.SlnClientId, dto.ServiceIds, GetBranchId() ?? branchId));
    }

    private int GetPersonnelId()
        => int.Parse(User.FindFirst("CustomerPersonnelId")?.Value ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
