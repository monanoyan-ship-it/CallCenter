using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-reviews")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnReviews)]
public class SlnReviewController : ControllerBase
{
    private readonly ISlnReviewFactory _factory;

    public SlnReviewController(ISlnReviewFactory factory) => _factory = factory;

    [HttpGet]
    public async Task<ActionResult<List<SlnReviewDto>>> GetReviews([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetReviewsAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<SlnReviewStatsDto>> GetStats([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetStatsAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnReviewDto>> GetReview(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var review = await _factory.GetReviewAsync(id, customerId, GetBranchId() ?? branchId);
        return review != null ? Ok(review) : NotFound();
    }

    /// <summary>
    /// Salon admin/personel TARAFINDAN yorum eklenemez (puanı şişirme açığı).
    /// Bu endpoint kaldırıldı — yorum sadece müşteri (PlatformUser) tarafından
    /// public flow üzerinden eklenebilir.
    /// </summary>
    [HttpPost]
    public ActionResult CreateReview() => Forbid();

    [HttpPut("{id}/status/{statusId}")]
    public async Task<ActionResult> UpdateStatus(int id, int statusId, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdateStatusAsync(id, statusId, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteReview(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeleteReviewAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var value = User.FindFirst("BranchId")?.Value;
        return int.TryParse(value, out var branchId) && branchId > 0 ? branchId : null;
    }
}
