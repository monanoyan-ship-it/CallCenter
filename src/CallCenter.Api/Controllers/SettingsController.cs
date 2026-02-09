using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Grup bazli ayar listesi (grup bos ise tumu)</summary>
    [HttpGet]
    public async Task<ActionResult<List<SystemSettingDto>>> GetAll([FromQuery] string? group = null)
    {
        var query = _db.SystemSettings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(group))
        {
            query = query.Where(s => s.Group == group);
        }

        var items = await query
            .OrderBy(s => s.Group).ThenBy(s => s.Key)
            .Select(s => new SystemSettingDto
            {
                Id = s.Id,
                Key = s.Key,
                Value = s.Value,
                Group = s.Group,
                ValueType = s.ValueType,
                Description = s.Description,
                IsSystem = s.IsSystem
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Ayar guncelle</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, SystemSettingUpdateDto dto)
    {
        var setting = await _db.SystemSettings.FindAsync(id);
        if (setting == null) return NotFound(new { message = "Ayar bulunamadi." });

        setting.Value = dto.Value;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Yeni ayar olustur</summary>
    [HttpPost]
    public async Task<ActionResult> Create(SystemSettingCreateDto dto)
    {
        if (await _db.SystemSettings.AnyAsync(s => s.Key == dto.Key))
            return BadRequest(new { message = "Bu key zaten mevcut." });

        var setting = new SystemSetting
        {
            Key = dto.Key,
            Value = dto.Value,
            Group = dto.Group,
            ValueType = dto.ValueType,
            Description = dto.Description,
            IsSystem = false
        };

        _db.SystemSettings.Add(setting);
        await _db.SaveChangesAsync();

        return Ok(new { id = setting.Id });
    }

    /// <summary>Ayar sil (IsSystem ise engelle)</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var setting = await _db.SystemSettings.FindAsync(id);
        if (setting == null) return NotFound(new { message = "Ayar bulunamadi." });

        if (setting.IsSystem)
            return BadRequest(new { message = "Sistem ayarlari silinemez." });

        _db.SystemSettings.Remove(setting);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
