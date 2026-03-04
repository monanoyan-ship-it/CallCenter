using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CallForwardingController : AuditableControllerBase
{
    private readonly ICallForwardingFactory _callForwardingFactory;

    public CallForwardingController(IAuditFactory auditFactory, ICallForwardingFactory callForwardingFactory) : base(auditFactory)
    {
        _callForwardingFactory = callForwardingFactory;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<ActionResult<List<CallForwardingRuleDto>>> GetAll(
        [FromQuery] int? customerId = null,
        [FromQuery] int? userId = null)
    {
        return Ok(await _callForwardingFactory.GetAllAsync(customerId, userId));
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<CallForwardingRuleDto>>> GetMyRules()
    {
        if (CurrentUserId == null) return Unauthorized();
        return Ok(await _callForwardingFactory.GetByUserIdAsync(CurrentUserId.Value));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CallForwardingRuleDto>> GetById(int id)
    {
        var result = await _callForwardingFactory.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Kural bulunamadi." });
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> Create(CallForwardingRuleCreateDto dto)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor"))
        {
            if (dto.UserId != CurrentUserId)
                return Forbid();
        }

        var (success, id, error) = await _callForwardingFactory.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "CallForwardingRule", id.ToString(),
            $"Yonlendirme kurali olusturuldu: UserId={dto.UserId}, Type={dto.ForwardType}, Dest={dto.Destination}",
            customerId: dto.CustomerId);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, CallForwardingRuleUpdateDto dto)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor"))
        {
            var existing = await _callForwardingFactory.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Kural bulunamadi." });
            if (existing.UserId != CurrentUserId) return Forbid();
        }

        var (success, error) = await _callForwardingFactory.UpdateAsync(id, dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "CallForwardingRule", id.ToString(),
            $"Yonlendirme kurali guncellendi");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor"))
        {
            var existing = await _callForwardingFactory.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Kural bulunamadi." });
            if (existing.UserId != CurrentUserId) return Forbid();
        }

        var (success, error) = await _callForwardingFactory.DeleteAsync(id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Delete", "CallForwardingRule", id.ToString(),
            $"Yonlendirme kurali silindi");

        return NoContent();
    }
}
