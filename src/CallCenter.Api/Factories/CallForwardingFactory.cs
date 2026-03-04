using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class CallForwardingFactory : ICallForwardingFactory
{
    private readonly ICallForwardingRuleEntityService _ruleEs;
    private readonly IUserEntityService _userEs;
    private readonly IUnitOfWork _uow;

    public CallForwardingFactory(ICallForwardingRuleEntityService ruleEs, IUserEntityService userEs, IUnitOfWork uow)
    {
        _ruleEs = ruleEs;
        _userEs = userEs;
        _uow = uow;
    }

    public async Task<List<CallForwardingRuleDto>> GetByUserIdAsync(int userId)
    {
        return await _ruleEs.GetAllQueryable()
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Priority)
            .Select(f => MapToDto(f))
            .ToListAsync();
    }

    public async Task<List<CallForwardingRuleDto>> GetAllAsync(int? customerId, int? userId)
    {
        var query = _ruleEs.GetAllQueryable()
            .Include(f => f.User)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(f => f.CustomerId == customerId.Value);

        if (userId.HasValue)
            query = query.Where(f => f.UserId == userId.Value);

        return await query
            .OrderBy(f => f.UserId)
            .ThenBy(f => f.Priority)
            .Select(f => new CallForwardingRuleDto
            {
                Id = f.Id,
                Uid = f.Uid,
                UserId = f.UserId,
                UserName = f.User != null ? f.User.FullName : null,
                CustomerId = f.CustomerId,
                ForwardType = f.ForwardType,
                ForwardTypeName = ForwardTypes.GetName(f.ForwardType),
                Destination = f.Destination,
                IsActive = f.IsActive,
                Priority = f.Priority,
                TimeoutSeconds = f.TimeoutSeconds,
                Description = f.Description,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CallForwardingRuleDto?> GetByIdAsync(int id)
    {
        var rule = await _ruleEs.GetByIdWithUserAsync(id);
        if (rule == null) return null;

        return new CallForwardingRuleDto
        {
            Id = rule.Id,
            Uid = rule.Uid,
            UserId = rule.UserId,
            UserName = rule.User?.FullName,
            CustomerId = rule.CustomerId,
            ForwardType = rule.ForwardType,
            ForwardTypeName = ForwardTypes.GetName(rule.ForwardType),
            Destination = rule.Destination,
            IsActive = rule.IsActive,
            Priority = rule.Priority,
            TimeoutSeconds = rule.TimeoutSeconds,
            Description = rule.Description,
            CreatedAt = rule.CreatedAt
        };
    }

    public async Task<(bool Success, int? Id, string? Error)> CreateAsync(CallForwardingRuleCreateDto dto)
    {
        var userExists = await _userEs.GetAllQueryable().AnyAsync(u => u.Id == dto.UserId);
        if (!userExists)
            return (false, null, "Kullanici bulunamadi");

        if (dto.ForwardType == ForwardTypes.Always && dto.IsActive)
        {
            var existingAlways = await _ruleEs.GetAllQueryable()
                .AnyAsync(f => f.UserId == dto.UserId && f.ForwardType == ForwardTypes.Always && f.IsActive);
            if (existingAlways)
                return (false, null, "Bu kullanici icin zaten aktif 'Always' yonlendirme kurali var");
        }

        var rule = new CallForwardingRule
        {
            UserId = dto.UserId,
            CustomerId = dto.CustomerId,
            ForwardType = dto.ForwardType,
            Destination = dto.Destination,
            IsActive = dto.IsActive,
            Priority = dto.Priority,
            TimeoutSeconds = dto.TimeoutSeconds,
            Description = dto.Description
        };

        _ruleEs.Add(rule);
        await _uow.SaveChangesAsync();

        return (true, rule.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, CallForwardingRuleUpdateDto dto)
    {
        var rule = await _ruleEs.GetByIdAsync(id);
        if (rule == null)
            return (false, "Kural bulunamadi");

        if (dto.ForwardType.HasValue) rule.ForwardType = dto.ForwardType.Value;
        if (dto.Destination != null) rule.Destination = dto.Destination;
        if (dto.IsActive.HasValue) rule.IsActive = dto.IsActive.Value;
        if (dto.Priority.HasValue) rule.Priority = dto.Priority.Value;
        if (dto.TimeoutSeconds.HasValue) rule.TimeoutSeconds = dto.TimeoutSeconds.Value;
        if (dto.Description != null) rule.Description = dto.Description;

        rule.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var rule = await _ruleEs.GetByIdAsync(id);
        if (rule == null)
            return (false, "Kural bulunamadi");

        _ruleEs.Remove(rule);
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<string?> GetForwardDestinationAsync(int userId, int forwardType)
    {
        var rule = await _ruleEs.GetAllQueryable()
            .Where(f => f.UserId == userId && f.IsActive &&
                        (f.ForwardType == forwardType || f.ForwardType == ForwardTypes.Always))
            .OrderBy(f => f.ForwardType == ForwardTypes.Always ? 1 : 0)
            .ThenBy(f => f.Priority)
            .FirstOrDefaultAsync();

        return rule?.Destination;
    }

    private static CallForwardingRuleDto MapToDto(CallForwardingRule f) => new()
    {
        Id = f.Id,
        Uid = f.Uid,
        UserId = f.UserId,
        CustomerId = f.CustomerId,
        ForwardType = f.ForwardType,
        ForwardTypeName = ForwardTypes.GetName(f.ForwardType),
        Destination = f.Destination,
        IsActive = f.IsActive,
        Priority = f.Priority,
        TimeoutSeconds = f.TimeoutSeconds,
        Description = f.Description,
        CreatedAt = f.CreatedAt
    };
}
