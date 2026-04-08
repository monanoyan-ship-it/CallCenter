using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class ServicePricingFactory
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;

    public ServicePricingFactory(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<List<object>> GetPeriodsAsync()
    {
        return await _db.ServicePricingPeriods
            .OrderByDescending(p => p.StartDate)
            .Select(p => (object)new
            {
                p.Id, p.Name, p.StartDate, p.EndDate, p.StatusId,
                statusName = p.StatusId == 1 ? "Aktif" : p.StatusId == 2 ? "Geçmiş" : "Taslak",
                itemCount = p.Items.Count
            })
            .ToListAsync();
    }

    public async Task<object?> GetPeriodDetailAsync(int periodId)
    {
        var period = await _db.ServicePricingPeriods
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == periodId);

        if (period == null) return null;

        var ccItems = period.Items.Where(i => i.ProductTypeId == PortalModules.ProductTypeId).OrderBy(i => i.ServiceId).ToList();
        var slnItems = period.Items.Where(i => i.ProductTypeId == SalonPortalModules.ProductTypeId).OrderBy(i => i.ServiceId).ToList();

        // Salon modullerini gruplara ayir
        var salonGrouped = slnItems.Select(i =>
        {
            var groupId = SalonModuleGroups.GetGroupId(i.ServiceId);
            var group = SalonModuleGroups.GetById(groupId ?? 0);
            var moduleDef = SalonPortalModules.GetById(i.ServiceId);
            return new
            {
                i.Id, i.ServiceId, i.ServiceName, i.MonthlyPrice, i.PreviousPrice,
                isDefault = moduleDef?.IsDefault ?? false,
                groupId = groupId ?? 0,
                groupName = group?.Description ?? "Diğer"
            };
        }).GroupBy(i => i.groupId).Select(g => new
        {
            groupId = g.Key,
            groupName = g.First().groupName,
            items = g.OrderBy(i => i.ServiceId).ToList()
        }).OrderBy(g => g.groupId).ToList();

        return new
        {
            period.Id, period.Name, period.StartDate, period.EndDate, period.StatusId,
            callCenter = ccItems.Select(i => new { i.Id, i.ServiceId, i.ServiceName, i.MonthlyPrice, i.PreviousPrice, isDefault = ServiceTypes.GetById(i.ServiceId)?.IsDefault ?? false }),
            salon = slnItems.Select(i => new { i.Id, i.ServiceId, i.ServiceName, i.MonthlyPrice, i.PreviousPrice, isDefault = SalonPortalModules.GetById(i.ServiceId)?.IsDefault ?? false }),
            salonGroups = salonGrouped,
            operatorUnitPrice = 0m // CC operatör birim fiyatı - TODO
        };
    }

    public async Task<(object? Result, string? Error)> CreatePeriodAsync(string name, DateTime startDate, DateTime endDate)
    {
        var period = new ServicePricingPeriod
        {
            Name = name,
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
            StatusId = 3 // Taslak
        };

        _db.ServicePricingPeriods.Add(period);
        await _uow.SaveChangesAsync();

        // Onceki donemi bul
        var previousPeriod = await _db.ServicePricingPeriods
            .Where(p => p.Id != period.Id)
            .OrderByDescending(p => p.StartDate)
            .Include(p => p.Items)
            .FirstOrDefaultAsync();

        if (previousPeriod != null && previousPeriod.Items.Count > 0)
        {
            // Onceki donemin fiyatlarini kopyala
            foreach (var prev in previousPeriod.Items)
            {
                period.Items.Add(new ServicePricingItem
                {
                    PeriodId = period.Id,
                    ProductTypeId = prev.ProductTypeId,
                    ServiceId = prev.ServiceId,
                    ServiceName = prev.ServiceName,
                    MonthlyPrice = prev.MonthlyPrice,
                    PreviousPrice = prev.MonthlyPrice
                });
            }
        }
        else
        {
            // Ilk donem — TypeDefinition'lardan tum hizmetleri yukle (fiyat 0)
            foreach (var svc in ServiceTypes.All.Where(s => !s.IsDefault))
            {
                period.Items.Add(new ServicePricingItem
                {
                    PeriodId = period.Id,
                    ProductTypeId = PortalModules.ProductTypeId,
                    ServiceId = svc.Id,
                    ServiceName = svc.Description ?? svc.SystemName,
                    MonthlyPrice = 0
                });
            }
            foreach (var mod in SalonPortalModules.All.Where(m => !m.IsDefault))
            {
                period.Items.Add(new ServicePricingItem
                {
                    PeriodId = period.Id,
                    ProductTypeId = SalonPortalModules.ProductTypeId,
                    ServiceId = mod.Id,
                    ServiceName = mod.Description ?? mod.SystemName,
                    MonthlyPrice = 0
                });
            }
        }

        await _uow.SaveChangesAsync();
        return (new { period.Id, period.Name, itemCount = period.Items.Count }, null);
    }

    public async Task<(bool Success, string? Error)> UpdateItemPriceAsync(int itemId, decimal newPrice)
    {
        var item = await _db.ServicePricingItems.FindAsync(itemId);
        if (item == null) return (false, "Kalem bulunamadı.");
        item.MonthlyPrice = newPrice;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(int Updated, string? Error)> BulkAdjustAsync(int periodId, int? productTypeId, string adjustType, decimal value)
    {
        var items = await _db.ServicePricingItems
            .Where(i => i.PeriodId == periodId)
            .ToListAsync();

        if (productTypeId.HasValue)
            items = items.Where(i => i.ProductTypeId == productTypeId.Value).ToList();

        foreach (var item in items)
        {
            if (adjustType == "percentage")
                item.MonthlyPrice = Math.Round(item.MonthlyPrice * (1 + value / 100), 2);
            else
                item.MonthlyPrice = Math.Max(0, item.MonthlyPrice + value);
        }

        await _uow.SaveChangesAsync();
        return (items.Count, null);
    }

    public async Task<(bool Success, string? Error)> ActivatePeriodAsync(int periodId)
    {
        // Mevcut aktif donemi gecmis yap
        var currentActive = await _db.ServicePricingPeriods.Where(p => p.StatusId == 1).ToListAsync();
        foreach (var p in currentActive) p.StatusId = 2;

        var period = await _db.ServicePricingPeriods.FindAsync(periodId);
        if (period == null) return (false, "Dönem bulunamadı.");
        period.StatusId = 1;

        // Aktif fiyatlari ModulePricing tablosuna yansit
        var items = await _db.ServicePricingItems.Where(i => i.PeriodId == periodId && i.ProductTypeId == SalonPortalModules.ProductTypeId).ToListAsync();
        foreach (var item in items)
        {
            var pricing = await _db.ModulePricings.FirstOrDefaultAsync(p => p.ModuleId == item.ServiceId);
            if (pricing != null)
            {
                pricing.MonthlyPrice = item.MonthlyPrice;
                pricing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.ModulePricings.Add(new ModulePricing { ModuleId = item.ServiceId, MonthlyPrice = item.MonthlyPrice });
            }
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeletePeriodAsync(int periodId)
    {
        var period = await _db.ServicePricingPeriods.FindAsync(periodId);
        if (period == null) return (false, "Dönem bulunamadı.");
        if (period.StatusId == 1) return (false, "Aktif dönem silinemez.");
        _db.ServicePricingPeriods.Remove(period);
        await _uow.SaveChangesAsync();
        return (true, null);
    }
}
