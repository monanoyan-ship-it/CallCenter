using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnEmailCampaignFactory : ISlnEmailCampaignFactory
{
    private readonly ISlnEmailCampaignEntityService _emailCampaignEs;
    private readonly ISlnMarketingFactory _marketingFactory;
    private readonly IUnitOfWork _uow;

    public SlnEmailCampaignFactory(
        ISlnEmailCampaignEntityService emailCampaignEs,
        ISlnMarketingFactory marketingFactory,
        IUnitOfWork uow)
    {
        _emailCampaignEs = emailCampaignEs;
        _marketingFactory = marketingFactory;
        _uow = uow;
    }

    public async Task<List<SlnEmailCampaignDto>> GetCampaignsAsync(int customerId, int? branchId = null)
    {
        var query = SalonBranchScope.ApplyToEmailCampaigns(
            _emailCampaignEs.GetAllQueryable().Where(c => c.CustomerId == customerId),
            branchId);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    public async Task<SlnEmailCampaignDto?> GetCampaignAsync(int id, int customerId, int? branchId = null)
    {
        var campaign = await SalonBranchScope.ApplyToEmailCampaigns(
                _emailCampaignEs.GetAllQueryable().Where(c => c.Id == id && c.CustomerId == customerId),
                branchId)
            .FirstOrDefaultAsync();
        return campaign != null ? MapToDto(campaign) : null;
    }

    public async Task<SlnEmailCampaignDto> CreateCampaignAsync(SlnEmailCampaignCreateDto dto, int customerId, int? branchId = null)
    {
        var campaign = new SlnEmailCampaign
        {
            CustomerId = customerId,
            BranchId = branchId,
            Subject = dto.Subject,
            HtmlBody = dto.HtmlBody,
            SegmentFilter = dto.SegmentFilter,
            ScheduledAt = dto.ScheduledAt,
            StatusId = dto.ScheduledAt.HasValue ? 2 : 1
        };

        var preview = await _marketingFactory.GetSegmentPreviewAsync(dto.SegmentFilter, customerId, branchId);
        campaign.TotalRecipients = preview.EmailReachableClients;

        _emailCampaignEs.Add(campaign);
        await _uow.SaveChangesAsync();
        return MapToDto(campaign);
    }

    public async Task<(bool Success, string? Error)> UpdateCampaignAsync(int id, SlnEmailCampaignUpdateDto dto, int customerId, int? branchId = null)
    {
        var campaign = await SalonBranchScope.ApplyToEmailCampaigns(
                _emailCampaignEs.GetAllQueryable().Where(c => c.Id == id && c.CustomerId == customerId),
                branchId)
            .FirstOrDefaultAsync();
        if (campaign == null) return (false, "Kampanya bulunamadi");
        if (campaign.StatusId >= 3) return (false, "Gonderilmis kampanya duzenlenemez");
        if (campaign.BranchId == null && branchId.HasValue)
        {
            campaign.BranchId = branchId;
        }

        campaign.Subject = dto.Subject;
        campaign.HtmlBody = dto.HtmlBody;
        campaign.SegmentFilter = dto.SegmentFilter;
        campaign.ScheduledAt = dto.ScheduledAt;
        campaign.StatusId = dto.ScheduledAt.HasValue ? 2 : 1;

        var preview = await _marketingFactory.GetSegmentPreviewAsync(dto.SegmentFilter, customerId, branchId);
        campaign.TotalRecipients = preview.EmailReachableClients;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCampaignAsync(int id, int customerId, int? branchId = null)
    {
        var campaign = await SalonBranchScope.ApplyToEmailCampaigns(
                _emailCampaignEs.GetAllQueryable().Where(c => c.Id == id && c.CustomerId == customerId),
                branchId)
            .FirstOrDefaultAsync();
        if (campaign == null) return (false, "Kampanya bulunamadi");

        _emailCampaignEs.Remove(campaign);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public Task<SlnSegmentPreviewDto> GetSegmentPreviewAsync(string? segmentFilter, int customerId, int? branchId = null)
        => _marketingFactory.GetSegmentPreviewAsync(segmentFilter, customerId, branchId);

    public Task<List<SlnSegmentPresetDto>> GetSegmentPresetsAsync(int customerId, int? branchId = null)
        => _marketingFactory.GetSegmentPresetsAsync(customerId, branchId);

    public async Task<(bool Success, string? Error)> SendCampaignAsync(int id, int customerId, int? branchId = null)
    {
        var campaign = await SalonBranchScope.ApplyToEmailCampaigns(
                _emailCampaignEs.GetAllQueryable().Where(c => c.Id == id && c.CustomerId == customerId),
                branchId)
            .FirstOrDefaultAsync();

        if (campaign == null) return (false, "Kampanya bulunamadi");
        if (campaign.StatusId >= 3) return (false, "Kampanya zaten gonderilmis");

        var preview = await _marketingFactory.GetSegmentPreviewAsync(campaign.SegmentFilter, customerId, branchId);

        // Gercek e-posta provider teslimat takibi sonraki asamada; burada gonderim simule edilir.
        campaign.StatusId = 4;
        campaign.SentAt = DateTime.UtcNow;
        campaign.TotalRecipients = preview.EmailReachableClients;
        campaign.SentCount = preview.EmailReachableClients;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static SlnEmailCampaignDto MapToDto(SlnEmailCampaign c) => new()
    {
        Id = c.Id,
        Subject = c.Subject,
        HtmlBody = c.HtmlBody,
        SegmentFilter = c.SegmentFilter,
        ScheduledAt = c.ScheduledAt,
        SentAt = c.SentAt,
        TotalRecipients = c.TotalRecipients,
        SentCount = c.SentCount,
        OpenCount = c.OpenCount,
        ClickCount = c.ClickCount,
        StatusId = c.StatusId,
        BranchId = c.BranchId,
        CreatedAt = c.CreatedAt
    };
}
