using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CallCenter.Api.Factories;

public class SlnMarketingFactory : ISlnMarketingFactory
{
    private readonly ISlnCampaignEntityService _campaigns;
    private readonly ISlnAutoReminderEntityService _reminders;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly ISlnClientMembershipEntityService _clientMemberships;
    private readonly ISlnClientPackageEntityService _clientPackages;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnMarketingFactory> _logger;

    private const decimal EstimatedSmsUnitCost = 0.80m;
    private const decimal EstimatedEmailUnitCost = 0.00m;

    private static readonly JsonSerializerOptions SegmentJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SlnMarketingFactory(
        ISlnCampaignEntityService campaigns,
        ISlnAutoReminderEntityService reminders,
        ISlnClientEntityService clients,
        ISlnInvoiceEntityService invoices,
        ISlnClientMembershipEntityService clientMemberships,
        ISlnClientPackageEntityService clientPackages,
        IUnitOfWork uow,
        ILogger<SlnMarketingFactory> logger)
    {
        _campaigns = campaigns;
        _reminders = reminders;
        _clients = clients;
        _invoices = invoices;
        _clientMemberships = clientMemberships;
        _clientPackages = clientPackages;
        _uow = uow;
        _logger = logger;
    }

    // ═══ Kampanya ═══

