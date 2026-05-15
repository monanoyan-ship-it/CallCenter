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
    Task<List<SlnClientPackageDto>> GetClientPackagesAsync(int customerId, int? clientId = null, int? branchId = null);
    Task<(SlnClientPackageDto? Package, string? Error)> SellPackageAsync(SlnClientPackageSellDto dto, int userId, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> UseSessionAsync(SlnPackageUseDto dto, int userId, int customerId, int? branchId = null);
    Task<List<SlnPackageBenefitDto>> GetUsablePackagesAsync(int customerId, int slnClientId, IEnumerable<int> serviceIds, int? branchId = null);
    Task<(bool Success, string? Error)> RecordUsageAsync(int customerId, int clientPackageId, int? serviceId, int? slnClientId, int userId, string? notes, int? branchId = null);
    Task<(bool Success, string? Error)> ReverseInvoiceUsagesAsync(int customerId, int invoiceId);
    Task<(bool Success, string? Error)> CancelPackageSaleFromInvoiceAsync(int customerId, string? invoiceNotes);
}
