using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CallForwardingController : AuditableControllerBase
{
    public CallForwardingController(ServiceFactory factory) : base(factory) { }

    /// <summary>Tum yonlendirme kurallarini listele (admin/supervisor)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<ActionResult<List<CallForwardingRuleDto>>> GetAll(
        [FromQuery] int? customerId = null,
        [FromQuery] int? userId = null)
    {
        var svc = Factory.CreateCallForwardingService();
        return Ok(await svc.GetAllAsync(customerId, userId));
    }

    /// <summary>Kendi yonlendirme kurallarim (agent)</summary>
    [HttpGet("my")]
    public async Task<ActionResult<List<CallForwardingRuleDto>>> GetMyRules()
    {
        if (CurrentUserId == null) return Unauthorized();
        var svc = Factory.CreateCallForwardingService();
        return Ok(await svc.GetByUserIdAsync(CurrentUserId.Value));
    }

    /// <summary>Kural detay</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CallForwardingRuleDto>> GetById(int id)
    {
        var svc = Factory.CreateCallForwardingService();
        var result = await svc.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Kural bulunamadi." });
        return Ok(result);
    }

    /// <summary>Yeni yonlendirme kurali olustur</summary>
    [HttpPost]
    public async Task<ActionResult> Create(CallForwardingRuleCreateDto dto)
    {
        // Agent kendi kurali icerik kontrolu
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor"))
        {
            if (dto.UserId != CurrentUserId)
                return Forbid();
        }

        var svc = Factory.CreateCallForwardingService();
        var (success, id, error) = await svc.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "CallForwardingRule", id.ToString(),
            $"Yonlendirme kurali olusturuldu: UserId={dto.UserId}, Type={dto.ForwardType}, Dest={dto.Destination}",
            customerId: dto.CustomerId);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Kural guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, CallForwardingRuleUpdateDto dto)
    {
        var svc = Factory.CreateCallForwardingService();

        // Yetki kontrolu: agent kendi kuralini duzenleyebilir
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor"))
        {
            var existing = await svc.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Kural bulunamadi." });
            if (existing.UserId != CurrentUserId) return Forbid();
        }

        var (success, error) = await svc.UpdateAsync(id, dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "CallForwardingRule", id.ToString(),
            $"Yonlendirme kurali guncellendi");

        return NoContent();
    }

    /// <summary>Kural sil</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var svc = Factory.CreateCallForwardingService();

        // Yetki kontrolu
        if (!User.IsInRole("Admin") && !User.IsInRole("Supervisor"))
        {
            var existing = await svc.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Kural bulunamadi." });
            if (existing.UserId != CurrentUserId) return Forbid();
        }

        var (success, error) = await svc.DeleteAsync(id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Delete", "CallForwardingRule", id.ToString(),
            $"Yonlendirme kurali silindi");

        return NoContent();
    }
}
