using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface ISettingService
{
    Task<List<SystemSettingDto>> GetAllAsync(string? group);
    Task<(bool Success, string? Error)> UpdateAsync(int id, SystemSettingUpdateDto dto);
    Task<(bool Success, int? Id, string? Error)> CreateAsync(SystemSettingCreateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
}
