using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.Factories.Interfaces;

public interface ICustomerFactory
{
    Task<PagedResult<CustomerListDto>> GetAllAsync(int page, int pageSize, string? search);
    Task<CustomerDetailDto?> GetByIdAsync(int id);
    Task<Customer?> GetRawByIdAsync(int id);
    Task<(int Id, string? Error)> CreateAsync(CustomerCreateDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(int id, CustomerUpdateDto dto);
    Task<(bool Success, string? Error)> UpdateSettingsAsync(int id, UpdateCustomerSettingsRequest dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<(bool Success, string? Error)> HardDeleteAsync(int id);
    Task<(bool Success, string? TempPassword, string? Error)> ResetAdminPasswordAsync(int id);

    Task<object?> GetCustomerModulesAsync(int customerId);
    Task<(bool Success, string? Error)> AssignModulesAsync(int customerId, AssignModulesRequest request);
    Task<(bool Success, string? Error)> DeactivateModuleAsync(int customerId, int moduleId);
    Task<(bool Success, int AddedCount, string? Error)> SyncMissingModulesAsync(int customerId);
    Task<(bool Success, int RemovedCount, string? Error)> CleanupModulesAsync(int customerId, string keep);

    /// <summary>Salon self-registration: musteri + admin + default moduller olustur + login token dondur</summary>
    Task<SlnRegisterResponse> SalonRegisterAsync(SlnRegisterRequest request);
}
