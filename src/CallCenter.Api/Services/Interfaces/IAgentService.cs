using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface IAgentService
{
    Task<object> GetAllAsync();
    Task<(bool Success, string? Error)> UpdateStatusAsync(int userId, int newStatusId);
    Task<List<MyQueueDto>> GetMyQueuesAsync(int userId, string role, int? customerId);
    Task<object?> GetCurrentAgentAsync(int userId);
}