    public async Task<List<SlnCampaignDto>> GetCampaignsAsync(int customerId, int? branchId = null)
    {
        var query = _campaigns.GetAllQueryable()
            .Where(c => c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToCampaigns(query, branchId);

        var campaigns = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return campaigns.Select(MapCampaignToDto).ToList();
    }

    public async Task<SlnCampaignDto?> GetCampaignAsync(int campaignId, int customerId, int? branchId = null)
    {
        var query = _campaigns.GetAllQueryable()
            .Where(c => c.Id == campaignId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToCampaigns(query, branchId);

        var campaign = await query.FirstOrDefaultAsync();

        return campaign != null ? MapCampaignToDto(campaign) : null;
    }

    public async Task<SlnCampaignDto> CreateCampaignAsync(SlnCampaignCreateDto dto, int customerId, int? branchId = null)
    {
        var campaign = new SlnCampaign
        {
            CustomerId = customerId,
            BranchId = branchId,
            Name = dto.Name,
            MessageTemplate = dto.MessageTemplate,
            SegmentFilter = dto.SegmentFilter,
            ScheduledAt = dto.ScheduledAt,
            StatusId = dto.ScheduledAt.HasValue ? 2 : 1 // Scheduled or Draft
        };

        // Segment preview ile alici sayisi hesapla
        var preview = await GetSegmentPreviewAsync(dto.SegmentFilter, customerId, branchId);
        campaign.TotalRecipients = preview.SmsReachableClients;

        _campaigns.Add(campaign);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni kampanya olusturuldu: {CampaignId} - {Name}", campaign.Id, campaign.Name);
        return MapCampaignToDto(campaign);
    }

    public async Task<(bool Success, string? Error)> UpdateCampaignAsync(int campaignId, SlnCampaignUpdateDto dto, int customerId, int? branchId = null)
    {
        var query = _campaigns.GetAllQueryable()
            .Where(c => c.Id == campaignId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToCampaigns(query, branchId);

        var campaign = await query.FirstOrDefaultAsync();

        if (campaign == null) return (false, "Kampanya bulunamadi");
        if (campaign.StatusId >= 3) return (false, "Gonderim baslamis kampanya duzenlenemez");

        campaign.Name = dto.Name;
        campaign.MessageTemplate = dto.MessageTemplate;
        campaign.SegmentFilter = dto.SegmentFilter;
        campaign.ScheduledAt = dto.ScheduledAt;
        campaign.StatusId = dto.ScheduledAt.HasValue ? 2 : 1;

        var preview = await GetSegmentPreviewAsync(dto.SegmentFilter, customerId, branchId);
        campaign.TotalRecipients = preview.SmsReachableClients;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCampaignAsync(int campaignId, int customerId, int? branchId = null)
    {
        var query = _campaigns.GetAllQueryable()
            .Where(c => c.Id == campaignId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToCampaigns(query, branchId);

        var campaign = await query.FirstOrDefaultAsync();

        if (campaign == null) return (false, "Kampanya bulunamadi");

        _campaigns.Remove(campaign);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Kampanya silindi: {CampaignId}", campaignId);
        return (true, null);
    }

    public async Task<SlnSegmentPreviewDto> GetSegmentPreviewAsync(string? segmentFilter, int customerId, int? branchId = null)
    {
        var now = DateTime.UtcNow;
        var query = _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId && c.IsActive);
        query = SalonBranchScope.ApplyToClients(query, branchId);

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
                        var maxBirthDate = now.AddYears(-filter.MinAge.Value);
                        query = query.Where(c => c.BirthDate.HasValue && c.BirthDate.Value <= maxBirthDate);
                    }

                    if (filter.MaxAge.HasValue)
                    {
                        var minBirthDate = now.AddYears(-filter.MaxAge.Value - 1);
                        query = query.Where(c => c.BirthDate.HasValue && c.BirthDate.Value >= minBirthDate);
                    }

                    if (filter.LastVisitDays.HasValue)
                    {
                        var sinceDate = now.AddDays(-filter.LastVisitDays.Value);
                        var clientIds = await _invoices.GetAllQueryable()
                            .Where(i => i.CustomerId == customerId
                                        && i.StatusId != 3
                                        && i.SlnClientId.HasValue
                                        && i.InvoiceDate >= sinceDate
                                        && (!branchId.HasValue || i.BranchId == branchId.Value))
                            .Select(i => i.SlnClientId!.Value)
                            .Distinct()
                            .ToListAsync();
                        query = query.Where(c => clientIds.Contains(c.Id));
                    }

                    if (filter.InactiveDays.HasValue)
                    {
                        var sinceDate = now.AddDays(-filter.InactiveDays.Value);
                        var recentlyVisitedClientIds = await _invoices.GetAllQueryable()
                            .Where(i => i.CustomerId == customerId
                                        && i.StatusId != 3
                                        && i.SlnClientId.HasValue
                                        && i.InvoiceDate >= sinceDate
                                        && (!branchId.HasValue || i.BranchId == branchId.Value))
                            .Select(i => i.SlnClientId!.Value)
                            .Distinct()
                            .ToListAsync();
                        query = query.Where(c => !recentlyVisitedClientIds.Contains(c.Id));
                    }

                    if (filter.BirthdayInDays.HasValue)
                    {
                        var birthdayClientIds = await GetBirthdayClientIdsAsync(customerId, filter.BirthdayInDays.Value, now, branchId);
                        query = query.Where(c => birthdayClientIds.Contains(c.Id));
                    }

                    if (filter.MinSpent.HasValue)
                    {
                        var spentClients = await _invoices.GetAllQueryable()
                            .Where(i => i.CustomerId == customerId
                                        && i.StatusId != 3
                                        && i.SlnClientId.HasValue
                                        && (!branchId.HasValue || i.BranchId == branchId.Value))
                            .GroupBy(i => i.SlnClientId!.Value)
                            .Where(g => g.Sum(i => i.NetAmount) >= filter.MinSpent.Value)
                            .Select(g => g.Key)
                            .ToListAsync();
                        query = query.Where(c => spentClients.Contains(c.Id));
                    }

                    if (filter.HasActiveMembership.HasValue)
                    {
                        var membershipClientIds = await GetActiveMembershipClientIdsAsync(customerId, now, branchId);
                        query = filter.HasActiveMembership.Value
                            ? query.Where(c => membershipClientIds.Contains(c.Id))
                            : query.Where(c => !membershipClientIds.Contains(c.Id));
                    }

                    if (filter.HasActivePackage.HasValue)
                    {
                        var packageClientIds = await GetActivePackageClientIdsAsync(customerId, now, branchId);
                        query = filter.HasActivePackage.Value
                            ? query.Where(c => packageClientIds.Contains(c.Id))
                            : query.Where(c => !packageClientIds.Contains(c.Id));
                    }
                }
            }
            catch (JsonException)
            {
                // Gecersiz JSON filtresi - tum musterileri say
            }
        }

        var clients = await query
            .Select(c => new
            {
                c.Id,
                c.Phone,
                c.Email,
                c.IsBlacklisted
            })
            .ToListAsync();

        var eligibleClients = clients.Where(c => !c.IsBlacklisted).ToList();
        var smsReachable = eligibleClients.Count(c => HasContactValue(c.Phone));
        var emailReachable = eligibleClients.Count(c => HasContactValue(c.Email));

        return new SlnSegmentPreviewDto
        {
            MatchingClients = clients.Count,
            SmsReachableClients = smsReachable,
            EmailReachableClients = emailReachable,
            MissingPhoneCount = eligibleClients.Count(c => !HasContactValue(c.Phone)),
            MissingEmailCount = eligibleClients.Count(c => !HasContactValue(c.Email)),
            ExcludedByOptOutCount = clients.Count(c => c.IsBlacklisted),
            EstimatedSmsCost = Math.Round(smsReachable * EstimatedSmsUnitCost, 2, MidpointRounding.AwayFromZero),
            EstimatedEmailCost = Math.Round(emailReachable * EstimatedEmailUnitCost, 2, MidpointRounding.AwayFromZero)
        };
    }

