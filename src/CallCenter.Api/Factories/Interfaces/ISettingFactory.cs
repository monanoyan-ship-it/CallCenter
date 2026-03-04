using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISettingFactory
{
    Task<List<SystemSettingDto>> GetAllAsync(string? group);
    Task<(bool Success, string? Error)> UpdateAsync(int id, SystemSettingUpdateDto dto);
    Task<(bool Success, int? Id, string? Error)> CreateAsync(SystemSettingCreateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
}
