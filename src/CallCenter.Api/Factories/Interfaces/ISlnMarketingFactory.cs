using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnMarketingFactory
{
    // Kampanya
    Task<List<SlnCampaignDto>> GetCampaignsAsync(int customerId);
    Task<SlnCampaignDto?> GetCampaignAsync(int campaignId, int customerId);
    Task<SlnCampaignDto> CreateCampaignAsync(SlnCampaignCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateCampaignAsync(int campaignId, SlnCampaignUpdateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteCampaignAsync(int campaignId, int customerId);
    Task<SlnSegmentPreviewDto> GetSegmentPreviewAsync(string? segmentFilter, int customerId);
    Task<(bool Success, string? Error)> SendCampaignAsync(int campaignId, int customerId);

    // Oto-Hatirlatma
    Task<List<SlnAutoReminderDto>> GetRemindersAsync(int customerId);
    Task<SlnAutoReminderDto> CreateReminderAsync(SlnAutoReminderCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateReminderAsync(int reminderId, SlnAutoReminderUpdateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteReminderAsync(int reminderId, int customerId);
    Task<(bool Success, string? Error)> ToggleReminderAsync(int reminderId, int customerId);
}
