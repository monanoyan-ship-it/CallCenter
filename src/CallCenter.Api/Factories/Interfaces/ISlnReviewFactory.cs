using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnReviewFactory
{
    Task<List<SlnReviewDto>> GetReviewsAsync(int customerId);
    Task<SlnReviewDto?> GetReviewAsync(int id, int customerId);
    Task<SlnReviewDto> CreateReviewAsync(SlnReviewCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateStatusAsync(int id, int statusId, int customerId);
    Task<(bool Success, string? Error)> DeleteReviewAsync(int id, int customerId);
    Task<SlnReviewStatsDto> GetStatsAsync(int customerId);
}
