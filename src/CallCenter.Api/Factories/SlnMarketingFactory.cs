using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CallCenter.Api.Factories;

public class SlnMarketingFactory : ISlnMarketingFactory
{
    private readonly ISlnCampaignEntityService _campaigns;
    private readonly ISlnAutoReminderEntityService _reminders;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnMarketingFactory> _logger;

    public SlnMarketingFactory(
        ISlnCampaignEntityService campaigns,
        ISlnAutoReminderEntityService reminders,
        ISlnClientEntityService clients,
        ISlnInvoiceEntityService invoices,
        IUnitOfWork uow,
        ILogger<SlnMarketingFactory> logger)
    {
        _campaigns = campaigns;
        _reminders = reminders;
        _clients = clients;
        _invoices = invoices;
        _uow = uow;
        _logger = logger;
    }

    // ═══ Kampanya ═══

    public async Task<List<SlnCampaignDto>> GetCampaignsAsync(int customerId)
    {
        var campaigns = await _campaigns.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return campaigns.Select(MapCampaignToDto).ToList();
    }

    public async Task<SlnCampaignDto?> GetCampaignAsync(int campaignId, int customerId)
    {
        var campaign = await _campaigns.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.CustomerId == customerId);

        return campaign != null ? MapCampaignToDto(campaign) : null;
    }

    public async Task<SlnCampaignDto> CreateCampaignAsync(SlnCampaignCreateDto dto, int customerId)
    {
        var campaign = new SlnCampaign
        {
            CustomerId = customerId,
            Name = dto.Name,
            MessageTemplate = dto.MessageTemplate,
            SegmentFilter = dto.SegmentFilter,
            ScheduledAt = dto.ScheduledAt,
            StatusId = dto.ScheduledAt.HasValue ? 2 : 1 // Scheduled or Draft
        };

        // Segment preview ile alici sayisi hesapla
        var preview = await GetSegmentPreviewAsync(dto.SegmentFilter, customerId);
        campaign.TotalRecipients = preview.MatchingClients;

        _campaigns.Add(campaign);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni kampanya olusturuldu: {CampaignId} - {Name}", campaign.Id, campaign.Name);
        return MapCampaignToDto(campaign);
    }

    public async Task<(bool Success, string? Error)> UpdateCampaignAsync(int campaignId, SlnCampaignUpdateDto dto, int customerId)
    {
        var campaign = await _campaigns.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.CustomerId == customerId);

        if (campaign == null) return (false, "Kampanya bulunamadi");
        if (campaign.StatusId >= 3) return (false, "Gonderim baslamis kampanya duzenlenemez");

        campaign.Name = dto.Name;
        campaign.MessageTemplate = dto.MessageTemplate;
        campaign.SegmentFilter = dto.SegmentFilter;
        campaign.ScheduledAt = dto.ScheduledAt;
        campaign.StatusId = dto.ScheduledAt.HasValue ? 2 : 1;

        var preview = await GetSegmentPreviewAsync(dto.SegmentFilter, customerId);
        campaign.TotalRecipients = preview.MatchingClients;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCampaignAsync(int campaignId, int customerId)
    {
        var campaign = await _campaigns.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.CustomerId == customerId);

        if (campaign == null) return (false, "Kampanya bulunamadi");

        _campaigns.Remove(campaign);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Kampanya silindi: {CampaignId}", campaignId);
        return (true, null);
    }

    public async Task<SlnSegmentPreviewDto> GetSegmentPreviewAsync(string? segmentFilter, int customerId)
    {
        var query = _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId);

        if (!string.IsNullOrEmpty(segmentFilter))
        {
            try
            {
                var filter = JsonSerializer.Deserialize<SegmentFilterModel>(segmentFilter,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (filter != null)
                {
                    if (filter.GenderId.HasValue)
                        query = query.Where(c => c.GenderId == filter.GenderId.Value);

                    if (!string.IsNullOrEmpty(filter.City))
                        query = query.Where(c => c.City != null && c.City.ToLower().Contains(filter.City.ToLower()));

                    if (filter.MinAge.HasValue)
                    {
                        var maxBirthDate = DateTime.UtcNow.AddYears(-filter.MinAge.Value);
                        query = query.Where(c => c.BirthDate.HasValue && c.BirthDate.Value <= maxBirthDate);
                    }

                    if (filter.MaxAge.HasValue)
                    {
                        var minBirthDate = DateTime.UtcNow.AddYears(-filter.MaxAge.Value - 1);
                        query = query.Where(c => c.BirthDate.HasValue && c.BirthDate.Value >= minBirthDate);
                    }

                    if (filter.LastVisitDays.HasValue)
                    {
                        var sinceDate = DateTime.UtcNow.AddDays(-filter.LastVisitDays.Value);
                        var clientIds = await _invoices.GetAllQueryable()
                            .Where(i => i.CustomerId == customerId && i.StatusId != 3 && i.InvoiceDate >= sinceDate)
                            .Select(i => i.SlnClientId)
                            .Distinct()
                            .ToListAsync();
                        query = query.Where(c => clientIds.Contains(c.Id));
                    }

                    if (filter.MinSpent.HasValue)
                    {
                        var spentClients = await _invoices.GetAllQueryable()
                            .Where(i => i.CustomerId == customerId && i.StatusId != 3)
                            .GroupBy(i => i.SlnClientId)
                            .Where(g => g.Sum(i => i.NetAmount) >= filter.MinSpent.Value)
                            .Select(g => g.Key)
                            .ToListAsync();
                        query = query.Where(c => spentClients.Contains(c.Id));
                    }
                }
            }
            catch (JsonException)
            {
                // Gecersiz JSON filtresi - tum musterileri say
            }
        }

        var count = await query.CountAsync();
        return new SlnSegmentPreviewDto { MatchingClients = count };
    }

    public async Task<(bool Success, string? Error)> SendCampaignAsync(int campaignId, int customerId)
    {
        var campaign = await _campaigns.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.CustomerId == customerId);

        if (campaign == null) return (false, "Kampanya bulunamadi");
        if (campaign.StatusId >= 3) return (false, "Kampanya zaten gonderilmis");

        // Gonderim simule et (gercek SMS entegrasyonu sonra eklenecek)
        campaign.StatusId = 4; // Completed
        campaign.SentAt = DateTime.UtcNow;
        campaign.SentCount = campaign.TotalRecipients;

        await _uow.SaveChangesAsync();

        _logger.LogInformation("Kampanya gonderildi: {CampaignId} - {SentCount} alici", campaignId, campaign.SentCount);
        return (true, null);
    }

    // ═══ Oto-Hatirlatma ═══

    public async Task<List<SlnAutoReminderDto>> GetRemindersAsync(int customerId)
    {
        var reminders = await _reminders.GetAllQueryable()
            .Where(r => r.CustomerId == customerId)
            .OrderBy(r => r.ReminderTypeId)
            .ToListAsync();

        return reminders.Select(MapReminderToDto).ToList();
    }

    public async Task<SlnAutoReminderDto> CreateReminderAsync(SlnAutoReminderCreateDto dto, int customerId)
    {
        var reminder = new SlnAutoReminder
        {
            CustomerId = customerId,
            ReminderTypeId = dto.ReminderTypeId,
            MessageTemplate = dto.MessageTemplate,
            DaysBefore = dto.DaysBefore,
            InactiveDaysThreshold = dto.InactiveDaysThreshold,
            IsActive = dto.IsActive
        };

        _reminders.Add(reminder);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni oto-hatirlatma olusturuldu: {ReminderId}", reminder.Id);
        return MapReminderToDto(reminder);
    }

    public async Task<(bool Success, string? Error)> UpdateReminderAsync(int reminderId, SlnAutoReminderUpdateDto dto, int customerId)
    {
        var reminder = await _reminders.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == reminderId && r.CustomerId == customerId);

        if (reminder == null) return (false, "Hatirlatma bulunamadi");

        reminder.ReminderTypeId = dto.ReminderTypeId;
        reminder.MessageTemplate = dto.MessageTemplate;
        reminder.DaysBefore = dto.DaysBefore;
        reminder.InactiveDaysThreshold = dto.InactiveDaysThreshold;
        reminder.IsActive = dto.IsActive;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteReminderAsync(int reminderId, int customerId)
    {
        var reminder = await _reminders.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == reminderId && r.CustomerId == customerId);

        if (reminder == null) return (false, "Hatirlatma bulunamadi");

        _reminders.Remove(reminder);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleReminderAsync(int reminderId, int customerId)
    {
        var reminder = await _reminders.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == reminderId && r.CustomerId == customerId);

        if (reminder == null) return (false, "Hatirlatma bulunamadi");

        reminder.IsActive = !reminder.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ Mappers ═══

    private static SlnCampaignDto MapCampaignToDto(SlnCampaign c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        MessageTemplate = c.MessageTemplate,
        SegmentFilter = c.SegmentFilter,
        ScheduledAt = c.ScheduledAt,
        SentAt = c.SentAt,
        TotalRecipients = c.TotalRecipients,
        SentCount = c.SentCount,
        StatusId = c.StatusId,
        CreatedAt = c.CreatedAt
    };

    private static readonly Dictionary<int, string> ReminderTypeNames = new()
    {
        { 1, "Dogum Gunu" },
        { 2, "Yildonumu" },
        { 3, "Randevu Hatirlatma" },
        { 4, "Pasif Musteri" }
    };

    private static SlnAutoReminderDto MapReminderToDto(SlnAutoReminder r) => new()
    {
        Id = r.Id,
        ReminderTypeId = r.ReminderTypeId,
        ReminderTypeName = ReminderTypeNames.GetValueOrDefault(r.ReminderTypeId, "Bilinmiyor"),
        MessageTemplate = r.MessageTemplate,
        DaysBefore = r.DaysBefore,
        InactiveDaysThreshold = r.InactiveDaysThreshold,
        IsActive = r.IsActive,
        CreatedAt = r.CreatedAt
    };

    /// <summary>Segment filtre modeli (JSON deserialize)</summary>
    private class SegmentFilterModel
    {
        public int? GenderId { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public string? City { get; set; }
        public int? LastVisitDays { get; set; }
        public decimal? MinSpent { get; set; }
        public string? ServiceName { get; set; }
    }
}
