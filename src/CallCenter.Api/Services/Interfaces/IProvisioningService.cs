using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Interfaces;

public interface IProvisioningService
{
    /// <summary>Belirli bir kullanici icin provisioning config olusturur ve token doner.</summary>
    Task<ProvisioningUrlResponse> CreateProvisioningAsync(CreateProvisioningRequest req, string baseUrl);

    /// <summary>Token ile provisioning config'i getirir (one-time).</summary>
    Task<ProvisioningConfigDto?> GetProvisioningByTokenAsync(string token);

    /// <summary>Authenticated kullanici icin kendi provisioning config'ini getirir.</summary>
    Task<ProvisioningConfigDto?> GetMyProvisioningAsync(int userId);
}
