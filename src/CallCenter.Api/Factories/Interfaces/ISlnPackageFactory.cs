using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnPackageFactory
{
    // Paket tanimlari
    Task<List<SlnPackageDefinitionDto>> GetDefinitionsAsync(int customerId);
    Task<SlnPackageDefinitionDto> CreateDefinitionAsync(SlnPackageDefinitionCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateDefinitionAsync(int id, SlnPackageDefinitionCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteDefinitionAsync(int id, int customerId);

    // Musteri paketleri
    Task<List<SlnClientPackageDto>> GetClientPackagesAsync(int customerId, int? clientId = null);
    Task<(SlnClientPackageDto? Package, string? Error)> SellPackageAsync(SlnClientPackageSellDto dto, int userId, int customerId);
    Task<(bool Success, string? Error)> UseSessionAsync(SlnPackageUseDto dto, int userId, int customerId);
}
