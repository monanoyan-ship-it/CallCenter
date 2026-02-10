using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class SettingService : ISettingService
{
    private readonly AppDbContext _db;

    public SettingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SystemSettingDto>> GetAllAsync(string? group)
    {
        var query = _db.SystemSettings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(group))
        {
            query = query.Where(s => s.Group == group);
        }

        return await query
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
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, SystemSettingUpdateDto dto)
    {
        var setting = await _db.SystemSettings.FindAsync(id);
        if (setting == null) return (false, "Ayar bulunamadi.");

        setting.Value = dto.Value;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateAsync(SystemSettingCreateDto dto)
    {
        if (await _db.SystemSettings.AnyAsync(s => s.Key == dto.Key))
            return (false, null, "Bu key zaten mevcut.");

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

        return (true, setting.Id, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var setting = await _db.SystemSettings.FindAsync(id);
        if (setting == null) return (false, "Ayar bulunamadi.");

        if (setting.IsSystem)
            return (false, "Sistem ayarlari silinemez.");

        _db.SystemSettings.Remove(setting);
        await _db.SaveChangesAsync();

        return (true, null);
    }
}
