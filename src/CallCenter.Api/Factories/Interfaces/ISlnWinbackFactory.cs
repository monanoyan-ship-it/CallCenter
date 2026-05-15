using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnWinbackFactory
{
    Task<List<SlnWinbackRuleDto>> GetRulesAsync(int customerId, int? branchId = null);
    Task<SlnWinbackRuleDto?> GetRuleAsync(int id, int customerId, int? branchId = null);
    Task<SlnWinbackRuleDto> CreateRuleAsync(SlnWinbackRuleCreateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> UpdateRuleAsync(int id, SlnWinbackRuleUpdateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> DeleteRuleAsync(int id, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> ToggleRuleAsync(int id, int customerId, int? branchId = null);
    Task<SlnWinbackPreviewDto?> GetPreviewAsync(int id, int customerId, int? branchId = null);
    Task<(SlnCampaignDto? Campaign, string? Error)> CreateCampaignFromRuleAsync(int id, int customerId, int? branchId = null);
}
