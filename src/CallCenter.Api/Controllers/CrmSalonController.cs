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
    private readonly ISlnMarketingFactory _marketing;
    private readonly ISlnEmailCampaignFactory _emailCampaigns;
    private readonly ISlnReviewFactory _reviews;
    private readonly ISlnWinbackFactory _winback;

    public CrmSalonController(
        ISlnClientFactory clients,
        ISlnGiftCardFactory giftCards,
        ISlnMembershipFactory memberships,
        ISlnLoyaltyFactory loyalty,
        ISlnMarketingFactory marketing,
        ISlnEmailCampaignFactory emailCampaigns,
        ISlnReviewFactory reviews,
        ISlnWinbackFactory winback)
    {
        _clients = clients;
        _giftCards = giftCards;
        _memberships = memberships;
        _loyalty = loyalty;
        _marketing = marketing;
        _emailCampaigns = emailCampaigns;
        _reviews = reviews;
        _winback = winback;
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

    [HttpGet("campaigns")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult<List<SlnCampaignDto>>> GetCampaigns([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _marketing.GetCampaignsAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("campaigns/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult<SlnCampaignDto>> GetCampaign(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var campaign = await _marketing.GetCampaignAsync(id, customerId, GetBranchId() ?? branchId);
        return campaign != null ? Ok(campaign) : NotFound();
    }

    [HttpPost("campaigns")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult<SlnCampaignDto>> CreateCampaign([FromBody] SlnCampaignCreateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = true)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        return Ok(await _marketing.CreateCampaignAsync(dto, customerId, target.BranchId));
    }

    [HttpPut("campaigns/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult> UpdateCampaign(int id, [FromBody] SlnCampaignUpdateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = true)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        var (success, error) = await _marketing.UpdateCampaignAsync(id, dto, customerId, target.BranchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("campaigns/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult> DeleteCampaign(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketing.DeleteCampaignAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPost("campaigns/segment-preview")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult<SlnSegmentPreviewDto>> GetCampaignSegmentPreview([FromBody] string? segmentFilter, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _marketing.GetSegmentPreviewAsync(segmentFilter, customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("campaigns/segment-presets")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult<List<SlnSegmentPresetDto>>> GetCampaignSegmentPresets([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _marketing.GetSegmentPresetsAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpPost("campaigns/{id:int}/send")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult> SendCampaign(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketing.SendCampaignAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpGet("reminders")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult<List<SlnAutoReminderDto>>> GetReminders([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _marketing.GetRemindersAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpPost("reminders")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult<SlnAutoReminderDto>> CreateReminder([FromBody] SlnAutoReminderCreateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = true)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        return Ok(await _marketing.CreateReminderAsync(dto, customerId, target.BranchId));
    }

    [HttpPut("reminders/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult> UpdateReminder(int id, [FromBody] SlnAutoReminderUpdateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = true)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        var (success, error) = await _marketing.UpdateReminderAsync(id, dto, customerId, target.BranchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("reminders/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult> DeleteReminder(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketing.DeleteReminderAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPost("reminders/{id:int}/toggle")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnCampaigns, CrmModules.Ids.SalonCampaigns)]
    public async Task<ActionResult> ToggleReminder(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _marketing.ToggleReminderAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpGet("email-campaigns")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnEmailCampaigns, CrmModules.Ids.SalonEmailCampaigns)]
    public async Task<ActionResult<List<SlnEmailCampaignDto>>> GetEmailCampaigns([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _emailCampaigns.GetCampaignsAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("email-campaigns/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnEmailCampaigns, CrmModules.Ids.SalonEmailCampaigns)]
    public async Task<ActionResult<SlnEmailCampaignDto>> GetEmailCampaign(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var campaign = await _emailCampaigns.GetCampaignAsync(id, customerId, GetBranchId() ?? branchId);
        return campaign != null ? Ok(campaign) : NotFound();
    }

    [HttpPost("email-campaigns")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnEmailCampaigns, CrmModules.Ids.SalonEmailCampaigns)]
    public async Task<ActionResult<SlnEmailCampaignDto>> CreateEmailCampaign([FromBody] SlnEmailCampaignCreateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = true)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        return Ok(await _emailCampaigns.CreateCampaignAsync(dto, customerId, target.BranchId));
    }

    [HttpPut("email-campaigns/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnEmailCampaigns, CrmModules.Ids.SalonEmailCampaigns)]
    public async Task<ActionResult> UpdateEmailCampaign(int id, [FromBody] SlnEmailCampaignUpdateDto dto, [FromQuery] int? branchId, [FromQuery] bool allBranches = true)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var target = ResolveMutationBranchTarget(branchId, allBranches);
        if (target.Error != null) return target.Error;

        var (success, error) = await _emailCampaigns.UpdateCampaignAsync(id, dto, customerId, target.BranchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("email-campaigns/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnEmailCampaigns, CrmModules.Ids.SalonEmailCampaigns)]
    public async Task<ActionResult> DeleteEmailCampaign(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _emailCampaigns.DeleteCampaignAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPost("email-campaigns/segment-preview")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnEmailCampaigns, CrmModules.Ids.SalonEmailCampaigns)]
    public async Task<ActionResult<SlnSegmentPreviewDto>> GetEmailSegmentPreview([FromBody] string? segmentFilter, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _emailCampaigns.GetSegmentPreviewAsync(segmentFilter, customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("email-campaigns/segment-presets")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnEmailCampaigns, CrmModules.Ids.SalonEmailCampaigns)]
    public async Task<ActionResult<List<SlnSegmentPresetDto>>> GetEmailSegmentPresets([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _emailCampaigns.GetSegmentPresetsAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpPost("email-campaigns/{id:int}/send")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnEmailCampaigns, CrmModules.Ids.SalonEmailCampaigns)]
    public async Task<ActionResult> SendEmailCampaign(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _emailCampaigns.SendCampaignAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpGet("reviews")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnReviews, CrmModules.Ids.SalonReviews)]
    public async Task<ActionResult<List<SlnReviewDto>>> GetReviews([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _reviews.GetReviewsAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("reviews/stats")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnReviews, CrmModules.Ids.SalonReviews)]
    public async Task<ActionResult<SlnReviewStatsDto>> GetReviewStats([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _reviews.GetStatsAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpPut("reviews/{id:int}/status/{statusId:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnReviews, CrmModules.Ids.SalonReviews)]
    public async Task<ActionResult> UpdateReviewStatus(int id, int statusId, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _reviews.UpdateStatusAsync(id, statusId, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("reviews/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnReviews, CrmModules.Ids.SalonReviews)]
    public async Task<ActionResult> DeleteReview(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _reviews.DeleteReviewAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpGet("winback")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnWinback, CrmModules.Ids.SalonWinback)]
    public async Task<ActionResult<List<SlnWinbackRuleDto>>> GetWinbackRules([FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _winback.GetRulesAsync(customerId, GetBranchId() ?? branchId));
    }

    [HttpGet("winback/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnWinback, CrmModules.Ids.SalonWinback)]
    public async Task<ActionResult<SlnWinbackRuleDto>> GetWinbackRule(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var rule = await _winback.GetRuleAsync(id, customerId, GetBranchId() ?? branchId);
        return rule != null ? Ok(rule) : NotFound();
    }

    [HttpPost("winback")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnWinback, CrmModules.Ids.SalonWinback)]
    public async Task<ActionResult<SlnWinbackRuleDto>> CreateWinbackRule([FromBody] SlnWinbackRuleCreateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        return Ok(await _winback.CreateRuleAsync(dto, customerId, GetBranchId() ?? branchId));
    }

    [HttpPut("winback/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnWinback, CrmModules.Ids.SalonWinback)]
    public async Task<ActionResult> UpdateWinbackRule(int id, [FromBody] SlnWinbackRuleUpdateDto dto, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _winback.UpdateRuleAsync(id, dto, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("winback/{id:int}")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnWinback, CrmModules.Ids.SalonWinback)]
    public async Task<ActionResult> DeleteWinbackRule(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _winback.DeleteRuleAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPost("winback/{id:int}/toggle")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnWinback, CrmModules.Ids.SalonWinback)]
    public async Task<ActionResult> ToggleWinbackRule(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (success, error) = await _winback.ToggleRuleAsync(id, customerId, GetBranchId() ?? branchId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpGet("winback/{id:int}/preview")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnWinback, CrmModules.Ids.SalonWinback)]
    public async Task<ActionResult<SlnWinbackPreviewDto>> GetWinbackPreview(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var preview = await _winback.GetPreviewAsync(id, customerId, GetBranchId() ?? branchId);
        return preview != null ? Ok(preview) : NotFound();
    }

    [HttpPost("winback/{id:int}/create-campaign")]
    [RequireAnyModule(SalonPortalModules.Ids.SlnWinback, CrmModules.Ids.SalonWinback)]
    public async Task<ActionResult<SlnCampaignDto>> CreateWinbackCampaign(int id, [FromQuery] int? branchId)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var (campaign, error) = await _winback.CreateCampaignFromRuleAsync(id, customerId, GetBranchId() ?? branchId);
        return campaign != null ? Ok(campaign) : BadRequest(new { error });
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
