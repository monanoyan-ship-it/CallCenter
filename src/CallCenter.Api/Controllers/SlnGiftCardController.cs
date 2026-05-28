using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-gift-cards")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnGiftCards)]
public class SlnGiftCardController : ControllerBase
{
    private readonly ISlnGiftCardFactory _giftCardFactory;

    public SlnGiftCardController(ISlnGiftCardFactory giftCardFactory) => _giftCardFactory = giftCardFactory;

    [HttpGet]
    public async Task<ActionResult<List<SlnGiftCardDto>>> GetGiftCards([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _giftCardFactory.GetGiftCardsAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SlnGiftCardDto>> GetGiftCard(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var card = await _giftCardFactory.GetGiftCardAsync(id, customerId, GetBranchId() ?? branchId);
        return card != null ? Ok(card) : NotFound();
    }

    [HttpGet("by-code/{code}")]
    public async Task<ActionResult<SlnGiftCardDto>> GetByCode(string code, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var card = await _giftCardFactory.GetGiftCardByCodeAsync(code, customerId, GetBranchId() ?? branchId);
        return card != null ? Ok(card) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<SlnGiftCardDto>> CreateGiftCard([FromBody] SlnGiftCardCreateDto dto, [FromQuery] int? branchId)
    {
        var personnelId = GetPersonnelId();
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (card, error) = await _giftCardFactory.CreateGiftCardAsync(dto, personnelId, customerId, GetBranchId() ?? branchId);
        return card != null ? Ok(card) : BadRequest(error);
    }

    [HttpPost("redeem")]
    public async Task<ActionResult> RedeemGiftCard([FromBody] SlnGiftCardRedeemDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _giftCardFactory.RedeemGiftCardAsync(dto, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult> Deactivate(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _giftCardFactory.DeactivateGiftCardAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(error);
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
