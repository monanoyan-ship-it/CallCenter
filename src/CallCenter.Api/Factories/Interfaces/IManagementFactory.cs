using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IManagementFactory
{
    Task<ManagementDashboardDto> GetDashboardAsync();
    Task<List<AdminSubMerchantDto>> GetSubMerchantsAsync(int? statusId, string? search);
    Task<List<ModuleRequestDto>> GetModuleRequestsAsync(bool all);
}
