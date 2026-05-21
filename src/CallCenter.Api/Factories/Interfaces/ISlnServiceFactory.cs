using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnServiceFactory
{
    Task<List<SlnServiceCategoryDto>> GetCategoriesWithServicesAsync(int customerId);
    Task<SlnServiceCategoryDto> CreateCategoryAsync(string name, int sortOrder, int customerId, string? iconClass = null, string? color = null, bool isActive = true);
    Task<(bool Success, string? Error)> UpdateCategoryAsync(int categoryId, string name, int sortOrder, bool? isActive, int customerId, string? iconClass = null, string? color = null);
    Task<(bool Success, string? Error)> DeleteCategoryAsync(int categoryId, int customerId);
    Task<List<SlnServiceDto>> GetServicesAsync(int customerId, int? categoryId = null);
    Task<(SlnServiceDto? Service, string? Error)> CreateServiceAsync(SlnServiceCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateServiceAsync(int serviceId, SlnServiceCreateDto dto, bool? isActive, int customerId, bool syncResourceRequirements = true);
    Task<(bool Success, string? Error)> DeleteServiceAsync(int serviceId, int customerId);
    Task<List<SlnResourceDto>> GetResourcesAsync(int customerId, int? branchScopeId = null);
    Task<SlnResourceDto> CreateResourceAsync(SlnResourceCreateDto dto, int customerId, int? branchScopeId = null);
    Task<(bool Success, string? Error)> UpdateResourceAsync(int resourceId, SlnResourceCreateDto dto, int customerId, int? branchScopeId = null);
    Task<(bool Success, string? Error)> DeleteResourceAsync(int resourceId, int customerId, int? branchScopeId = null);
    Task<List<SlnServiceComboDto>> GetCombosAsync(int customerId);
    Task<(SlnServiceComboDto? Combo, string? Error)> CreateComboAsync(SlnServiceComboCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateComboAsync(int comboId, SlnServiceComboCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteComboAsync(int comboId, int customerId);
}
