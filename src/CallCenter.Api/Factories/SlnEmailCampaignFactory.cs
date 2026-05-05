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

    public async Task<List<SlnEmailCampaignDto>> GetCampaignsAsync(int customerId)
    {
        return await _emailCampaignEs.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    public async Task<SlnEmailCampaignDto?> GetCampaignAsync(int id, int customerId)
    {
        var campaign = await _emailCampaignEs.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);
        return campaign != null ? MapToDto(campaign) : null;
    }

    public async Task<SlnEmailCampaignDto> CreateCampaignAsync(SlnEmailCampaignCreateDto dto, int customerId)
    {
        var campaign = new SlnEmailCampaign
        {
            CustomerId = customerId,
            Subject = dto.Subject,
            HtmlBody = dto.HtmlBody,
            SegmentFilter = dto.SegmentFilter,
            ScheduledAt = dto.ScheduledAt,
            StatusId = dto.ScheduledAt.HasValue ? 2 : 1
        };

        var preview = await _marketingFactory.GetSegmentPreviewAsync(dto.SegmentFilter, customerId);
        campaign.TotalRecipients = preview.EmailReachableClients;

        _emailCampaignEs.Add(campaign);
        await _uow.SaveChangesAsync();
        return MapToDto(campaign);
    }

    public async Task<(bool Success, string? Error)> UpdateCampaignAsync(int id, SlnEmailCampaignUpdateDto dto, int customerId)
    {
        var campaign = await _emailCampaignEs.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);
        if (campaign == null) return (false, "Kampanya bulunamadi");
        if (campaign.StatusId >= 3) return (false, "Gonderilmis kampanya duzenlenemez");

        campaign.Subject = dto.Subject;
        campaign.HtmlBody = dto.HtmlBody;
        campaign.SegmentFilter = dto.SegmentFilter;
        campaign.ScheduledAt = dto.ScheduledAt;
        campaign.StatusId = dto.ScheduledAt.HasValue ? 2 : 1;

        var preview = await _marketingFactory.GetSegmentPreviewAsync(dto.SegmentFilter, customerId);
        campaign.TotalRecipients = preview.EmailReachableClients;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCampaignAsync(int id, int customerId)
    {
        var campaign = await _emailCampaignEs.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);
        if (campaign == null) return (false, "Kampanya bulunamadi");

        _emailCampaignEs.Remove(campaign);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public Task<SlnSegmentPreviewDto> GetSegmentPreviewAsync(string? segmentFilter, int customerId)
        => _marketingFactory.GetSegmentPreviewAsync(segmentFilter, customerId);

    public Task<List<SlnSegmentPresetDto>> GetSegmentPresetsAsync(int customerId)
        => _marketingFactory.GetSegmentPresetsAsync(customerId);

    public async Task<(bool Success, string? Error)> SendCampaignAsync(int id, int customerId)
    {
        var campaign = await _emailCampaignEs.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

        if (campaign == null) return (false, "Kampanya bulunamadi");
        if (campaign.StatusId >= 3) return (false, "Kampanya zaten gonderilmis");

        var preview = await _marketingFactory.GetSegmentPreviewAsync(campaign.SegmentFilter, customerId);

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
        CreatedAt = c.CreatedAt
    };
}
