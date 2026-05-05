using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnEmailCampaignFactory
{
    Task<List<SlnEmailCampaignDto>> GetCampaignsAsync(int customerId);
    Task<SlnEmailCampaignDto?> GetCampaignAsync(int id, int customerId);
    Task<SlnEmailCampaignDto> CreateCampaignAsync(SlnEmailCampaignCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateCampaignAsync(int id, SlnEmailCampaignUpdateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteCampaignAsync(int id, int customerId);
    Task<SlnSegmentPreviewDto> GetSegmentPreviewAsync(string? segmentFilter, int customerId);
    Task<List<SlnSegmentPresetDto>> GetSegmentPresetsAsync(int customerId);
    Task<(bool Success, string? Error)> SendCampaignAsync(int id, int customerId);
}
