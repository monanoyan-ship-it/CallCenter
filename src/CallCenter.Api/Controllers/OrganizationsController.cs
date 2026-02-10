using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class OrganizationsController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public OrganizationsController(ServiceFactory factory) => _factory = factory;

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree([FromQuery] int customerId)
    {
        var svc = _factory.CreateOrganizationService();
        var tree = await svc.GetTreeAsync(customerId);
        return Ok(tree);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int customerId)
    {
        var svc = _factory.CreateOrganizationService();
        var list = await svc.GetListAsync(customerId);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int customerId)
    {
        var svc = _factory.CreateOrganizationService();
        var detail = await svc.GetByIdAsync(customerId, id);
        if (detail == null) return NotFound();
        return Ok(detail);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] int customerId, [FromBody] OrgUnitCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var svc = _factory.CreateOrganizationService();
        var (success, result) = await svc.CreateAsync(customerId, dto);
        if (!success) return BadRequest(new { error = result });
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int customerId, [FromBody] OrgUnitUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var svc = _factory.CreateOrganizationService();
        var (success, error) = await svc.UpdateAsync(customerId, id, dto);
        if (!success) return BadRequest(new { error });
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int customerId)
    {
        var svc = _factory.CreateOrganizationService();
        var (success, error) = await svc.DeleteAsync(customerId, id);
        if (!success) return BadRequest(new { error });
        return Ok();
    }

    [HttpGet("parents")]
    public async Task<IActionResult> GetPotentialParents([FromQuery] int customerId, [FromQuery] int? excludeId)
    {
        var svc = _factory.CreateOrganizationService();
        var list = await svc.GetPotentialParentsAsync(customerId, excludeId);
        return Ok(list);
    }
}
