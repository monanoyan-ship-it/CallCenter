using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnMembershipFactory : ISlnMembershipFactory
{
    private readonly ISlnMembershipPlanEntityService _planEs;
    private readonly ISlnClientMembershipEntityService _membershipEs;
    private readonly ISlnMembershipPlanServiceEntityService _planServiceEs;
    private readonly ISlnMembershipUsageEntityService _usageEs;
    private readonly IUnitOfWork _uow;

    public SlnMembershipFactory(
        ISlnMembershipPlanEntityService planEs,
        ISlnClientMembershipEntityService membershipEs,
        ISlnMembershipPlanServiceEntityService planServiceEs,
        ISlnMembershipUsageEntityService usageEs,
        IUnitOfWork uow)
    {
        _planEs = planEs;
        _membershipEs = membershipEs;
        _planServiceEs = planServiceEs;
        _usageEs = usageEs;
        _uow = uow;
    }

    public async Task<List<SlnMembershipPlanDto>> GetPlansAsync(int customerId)
    {
        var plans = await _planEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId)
            .Include(p => p.Services).ThenInclude(s => s.Service)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var memberCounts = await _membershipEs.GetAllQueryable()
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
            DurationType = p.DurationType,
            DurationDays = p.DurationDays,
            Price = p.Price,
            DiscountPercent = p.DiscountPercent,
            PriorityBooking = p.PriorityBooking,
            IsActive = p.IsActive,
            ActiveMembers = memberCounts.GetValueOrDefault(p.Id, 0),
            ServiceIds = p.Services.Select(s => s.ServiceId).ToList(),
            ServiceNames = p.Services.Where(s => s.Service != null).Select(s => s.Service!.Name).ToList(),
            ServiceDetails = p.Services.Select(s => new MembershipServiceDetailDto
            {
                ServiceId = s.ServiceId,
                ServiceName = s.Service?.Name ?? "",
                FreeCount = s.FreeCount,
                DiscountPercent = s.DiscountPercent ?? 0
            }).ToList()
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
            DurationType = dto.DurationType,
            DurationDays = dto.DurationDays,
            Price = dto.Price,
            DiscountPercent = dto.DiscountPercent,
            PriorityBooking = dto.PriorityBooking,
            IsActive = dto.IsActive,
            SortOrder = await _planEs.GetAllQueryable().CountAsync(p => p.CustomerId == customerId) + 1
        };
        _planEs.Add(plan);
        await _uow.SaveChangesAsync();

        // Hizmet iliskilerini ekle (detayli: freeCount + discount)
        if (dto.ServiceDetails.Count > 0)
        {
            foreach (var sd in dto.ServiceDetails)
                _planServiceEs.Add(new SlnMembershipPlanService
                {
                    PlanId = plan.Id, ServiceId = sd.ServiceId,
                    FreeCount = sd.FreeCount,
                    DiscountPercent = sd.DiscountPercent > 0 ? sd.DiscountPercent : null
                });
            await _uow.SaveChangesAsync();
        }
        else if (dto.ServiceIds.Count > 0)
        {
            foreach (var svcId in dto.ServiceIds)
                _planServiceEs.Add(new SlnMembershipPlanService { PlanId = plan.Id, ServiceId = svcId });
            await _uow.SaveChangesAsync();
        }

        return (await GetPlansAsync(customerId)).First(p => p.Id == plan.Id);
    }

    public async Task<(bool Success, string? Error)> UpdatePlanAsync(int id, SlnMembershipPlanCreateDto dto, int customerId)
    {
        var plan = await _planEs.GetAllQueryable().FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        if (plan == null) return (false, "Plan bulunamadi");

        plan.Name = dto.Name;
        plan.Description = dto.Description;
        plan.IconClass = dto.IconClass;
        plan.Color = dto.Color;
        plan.DurationType = dto.DurationType;
        plan.DurationDays = dto.DurationDays;
        plan.Price = dto.Price;
        plan.DiscountPercent = dto.DiscountPercent;
        plan.PriorityBooking = dto.PriorityBooking;
        plan.IsActive = dto.IsActive;

        // Hizmet iliskilerini guncelle (sil + yeniden ekle)
        var existingServices = await _planServiceEs.GetAllQueryable().Where(s => s.PlanId == id).ToListAsync();
        _planServiceEs.RemoveRange(existingServices);
        if (dto.ServiceDetails.Count > 0)
        {
            foreach (var sd in dto.ServiceDetails)
                _planServiceEs.Add(new SlnMembershipPlanService
                {
                    PlanId = id, ServiceId = sd.ServiceId,
                    FreeCount = sd.FreeCount,
                    DiscountPercent = sd.DiscountPercent > 0 ? sd.DiscountPercent : null
                });
        }
        else
        {
            foreach (var svcId in dto.ServiceIds)
                _planServiceEs.Add(new SlnMembershipPlanService { PlanId = id, ServiceId = svcId });
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeletePlanAsync(int id, int customerId)
    {
        var plan = await _planEs.GetAllQueryable().FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);
        if (plan == null) return (false, "Plan bulunamadi");
        var hasMembers = await _membershipEs.GetAllQueryable().AnyAsync(m => m.PlanId == id && m.StatusId == 1);
        if (hasMembers) return (false, "Aktif uyesi olan plan silinemez");
        _planEs.Remove(plan);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<SlnClientMembershipDto>> GetMembershipsAsync(int customerId, int? clientId = null)
    {
        var query = _membershipEs.GetAllQueryable()
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
            CurrentPeriodStart = m.CurrentPeriodStart,
            CurrentPeriodEnd = m.CurrentPeriodEnd,
            PaidAmount = m.PaidAmount,
            StatusId = m.StatusId
        }).ToListAsync();
    }

    public async Task<(SlnClientMembershipDto? Membership, string? Error)> CreateMembershipAsync(SlnClientMembershipCreateDto dto, int customerId)
    {
        var plan = await _planEs.GetAllQueryable().FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.CustomerId == customerId);
        if (plan == null) return (null, "Plan bulunamadi");

        var existing = await _membershipEs.GetAllQueryable()
            .AnyAsync(m => m.SlnClientId == dto.SlnClientId && m.CustomerId == customerId && m.StatusId == 1);
        if (existing) return (null, "Bu musterinin zaten aktif uyeligi var");

        var now = DateTime.UtcNow;
        var membership = new SlnClientMembership
        {
            CustomerId = customerId,
            PlanId = dto.PlanId,
            SlnClientId = dto.SlnClientId,
            StartDate = now,
            CurrentPeriodStart = plan.DurationType == 1 ? now : null,
            CurrentPeriodEnd = plan.DurationType == 1 ? now.AddDays(plan.DurationDays) : null,
            EndDate = plan.DurationType == 1 ? now.AddDays(plan.DurationDays) : null,
            PaidAmount = plan.Price,
            StatusId = 1
        };
        _membershipEs.Add(membership);
        await _uow.SaveChangesAsync();

        var result = (await GetMembershipsAsync(customerId)).First(m => m.Id == membership.Id);
        return (result, null);
    }

    public async Task<(bool Success, string? Error)> CancelMembershipAsync(int id, int customerId)
    {
        var m = await _membershipEs.GetAllQueryable().FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId);
        if (m == null) return (false, "Uyelik bulunamadi");
        m.StatusId = 3;
        m.EndDate = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> FreezeMembershipAsync(int id, int customerId)
    {
        var m = await _membershipEs.GetAllQueryable().FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId && m.StatusId == 1);
        if (m == null) return (false, "Aktif uyelik bulunamadi");
        m.StatusId = 2;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ReactivateMembershipAsync(int id, int customerId)
    {
        var m = await _membershipEs.GetAllQueryable().FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId && m.StatusId == 2);
        if (m == null) return (false, "Dondurulmus uyelik bulunamadi");
        m.StatusId = 1;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<ServiceMembershipBenefit>> CheckBenefitsAsync(int customerId, int slnClientId, List<int> serviceIds)
    {
        // Musterinin aktif uyeligi
        var membership = await _membershipEs.GetAllQueryable()
            .Include(m => m.Plan).ThenInclude(p => p!.Services).ThenInclude(s => s.Service)
            .FirstOrDefaultAsync(m => m.CustomerId == customerId && m.SlnClientId == slnClientId && m.StatusId == 1);

        if (membership?.Plan == null)
            return serviceIds.Select(id => new ServiceMembershipBenefit { ServiceId = id }).ToList();

        // Mevcut donem baslangicinı belirle
        var periodStart = membership.CurrentPeriodStart;

        // Bu donemdeki kullanim kayitlari
        var usages = await _usageEs.GetAllQueryable()
            .Where(u => u.MembershipId == membership.Id && u.PeriodStart == periodStart)
            .ToListAsync();

        return serviceIds.Select(serviceId =>
        {
            var planService = membership.Plan.Services.FirstOrDefault(s => s.ServiceId == serviceId);
            if (planService == null)
            {
                // Bu hizmet planda yok — genel plan indirimi uygula
                return new ServiceMembershipBenefit
                {
                    MembershipId = membership.Id,
                    ServiceId = serviceId,
                    ServiceName = "",
                    DiscountPercent = membership.Plan.DiscountPercent > 0 ? membership.Plan.DiscountPercent : null,
                    PlanName = membership.Plan.Name
                };
            }

            var usage = usages.FirstOrDefault(u => u.ServiceId == serviceId);
            var usedCount = usage?.UsedCount ?? 0;

            return new ServiceMembershipBenefit
            {
                MembershipId = membership.Id,
                ServiceId = serviceId,
                ServiceName = planService.Service?.Name ?? "",
                HasFreeBenefit = planService.FreeCount > 0,
                FreeCount = planService.FreeCount,
                UsedThisPeriod = usedCount,
                DiscountPercent = planService.DiscountPercent ?? (membership.Plan.DiscountPercent > 0 ? membership.Plan.DiscountPercent : null),
                PlanName = membership.Plan.Name
            };
        }).ToList();
    }

    public async Task RecordUsageAsync(int customerId, int membershipId, int serviceId)
    {
        // Uyeligin mevcut donem baslangicini al
        var membership = await _membershipEs.GetByIdAsync(membershipId);
        if (membership == null || membership.CustomerId != customerId || membership.StatusId != 1)
            return;

        var periodStart = membership?.CurrentPeriodStart;

        var usage = await _usageEs.GetAllQueryable()
            .FirstOrDefaultAsync(u => u.MembershipId == membershipId && u.ServiceId == serviceId && u.PeriodStart == periodStart);

        if (usage != null)
        {
            usage.UsedCount++;
            usage.LastUsedAt = DateTime.UtcNow;
        }
        else
        {
            _usageEs.Add(new SlnMembershipUsage
            {
                CustomerId = customerId,
                MembershipId = membershipId,
                ServiceId = serviceId,
                PeriodStart = periodStart,
                UsedCount = 1
            });
        }
        await _uow.SaveChangesAsync();
    }
}
