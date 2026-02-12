using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class CustomersController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public CustomersController(ServiceFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Sayfalamali musteri listesi</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var svc = _factory.CreateCustomerService();
        return Ok(await svc.GetAllAsync(page, pageSize, search));
    }

    /// <summary>Musteri detay (personel listesi dahil)</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(int id)
    {
        var svc = _factory.CreateCustomerService();
        var result = await svc.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Musteri bulunamadi." });
        return Ok(result);
    }

    /// <summary>Yeni musteri olustur (varsayilan portal modullerini otomatik ata)</summary>
    [HttpPost]
    public async Task<ActionResult> Create(CustomerCreateDto dto)
    {
        var svc = _factory.CreateCustomerService();
        var id = await svc.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Musteri guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, CustomerUpdateDto dto)
    {
        var svc = _factory.CreateCustomerService();
        var (success, error) = await svc.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });
        return NoContent();
    }

    /// <summary>Musteri sil (soft delete)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var svc = _factory.CreateCustomerService();
        var (success, error) = await svc.DeleteAsync(id);
        if (!success) return NotFound(new { message = error });
        return NoContent();
    }

    /// <summary>Musteri admin sifresini sifirla — yeni gecici sifre uretir</summary>
    [HttpPost("{id}/reset-admin-password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ResetAdminPassword(int id)
    {
        var svc = _factory.CreateCustomerService();
        var (success, tempPassword, error) = await svc.ResetAdminPasswordAsync(id);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { password = tempPassword });
    }
}
