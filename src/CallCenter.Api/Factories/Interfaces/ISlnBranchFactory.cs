using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnBranchFactory
{
    Task<List<SlnBranchDto>> GetBranchesAsync(int customerId);
    Task<SlnBranchDto?> GetBranchAsync(int branchId, int customerId);
    Task<SlnBranchDto> CreateBranchAsync(SlnBranchCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateBranchAsync(int branchId, SlnBranchUpdateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeleteBranchAsync(int branchId, int customerId);
}
