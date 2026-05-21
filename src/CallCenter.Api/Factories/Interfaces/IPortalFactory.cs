using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IPortalFactory
{
    Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null);
    Task<PortalDashboardDto> GetDashboardAsync(int customerId, int? callerPersonnelId = null, int? callerRoleId = null);
    Task<List<PortalPersonnelListDto>> GetPersonnelAsync(int customerId, int? callerPersonnelId = null, int? callerRoleId = null, int? callerBranchId = null);
    Task<(bool Success, object Result)> CreatePersonnelAsync(int customerId, PortalPersonnelCreateDto dto, int createdByUserId);
    Task<(bool Success, string? Error)> UpdatePersonnelAsync(int customerId, int id, PortalPersonnelUpdateDto dto, bool isSystemAdmin = false);
    Task<(bool Success, string? Error)> ResetPersonnelPasswordAsync(int customerId, int id, string password);
    Task<(bool Success, string? Error, string? PhotoUrl)> GetPersonnelPhotoUrlAsync(int customerId, int id);
    Task<(bool Success, string? Error)> UpdatePersonnelPhotoAsync(int customerId, int id, string photoUrl);
    Task<(bool Success, string? Error)> DeactivatePersonnelAsync(int customerId, int id);
    Task<(bool Success, string? Error)> ReactivatePersonnelAsync(int customerId, int id);
    Task<(bool Success, string? Error)> UnlockPersonnelAsync(int customerId, int id);
    Task<List<PortalModuleDto>> GetModulesAsync(int customerId);
    Task<List<PortalSipAccountDto>> GetSipAccountsAsync(int customerId);
    Task<(bool Success, string? Error)> UpdateSipAccountAsync(int customerId, int id, PortalSipUpdateDto dto);
    Task<(bool Success, int? Id, string? Error)> CreateSipAccountAsync(int customerId, PortalSipCreateDto dto);
    Task<(bool Success, string? Error)> DeleteSipAccountAsync(int customerId, int id);
    Task<(bool Success, string? Error)> SetReportsToAsync(int customerId, int personnelId, int? reportsToPersonnelId);
    Task<PortalPersonnelOpsDto> GetPersonnelOpsAsync(int customerId, DateTime from, DateTime to, int? callerPersonnelId = null, int? callerRoleId = null, int? callerBranchId = null);
    Task<(bool Success, string? Error)> UpsertPersonnelShiftAsync(int customerId, int? id, PortalPersonnelShiftUpsertDto dto, int? callerRoleId = null, int? callerBranchId = null);
    Task<(bool Success, string? Error)> DeletePersonnelShiftAsync(int customerId, int id, int? callerRoleId = null, int? callerBranchId = null);
    Task<(bool Success, string? Error)> CreatePersonnelLeaveAsync(int customerId, PortalPersonnelLeaveCreateDto dto, int? callerRoleId = null, int? callerBranchId = null);
    Task<(bool Success, string? Error)> UpdatePersonnelLeaveStatusAsync(int customerId, int id, PortalPersonnelLeaveStatusDto dto, int? reviewedByPersonnelId, int? callerRoleId = null, int? callerBranchId = null);
    Task<(bool Success, string? Error)> UpsertPersonnelTimesheetAsync(int customerId, int? id, PortalPersonnelTimesheetUpsertDto dto, int? callerRoleId = null, int? callerBranchId = null);
    Task<(bool Success, string? Error)> CreatePersonnelAdvanceAsync(int customerId, PortalAdvanceCreateDto dto, int? callerRoleId = null, int? callerBranchId = null);
    Task<(bool Success, string? Error)> GeneratePayrollAsync(int customerId, PortalPayrollGenerateDto dto, int? callerRoleId = null, int? callerBranchId = null);
}
