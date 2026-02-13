using System.Security.Claims;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SipAccountsController : AuditableControllerBase
{
    public SipAccountsController(ServiceFactory factory) : base(factory) { }

    /// <summary>
    /// Agent'in musterisinin default SIP hesabinin baglanti bilgilerini dondurur.
    /// WebSocket URI otomatik olusturulur: wss://server:port/ws
    /// Tum authenticated kullanicilar erisebilir.
    /// </summary>
    [HttpGet("my/connection")]
    public async Task<ActionResult<SipConnectionInfoDto>> GetMyConnection()
    {
        var customerIdClaim = User.FindFirstValue("CustomerId");
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
        {
            return BadRequest(new { message = "Musteri bilgisi bulunamadi. Lutfen tekrar giris yapin." });
        }

        var displayName = User.FindFirstValue(ClaimTypes.GivenName) ?? "User";
        var svc = Factory.CreateSipAccountService();
        var result = await svc.GetMyConnectionAsync(customerId, displayName);

        if (result == null)
            return NotFound(new { message = "Firmaniza ait aktif SIP hesabi bulunamadi." });

        return Ok(result);
    }

    /// <summary>Sayfalamali SIP hesap listesi (Password yok)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<SipAccountListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? customerId = null)
    {
        var svc = Factory.CreateSipAccountService();
        return Ok(await svc.GetAllAsync(page, pageSize, customerId));
    }

    /// <summary>SIP hesap detay (Password maskelenmis)</summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetById(int id)
    {
        var svc = Factory.CreateSipAccountService();
        var result = await svc.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "SIP hesabi bulunamadi." });
        return Ok(result);
    }

    /// <summary>Yeni SIP hesap olustur</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create(SipAccountCreateDto dto)
    {
        var svc = Factory.CreateSipAccountService();
        var (success, id, error) = await svc.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "SipAccount", id.ToString(),
            $"SIP hesabi olusturuldu: '{dto.Name}' (server: {dto.Server})", customerId: dto.CustomerId);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>SIP hesap guncelle (Password null ise degismez)</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(int id, SipAccountUpdateDto dto)
    {
        var svc = Factory.CreateSipAccountService();
        var (success, error) = await svc.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "SipAccount", id.ToString(),
            $"SIP hesabi guncellendi: ID={id}");

        return NoContent();
    }

    /// <summary>SIP hesap sil (soft delete)</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var svc = Factory.CreateSipAccountService();
        var (success, error) = await svc.DeleteAsync(id);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "SipAccount", id.ToString(),
            $"SIP hesabi silindi: ID={id}");

        return NoContent();
    }
}
