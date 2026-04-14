using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-branches")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnBranches)]
public class SlnBranchController : ControllerBase
{
    private readonly ISlnBranchFactory _branchFactory;

    public SlnBranchController(ISlnBranchFactory branchFactory) => _branchFactory = branchFactory;

    [HttpGet]
    public async Task<ActionResult<List<SlnBranchDto>>> GetBranches()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var branches = await _branchFactory.GetBranchesAsync(customerId);
        return Ok(branches);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnBranchDto>> GetBranch(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var branch = await _branchFactory.GetBranchAsync(id, customerId);
        return branch != null ? Ok(branch) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnBranchDto>> CreateBranch([FromBody] SlnBranchCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var branch = await _branchFactory.CreateBranchAsync(dto, customerId);
        return Ok(branch);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateBranch(int id, [FromBody] SlnBranchUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _branchFactory.UpdateBranchAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBranch(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _branchFactory.DeleteBranchAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    /// <summary>Firmaya ait tum subelerin city/district alanlarini TR-normalize eder</summary>
    [HttpPost("normalize-addresses")]
    public async Task<ActionResult> NormalizeAddresses()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var result = await _branchFactory.NormalizeAddressesAsync(customerId);
        return Ok(result);
    }

    /// <summary>WorkingHoursJson NULL olan subelere default 09:00-19:00 (Pzt-Cmt) seed eder</summary>
    [HttpPost("normalize-working-hours")]
    public async Task<ActionResult> NormalizeWorkingHours()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var result = await _branchFactory.NormalizeWorkingHoursAsync(customerId);
        return Ok(result);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
