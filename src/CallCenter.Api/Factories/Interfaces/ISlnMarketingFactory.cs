using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnMarketingFactory
{
    // Kampanya
    Task<List<SlnCampaignDto>> GetCampaignsAsync(int customerId, int? branchId = null);
    Task<SlnCampaignDto?> GetCampaignAsync(int campaignId, int customerId, int? branchId = null);
    Task<SlnCampaignDto> CreateCampaignAsync(SlnCampaignCreateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> UpdateCampaignAsync(int campaignId, SlnCampaignUpdateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> DeleteCampaignAsync(int campaignId, int customerId, int? branchId = null);
    Task<SlnSegmentPreviewDto> GetSegmentPreviewAsync(string? segmentFilter, int customerId, int? branchId = null);
    Task<List<SlnSegmentRecipientDto>> GetSegmentRecipientsAsync(string? segmentFilter, int customerId, int? branchId = null);
    Task<List<SlnSegmentPresetDto>> GetSegmentPresetsAsync(int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> SendCampaignAsync(int campaignId, int customerId, int? branchId = null);

    // Oto-Hatirlatma
    Task<List<SlnAutoReminderDto>> GetRemindersAsync(int customerId, int? branchId = null);
    Task<SlnAutoReminderDto> CreateReminderAsync(SlnAutoReminderCreateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> UpdateReminderAsync(int reminderId, SlnAutoReminderUpdateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> DeleteReminderAsync(int reminderId, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> ToggleReminderAsync(int reminderId, int customerId, int? branchId = null);
}
