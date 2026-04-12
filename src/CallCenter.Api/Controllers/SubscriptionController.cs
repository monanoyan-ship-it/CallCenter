using CallCenter.Api.Factories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize(Roles = "Admin")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionFactory _factory;

    public SubscriptionController(ISubscriptionFactory factory) => _factory = factory;

    // ═══ PLAN ═══

    [HttpGet("plans")]
    public async Task<ActionResult> GetPlans()
        => Ok(await _factory.GetPlansAsync());

    [HttpPost("plans")]
    public async Task<ActionResult> CreatePlan([FromBody] PlanRequest request)
        => Ok(await _factory.CreatePlanAsync(request.Name, request.IntervalMonths, request.DiscountPercent, request.BranchPrice));

    [HttpPut("plans/{id}")]
    public async Task<ActionResult> UpdatePlan(int id, [FromBody] PlanRequest request)
    {
        var (s, e) = await _factory.UpdatePlanAsync(id, request.Name, request.IntervalMonths, request.DiscountPercent, request.BranchPrice, request.IsActive);
        return s ? Ok() : BadRequest(new { message = e });
    }

    [HttpDelete("plans/{id}")]
    public async Task<ActionResult> DeletePlan(int id)
    {
        var (s, e) = await _factory.DeletePlanAsync(id);
        return s ? Ok() : BadRequest(new { message = e });
    }

    // ═══ ABONELİK ═══

    [HttpGet]
    public async Task<ActionResult> GetSubscriptions([FromQuery] int? customerId)
        => Ok(await _factory.GetCustomerSubscriptionsAsync(customerId));

    [HttpPost]
    public async Task<ActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var (result, error) = await _factory.CreateSubscriptionAsync(request.CustomerId, request.PlanId, request.StartDate, request.MonthlyPrice);
        return result != null ? Ok(result) : BadRequest(new { message = error });
    }

    [HttpPut("{id}/cancel")]
    public async Task<ActionResult> CancelSubscription(int id)
    {
        var (s, e) = await _factory.CancelSubscriptionAsync(id);
        return s ? Ok() : BadRequest(new { message = e });
    }

    // ═══ MÜŞTERİ KENDİ ABONELİĞİ (Salon tarafı) ═══

    /// <summary>Musteri kendi abonelik ve odeme durumunu gorur</summary>
    [HttpGet("my")]
    [Authorize] // Admin + CustomerUser
    public async Task<ActionResult> GetMySubscription()
    {
        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
            return Unauthorized();

        return Ok(await _factory.GetMySubscriptionAsync(customerId));
    }

    // ═══ TAHAKKUK ═══

    [HttpPost("generate-billing")]
    public async Task<ActionResult> GenerateBilling([FromBody] GenerateBillingRequest request)
    {
        var (created, skipped) = await _factory.GenerateBillingForMonthAsync(request.Year, request.Month);
        return Ok(new { created, skipped });
    }
}

public class PlanRequest
{
    public string Name { get; set; } = string.Empty;
    public int IntervalMonths { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal BranchPrice { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateSubscriptionRequest
{
    public int CustomerId { get; set; }
    public int PlanId { get; set; }
    public DateTime StartDate { get; set; }
    public decimal MonthlyPrice { get; set; }
}

public class GenerateBillingRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
}
