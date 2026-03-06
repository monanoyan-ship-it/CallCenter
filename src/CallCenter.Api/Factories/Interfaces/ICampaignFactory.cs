using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ICampaignFactory
{
    Task<List<CampaignDto>> GetCampaignsAsync(int? customerId);
    Task<CampaignDetailDto?> GetCampaignByUidAsync(Guid uid, int? customerId);
    Task<CampaignDto> CreateCampaignAsync(CreateCampaignRequest req, int personnelId, int? customerId);
    Task<(bool Success, string? Error)> UpdateCampaignAsync(Guid uid, UpdateCampaignRequest req, int? customerId);
    Task<(bool Success, string? Error)> DeleteCampaignAsync(Guid uid, int? customerId);
    Task<(bool Success, string? Error)> AddContactsAsync(Guid campaignUid, AddCampaignContactsRequest req, int? customerId);
    Task<(bool Success, string? Error)> RemoveContactAsync(Guid campaignUid, int campaignContactId, int? customerId);
    Task<(bool Success, string? Error)> AssignContactsAsync(Guid campaignUid, AssignCampaignContactsRequest req, int? customerId);
    Task<(bool Success, string? Error)> UpdateContactStatusAsync(Guid campaignUid, int campaignContactId, UpdateCampaignContactStatusRequest req, int? customerId);
    Task<List<MyCallListItemDto>> GetMyCallListAsync(int personnelId);
    Task<(bool Success, string? Error)> UpdateMyListItemAsync(int campaignContactId, UpdateCampaignContactStatusRequest req, int personnelId);
    Task<List<PersonnelSimpleDto>> GetOperatorsAsync(int? customerId);
}