    public async Task<List<SlnSegmentPresetDto>> GetSegmentPresetsAsync(int customerId, int? branchId = null)
    {
        var definitions = new[]
        {
            new SegmentPresetDefinition("all", "Tum aktif musteriler", "Aktif durumdaki tum musteri kayitlari.", null),
            new SegmentPresetDefinition("birthday-30", "Dogum gunu yaklasanlar", "Onumuzdeki 30 gun icinde dogum gunu olan musteriler.", new SegmentFilterModel { BirthdayInDays = 30 }),
            new SegmentPresetDefinition("inactive-60", "60 gundur gelmeyenler", "Son 60 gunde adisyonu olmayan aktif musteriler.", new SegmentFilterModel { InactiveDays = 60 }),
            new SegmentPresetDefinition("high-spend", "Yuksek harcama", "Toplam harcamasi 3.000 TL ve uzeri olan musteriler.", new SegmentFilterModel { MinSpent = 3000 }),
            new SegmentPresetDefinition("active-membership", "Aktif uyeligi olanlar", "Aktif salon uyeligi devam eden musteriler.", new SegmentFilterModel { HasActiveMembership = true }),
            new SegmentPresetDefinition("active-package", "Aktif paketi olanlar", "Kalan seansi olan aktif paket sahibi musteriler.", new SegmentFilterModel { HasActivePackage = true }),
            new SegmentPresetDefinition("no-membership-package", "Uyelik veya paketi olmayanlar", "Aktif uyeligi ve aktif paketi bulunmayan musteriler.", new SegmentFilterModel { HasActiveMembership = false, HasActivePackage = false })
        };

        var result = new List<SlnSegmentPresetDto>();
        foreach (var definition in definitions)
        {
            var filterJson = SerializeSegmentFilter(definition.Filter);
            var preview = await GetSegmentPreviewAsync(filterJson, customerId, branchId);
            result.Add(new SlnSegmentPresetDto
            {
                Key = definition.Key,
                Name = definition.Name,
                Description = definition.Description,
                FilterJson = filterJson,
                MatchingClients = preview.MatchingClients,
                SmsReachableClients = preview.SmsReachableClients,
                EmailReachableClients = preview.EmailReachableClients,
                MissingPhoneCount = preview.MissingPhoneCount,
                MissingEmailCount = preview.MissingEmailCount,
                ExcludedByOptOutCount = preview.ExcludedByOptOutCount,
                EstimatedSmsCost = preview.EstimatedSmsCost,
                EstimatedEmailCost = preview.EstimatedEmailCost
            });
        }

        return result;
    }

