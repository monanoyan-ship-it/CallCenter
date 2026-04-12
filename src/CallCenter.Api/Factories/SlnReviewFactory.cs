using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnReviewFactory : ISlnReviewFactory
{
    private readonly ISlnReviewEntityService _reviewEs;
    private readonly IUnitOfWork _uow;

    public SlnReviewFactory(ISlnReviewEntityService reviewEs, IUnitOfWork uow)
    {
        _reviewEs = reviewEs;
        _uow = uow;
    }

    public async Task<List<SlnReviewDto>> GetReviewsAsync(int customerId)
    {
        return await _reviewEs.GetAllQueryable()
            .Where(r => r.CustomerId == customerId)
            .Include(r => r.SlnClient)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    public async Task<SlnReviewDto?> GetReviewAsync(int id, int customerId)
    {
        var review = await _reviewEs.GetAllQueryable()
            .Include(r => r.SlnClient)
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        return review != null ? MapToDto(review) : null;
    }

    public async Task<SlnReviewDto> CreateReviewAsync(SlnReviewCreateDto dto, int customerId)
    {
        var review = new SlnReview
        {
            CustomerId = customerId,
            SlnClientId = dto.SlnClientId,
            ClientName = dto.ClientName,
            Rating = dto.Rating,
            Comment = dto.Comment,
            SourceId = dto.SourceId,
            ExternalUrl = dto.ExternalUrl,
            StatusId = 1
        };
        _reviewEs.Add(review);
        await _uow.SaveChangesAsync();
        return (await GetReviewAsync(review.Id, customerId))!;
    }

    public async Task<(bool Success, string? Error)> UpdateStatusAsync(int id, int statusId, int customerId)
    {
        var review = await _reviewEs.GetAllQueryable().FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        if (review == null) return (false, "Yorum bulunamadi");

        review.StatusId = statusId;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteReviewAsync(int id, int customerId)
    {
        var review = await _reviewEs.GetAllQueryable().FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        if (review == null) return (false, "Yorum bulunamadi");

        _reviewEs.Remove(review);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<SlnReviewStatsDto> GetStatsAsync(int customerId)
    {
        var reviews = await _reviewEs.GetAllQueryable().Where(r => r.CustomerId == customerId).ToListAsync();
        return new SlnReviewStatsDto
        {
            TotalReviews = reviews.Count,
            PendingCount = reviews.Count(r => r.StatusId == 1),
            ApprovedCount = reviews.Count(r => r.StatusId == 2),
            RejectedCount = reviews.Count(r => r.StatusId == 3),
            AverageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0
        };
    }

    private static SlnReviewDto MapToDto(SlnReview r) => new()
    {
        Id = r.Id,
        SlnClientId = r.SlnClientId,
        ClientName = r.SlnClient?.FullName ?? r.ClientName ?? "",
        Rating = r.Rating,
        Comment = r.Comment,
        SourceId = r.SourceId,
        ExternalUrl = r.ExternalUrl,
        StatusId = r.StatusId,
        CreatedAt = r.CreatedAt
    };
}
