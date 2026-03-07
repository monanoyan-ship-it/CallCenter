using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class ServiceSubscriptionFactory : IServiceSubscriptionFactory
{
    private readonly ICustomerServiceSubscriptionEntityService _subscriptionEs;
    private readonly IServiceBillingItemEntityService _billingItemEs;
    private readonly IUnitOfWork _uow;

    public ServiceSubscriptionFactory(
        ICustomerServiceSubscriptionEntityService subscriptionEs,
        IServiceBillingItemEntityService billingItemEs,
        IUnitOfWork uow)
    {
        _subscriptionEs = subscriptionEs;
        _billingItemEs = billingItemEs;
        _uow = uow;
    }

    // ═══════════════════════════════════════════════════
    // HİZMET TANIMLARI (TypeDefinition bazlı)
    // ═══════════════════════════════════════════════════

    public List<ServiceTypeDto> GetAllServices()
    {
        return ServiceTypes.All
            .OrderBy(s => s.DisplayOrder)
            .Select(MapServiceTypeDto)
            .ToList();
    }

    public List<ServiceTypeDto> GetPremiumServices()
    {
        return ServiceTypes.Premium
            .OrderBy(s => s.DisplayOrder)
            .Select(MapServiceTypeDto)
            .ToList();
    }

    // ═══════════════════════════════════════════════════
    // ABONELİK CRUD
    // ═══════════════════════════════════════════════════

    public async Task<List<SubscriptionDto>> GetSubscriptionsByCustomerAsync(int customerId)
    {
        var list = await _subscriptionEs.GetAllQueryable()
            .Where(s => s.CustomerId == customerId)
            .OrderBy(s => s.ServiceTypeId)
            .ToListAsync();

        return list.Select(MapSubscriptionDto).ToList();
    }

    public async Task<(SubscriptionDto? Result, string? Error)> CreateSubscriptionAsync(SubscriptionCreateDto dto)
    {
        // TypeDefinition kontrolü
        var serviceType = ServiceTypes.GetById(dto.ServiceTypeId);
        if (serviceType == null)
            return (null, "Hizmet tanimi bulunamadi.");

        // Duplicate kontrolü
        var exists = await _subscriptionEs.GetAllQueryable()
            .AnyAsync(s => s.CustomerId == dto.CustomerId && s.ServiceTypeId == dto.ServiceTypeId);
        if (exists)
            return (null, "Bu musteri icin bu hizmet zaten tanimli.");

        var entity = new CustomerServiceSubscription
        {
            CustomerId = dto.CustomerId,
            ServiceTypeId = dto.ServiceTypeId,
            StatusId = SubscriptionStatuses.Ids.Active,
            MonthlyPrice = dto.MonthlyPrice ?? 0,
            StartDate = DateTime.UtcNow,
            Notes = dto.Notes
        };

        _subscriptionEs.Add(entity);
        await _uow.SaveChangesAsync();

        return (MapSubscriptionDto(entity), null);
    }

    public async Task<(bool Success, string? Error)> UpdateSubscriptionAsync(Guid uid, SubscriptionUpdateDto dto)
    {
        var entity = await _subscriptionEs.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Uid == uid);
        if (entity == null)
            return (false, "Abonelik bulunamadi.");

        if (dto.StatusId.HasValue)
            entity.StatusId = dto.StatusId.Value;
        if (dto.MonthlyPrice.HasValue)
            entity.MonthlyPrice = dto.MonthlyPrice.Value;
        if (dto.EndDate.HasValue)
            entity.EndDate = dto.EndDate.Value;
        if (dto.Notes != null)
            entity.Notes = dto.Notes;

        entity.UpdatedAt = DateTime.UtcNow;
        _subscriptionEs.Update(entity);
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelSubscriptionAsync(Guid uid)
    {
        var entity = await _subscriptionEs.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Uid == uid);
        if (entity == null)
            return (false, "Abonelik bulunamadi.");

        entity.StatusId = SubscriptionStatuses.Ids.Cancelled;
        entity.EndDate = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        _subscriptionEs.Update(entity);
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    // ═══════════════════════════════════════════════════
    // TOPLU FİYAT ARTIRMA
    // ═══════════════════════════════════════════════════

    public async Task<(BulkPriceAdjustResultDto? Result, string? Error)> AdjustPricesAsync(BulkPriceAdjustDto dto)
    {
        if (dto.AdjustmentType != "amount" && dto.AdjustmentType != "percent")
            return (null, "AdjustmentType 'amount' veya 'percent' olmali.");

        var query = _subscriptionEs.GetAllQueryable()
            .Where(s => s.StatusId == SubscriptionStatuses.Ids.Active);

        if (dto.ServiceTypeId.HasValue)
            query = query.Where(s => s.ServiceTypeId == dto.ServiceTypeId.Value);

        var subscriptions = await query.ToListAsync();
        if (subscriptions.Count == 0)
            return (new BulkPriceAdjustResultDto(), null);

        var oldTotal = subscriptions.Sum(s => s.MonthlyPrice);

        foreach (var sub in subscriptions)
        {
            if (dto.AdjustmentType == "amount")
                sub.MonthlyPrice += dto.Value;
            else
                sub.MonthlyPrice *= (1 + dto.Value / 100);

            if (sub.MonthlyPrice < 0)
                sub.MonthlyPrice = 0;

            sub.UpdatedAt = DateTime.UtcNow;
            _subscriptionEs.Update(sub);
        }

        await _uow.SaveChangesAsync();

        var newTotal = subscriptions.Sum(s => s.MonthlyPrice);

        return (new BulkPriceAdjustResultDto
        {
            AffectedCount = subscriptions.Count,
            OldTotalMonthly = oldTotal,
            NewTotalMonthly = newTotal
        }, null);
    }

    // ═══════════════════════════════════════════════════
    // FATURALANDIRMA
    // ═══════════════════════════════════════════════════

    public async Task<int> GenerateServiceBillingAsync(int year, int month)
    {
        var activeSubscriptions = await _subscriptionEs.GetAllQueryable()
            .Where(s => s.StatusId == SubscriptionStatuses.Ids.Active)
            .ToListAsync();

        var existingKeys = await _billingItemEs.GetAllQueryable()
            .Where(b => b.Year == year && b.Month == month)
            .Select(b => b.CustomerServiceSubscriptionId)
            .ToListAsync();

        var existingSet = new HashSet<int>(existingKeys);
        var newItems = new List<ServiceBillingItem>();

        foreach (var sub in activeSubscriptions)
        {
            if (existingSet.Contains(sub.Id))
                continue;

            newItems.Add(new ServiceBillingItem
            {
                CustomerId = sub.CustomerId,
                CustomerServiceSubscriptionId = sub.Id,
                StatusId = BillingItemStatuses.Ids.Pending,
                Year = year,
                Month = month,
                Amount = sub.MonthlyPrice,
                IsPaid = false
            });
        }

        if (newItems.Count > 0)
        {
            _billingItemEs.AddRange(newItems);
            await _uow.SaveChangesAsync();
        }

        return newItems.Count;
    }

    public async Task<List<ServiceBillingItemDto>> GetServiceBillingByCustomerAsync(int customerId, int? year, int? month)
    {
        var query = _billingItemEs.GetAllQueryable()
            .Include(b => b.CustomerServiceSubscription)
            .Where(b => b.CustomerId == customerId);

        if (year.HasValue)
            query = query.Where(b => b.Year == year.Value);
        if (month.HasValue)
            query = query.Where(b => b.Month == month.Value);

        var items = await query
            .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month)
            .ToListAsync();

        return items.Select(b => new ServiceBillingItemDto
        {
            Id = b.Id,
            CustomerId = b.CustomerId,
            SubscriptionId = b.CustomerServiceSubscriptionId,
            ServiceName = ServiceTypes.GetById(b.CustomerServiceSubscription.ServiceTypeId)?.Description ?? "?",
            StatusId = b.StatusId,
            StatusName = BillingItemStatuses.GetById(b.StatusId)?.SystemName ?? "?",
            Year = b.Year,
            Month = b.Month,
            Amount = b.Amount,
            IsPaid = b.IsPaid,
            PaidAt = b.PaidAt,
            Notes = b.Notes
        }).ToList();
    }

    public async Task<(bool Success, string? Error)> MarkServiceBillingPaidAsync(int billingItemId)
    {
        var item = await _billingItemEs.GetByIdAsync(billingItemId);
        if (item == null)
            return (false, "Fatura kalemi bulunamadi.");

        item.IsPaid = true;
        item.PaidAt = DateTime.UtcNow;
        item.StatusId = BillingItemStatuses.Ids.Paid;
        _billingItemEs.Update(item);
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    // ═══════════════════════════════════════════════════
    // ÖZET
    // ═══════════════════════════════════════════════════

    public async Task<ServiceSubscriptionSummaryDto> GetSubscriptionSummaryAsync()
    {
        var activeSubscriptions = await _subscriptionEs.GetAllQueryable()
            .Where(s => s.StatusId == SubscriptionStatuses.Ids.Active)
            .ToListAsync();

        var breakdown = activeSubscriptions
            .GroupBy(s => ServiceTypes.GetById(s.ServiceTypeId)?.Description ?? "?")
            .Select(g => new ServiceRevenueItem
            {
                ServiceName = g.Key,
                ActiveCount = g.Count(),
                TotalMonthly = g.Sum(s => s.MonthlyPrice)
            })
            .OrderByDescending(r => r.TotalMonthly)
            .ToList();

        return new ServiceSubscriptionSummaryDto
        {
            TotalActiveSubscriptions = activeSubscriptions.Count,
            TotalMonthlyRevenue = activeSubscriptions.Sum(s => s.MonthlyPrice),
            ServiceBreakdown = breakdown
        };
    }

    // ═══════════════════════════════════════════════════
    // MAPPING HELPERS
    // ═══════════════════════════════════════════════════

    private static ServiceTypeDto MapServiceTypeDto(TypeItem s) => new()
    {
        Id = s.Id,
        Name = s.Description ?? s.SystemName,
        Code = s.SystemName,
        Description = s.Description,
        Icon = s.Icon ?? "",
        BadgeCss = s.CssClass ?? "",
        CategoryId = ServiceTypes.GetCategoryId(s.Id),
        CategoryName = s.IsDefault ? "Standart" : "Premium",
        IsDefault = s.IsDefault
    };

    private static SubscriptionDto MapSubscriptionDto(CustomerServiceSubscription s)
    {
        var serviceType = ServiceTypes.GetById(s.ServiceTypeId);
        return new()
        {
            Id = s.Id,
            Uid = s.Uid,
            CustomerId = s.CustomerId,
            ServiceTypeId = s.ServiceTypeId,
            ServiceName = serviceType?.Description ?? "?",
            ServiceCode = serviceType?.SystemName ?? "?",
            StatusId = s.StatusId,
            StatusName = SubscriptionStatuses.GetById(s.StatusId)?.SystemName ?? "?",
            MonthlyPrice = s.MonthlyPrice,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Notes = s.Notes
        };
    }
}
