using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class CustomersController : AuditableControllerBase
{
    public CustomersController(ServiceFactory factory) : base(factory) { }

    /// <summary>Sayfalamali musteri listesi</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var svc = Factory.CreateCustomerService();
        return Ok(await svc.GetAllAsync(page, pageSize, search));
    }

    /// <summary>Musteri detay (personel listesi dahil)</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(int id)
    {
        var svc = Factory.CreateCustomerService();
        var result = await svc.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Musteri bulunamadi." });
        return Ok(result);
    }

    /// <summary>Yeni musteri olustur (varsayilan portal modullerini otomatik ata)</summary>
    [HttpPost]
    public async Task<ActionResult> Create(CustomerCreateDto dto)
    {
        var svc = Factory.CreateCustomerService();
        var id = await svc.CreateAsync(dto);

        await AuditCrudAsync("Create", "Customer", id.ToString(),
            $"Musteri olusturuldu: '{dto.Name}'");

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Musteri guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, CustomerUpdateDto dto)
    {
        var svc = Factory.CreateCustomerService();
        var (success, error) = await svc.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "Customer", id.ToString(),
            $"Musteri guncellendi: ID={id}");

        return NoContent();
    }

    /// <summary>Musteri sil (soft delete)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var svc = Factory.CreateCustomerService();
        var (success, error) = await svc.DeleteAsync(id);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Delete", "Customer", id.ToString(),
            $"Musteri silindi (deaktif): ID={id}");

        return NoContent();
    }

    /// <summary>Musteri admin sifresini sifirla — yeni gecici sifre uretir</summary>
    [HttpPost("{id}/reset-admin-password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ResetAdminPassword(int id)
    {
        var svc = Factory.CreateCustomerService();
        var (success, tempPassword, error) = await svc.ResetAdminPasswordAsync(id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("PasswordReset", "Customer", id.ToString(),
            $"Musteri admin sifresi sifirlandi: ID={id}");

        return Ok(new { password = tempPassword });
    }
}
