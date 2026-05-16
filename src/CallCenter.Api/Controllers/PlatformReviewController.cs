using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ISlnReviewFactory _reviewFactory;

    public PlatformReviewController(ISlnReviewFactory reviewFactory)
    {
        _reviewFactory = reviewFactory;
    }

    /// <summary>Yeni yorum yaz veya mevcut yorumu guncelle. Bir PlatformUser her salona 1 yorum yazabilir.</summary>
    [HttpPost]
    public async Task<ActionResult<PlatformReviewDto>> Create([FromBody] PlatformReviewCreateRequest request)
    {
        var platformUserId = GetPlatformUserId();
        if (platformUserId == 0) return Unauthorized();

        var (review, error, statusCode) = await _reviewFactory.UpsertPlatformReviewAsync(request, platformUserId);
        if (error != null)
            return StatusCode(statusCode, new { message = error });

        return Ok(review);
    }

    /// <summary>PlatformUser'in yazdigi tum yorumlar.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<List<PlatformReviewDto>>> GetMine()
    {
        var platformUserId = GetPlatformUserId();
        if (platformUserId == 0) return Unauthorized();

        return Ok(await _reviewFactory.GetPlatformReviewsAsync(platformUserId));
    }

    private int GetPlatformUserId()
        => int.Parse(User.FindFirst("PlatformUserId")?.Value ?? "0");
}
