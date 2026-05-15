using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-loyalty")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnLoyalty)]
public class SlnLoyaltyController : ControllerBase
{
    private readonly ISlnLoyaltyFactory _loyaltyFactory;

    public SlnLoyaltyController(ISlnLoyaltyFactory loyaltyFactory) => _loyaltyFactory = loyaltyFactory;

    [HttpGet("config")]
    public async Task<ActionResult> GetConfig()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var config = await _loyaltyFactory.GetConfigAsync(customerId);
        return Ok(config ?? new SlnLoyaltyConfigDto { PointsPerTL = 1, PointValue = 0.1m, MinRedeemPoints = 100 });
    }

    [HttpPost("config")]
    public async Task<ActionResult<SlnLoyaltyConfigDto>> SaveConfig([FromBody] SlnLoyaltyConfigUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _loyaltyFactory.SaveConfigAsync(dto, customerId));
    }

    [HttpGet("clients")]
    public async Task<ActionResult<List<SlnClientLoyaltyDto>>> GetClientLoyalties([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _loyaltyFactory.GetClientLoyaltiesAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("clients/{slnClientId}")]
    public async Task<ActionResult<SlnClientLoyaltyDto>> GetClientLoyalty(int slnClientId, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var loyalty = await _loyaltyFactory.GetClientLoyaltyAsync(slnClientId, customerId, GetBranchId() ?? branchId);
        return Ok(loyalty);
    }

    [HttpGet("clients/{slnClientId}/transactions")]
    public async Task<ActionResult<List<SlnLoyaltyTransactionDto>>> GetTransactions(int slnClientId, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _loyaltyFactory.GetTransactionsAsync(slnClientId, customerId, GetBranchId() ?? branchId));
    }

    [HttpPost("redeem")]
    public async Task<ActionResult> RedeemPoints([FromBody] SlnLoyaltyRedeemDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _loyaltyFactory.RedeemPointsAsync(dto, customerId, GetBranchId() ?? branchId);
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
