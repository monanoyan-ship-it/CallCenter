using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public SettingsController(ServiceFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Grup bazli ayar listesi (grup bos ise tumu)</summary>
    [HttpGet]
    public async Task<ActionResult<List<SystemSettingDto>>> GetAll([FromQuery] string? group = null)
    {
        var svc = _factory.CreateSettingService();
        return Ok(await svc.GetAllAsync(group));
    }

    /// <summary>Ayar guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, SystemSettingUpdateDto dto)
    {
        var svc = _factory.CreateSettingService();
        var (success, error) = await svc.UpdateAsync(id, dto);
        if (!success) return NotFound(new { message = error });
        return NoContent();
    }

    /// <summary>Yeni ayar olustur</summary>
    [HttpPost]
    public async Task<ActionResult> Create(SystemSettingCreateDto dto)
    {
        var svc = _factory.CreateSettingService();
        var (success, id, error) = await svc.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { id });
    }

    /// <summary>Ayar sil (IsSystem ise engelle)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var svc = _factory.CreateSettingService();
        var (success, error) = await svc.DeleteAsync(id);
        if (!success)
        {
            if (error == "Ayar bulunamadi.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return NoContent();
    }
}
