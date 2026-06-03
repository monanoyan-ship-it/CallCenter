using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services.Email;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnEmailCampaignFactory : ISlnEmailCampaignFactory
{
    private readonly ISlnEmailCampaignEntityService _emailCampaignEs;
    private readonly ISlnMarketingFactory _marketingFactory;
    private readonly IEmailSendService _emailSend;
    private readonly IUnitOfWork _uow;

    public SlnEmailCampaignFactory(
        ISlnEmailCampaignEntityService emailCampaignEs,
        ISlnMarketingFactory marketingFactory,
        IEmailSendService emailSend,
        IUnitOfWork uow)
    {
        _emailCampaignEs = emailCampaignEs;
        _marketingFactory = marketingFactory;
        _emailSend = emailSend;
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

        campaign.BranchId = branchId;
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

        var recipients = await _marketingFactory.GetSegmentRecipientsAsync(campaign.SegmentFilter, customerId, branchId);
        if (recipients.Count == 0)
            return (false, "E-posta adresi olan uygun müşteri bulunamadı.");

        var sentCount = 0;
        var firstError = string.Empty;
        foreach (var recipient in recipients)
        {
            var result = await _emailSend.SendAsync(new EmailSendRequest
            {
                CustomerId = customerId,
                ToAddress = recipient.Email!,
                ToName = recipient.FullName,
                Subject = ReplaceClientPlaceholders(campaign.Subject, recipient),
                HtmlBody = ReplaceClientPlaceholders(campaign.HtmlBody, recipient)
            });

            if (result.Success)
            {
                sentCount++;
            }
            else if (string.IsNullOrWhiteSpace(firstError))
            {
                firstError = result.Error ?? "Gönderim başarısız.";
            }
        }

        if (sentCount > 0)
        {
            campaign.StatusId = 4;
            campaign.SentAt = DateTime.UtcNow;
        }
        campaign.TotalRecipients = recipients.Count;
        campaign.SentCount = sentCount;

        await _uow.SaveChangesAsync();
        if (sentCount == recipients.Count)
            return (true, null);

        return (false, $"{sentCount}/{recipients.Count} e-posta gönderildi. İlk hata: {firstError}");
    }

    private static string ReplaceClientPlaceholders(string text, SlnSegmentRecipientDto recipient)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MusteriAdi"] = recipient.FullName,
            ["MüşteriAdı"] = recipient.FullName,
            ["ClientName"] = recipient.FullName,
            ["ClientFullName"] = recipient.FullName,
            ["FullName"] = recipient.FullName,
            ["Email"] = recipient.Email ?? string.Empty,
            ["Phone"] = recipient.Phone ?? string.Empty,
            ["Telefon"] = recipient.Phone ?? string.Empty
        };

        foreach (var (key, value) in values)
            text = text.Replace($"{{{{{key}}}}}", value);

        return text;
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
