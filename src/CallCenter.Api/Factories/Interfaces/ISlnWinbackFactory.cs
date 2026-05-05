using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnWinbackFactory
{
    Task<List<SlnWinbackRuleDto>> GetRulesAsync(int customerId);
    Task<SlnWinbackRuleDto?> GetRuleAsync(int id, int customerId);
    Task<SlnWinbackRuleDto> CreateRuleAsync(SlnWinbackRuleCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateRuleAsync(int id, SlnWinbackRuleUpdateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteRuleAsync(int id, int customerId);
    Task<(bool Success, string? Error)> ToggleRuleAsync(int id, int customerId);
    Task<SlnWinbackPreviewDto?> GetPreviewAsync(int id, int customerId);
    Task<(SlnCampaignDto? Campaign, string? Error)> CreateCampaignFromRuleAsync(int id, int customerId);
}
