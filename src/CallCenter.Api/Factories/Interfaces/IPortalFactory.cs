using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IPortalFactory
{
    Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null);
    Task<PortalDashboardDto> GetDashboardAsync(int customerId, int? callerPersonnelId = null, int? callerRoleId = null);
    Task<List<PortalPersonnelListDto>> GetPersonnelAsync(int customerId, int? callerPersonnelId = null, int? callerRoleId = null);
    Task<(bool Success, object Result)> CreatePersonnelAsync(int customerId, PortalPersonnelCreateDto dto, int createdByUserId);
    Task<(bool Success, string? Error)> UpdatePersonnelAsync(int customerId, int id, PortalPersonnelUpdateDto dto, bool isSystemAdmin = false);
    Task<(bool Success, string? Error)> DeactivatePersonnelAsync(int customerId, int id);
    Task<(bool Success, string? Error)> ReactivatePersonnelAsync(int customerId, int id);
    Task<List<PortalModuleDto>> GetModulesAsync(int customerId);
    Task<List<PortalSipAccountDto>> GetSipAccountsAsync(int customerId);
    Task<(bool Success, string? Error)> UpdateSipAccountAsync(int customerId, int id, PortalSipUpdateDto dto);
    Task<(bool Success, int? Id, string? Error)> CreateSipAccountAsync(int customerId, PortalSipCreateDto dto);
    Task<(bool Success, string? Error)> DeleteSipAccountAsync(int customerId, int id);
    Task<(bool Success, string? Error)> SetReportsToAsync(int customerId, int personnelId, int? reportsToPersonnelId);
}
