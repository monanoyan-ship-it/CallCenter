using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface IPortalService
{
    // Dashboard
    Task<PortalDashboardDto> GetDashboardAsync(int customerId);

    // Personnel
    Task<List<PortalPersonnelListDto>> GetPersonnelAsync(int customerId);
    Task<(bool Success, object Result)> CreatePersonnelAsync(int customerId, PortalPersonnelCreateDto dto, int createdByUserId);
    Task<(bool Success, string? Error)> UpdatePersonnelAsync(int customerId, int id, PortalPersonnelUpdateDto dto);
    Task<(bool Success, string? Error)> DeactivatePersonnelAsync(int customerId, int id);
    Task<List<PersonnelPermissionDto>> GetPersonnelPermissionsAsync(int customerId, int personnelId);
    Task<(bool Success, string? Error)> SetPersonnelPermissionsAsync(int customerId, int personnelId, int[] permissionTypeIds, int scopeId, int userId);

    // Modules
    Task<List<PortalModuleDto>> GetModulesAsync(int customerId);

    // SIP
    Task<List<PortalSipAccountDto>> GetSipAccountsAsync(int customerId);
    Task<(bool Success, string? Error)> UpdateSipAccountAsync(int customerId, int id, PortalSipUpdateDto dto);
}
