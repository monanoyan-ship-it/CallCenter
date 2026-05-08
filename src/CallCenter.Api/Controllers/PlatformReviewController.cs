using System.Security.Claims;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

/// <summary>
/// Platform kullanicisi (mobil/public musteri) yorum endpoint'leri.
/// Salon admin yorum girisi SlnReviewController.CreateReview ile ENGELLENMIS;
/// yorum SADECE bu controller'dan eklenebilir.
/// </summary>
[ApiController]
[Route("api/platform/reviews")]
[Authorize(Roles = "PlatformUser")]
public class PlatformReviewController : ControllerBase
{
    private readonly ISlnReviewEntityService _reviewEs;
    private readonly ISlnBranchEntityService _branchEs;
    private readonly ISlnSalonProfileEntityService _profileEs;
    private readonly IUnitOfWork _uow;

    public PlatformReviewController(
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

    /// <summary>Yeni yorum yaz veya mevcut yorumu guncelle. Bir PlatformUser her salona 1 yorum yazabilir.</summary>
    [HttpPost]
    public async Task<ActionResult<PlatformReviewDto>> Create([FromBody] PlatformReviewCreateRequest request)
    {
        if (request == null) return BadRequest(new { message = "İstek gövdesi boş." });
        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { message = "Puan 1 ile 5 arasında olmalı." });
        if (string.IsNullOrWhiteSpace(request.SalonSlug))
            return BadRequest(new { message = "Salon belirtilmedi." });

        var comment = (request.Comment ?? "").Trim();
        if (comment.Length > 1000)
            return BadRequest(new { message = "Yorum en fazla 1000 karakter olabilir." });

        var customerId = await ResolveCustomerIdAsync(request.SalonSlug);
        if (customerId == null)
            return NotFound(new { message = "Salon bulunamadı." });

        var platformUserId = GetPlatformUserId();
        if (platformUserId == 0) return Unauthorized();

        // Mevcut yorum var mi? (Ayni salon + ayni PlatformUser)
        var existing = await _reviewEs.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.CustomerId == customerId.Value && r.PlatformUserId == platformUserId);

        if (existing != null)
        {
            existing.Rating = request.Rating;
            existing.Comment = string.IsNullOrEmpty(comment) ? null : comment;
            existing.StatusId = 1; // Bekliyor (admin tekrar onaylasin)
            existing.CreatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return Ok(MapDto(existing));
        }

        var review = new SlnReview
        {
            CustomerId = customerId.Value,
            PlatformUserId = platformUserId,
            ClientName = request.DisplayName,
            Rating = request.Rating,
            Comment = string.IsNullOrEmpty(comment) ? null : comment,
            SourceId = 1,
            StatusId = 1
        };
        _reviewEs.Add(review);
        await _uow.SaveChangesAsync();
        return Ok(MapDto(review));
    }

    /// <summary>PlatformUser'in yazdigi tum yorumlar.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<List<PlatformReviewDto>>> GetMine()
    {
        var platformUserId = GetPlatformUserId();
        if (platformUserId == 0) return Unauthorized();

        var list = await _reviewEs.GetAllQueryable()
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

        return Ok(list);
    }

    private int GetPlatformUserId()
        => int.Parse(User.FindFirst("PlatformUserId")?.Value ?? "0");

    private async Task<int?> ResolveCustomerIdAsync(string slug)
    {
        var s = slug.Trim();
        var branch = await _branchEs.GetAllQueryable().FirstOrDefaultAsync(b => b.Slug == s);
        if (branch != null) return branch.CustomerId;

        var profile = await _profileEs.GetAllQueryable().FirstOrDefaultAsync(p => p.Slug == s);
        return profile?.CustomerId;
    }

    private static PlatformReviewDto MapDto(SlnReview r) => new()
    {
        Id = r.Id,
        CustomerId = r.CustomerId,
        SalonName = r.Customer?.Name ?? "",
        Rating = r.Rating,
        Comment = r.Comment,
        StatusId = r.StatusId,
        CreatedAt = r.CreatedAt
    };
}
