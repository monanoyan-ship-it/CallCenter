using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ICallForwardingFactory
{
    Task<List<CallForwardingRuleDto>> GetByUserIdAsync(int userId);
    Task<List<CallForwardingRuleDto>> GetAllAsync(int? customerId, int? userId);
    Task<CallForwardingRuleDto?> GetByIdAsync(int id, int? customerId = null);
    Task<(bool Success, int? Id, string? Error)> CreateAsync(CallForwardingRuleCreateDto dto, int? customerId = null);
    Task<(bool Success, string? Error)> UpdateAsync(int id, CallForwardingRuleUpdateDto dto, int? customerId = null);
    Task<(bool Success, string? Error)> DeleteAsync(int id, int? customerId = null);
    Task<string?> GetForwardDestinationAsync(int userId, int forwardType);
}
