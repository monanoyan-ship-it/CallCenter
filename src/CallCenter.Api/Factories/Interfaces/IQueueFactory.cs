using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IQueueFactory
{
    Task<PagedResult<QueueListDto>> GetAllAsync(int page, int pageSize, int? customerId, string? search);
    Task<QueueDetailDto?> GetByIdAsync(int id);
    Task<(bool Success, int? Id, string? Error)> CreateAsync(QueueCreateDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(int id, QueueUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<(bool Success, string? Error)> AssignAgentAsync(int queueId, QueueAgentAssignDto dto);
    Task<(bool Success, string? Error)> RemoveAgentAsync(int queueId, int agentId);
}
