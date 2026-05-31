using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-loyalty-packages")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnLoyaltyPackages)]
public class SlnLoyaltyPackageController : ControllerBase
{
    private readonly ISlnLoyaltyPackageFactory _factory;

    public SlnLoyaltyPackageController(ISlnLoyaltyPackageFactory factory) => _factory = factory;

    [HttpGet("offers")]
    public async Task<ActionResult<List<SlnLoyaltyPackageOfferDto>>> GetOffers()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetOffersAsync(customerId));
    }

    [HttpPost("offers")]
    public async Task<ActionResult<SlnLoyaltyPackageOfferDto>> CreateOffer([FromBody] SlnLoyaltyPackageOfferCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.CreateOfferAsync(dto, customerId));
    }

    [HttpPut("offers/{id}")]
    public async Task<ActionResult> UpdateOffer(int id, [FromBody] SlnLoyaltyPackageOfferCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdateOfferAsync(id, dto, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("offers/{id}")]
    public async Task<ActionResult> DeleteOffer(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeleteOfferAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpGet("purchases")]
    public async Task<ActionResult<List<SlnLoyaltyPackagePurchaseDto>>> GetPurchases([FromQuery] int? clientId, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetPurchasesAsync(customerId, clientId, GetBranchId() ?? branchId));
    }

    [HttpPost("sell")]
    public async Task<ActionResult<SlnLoyaltyPackagePurchaseDto>> SellPurchase([FromBody] SlnLoyaltyPackagePurchaseSellDto dto, [FromQuery] int? branchId)
    {
        var personnelId = GetPersonnelId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (purchase, error) = await _factory.SellPurchaseAsync(dto, personnelId, customerId, GetBranchId() ?? branchId);
        return purchase != null ? Ok(purchase) : BadRequest(error);
    }

    [HttpPost("redeem")]
    public async Task<ActionResult> RedeemSession([FromBody] SlnLoyaltyPackageRedeemDto dto, [FromQuery] int? branchId)
    {
        var personnelId = GetPersonnelId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.RedeemSessionAsync(dto, personnelId, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpGet("redemptions")]
    public async Task<ActionResult<List<SlnLoyaltyPackageRedemptionDto>>> GetRedemptions([FromQuery] int? purchaseId, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetRedemptionHistoryAsync(customerId, purchaseId, GetBranchId() ?? branchId));
    }

    [HttpPost("usable")]
    public async Task<ActionResult<List<SlnLoyaltyPackageBenefitDto>>> GetUsablePurchases([FromBody] SlnLoyaltyPackageBenefitCheckDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetUsablePurchasesAsync(customerId, dto.SlnClientId, dto.ServiceIds, GetBranchId() ?? branchId));
    }

    private int GetPersonnelId()
        => int.Parse(User.FindFirst("CustomerPersonnelId")?.Value ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int? GetBranchId()
    {
        var claim = User.FindFirst("BranchId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
