using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface ICallForwardingService
{
    Task<List<CallForwardingRuleDto>> GetByUserIdAsync(int userId);
    Task<List<CallForwardingRuleDto>> GetAllAsync(int? customerId, int? userId);
    Task<CallForwardingRuleDto?> GetByIdAsync(int id);
    Task<(bool Success, int? Id, string? Error)> CreateAsync(CallForwardingRuleCreateDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(int id, CallForwardingRuleUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);

    /// <summary>
    /// Arama dagitimi sirasinda yonlendirme kurali kontrol et.
    /// Uygun kural varsa hedef numarayi dondurur.
    /// </summary>
    Task<string?> GetForwardDestinationAsync(int userId, int forwardType);
}
