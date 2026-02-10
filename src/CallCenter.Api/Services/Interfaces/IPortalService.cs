using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface IPortalService
{
    // Dashboard
    Task<PortalDashboardDto> GetDashboardAsync(int customerId);

    // UserTypes
    Task<List<UserTypeListDto>> GetUserTypesAsync(int customerId);
    Task<(bool Success, object Result)> CreateUserTypeAsync(int customerId, UserTypeCreateDto dto);
    Task<(bool Success, string? Error)> UpdateUserTypeAsync(int customerId, int id, UserTypeUpdateDto dto);
    Task<(bool Success, string? Error)> DeactivateUserTypeAsync(int customerId, int id);
    Task<int[]> GetUserTypePermissionsAsync(int customerId, int id);
    Task<(bool Success, string? Error)> SetUserTypePermissionsAsync(int customerId, int id, int[] permissionTypeIds);

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
