using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnReviewFactory
{
    Task<List<SlnReviewDto>> GetReviewsAsync(int customerId, int? branchId = null);
    Task<SlnReviewDto?> GetReviewAsync(int id, int customerId, int? branchId = null);
    Task<SlnReviewDto> CreateReviewAsync(SlnReviewCreateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> UpdateStatusAsync(int id, int statusId, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> DeleteReviewAsync(int id, int customerId, int? branchId = null);
    Task<SlnReviewStatsDto> GetStatsAsync(int customerId, int? branchId = null);
}
