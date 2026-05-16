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
    private readonly ISlnBranchEntityService _branchEs;
    private readonly ISlnSalonProfileEntityService _profileEs;
    private readonly IUnitOfWork _uow;

    public SlnReviewFactory(
        ISlnReviewEntityService reviewEs,
        ISlnBranchEntityService branchEs,
        ISlnSalonProfileEntityService profileEs,
        IUnitOfWork uow)
    {
        _reviewEs = reviewEs;
        _branchEs = branchEs;
        _profileEs = profileEs;
        _uow = uow;
    }

    public async Task<List<SlnReviewDto>> GetReviewsAsync(int customerId, int? branchId = null)
    {
        var query = SalonBranchScope.ApplyToReviews(
            _reviewEs.GetAllQueryable().Where(r => r.CustomerId == customerId),
            branchId);

        return await query
            .Include(r => r.SlnClient)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    public async Task<SlnReviewDto?> GetReviewAsync(int id, int customerId, int? branchId = null)
    {
        var review = await SalonBranchScope.ApplyToReviews(
                _reviewEs.GetAllQueryable().Where(r => r.Id == id && r.CustomerId == customerId),
                branchId)
            .Include(r => r.SlnClient)
            .FirstOrDefaultAsync();
        return review != null ? MapToDto(review) : null;
    }

    public async Task<SlnReviewDto> CreateReviewAsync(SlnReviewCreateDto dto, int customerId, int? branchId = null)
    {
        var review = new SlnReview
        {
            CustomerId = customerId,
            BranchId = branchId,
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
        return (await GetReviewAsync(review.Id, customerId, branchId))!;
    }

    public async Task<(bool Success, string? Error)> UpdateStatusAsync(int id, int statusId, int customerId, int? branchId = null)
    {
        var review = await SalonBranchScope.ApplyToReviews(
                _reviewEs.GetAllQueryable().Where(r => r.Id == id && r.CustomerId == customerId),
                branchId)
            .FirstOrDefaultAsync();
        if (review == null) return (false, "Yorum bulunamadi");
        if (review.BranchId == null && branchId.HasValue)
        {
            review.BranchId = branchId;
        }

        review.StatusId = statusId;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteReviewAsync(int id, int customerId, int? branchId = null)
    {
        var review = await SalonBranchScope.ApplyToReviews(
                _reviewEs.GetAllQueryable().Where(r => r.Id == id && r.CustomerId == customerId),
                branchId)
            .FirstOrDefaultAsync();
        if (review == null) return (false, "Yorum bulunamadi");

        _reviewEs.Remove(review);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<SlnReviewStatsDto> GetStatsAsync(int customerId, int? branchId = null)
    {
        var reviews = await SalonBranchScope.ApplyToReviews(
                _reviewEs.GetAllQueryable().Where(r => r.CustomerId == customerId),
                branchId)
            .ToListAsync();
        return new SlnReviewStatsDto
        {
            TotalReviews = reviews.Count,
            PendingCount = reviews.Count(r => r.StatusId == 1),
            ApprovedCount = reviews.Count(r => r.StatusId == 2),
            RejectedCount = reviews.Count(r => r.StatusId == 3),
            AverageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0
        };
    }

    public async Task<(PlatformReviewDto? Review, string? Error, int StatusCode)> UpsertPlatformReviewAsync(
        PlatformReviewCreateRequest request,
        int platformUserId)
    {
        if (platformUserId == 0) return (null, "Platform kullanicisi bulunamadi.", 401);
        if (request == null) return (null, "Istek govdesi bos.", 400);
        if (request.Rating < 1 || request.Rating > 5)
            return (null, "Puan 1 ile 5 arasinda olmali.", 400);
        if (string.IsNullOrWhiteSpace(request.SalonSlug))
            return (null, "Salon belirtilmedi.", 400);

        var comment = (request.Comment ?? "").Trim();
        if (comment.Length > 1000)
            return (null, "Yorum en fazla 1000 karakter olabilir.", 400);

        var target = await ResolveSalonTargetAsync(request.SalonSlug);
        if (target == null)
            return (null, "Salon bulunamadi.", 404);

        var existing = await _reviewEs.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.CustomerId == target.CustomerId
                                      && r.BranchId == target.BranchId
                                      && r.PlatformUserId == platformUserId);

        if (existing != null)
        {
            existing.Rating = request.Rating;
            existing.Comment = string.IsNullOrEmpty(comment) ? null : comment;
            existing.StatusId = 1;
            existing.CreatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return (MapPlatformDto(existing, target.SalonName), null, 200);
        }

        var review = new SlnReview
        {
            CustomerId = target.CustomerId,
            BranchId = target.BranchId,
            PlatformUserId = platformUserId,
            ClientName = request.DisplayName,
            Rating = request.Rating,
            Comment = string.IsNullOrEmpty(comment) ? null : comment,
            SourceId = 1,
            StatusId = 1
        };
        _reviewEs.Add(review);
        await _uow.SaveChangesAsync();
        return (MapPlatformDto(review, target.SalonName), null, 200);
    }

    public async Task<List<PlatformReviewDto>> GetPlatformReviewsAsync(int platformUserId)
    {
        return await _reviewEs.GetAllQueryable()
            .Where(r => r.PlatformUserId == platformUserId)
            .Include(r => r.Customer)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PlatformReviewDto
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                SalonName = r.Customer != null ? r.Customer.Name : "",
                Rating = r.Rating,
                Comment = r.Comment,
                StatusId = r.StatusId,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    private async Task<ReviewSalonTarget?> ResolveSalonTargetAsync(string slug)
    {
        var normalized = slug.Trim();
        var branch = await _branchEs.GetAllQueryable()
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Slug == normalized);
        if (branch != null)
            return new ReviewSalonTarget(branch.CustomerId, branch.Id, branch.Customer?.Name ?? branch.Name);

        var profile = await _profileEs.GetAllQueryable()
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Slug == normalized);
        if (profile == null) return null;

        var headquarterBranchId = await _branchEs.GetAllQueryable()
            .Where(b => b.CustomerId == profile.CustomerId && b.IsHeadquarter && b.IsActive)
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

        return new ReviewSalonTarget(profile.CustomerId, headquarterBranchId, profile.Customer?.Name ?? profile.Slug);
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
        BranchId = r.BranchId,
        CreatedAt = r.CreatedAt
    };

    private static PlatformReviewDto MapPlatformDto(SlnReview r, string? salonName = null) => new()
    {
        Id = r.Id,
        CustomerId = r.CustomerId,
        SalonName = salonName ?? r.Customer?.Name ?? "",
        Rating = r.Rating,
        Comment = r.Comment,
        StatusId = r.StatusId,
        CreatedAt = r.CreatedAt
    };

    private sealed record ReviewSalonTarget(int CustomerId, int? BranchId, string SalonName);
}
