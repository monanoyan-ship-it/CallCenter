using System.Security.Claims;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SipAccountsController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public SipAccountsController(ServiceFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Agent'ın müşterisinin default SIP hesabının bağlantı bilgilerini döndürür.
    /// WebSocket URI otomatik oluşturulur: wss://server:port/ws
    /// Tüm authenticated kullanıcılar erişebilir.
    /// </summary>
    [HttpGet("my/connection")]
    public async Task<ActionResult<SipConnectionInfoDto>> GetMyConnection()
    {
        var customerIdClaim = User.FindFirstValue("CustomerId");
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
        {
            return BadRequest(new { message = "Müşteri bilgisi bulunamadı. Lütfen tekrar giriş yapın." });
        }

        var displayName = User.FindFirstValue(ClaimTypes.GivenName) ?? "User";
        var svc = _factory.CreateSipAccountService();
        var result = await svc.GetMyConnectionAsync(customerId, displayName);

        if (result == null)
            return NotFound(new { message = "Firmanıza ait aktif SIP hesabı bulunamadı." });

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
        var svc = _factory.CreateSipAccountService();
        return Ok(await svc.GetAllAsync(page, pageSize, customerId));
    }

    /// <summary>SIP hesap detay (Password maskelenmis)</summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetById(int id)
    {
        var svc = _factory.CreateSipAccountService();
        var result = await svc.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "SIP hesabi bulunamadi." });
        return Ok(result);
    }

    /// <summary>Yeni SIP hesap olustur</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create(SipAccountCreateDto dto)
    {
        var svc = _factory.CreateSipAccountService();
        var (success, id, error) = await svc.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>SIP hesap guncelle (Password null ise degismez)</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(int id, SipAccountUpdateDto dto)
    {
        var svc = _factory.CreateSipAccountService();
        var (success, error) = await svc.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });
        return NoContent();
    }

    /// <summary>SIP hesap sil (soft delete)</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id)
    {
        var svc = _factory.CreateSipAccountService();
        var (success, error) = await svc.DeleteAsync(id);
        if (!success) return NotFound(new { message = error });
        return NoContent();
    }
}
