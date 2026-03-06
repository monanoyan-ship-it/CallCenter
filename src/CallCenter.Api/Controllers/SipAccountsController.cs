using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SipAccountsController : AuditableControllerBase
{
    private readonly ISipAccountFactory _sipFactory;

    public SipAccountsController(IAuditFactory auditFactory, ISipAccountFactory sipFactory) : base(auditFactory)
    {
        _sipFactory = sipFactory;
    }

    [HttpGet("my/connection")]
    public async Task<ActionResult<SipConnectionInfoDto>> GetMyConnection()
    {
        var customerIdClaim = User.FindFirstValue("CustomerId");
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
        {
            return BadRequest(new { message = "Musteri bilgisi bulunamadi. Lutfen tekrar giris yapin." });
        }

        var displayName = User.FindFirstValue(ClaimTypes.GivenName) ?? "User";
        var result = await _sipFactory.GetMyConnectionAsync(customerId, displayName);

        if (result == null)
            return NotFound(new { message = "Firmaniza ait aktif SIP hesabi bulunamadi." });

        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,CustomerUser")]
    public async Task<ActionResult<PagedResult<SipAccountListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? customerId = null)
    {
        // CustomerUser sadece kendi firmasinin SIP hesaplarini gorebilir
        if (User.IsInRole("CustomerUser"))
        {
            var cid = User.FindFirstValue("CustomerId");
            if (string.IsNullOrEmpty(cid) || !int.TryParse(cid, out var customerIdFromClaim))
                return Forbid();

            var isAdmin = User.FindFirstValue("IsCustomerAdmin");
            if (!string.Equals(isAdmin, "true", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            customerId = customerIdFromClaim;
        }

        return Ok(await _sipFactory.GetAllAsync(page, pageSize, customerId));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,CustomerUser")]
    public async Task<ActionResult> GetById(int id)
    {
        var result = await _sipFactory.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "SIP hesabi bulunamadi." });

        // CustomerUser firma kontrolu
        if (User.IsInRole("CustomerUser"))
        {
            var cid = User.FindFirstValue("CustomerId");
            if (string.IsNullOrEmpty(cid) || !int.TryParse(cid, out var customerIdFromClaim))
                return Forbid();
            // Anonymous type - dynamic ile CustomerId kontrolu
            var customerId = (int)((dynamic)result).CustomerId;
            if (customerId != customerIdFromClaim)
                return Forbid();
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create(SipAccountCreateDto dto)
    {
        var (success, id, error) = await _sipFactory.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "SipAccount", id.ToString(),
            $"SIP hesabi olusturuldu: '{dto.Name}' (server: {dto.Server})", customerId: dto.CustomerId);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(int id, SipAccountUpdateDto dto)
    {
        var (success, error) = await _sipFactory.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "SipAccount", id.ToString(),
            $"SIP hesabi guncellendi: ID={id}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var (success, error) = await _sipFactory.DeleteAsync(id);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "SipAccount", id.ToString(),
            $"SIP hesabi silindi: ID={id}");

        return NoContent();
    }
}
