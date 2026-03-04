using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface ICustomerService
{
    // Customers
    Task<PagedResult<CustomerListDto>> GetAllAsync(int page, int pageSize, string? search);
    Task<CustomerDetailDto?> GetByIdAsync(int id);
    Task<(int Id, string? Error)> CreateAsync(CustomerCreateDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(int id, CustomerUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<(bool Success, string? TempPassword, string? Error)> ResetAdminPasswordAsync(int customerId);

    // CustomerPermissions — Modules
    Task<object?> GetCustomerModulesAsync(int customerId);
    Task<(bool Success, string? Error)> AssignModulesAsync(int customerId, AssignModulesRequest request);
    Task<(bool Success, string? Error)> DeactivateModuleAsync(int customerId, int moduleId);

}