    public async Task<(bool Success, string? Error)> SendCampaignAsync(int campaignId, int customerId, int? branchId = null)
    {
        var query = _campaigns.GetAllQueryable()
            .Where(c => c.Id == campaignId && c.CustomerId == customerId);
        query = SalonBranchScope.ApplyToCampaigns(query, branchId);

        var campaign = await query.FirstOrDefaultAsync();

        if (campaign == null) return (false, "Kampanya bulunamadi");
        if (campaign.StatusId >= 3) return (false, "Kampanya zaten gonderilmis");

        var preview = await GetSegmentPreviewAsync(campaign.SegmentFilter, customerId, branchId);

        // Gonderim simule et (gercek SMS entegrasyonu sonra eklenecek)
        campaign.StatusId = 4; // Completed
        campaign.SentAt = DateTime.UtcNow;
        campaign.TotalRecipients = preview.SmsReachableClients;
        campaign.SentCount = preview.SmsReachableClients;

        await _uow.SaveChangesAsync();

        _logger.LogInformation("Kampanya gonderildi: {CampaignId} - {SentCount} alici", campaignId, campaign.SentCount);
        return (true, null);
    }

    // ═══ Oto-Hatirlatma ═══

    public async Task<List<SlnAutoReminderDto>> GetRemindersAsync(int customerId, int? branchId = null)
    {
        var query = _reminders.GetAllQueryable()
            .Where(r => r.CustomerId == customerId);
        query = SalonBranchScope.ApplyToReminders(query, branchId);

        var reminders = await query
            .OrderBy(r => r.ReminderTypeId)
            .ToListAsync();

        return reminders.Select(MapReminderToDto).ToList();
    }

