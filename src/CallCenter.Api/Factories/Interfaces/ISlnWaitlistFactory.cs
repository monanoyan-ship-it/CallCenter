using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnWaitlistFactory
{
    Task<List<SlnWaitlistEntryDto>> GetEntriesAsync(int customerId, DateTime? date = null, int? branchId = null);
    Task<SlnWaitlistEntryDto?> GetEntryAsync(int id, int customerId);
    Task<SlnWaitlistEntryDto> CreateEntryAsync(SlnWaitlistEntryCreateDto dto, int customerId, int? branchScopeId = null);
    Task<(bool Success, string? Error)> UpdateEntryAsync(int id, SlnWaitlistEntryUpdateDto dto, int customerId, int? branchScopeId = null);
    Task<(bool Success, string? Error)> UpdateStatusAsync(int id, int statusId, int customerId);
    Task<(bool Success, string? Error)> DeleteEntryAsync(int id, int customerId);
    Task<object> NormalizeBranchesAsync(int customerId);
}
