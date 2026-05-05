using System.Text.Json;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnWinbackFactory : ISlnWinbackFactory
{
    private readonly ISlnWinbackRuleEntityService _winbackRuleEs;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly ISlnMarketingFactory _marketingFactory;
    private readonly IUnitOfWork _uow;

    public SlnWinbackFactory(
        ISlnWinbackRuleEntityService winbackRuleEs,
        ISlnClientEntityService clients,
        ISlnInvoiceEntityService invoices,
        ISlnMarketingFactory marketingFactory,
        IUnitOfWork uow)
    {
        _winbackRuleEs = winbackRuleEs;
        _clients = clients;
        _invoices = invoices;
        _marketingFactory = marketingFactory;
        _uow = uow;
    }

    public async Task<List<SlnWinbackRuleDto>> GetRulesAsync(int customerId)
    {
        return await _winbackRuleEs.GetAllQueryable()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    public async Task<SlnWinbackRuleDto?> GetRuleAsync(int id, int customerId)
    {
        var rule = await _winbackRuleEs.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        return rule != null ? MapToDto(rule) : null;
    }

    public async Task<SlnWinbackRuleDto> CreateRuleAsync(SlnWinbackRuleCreateDto dto, int customerId)
    {
        var rule = new SlnWinbackRule
        {
            CustomerId = customerId,
            Name = dto.Name,
            InactiveDays = dto.InactiveDays,
            ChannelId = dto.ChannelId,
            MessageTemplate = dto.MessageTemplate,
            DiscountPercent = dto.DiscountPercent,
            IsActive = dto.IsActive
        };
        _winbackRuleEs.Add(rule);
        await _uow.SaveChangesAsync();
        return MapToDto(rule);
    }

    public async Task<(bool Success, string? Error)> UpdateRuleAsync(int id, SlnWinbackRuleUpdateDto dto, int customerId)
    {
        var rule = await _winbackRuleEs.GetAllQueryable().FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        if (rule == null) return (false, "Kural bulunamadi");

        rule.Name = dto.Name;
        rule.InactiveDays = dto.InactiveDays;
        rule.ChannelId = dto.ChannelId;
        rule.MessageTemplate = dto.MessageTemplate;
        rule.DiscountPercent = dto.DiscountPercent;
        rule.IsActive = dto.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteRuleAsync(int id, int customerId)
    {
        var rule = await _winbackRuleEs.GetAllQueryable().FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        if (rule == null) return (false, "Kural bulunamadi");

        _winbackRuleEs.Remove(rule);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleRuleAsync(int id, int customerId)
    {
        var rule = await _winbackRuleEs.GetAllQueryable().FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        if (rule == null) return (false, "Kural bulunamadi");

        rule.IsActive = !rule.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<SlnWinbackPreviewDto?> GetPreviewAsync(int id, int customerId)
    {
        var rule = await _winbackRuleEs.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        if (rule == null) return null;

        var candidates = await GetCandidatesAsync(rule, customerId);
        return new SlnWinbackPreviewDto
        {
            RuleId = rule.Id,
            RuleName = rule.Name,
            InactiveDays = rule.InactiveDays,
            EligibleClients = candidates.Count,
            SmsReachableClients = candidates.Count(c => !string.IsNullOrWhiteSpace(c.Phone)),
            EmailReachableClients = candidates.Count(c => !string.IsNullOrWhiteSpace(c.Email)),
            MissingContactCount = candidates.Count(c => string.IsNullOrWhiteSpace(c.Phone) && string.IsNullOrWhiteSpace(c.Email)),
            DiscountPercent = rule.DiscountPercent ?? 0,
            MessagePreview = BuildMessage(rule),
            Candidates = candidates.Take(50).ToList()
        };
    }

    public async Task<(SlnCampaignDto? Campaign, string? Error)> CreateCampaignFromRuleAsync(int id, int customerId)
    {
        var rule = await _winbackRuleEs.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        if (rule == null) return (null, "Kural bulunamadi");
        if (!rule.IsActive) return (null, "Pasif kuraldan kampanya olusturulamaz");

        var preview = await GetPreviewAsync(id, customerId);
        if (preview == null) return (null, "Kural bulunamadi");
        if (preview.EligibleClients == 0) return (null, "Bu kural icin uygun pasif musteri yok");

        var segmentFilter = JsonSerializer.Serialize(new { inactiveDays = rule.InactiveDays });
        var campaign = await _marketingFactory.CreateCampaignAsync(new SlnCampaignCreateDto
        {
            Name = $"Winback - {rule.Name} - {DateTime.UtcNow:yyyyMMdd}",
            MessageTemplate = BuildMessage(rule),
            SegmentFilter = segmentFilter
        }, customerId);

        return (campaign, null);
    }

    private async Task<List<SlnWinbackCandidateDto>> GetCandidatesAsync(SlnWinbackRule rule, int customerId)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-rule.InactiveDays);
        var clients = await _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId && c.IsActive && !c.IsBlacklisted && c.CreatedAt <= cutoff)
            .OrderBy(c => c.FullName)
            .ToListAsync();

        if (clients.Count == 0)
            return [];

        var clientIds = clients.Select(c => c.Id).ToList();
        var lastVisits = await _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId
                && i.SlnClientId.HasValue
                && clientIds.Contains(i.SlnClientId.Value)
                && i.StatusId != 3)
            .GroupBy(i => i.SlnClientId!.Value)
            .Select(g => new { ClientId = g.Key, LastVisitAt = g.Max(i => i.InvoiceDate) })
            .ToDictionaryAsync(x => x.ClientId, x => x.LastVisitAt);

        return clients
            .Select(c =>
            {
                lastVisits.TryGetValue(c.Id, out var lastVisitAt);
                var referenceDate = lastVisitAt == default ? c.CreatedAt : lastVisitAt;
                return new { Client = c, LastVisitAt = lastVisitAt == default ? (DateTime?)null : lastVisitAt, Days = (int)Math.Floor((now - referenceDate).TotalDays) };
            })
            .Where(x => x.Days >= rule.InactiveDays)
            .OrderByDescending(x => x.Days)
            .ThenBy(x => x.Client.FullName)
            .Select(x => new SlnWinbackCandidateDto
            {
                ClientId = x.Client.Id,
                ClientName = x.Client.FullName,
                Phone = x.Client.Phone,
                Email = x.Client.Email,
                LastVisitAt = x.LastVisitAt,
                InactiveDays = x.Days
            })
            .ToList();
    }

    private static string BuildMessage(SlnWinbackRule rule)
    {
        var discount = rule.DiscountPercent.HasValue && rule.DiscountPercent.Value > 0
            ? $"%{rule.DiscountPercent.Value}"
            : "";

        return (rule.MessageTemplate ?? string.Empty)
            .Replace("{gun}", rule.InactiveDays.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{indirim}", discount, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static SlnWinbackRuleDto MapToDto(SlnWinbackRule r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        InactiveDays = r.InactiveDays,
        ChannelId = r.ChannelId,
        MessageTemplate = r.MessageTemplate,
        DiscountPercent = r.DiscountPercent,
        IsActive = r.IsActive,
        CreatedAt = r.CreatedAt
    };
}
