using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnServiceFactory
{
    Task<List<SlnServiceCategoryDto>> GetCategoriesWithServicesAsync(int customerId);
    Task<SlnServiceCategoryDto> CreateCategoryAsync(string name, int sortOrder, int customerId);
    Task<(bool Success, string? Error)> UpdateCategoryAsync(int categoryId, string name, int sortOrder, bool isActive, int customerId);
    Task<(bool Success, string? Error)> DeleteCategoryAsync(int categoryId, int customerId);
    Task<List<SlnServiceDto>> GetServicesAsync(int customerId, int? categoryId = null);
    Task<SlnServiceDto> CreateServiceAsync(SlnServiceCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateServiceAsync(int serviceId, SlnServiceCreateDto dto, bool isActive, int customerId);
    Task<(bool Success, string? Error)> DeleteServiceAsync(int serviceId, int customerId);
}
