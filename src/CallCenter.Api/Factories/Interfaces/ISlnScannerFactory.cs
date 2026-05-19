using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnScannerFactory
{
    Task<SlnScanResolveDto> ResolvePublicAsync(SlnScanResolveRequest request);
    Task<SlnScanResolveDto> ResolveSalonAsync(SlnScanResolveRequest request, int customerId, int? claimBranchId);
    Task<SlnScanTokenDto> CreateTokenAsync(SlnScanTokenCreateRequest request, int customerId, int? claimBranchId, bool isSalonOwner);
}
