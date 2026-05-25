using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IQueueFactory
{
    Task<PagedResult<QueueListDto>> GetAllAsync(int page, int pageSize, int? customerId, string? search);
    Task<QueueDetailDto?> GetByIdAsync(int id, int? customerId = null);
    Task<(bool Success, int? Id, string? Error)> CreateAsync(QueueCreateDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(int id, QueueUpdateDto dto, int? customerId = null);
    Task<(bool Success, string? Error)> DeleteAsync(int id, int? customerId = null);
    Task<(bool Success, string? Error)> AssignAgentAsync(int queueId, QueueAgentAssignDto dto, int? customerId = null);
    Task<(bool Success, string? Error)> RemoveAgentAsync(int queueId, int agentId, int? customerId = null);
}
