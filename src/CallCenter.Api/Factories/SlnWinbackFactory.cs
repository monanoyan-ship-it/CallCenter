using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnWinbackFactory : ISlnWinbackFactory
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;

    public SlnWinbackFactory(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<List<SlnWinbackRuleDto>> GetRulesAsync(int customerId)
    {
        return await _db.SlnWinbackRules
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    public async Task<SlnWinbackRuleDto?> GetRuleAsync(int id, int customerId)
    {
        var rule = await _db.SlnWinbackRules
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
        _db.SlnWinbackRules.Add(rule);
        await _uow.SaveChangesAsync();
        return MapToDto(rule);
    }

    public async Task<(bool Success, string? Error)> UpdateRuleAsync(int id, SlnWinbackRuleUpdateDto dto, int customerId)
    {
        var rule = await _db.SlnWinbackRules.FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
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
        var rule = await _db.SlnWinbackRules.FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        if (rule == null) return (false, "Kural bulunamadi");

        _db.SlnWinbackRules.Remove(rule);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ToggleRuleAsync(int id, int customerId)
    {
        var rule = await _db.SlnWinbackRules.FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customerId);
        if (rule == null) return (false, "Kural bulunamadi");

        rule.IsActive = !rule.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
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
