using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISipAccountFactory
{
    Task<SipConnectionInfoDto?> GetMyConnectionAsync(int customerId, int? personnelId, string displayName);
    Task<PagedResult<SipAccountListDto>> GetAllAsync(int page, int pageSize, int? customerId);
    Task<object?> GetByIdAsync(int id);
    Task<(bool Success, int? Id, string? Error)> CreateAsync(SipAccountCreateDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(int id, SipAccountUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
}
