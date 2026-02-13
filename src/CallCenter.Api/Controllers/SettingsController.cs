using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SettingsController : AuditableControllerBase
{
    public SettingsController(ServiceFactory factory) : base(factory) { }

    /// <summary>Grup bazli ayar listesi (grup bos ise tumu)</summary>
    [HttpGet]
    public async Task<ActionResult<List<SystemSettingDto>>> GetAll([FromQuery] string? group = null)
    {
        var svc = Factory.CreateSettingService();
        return Ok(await svc.GetAllAsync(group));
    }

    /// <summary>Ayar guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, SystemSettingUpdateDto dto)
    {
        var svc = Factory.CreateSettingService();
        var (success, error) = await svc.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "SystemSetting", id.ToString(),
            $"Sistem ayari guncellendi: ID={id}, Value='{dto.Value}'");

        return NoContent();
    }

    /// <summary>Yeni ayar olustur</summary>
    [HttpPost]
    public async Task<ActionResult> Create(SystemSettingCreateDto dto)
    {
        var svc = Factory.CreateSettingService();
        var (success, id, error) = await svc.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "SystemSetting", id.ToString(),
            $"Sistem ayari olusturuldu: '{dto.Key}'='{dto.Value}'");

        return Ok(new { id });
    }

    /// <summary>Ayar sil (IsSystem ise engelle)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var svc = Factory.CreateSettingService();
        var (success, error) = await svc.DeleteAsync(id);
        if (!success)
        {
            if (error == "Ayar bulunamadi.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }

        await AuditCrudAsync("Delete", "SystemSetting", id.ToString(),
            $"Sistem ayari silindi: ID={id}");

        return NoContent();
    }
}
