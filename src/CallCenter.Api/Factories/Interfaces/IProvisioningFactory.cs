using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IProvisioningFactory
{
    Task<ProvisioningUrlResponse> CreateProvisioningAsync(CreateProvisioningRequest req, string baseUrl);
    Task<ProvisioningConfigDto?> GetProvisioningByTokenAsync(string token);
    Task<ProvisioningConfigDto?> GetMyProvisioningAsync(int userId);
}
