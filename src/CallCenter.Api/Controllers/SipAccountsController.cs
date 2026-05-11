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

    // ═══════════════════════════════════════════════════════════════
    // OPERATOR
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("my/connection")]
    public async Task<ActionResult<SipConnectionInfoDto>> GetMyConnection(
        [FromQuery] int? gatewayId = null,
        [FromQuery] int? excludeLineId = null,
        [FromQuery] bool forceNewLine = false)
    {
        var customerIdClaim = User.FindFirstValue("CustomerId");
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
        {
            return BadRequest(new { message = "Musteri bilgisi bulunamadi. Lutfen tekrar giris yapin." });
        }

        int? personnelId = null;
        var personnelIdClaim = User.FindFirstValue("CustomerPersonnelId");
        if (!string.IsNullOrEmpty(personnelIdClaim) && int.TryParse(personnelIdClaim, out var pid))
            personnelId = pid;

        var displayName = User.FindFirstValue(ClaimTypes.GivenName) ?? "User";
        var result = await _sipFactory.GetMyConnectionAsync(
            customerId,
            personnelId,
            displayName,
            gatewayId,
            excludeLineId,
            forceNewLine);

        if (result == null)
            return NotFound(new { message = "Firmaniza ait bos hat bulunamadi. Tum hatlar dolu, secilen kanal mesgul veya gateway tanimli degil." });

        return Ok(result);
    }

    [HttpGet("my/gateways")]
    public async Task<ActionResult<List<SipGatewaySummaryDto>>> GetMyGateways()
    {
        var customerIdClaim = User.FindFirstValue("CustomerId");
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
            return BadRequest(new { message = "Musteri bilgisi bulunamadi." });

        return Ok(await _sipFactory.GetMyGatewaysAsync(customerId));
    }

    [HttpPost("my/release")]
    public async Task<ActionResult> ReleaseMyLine()
    {
        var personnelIdClaim = User.FindFirstValue("CustomerPersonnelId");
        if (string.IsNullOrEmpty(personnelIdClaim) || !int.TryParse(personnelIdClaim, out var personnelId))
            return BadRequest(new { message = "Personel bilgisi bulunamadi." });

        await _sipFactory.ReleaseLineAsync(personnelId);
        return Ok();
    }

    // ═══════════════════════════════════════════════════════════════
    // ADMIN: GATEWAY
    // ═══════════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize(Roles = "Admin,CustomerUser")]
    public async Task<ActionResult<PagedResult<SipAccountListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? customerId = null)
    {
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
        if (result == null) return NotFound(new { message = "Gateway bulunamadi." });

        if (User.IsInRole("CustomerUser"))
        {
            var cid = User.FindFirstValue("CustomerId");
            if (string.IsNullOrEmpty(cid) || !int.TryParse(cid, out var customerIdFromClaim))
                return Forbid();
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
            $"Gateway olusturuldu: '{dto.Name}' (server: {dto.Server})", customerId: dto.CustomerId);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(int id, SipAccountUpdateDto dto)
    {
        var (success, error) = await _sipFactory.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "SipAccount", id.ToString(),
            $"Gateway guncellendi: ID={id}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var (success, error) = await _sipFactory.DeleteAsync(id);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "SipAccount", id.ToString(),
            $"Gateway silindi: ID={id}");

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════
    // ADMIN: LINE
    // ═══════════════════════════════════════════════════════════════

    [HttpGet("{gatewayId}/lines")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<SipLineListDto>>> GetLines(int gatewayId)
    {
        return Ok(await _sipFactory.GetLinesByGatewayAsync(gatewayId));
    }

    [HttpPost("{gatewayId}/lines")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreateLine(int gatewayId, SipLineCreateDto dto)
    {
        var (success, id, error) = await _sipFactory.CreateLineAsync(gatewayId, dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "SipLine", id.ToString(),
            $"Hat olusturuldu: CH{dto.ChannelNumber} '{dto.Username}' (gateway: {gatewayId})");

        return Ok(new { id });
    }

    [HttpPut("lines/{lineId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateLine(int lineId, SipLineUpdateDto dto)
    {
        var (success, error) = await _sipFactory.UpdateLineAsync(lineId, dto);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "SipLine", lineId.ToString(),
            $"Hat guncellendi: ID={lineId}");

        return NoContent();
    }

    [HttpDelete("lines/{lineId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteLine(int lineId)
    {
        var (success, error) = await _sipFactory.DeleteLineAsync(lineId);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "SipLine", lineId.ToString(),
            $"Hat silindi: ID={lineId}");

        return NoContent();
    }
}
