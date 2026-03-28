using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnMembershipFactory : ISlnMembershipFactory
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;

    public SlnMembershipFactory(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<List<SlnMembershipPlanDto>> GetPlansAsync(int customerId)
    {
        var plans = await _db.SlnMembershipPlans
            .Where(p => p.CustomerId == customerId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var memberCounts = await _db.SlnClientMemberships
            .Where(m => m.CustomerId == customerId && m.StatusId == 1)
            .GroupBy(m => m.PlanId)
            .Select(g => new { PlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Count);

        return plans.Select(p => new SlnMembershipPlanDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            IconClass = p.IconClass,
            Color = p.Color,
            MonthlyPrice = p.MonthlyPrice,
            DiscountPercent = p.DiscountPercent,
            FreeSessionsPerMonth = p.FreeSessionsPerMonth,
            PriorityBooking = p.PriorityBooking,
            IsActive = p.IsActive,
            ActiveMembers = memberCounts.GetValueOrDefault(p.Id, 0)
        }).ToList();
    }

    public async Task<SlnMembershipPlanDto> CreatePlanAsync(SlnMembershipPlanCreateDto dto, int customerId)
    {
        var plan = new SlnMembershipPlan
        {
            CustomerId = customerId,
            Name = dto.Name,
            Description = dto.Description,
            IconClass = dto.IconClass,
            Color = dto.Color,
            MonthlyPrice = dto.MonthlyPrice,
            DiscountPercent = dto.DiscountPercent,
            FreeSessionsPerMonth = dto.FreeSessionsPerMonth,
            PriorityBooking = dto.PriorityBooking,
            IsActive = dto.IsActive,
            SortOrder = await _db.SlnMembershipPlans.CountAsync(p => p.CustomerId == customerId) + 1
        };
        _db.SlnMembershipPlans.Add(plan);
        await _uow.SaveChangesAsync();
        return (await GetPlansAsync(customerId)).First(p => p.Id == plan.Id);
    }

    public async Task<(bool Success, string? Error)> UpdatePlanAsync(int id, SlnMembershipPlanCreateDto dto, int customerId)
    {
        var plan = await _db.SlnMembershipPlans.FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        if (plan == null) return (false, "Plan bulunamadi");

        plan.Name = dto.Name;
        plan.Description = dto.Description;
        plan.IconClass = dto.IconClass;
        plan.Color = dto.Color;
        plan.MonthlyPrice = dto.MonthlyPrice;
        plan.DiscountPercent = dto.DiscountPercent;
        plan.FreeSessionsPerMonth = dto.FreeSessionsPerMonth;
        plan.PriorityBooking = dto.PriorityBooking;
        plan.IsActive = dto.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeletePlanAsync(int id, int customerId)
    {
        var plan = await _db.SlnMembershipPlans.FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        if (plan == null) return (false, "Plan bulunamadi");
        var hasMembers = await _db.SlnClientMemberships.AnyAsync(m => m.PlanId == id && m.StatusId == 1);
        if (hasMembers) return (false, "Aktif uyesi olan plan silinemez");
        _db.SlnMembershipPlans.Remove(plan);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<SlnClientMembershipDto>> GetMembershipsAsync(int customerId, int? clientId = null)
    {
        var query = _db.SlnClientMemberships
            .Where(m => m.CustomerId == customerId)
            .Include(m => m.Plan)
            .Include(m => m.SlnClient)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(m => m.SlnClientId == clientId.Value);

        return await query.OrderByDescending(m => m.CreatedAt).Select(m => new SlnClientMembershipDto
        {
            Id = m.Id,
            PlanName = m.Plan != null ? m.Plan.Name : "",
            PlanColor = m.Plan != null ? m.Plan.Color : null,
            ClientName = m.SlnClient != null ? m.SlnClient.FullName : "",
            DiscountPercent = m.Plan != null ? m.Plan.DiscountPercent : 0,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            NextPaymentDate = m.NextPaymentDate,
            StatusId = m.StatusId
        }).ToListAsync();
    }

    public async Task<(SlnClientMembershipDto? Membership, string? Error)> CreateMembershipAsync(SlnClientMembershipCreateDto dto, int customerId)
    {
        var plan = await _db.SlnMembershipPlans.FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.CustomerId == customerId);
        if (plan == null) return (null, "Plan bulunamadi");

        var existing = await _db.SlnClientMemberships
            .AnyAsync(m => m.SlnClientId == dto.SlnClientId && m.CustomerId == customerId && m.StatusId == 1);
        if (existing) return (null, "Bu musterinin zaten aktif uyeligi var");

        var membership = new SlnClientMembership
        {
            CustomerId = customerId,
            PlanId = dto.PlanId,
            SlnClientId = dto.SlnClientId,
            StartDate = DateTime.UtcNow,
            NextPaymentDate = DateTime.UtcNow.AddMonths(1),
            StatusId = 1
        };
        _db.SlnClientMemberships.Add(membership);
        await _uow.SaveChangesAsync();

        var result = (await GetMembershipsAsync(customerId)).First(m => m.Id == membership.Id);
        return (result, null);
    }

    public async Task<(bool Success, string? Error)> CancelMembershipAsync(int id, int customerId)
    {
        var m = await _db.SlnClientMemberships.FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId);
        if (m == null) return (false, "Uyelik bulunamadi");
        m.StatusId = 3;
        m.EndDate = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> FreezeMembershipAsync(int id, int customerId)
    {
        var m = await _db.SlnClientMemberships.FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId && m.StatusId == 1);
        if (m == null) return (false, "Aktif uyelik bulunamadi");
        m.StatusId = 2;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ReactivateMembershipAsync(int id, int customerId)
    {
        var m = await _db.SlnClientMemberships.FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId && m.StatusId == 2);
        if (m == null) return (false, "Dondurulmus uyelik bulunamadi");
        m.StatusId = 1;
        await _uow.SaveChangesAsync();
        return (true, null);
    }
}
