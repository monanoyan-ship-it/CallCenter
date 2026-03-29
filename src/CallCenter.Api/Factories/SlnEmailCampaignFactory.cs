using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnEmailCampaignFactory : ISlnEmailCampaignFactory
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;

    public SlnEmailCampaignFactory(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<List<SlnEmailCampaignDto>> GetCampaignsAsync(int customerId)
    {
        return await _db.SlnEmailCampaigns
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    public async Task<SlnEmailCampaignDto?> GetCampaignAsync(int id, int customerId)
    {
        var campaign = await _db.SlnEmailCampaigns
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
        _db.SlnEmailCampaigns.Add(campaign);
        await _uow.SaveChangesAsync();
        return MapToDto(campaign);
    }

    public async Task<(bool Success, string? Error)> UpdateCampaignAsync(int id, SlnEmailCampaignUpdateDto dto, int customerId)
    {
        var campaign = await _db.SlnEmailCampaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);
        if (campaign == null) return (false, "Kampanya bulunamadi");
        if (campaign.StatusId >= 3) return (false, "Gonderilmis kampanya duzenlenemez");

        campaign.Subject = dto.Subject;
        campaign.HtmlBody = dto.HtmlBody;
        campaign.SegmentFilter = dto.SegmentFilter;
        campaign.ScheduledAt = dto.ScheduledAt;
        campaign.StatusId = dto.ScheduledAt.HasValue ? 2 : 1;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCampaignAsync(int id, int customerId)
    {
        var campaign = await _db.SlnEmailCampaigns
            .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);
        if (campaign == null) return (false, "Kampanya bulunamadi");

        _db.SlnEmailCampaigns.Remove(campaign);
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
