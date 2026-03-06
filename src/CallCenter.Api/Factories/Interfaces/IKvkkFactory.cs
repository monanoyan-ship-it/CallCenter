using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.Factories.Interfaces;

public interface IKvkkFactory
{
    // PrivacyNotice
    Task<List<PrivacyNoticeListDto>> GetPrivacyNoticesAsync(int? customerId, int? typeId);
    Task<PrivacyNoticeDto?> GetPrivacyNoticeByUidAsync(Guid uid);
    Task<PrivacyNoticeDto?> GetActivePrivacyNoticeAsync(int customerId, int typeId);
    Task<(bool Success, object Result)> CreatePrivacyNoticeAsync(PrivacyNoticeCreateDto dto);
    Task<(bool Success, string? Error)> UpdatePrivacyNoticeAsync(Guid uid, PrivacyNoticeUpdateDto dto);
    Task<(bool Success, string? Error)> ActivatePrivacyNoticeAsync(Guid uid);
    Task<(bool Success, object Result)> RecordCallConsentAsync(CallConsentDto dto);
    Task<ConsentRecordDto?> GetCallConsentAsync(int callRecordId);

    // Consent
    Task<(List<ConsentRecordDto> Items, int TotalCount)> GetConsentsAsync(int? customerId, int page, int pageSize);
    Task<(bool Success, object Result)> CreateConsentAsync(ConsentCreateDto dto);
    Task<(bool Success, string? Error)> RevokeConsentAsync(Guid uid, string revokedBy);
    Task<List<ConsentRecordDto>> GetConsentBySubjectAsync(int customerId, string identifier);

    // DataSubjectRequest
    Task<(List<DataSubjectRequestDto> Items, int TotalCount)> GetRequestsAsync(int? customerId, int? statusId, int page, int pageSize);
    Task<(bool Success, object Result)> CreateRequestAsync(DataSubjectRequestCreateDto dto);
    Task<(bool Success, string? Error)> UpdateRequestAsync(Guid uid, DataSubjectRequestUpdateDto dto);
    Task<List<DataSubjectRequestDto>> GetOverdueRequestsAsync();

    // DataBreach
    Task<(List<DataBreachDto> Items, int TotalCount)> GetBreachesAsync(int? customerId, int page, int pageSize);
    Task<(bool Success, object Result)> CreateBreachAsync(DataBreachCreateDto dto, string? reportedByUserName, int? reportedByUserId);
    Task<(bool Success, string? Error)> UpdateBreachAsync(Guid uid, DataBreachUpdateDto dto);

    // RetentionPolicy
    Task<List<RetentionPolicyDto>> GetPoliciesAsync(int customerId);
    Task<(bool Success, object Result)> CreateOrUpdatePolicyAsync(RetentionPolicyCreateDto dto);
    Task SeedDefaultPoliciesAsync(int customerId);

    // DataDestructionLog
    Task<(List<DataDestructionLogDto> Items, int TotalCount)> GetDestructionLogsAsync(int? customerId, int page, int pageSize);
    Task<(bool Success, object Result)> LogDestructionAsync(DataDestructionCreateDto dto, string approvedByUserName, int? approvedByUserId);

    // DataInventory
    Task<List<DataInventoryItemDto>> GetInventoryAsync(int? customerId);
    Task<(bool Success, object Result)> CreateOrUpdateItemAsync(DataInventoryCreateDto dto);

    // CrossBorderTransfer
    Task<List<CrossBorderTransferDto>> GetTransfersAsync(int? customerId);
    Task<(bool Success, object Result)> CreateTransferAsync(CrossBorderTransferCreateDto dto, string? createdByUserName);
    Task<(bool Success, string? Error)> UpdateTransferAsync(Guid uid, CrossBorderTransferUpdateDto dto);

    // Dashboard
    Task<KvkkDashboardDto> GetDashboardAsync(int? customerId);
}
