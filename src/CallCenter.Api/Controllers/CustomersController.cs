using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class CustomersController : AuditableControllerBase
{
    private readonly ICustomerFactory _customerFactory;

    public CustomersController(IAuditFactory auditFactory, ICustomerFactory customerFactory) : base(auditFactory)
    {
        _customerFactory = customerFactory;
    }

    /// <summary>Sayfalamali musteri listesi</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        return Ok(await _customerFactory.GetAllAsync(page, pageSize, search));
    }

    /// <summary>Musteri detay (personel listesi dahil)</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(int id)
    {
        var result = await _customerFactory.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Musteri bulunamadi." });
        return Ok(result);
    }

    /// <summary>Yeni musteri olustur (admin kullanici bilgileri form'dan alinir)</summary>
    [HttpPost]
    public async Task<ActionResult> Create(CustomerCreateDto dto)
    {
        var (id, error) = await _customerFactory.CreateAsync(dto);

        if (id == 0)
            return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "Customer", id.ToString(),
            $"Musteri olusturuldu: '{dto.Name}' (admin: {dto.AdminUserName})");

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Musteri guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, CustomerUpdateDto dto)
    {
        var (success, error) = await _customerFactory.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "Customer", id.ToString(),
            $"Musteri guncellendi: ID={id}");

        return NoContent();
    }

    /// <summary>Musteri sil (soft delete)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var (success, error) = await _customerFactory.DeleteAsync(id);
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
        var (success, tempPassword, error) = await _customerFactory.ResetAdminPasswordAsync(id);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("PasswordReset", "Customer", id.ToString(),
            $"Musteri admin sifresi sifirlandi: ID={id}");

        return Ok(new { password = tempPassword });
    }
}
