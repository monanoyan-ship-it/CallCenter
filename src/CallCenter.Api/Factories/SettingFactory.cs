using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SettingFactory : ISettingFactory
{
    private readonly ISettingEntityService _settings;
    private readonly IUnitOfWork _uow;

    public SettingFactory(ISettingEntityService settings, IUnitOfWork uow)
    {
        _settings = settings;
        _uow = uow;
    }

    public async Task<List<SystemSettingDto>> GetAllAsync(string? group)
    {
        var query = _settings.GetAllQueryable();

        if (!string.IsNullOrWhiteSpace(group))
            query = query.Where(s => s.Group == group);

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
        var setting = await _settings.GetByIdAsync(id);
        if (setting == null) return (false, "Ayar bulunamadi.");

        setting.Value = dto.Value;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateAsync(SystemSettingCreateDto dto)
    {
        if (await _settings.ExistsByKeyAsync(dto.Key))
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

        _settings.Add(setting);
        await _uow.SaveChangesAsync();

        return (true, setting.Id, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var setting = await _settings.GetByIdAsync(id);
        if (setting == null) return (false, "Ayar bulunamadi.");

        if (setting.IsSystem)
            return (false, "Sistem ayarlari silinemez.");

        _settings.Remove(setting);
        await _uow.SaveChangesAsync();

        return (true, null);
    }
}
