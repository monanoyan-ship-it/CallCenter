using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnEmailCampaignFactory
{
    Task<List<SlnEmailCampaignDto>> GetCampaignsAsync(int customerId, int? branchId = null);
    Task<SlnEmailCampaignDto?> GetCampaignAsync(int id, int customerId, int? branchId = null);
    Task<SlnEmailCampaignDto> CreateCampaignAsync(SlnEmailCampaignCreateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> UpdateCampaignAsync(int id, SlnEmailCampaignUpdateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> DeleteCampaignAsync(int id, int customerId, int? branchId = null);
    Task<SlnSegmentPreviewDto> GetSegmentPreviewAsync(string? segmentFilter, int customerId, int? branchId = null);
    Task<List<SlnSegmentPresetDto>> GetSegmentPresetsAsync(int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> SendCampaignAsync(int id, int customerId, int? branchId = null);
}
