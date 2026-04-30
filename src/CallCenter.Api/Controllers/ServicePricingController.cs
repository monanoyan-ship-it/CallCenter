using CallCenter.Api.Factories;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/service-pricing")]
[Authorize(Roles = "Admin")]
public class ServicePricingController : ControllerBase
{
    private readonly ServicePricingFactory _factory;

    public ServicePricingController(ServicePricingFactory factory) => _factory = factory;

    [HttpGet("periods")]
    public async Task<ActionResult> GetPeriods()
        => Ok(await _factory.GetPeriodsAsync());

    [HttpGet("periods/{id}")]
    public async Task<ActionResult> GetPeriodDetail(int id)
    {
        var result = await _factory.GetPeriodDetailAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpPost("periods")]
    public async Task<ActionResult> CreatePeriod([FromBody] CreatePeriodRequest request)
    {
        var (result, error) = await _factory.CreatePeriodAsync(request.Name, request.StartDate, request.EndDate);
        return result != null ? Ok(result) : BadRequest(new { message = error });
    }

    [HttpPut("items/{itemId}/price")]
    public async Task<ActionResult> UpdateItemPrice(int itemId, [FromBody] UpdatePriceRequest request)
    {
        var (s, e) = await _factory.UpdateItemPriceAsync(itemId, request.MonthlyPrice);
        return s ? Ok() : BadRequest(new { message = e });
    }

    [HttpPost("periods/{periodId}/bulk-adjust")]
    public async Task<ActionResult> BulkAdjust(int periodId, [FromBody] BulkAdjustRequest request)
    {
        var (updated, error) = await _factory.BulkAdjustAsync(periodId, request.ProductTypeId, request.AdjustType, request.Value);
        return error == null ? Ok(new { updated }) : BadRequest(new { message = error });
    }

    [HttpPut("periods/{periodId}/activate")]
    public async Task<ActionResult> ActivatePeriod(int periodId)
    {
        var (s, e) = await _factory.ActivatePeriodAsync(periodId);
        return s ? Ok() : BadRequest(new { message = e });
    }

    [HttpPut("periods/{periodId}/branch-discount-tiers")]
    public async Task<ActionResult> ReplaceBranchDiscountTiers(int periodId, [FromBody] ReplaceBranchDiscountTiersRequest request)
    {
        var (s, e) = await _factory.ReplaceBranchDiscountTiersAsync(periodId, request.Tiers ?? []);
        return s ? Ok() : BadRequest(new { message = e });
    }

    [HttpDelete("periods/{periodId}")]
    public async Task<ActionResult> DeletePeriod(int periodId)
    {
        var (s, e) = await _factory.DeletePeriodAsync(periodId);
        return s ? Ok() : BadRequest(new { message = e });
    }
}

public class CreatePeriodRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdatePriceRequest
{
    public decimal MonthlyPrice { get; set; }
}

public class BulkAdjustRequest
{
    public int? ProductTypeId { get; set; }
    public string AdjustType { get; set; } = "amount";
    public decimal Value { get; set; }
}
