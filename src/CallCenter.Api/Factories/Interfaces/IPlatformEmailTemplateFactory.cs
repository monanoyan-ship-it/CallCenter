using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IPlatformEmailTemplateFactory
{
    Task<List<PlatformEmailTemplateDto>> GetAllAsync();
    Task<PlatformEmailTemplateDto?> GetByIdAsync(int id);
    Task<PlatformEmailTemplateDto> CreateAsync(PlatformEmailTemplateCreateDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(int id, PlatformEmailTemplateUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
}
