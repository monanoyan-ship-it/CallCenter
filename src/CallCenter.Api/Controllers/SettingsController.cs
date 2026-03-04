using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SettingsController : AuditableControllerBase
{
    private readonly ISettingFactory _settingFactory;

    public SettingsController(IAuditFactory auditFactory, ISettingFactory settingFactory) : base(auditFactory)
    {
        _settingFactory = settingFactory;
    }

    /// <summary>Grup bazli ayar listesi (grup bos ise tumu)</summary>
    [HttpGet]
    public async Task<ActionResult<List<SystemSettingDto>>> GetAll([FromQuery] string? group = null)
    {
        return Ok(await _settingFactory.GetAllAsync(group));
    }

    /// <summary>Ayar guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, SystemSettingUpdateDto dto)
    {
        var (success, error) = await _settingFactory.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("Update", "SystemSetting", id.ToString(),
            $"Sistem ayari guncellendi: ID={id}, Value='{dto.Value}'");

        return NoContent();
    }

    /// <summary>Yeni ayar olustur</summary>
    [HttpPost]
    public async Task<ActionResult> Create(SystemSettingCreateDto dto)
    {
        var (success, id, error) = await _settingFactory.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "SystemSetting", id.ToString(),
            $"Sistem ayari olusturuldu: '{dto.Key}'='{dto.Value}'");

        return Ok(new { id });
    }

    /// <summary>Ayar sil (IsSystem ise engelle)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var (success, error) = await _settingFactory.DeleteAsync(id);
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