    public async Task<SlnAutoReminderDto> CreateReminderAsync(SlnAutoReminderCreateDto dto, int customerId, int? branchId = null)
    {
        var reminder = new SlnAutoReminder
        {
            CustomerId = customerId,
            BranchId = branchId,
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

    public async Task<(bool Success, string? Error)> UpdateReminderAsync(int reminderId, SlnAutoReminderUpdateDto dto, int customerId, int? branchId = null)
    {
        var query = _reminders.GetAllQueryable()
            .Where(r => r.Id == reminderId && r.CustomerId == customerId);
        query = SalonBranchScope.ApplyToReminders(query, branchId);

        var reminder = await query.FirstOrDefaultAsync();

        if (reminder == null) return (false, "Hatirlatma bulunamadi");

        reminder.ReminderTypeId = dto.ReminderTypeId;
        reminder.MessageTemplate = dto.MessageTemplate;
        reminder.DaysBefore = dto.DaysBefore;
        reminder.InactiveDaysThreshold = dto.InactiveDaysThreshold;
        reminder.IsActive = dto.IsActive;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteReminderAsync(int reminderId, int customerId, int? branchId = null)
    {
        var query = _reminders.GetAllQueryable()
            .Where(r => r.Id == reminderId && r.CustomerId == customerId);
        query = SalonBranchScope.ApplyToReminders(query, branchId);

        var reminder = await query.FirstOrDefaultAsync();

        if (reminder == null) return (false, "Hatirlatma bulunamadi");

        _reminders.Remove(reminder);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleReminderAsync(int reminderId, int customerId, int? branchId = null)
    {
        var query = _reminders.GetAllQueryable()
            .Where(r => r.Id == reminderId && r.CustomerId == customerId);
        query = SalonBranchScope.ApplyToReminders(query, branchId);

        var reminder = await query.FirstOrDefaultAsync();

        if (reminder == null) return (false, "Hatirlatma bulunamadi");

        reminder.IsActive = !reminder.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ Mappers ═══

    private static SlnCampaignDto MapCampaignToDto(SlnCampaign c) => new()
    {
        Id = c.Id,
        BranchId = c.BranchId,
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
        BranchId = r.BranchId,
        ReminderTypeId = r.ReminderTypeId,
        ReminderTypeName = ReminderTypeNames.GetValueOrDefault(r.ReminderTypeId, "Bilinmiyor"),
        MessageTemplate = r.MessageTemplate,
        DaysBefore = r.DaysBefore,
        InactiveDaysThreshold = r.InactiveDaysThreshold,
        IsActive = r.IsActive,
        CreatedAt = r.CreatedAt
    };

    private async Task<List<int>> GetBirthdayClientIdsAsync(int customerId, int daysAhead, DateTime now, int? branchId = null)
    {
        var start = now.Date;
        var end = start.AddDays(Math.Max(daysAhead, 0));
        var query = _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId && c.IsActive && c.BirthDate.HasValue);
        query = SalonBranchScope.ApplyToClients(query, branchId);

        var clients = await query
            .Select(c => new { c.Id, c.BirthDate })
            .ToListAsync();

        return clients
            .Where(c => IsBirthdayInRange(c.BirthDate!.Value, start, end))
            .Select(c => c.Id)
            .ToList();
    }

    private async Task<List<int>> GetActiveMembershipClientIdsAsync(int customerId, DateTime now, int? branchId = null)
    {
        var query = _clientMemberships.GetAllQueryable()
            .Where(m => m.CustomerId == customerId
                && m.StatusId == 1
                && m.StartDate <= now
                && (!m.EndDate.HasValue || m.EndDate.Value >= now));
        query = SalonBranchScope.ApplyToMemberships(query, branchId);

        return await query
            .Select(m => m.SlnClientId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<List<int>> GetActivePackageClientIdsAsync(int customerId, DateTime now, int? branchId = null)
    {
        var query = _clientPackages.GetAllQueryable()
            .Where(p => p.CustomerId == customerId
                && p.SlnClientId.HasValue
                && p.IsActive
                && p.RemainingSessions > 0
                && (!p.ExpiresAt.HasValue || p.ExpiresAt.Value >= now));
        query = SalonBranchScope.ApplyToClientPackages(query, branchId);

        return await query
            .Select(p => p.SlnClientId!.Value)
            .Distinct()
            .ToListAsync();
    }

    private static bool IsBirthdayInRange(DateTime birthDate, DateTime start, DateTime end)
    {
        var nextBirthday = BuildBirthday(start.Year, birthDate);
        if (nextBirthday < start)
            nextBirthday = BuildBirthday(start.Year + 1, birthDate);

        return nextBirthday <= end;
    }

    private static DateTime BuildBirthday(int year, DateTime birthDate)
    {
        if (birthDate.Month == 2 && birthDate.Day == 29 && !DateTime.IsLeapYear(year))
            return new DateTime(year, 2, 28);

        return new DateTime(year, birthDate.Month, birthDate.Day);
    }

    private static string? SerializeSegmentFilter(SegmentFilterModel? filter)
        => filter == null ? null : JsonSerializer.Serialize(filter, SegmentJsonOptions);

    private static bool HasContactValue(string? value)
        => !string.IsNullOrWhiteSpace(value);

    private sealed record SegmentPresetDefinition(string Key, string Name, string Description, SegmentFilterModel? Filter);

    /// <summary>Segment filtre modeli (JSON deserialize)</summary>
    private class SegmentFilterModel
    {
        public int? GenderId { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public string? City { get; set; }
        public int? LastVisitDays { get; set; }
        public int? InactiveDays { get; set; }
        public int? BirthdayInDays { get; set; }
        public decimal? MinSpent { get; set; }
        public bool? HasActiveMembership { get; set; }
        public bool? HasActivePackage { get; set; }
        public string? ServiceName { get; set; }
    }
}
