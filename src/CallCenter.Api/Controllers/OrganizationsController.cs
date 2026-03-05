using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class OrganizationsController : AuditableControllerBase
{
    private readonly IOrganizationFactory _orgFactory;

    public OrganizationsController(IAuditFactory auditFactory, IOrganizationFactory orgFactory) : base(auditFactory)
    {
        _orgFactory = orgFactory;
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree([FromQuery] int customerId)
    {
        var tree = await _orgFactory.GetTreeAsync(customerId);
        return Ok(tree);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int customerId)
    {
        var list = await _orgFactory.GetListAsync(customerId);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] int customerId)
    {
        var detail = await _orgFactory.GetByIdAsync(customerId, id);
        if (detail == null) return NotFound();
        return Ok(detail);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] int customerId, [FromBody] OrgUnitCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, result) = await _orgFactory.CreateAsync(customerId, dto);
        if (!success) return BadRequest(new { error = result });

        await AuditCrudAsync("Create", "OrganizationUnit", result?.ToString(),
            $"Organizasyon birimi olusturuldu: '{dto.Name}'", customerId: customerId);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromQuery] int customerId, [FromBody] OrgUnitUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (success, error) = await _orgFactory.UpdateAsync(customerId, id, dto);
        if (!success) return BadRequest(new { error });

        await AuditCrudAsync("Update", "OrganizationUnit", id.ToString(),
            $"Organizasyon birimi guncellendi: ID={id}", customerId: customerId);

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int customerId)
    {
        var (success, error) = await _orgFactory.DeleteAsync(customerId, id);
        if (!success) return BadRequest(new { error });

        await AuditCrudAsync("Delete", "OrganizationUnit", id.ToString(),
            $"Organizasyon birimi silindi: ID={id}", customerId: customerId);

        return Ok();
    }

    [HttpGet("parents")]
    public async Task<IActionResult> GetPotentialParents([FromQuery] int customerId, [FromQuery] int? excludeId)
    {
        var list = await _orgFactory.GetPotentialParentsAsync(customerId, excludeId);
        return Ok(list);
    }

    // ─── PERSONEL ATAMA ───

    [HttpGet("{id}/available-personnel")]
    public async Task<IActionResult> GetAvailablePersonnel(int id, [FromQuery] int customerId)
    {
        var list = await _orgFactory.GetAvailablePersonnelAsync(customerId, id);
        return Ok(list);
    }

    [HttpPost("{id}/personnel")]
    public async Task<IActionResult> AssignPersonnel(int id, [FromQuery] int customerId, [FromBody] OrgPersonnelAssignDto dto)
    {
        var (success, error) = await _orgFactory.AssignPersonnelAsync(customerId, id, dto.PersonnelId);
        if (!success) return BadRequest(new { error });

        await AuditCrudAsync("AssignPersonnel", "OrganizationUnit", id.ToString(),
            $"Personel atandi: OrgID={id}, PersonelID={dto.PersonnelId}", customerId: customerId);

        return Ok();
    }

    [HttpDelete("{id}/personnel/{personnelId}")]
    public async Task<IActionResult> RemovePersonnel(int id, int personnelId, [FromQuery] int customerId)
    {
        var (success, error) = await _orgFactory.RemovePersonnelAsync(customerId, id, personnelId);
        if (!success) return BadRequest(new { error });

        await AuditCrudAsync("RemovePersonnel", "OrganizationUnit", id.ToString(),
            $"Personel cikarildi: OrgID={id}, PersonelID={personnelId}", customerId: customerId);

        return Ok();
    }
}
