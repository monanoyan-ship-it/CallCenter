using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IManagementFactory
{
    Task<ManagementDashboardDto> GetDashboardAsync();
    Task<List<ModulePricingDto>> GetModulePricingAsync();
    Task UpdateModulePricingAsync(int moduleId, decimal monthlyPrice);
    Task<int> BulkUpdateModulePricingAsync(List<UpdateModulePricingRequest> prices);
    Task<List<ModuleRequestDto>> GetModuleRequestsAsync(bool all);
}
