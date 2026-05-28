using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Filters;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/crm/salon")]
[Authorize]
public class CrmSalonController : ControllerBase
{
    private const string BranchTargetRequiredMessage = "Sube secin veya Tum Subeler secenegini secin";

    private readonly ISlnClientFactory _clients;
    private readonly ISlnGiftCardFactory _giftCards;
    private readonly ISlnMembershipFactory _memberships;
    private readonly ISlnLoyaltyFactory _loyalty;

    public CrmSalonController(
        ISlnClientFactory clients,
        ISlnGiftCardFactory giftCards,
        ISlnMembershipFactory memberships,
        ISlnLoyaltyFactory loyalty)
    {
        _clients = clients;
        _giftCards = giftCards;
        _memberships = memberships;
        _loyalty = loyalty;
    }

    [HttpGet("clients")]
    [RequireModule(SalonPortalModules.Ids.SlnClients)]
    public async Task<ActionResult> GetClients([FromQuery] string? search, [FromQuery] int? branchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _clients.GetClientsAsync(customerId, search, GetBranchId() ?? branchId, page, pageSize));
    }

    [HttpPost("clients")]
    [RequireModule(SalonPortalModules.Ids.SlnClients)]
    public async Task<ActionResult<SlnClientDto>> CreateClient([FromBody] SlnClientCreateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _clients.CreateClientAsync(dto, customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("gift-cards")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnGiftCards, CrmModules.Ids.SalonGiftCards)]
    public async Task<ActionResult<List<SlnGiftCardDto>>> GetGiftCards([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _giftCards.GetGiftCardsAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("gift-cards/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnGiftCards, CrmModules.Ids.SalonGiftCards)]
    public async Task<ActionResult<SlnGiftCardDto>> GetGiftCard(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var card = await _giftCards.GetGiftCardAsync(id, customerId, GetBranchId() ?? branchId);
        return card != null ? Ok(card) : NotFound();
    }

    [HttpPost("gift-cards")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnGiftCards, CrmModules.Ids.SalonGiftCards)]
    public async Task<ActionResult<SlnGiftCardDto>> CreateGiftCard([FromBody] SlnGiftCardCreateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (card, error) = await _giftCards.CreateGiftCardAsync(dto, GetPersonnelId(), customerId, GetBranchId() ?? branchId);
        return card != null ? Ok(card) : BadRequest(new { error });
    }

    [HttpPut("gift-cards/{id:int}/deactivate")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnGiftCards, CrmModules.Ids.SalonGiftCards)]
    public async Task<ActionResult> DeactivateGiftCard(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _giftCards.DeactivateGiftCardAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpGet("memberships/plans")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnMemberships, CrmModules.Ids.SalonMemberships)]
    public async Task<ActionResult<List<SlnMembershipPlanDto>>> GetMembershipPlans([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _memberships.GetPlansAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpPost("memberships/plans")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnMemberships, CrmModules.Ids.SalonMemberships)]
    public async Task<ActionResult<SlnMembershipPlanDto>> CreateMembershipPlan([FromBody] SlnMembershipPlanCreateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = true)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        return Ok(await _memberships.CreatePlanAsync(dto, customerId, target.BranchId));
    }

    [HttpPut("memberships/plans/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnMemberships, CrmModules.Ids.SalonMemberships)]
    public async Task<ActionResult> UpdateMembershipPlan(int id, [FromBody] SlnMembershipPlanCreateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = true)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        var (success, error) = await _memberships.UpdatePlanAsync(id, dto, customerId, target.BranchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("memberships/plans/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnMemberships, CrmModules.Ids.SalonMemberships)]
    public async Task<ActionResult> DeleteMembershipPlan(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _memberships.DeletePlanAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpGet("memberships")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnMemberships, CrmModules.Ids.SalonMemberships)]
    public async Task<ActionResult<List<SlnClientMembershipDto>>> GetMemberships([FromQuery] int? clientId, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _memberships.GetMembershipsAsync(customerId, clientId, GetBranchId() ?? branchId));
    }

    [HttpPost("memberships")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnMemberships, CrmModules.Ids.SalonMemberships)]
    public async Task<ActionResult<SlnClientMembershipDto>> CreateMembership([FromBody] SlnClientMembershipCreateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (membership, error) = await _memberships.CreateMembershipAsync(dto, customerId, GetBranchId() ?? branchId);
        return membership != null ? Ok(membership) : BadRequest(new { error });
    }

    [HttpPut("memberships/{id:int}/freeze")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnMemberships, CrmModules.Ids.SalonMemberships)]
    public async Task<ActionResult> FreezeMembership(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _memberships.FreezeMembershipAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPut("memberships/{id:int}/cancel")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnMemberships, CrmModules.Ids.SalonMemberships)]
    public async Task<ActionResult> CancelMembership(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _memberships.CancelMembershipAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPut("memberships/{id:int}/reactivate")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnMemberships, CrmModules.Ids.SalonMemberships)]
    public async Task<ActionResult> ReactivateMembership(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _memberships.ReactivateMembershipAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpGet("loyalty/config")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnLoyalty, CrmModules.Ids.SalonLoyalty)]
    public async Task<ActionResult<SlnLoyaltyConfigDto>> GetLoyaltyConfig()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _loyalty.GetConfigAsync(customerId) ?? new SlnLoyaltyConfigDto { PointsPerTL = 1, PointValue = 0.1m, MinRedeemPoints = 100 });
    }

    [HttpPost("loyalty/config")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnLoyalty, CrmModules.Ids.SalonLoyalty)]
    public async Task<ActionResult<SlnLoyaltyConfigDto>> SaveLoyaltyConfig([FromBody] SlnLoyaltyConfigUpdateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _loyalty.SaveConfigAsync(dto, customerId));
    }

    [HttpGet("loyalty/clients")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnLoyalty, CrmModules.Ids.SalonLoyalty)]
    public async Task<ActionResult<List<SlnClientLoyaltyDto>>> GetClientLoyalties([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _loyalty.GetClientLoyaltiesAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpPost("loyalty/redeem")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnLoyalty, CrmModules.Ids.SalonLoyalty)]
    public async Task<ActionResult> RedeemLoyaltyPoints([FromBody] SlnLoyaltyRedeemDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _loyalty.RedeemPointsAsync(dto, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    private int GetCustomerId()
        => int.TryParse(User.FindFirst("CustomerId")?.Value, out var id) ? id : 0;

    private int GetPersonnelId()
        => int.TryParse(User.FindFirst("CustomerPersonnelId")?.Value, out var id) ? id : 0;

    private int? GetBranchId()
    {
        var value = User.FindFirst("BranchId")?.Value;
        return int.TryParse(value, out var branchId) && branchId > 0 ? branchId : null;
    }

    private (int? BranchId, ActionResult? Error) ResolveMutationBranchTarget(int? requestedBranchId, bool allBranches)
    {
        var claimBranchId = GetBranchId();
        if (claimBranchId.HasValue) return (claimBranchId.Value, null);
        if (allBranches) return (null, null);
        if (requestedBranchId.HasValue && requestedBranchId.Value > 0) return (requestedBranchId.Value, null);
        return (null, BadRequest(new { error = BranchTargetRequiredMessage }));
    }
}
