using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class ServicePricingFactory
{
    private readonly IServicePricingPeriodEntityService _periodEs;
    private readonly IServicePricingItemEntityService _itemEs;
    private readonly IModulePricingEntityService _modulePricingEs;
    private readonly IUnitOfWork _uow;

    public ServicePricingFactory(
        IServicePricingPeriodEntityService periodEs,
        IServicePricingItemEntityService itemEs,
        IModulePricingEntityService modulePricingEs,
        IUnitOfWork uow)
    {
        _periodEs = periodEs;
        _itemEs = itemEs;
        _modulePricingEs = modulePricingEs;
        _uow = uow;
    }

    public async Task<List<object>> GetPeriodsAsync()
    {
        return await _periodEs.GetAllQueryable()
            .OrderByDescending(p => p.StartDate)
            .Select(p => (object)new
            {
                p.Id, p.Name, p.StartDate, p.EndDate, p.StatusId,
                statusName = p.StatusId == 1 ? "Aktif" : p.StatusId == 2 ? "Geçmiş" : "Taslak",
                itemCount = p.Items.Count
            })
            .ToListAsync();
    }

    /// <summary>
    /// Aktif dönemin salon paket fiyatlarını döndürür.
    /// Dict: groupId → MonthlyPrice. 0 = Temel Paket. Dönem/kayıt yoksa enum fallback.
    /// </summary>
    public async Task<Dictionary<int, decimal>> GetActiveSalonPackagePricesAsync()
    {
        var activePeriod = await _periodEs.GetAllQueryable()
            .Where(p => p.StatusId == 1)
            .OrderByDescending(p => p.StartDate)
            .Include(p => p.Items)
            .FirstOrDefaultAsync();

        var result = new Dictionary<int, decimal>();

        // Default: enum değerleri
        result[0] = 1700m; // Temel Paket
        foreach (var pkg in SalonModuleGroups.All) result[pkg.Id] = pkg.MonthlyPrice;

        if (activePeriod != null)
        {
            foreach (var item in activePeriod.Items.Where(i => i.ProductTypeId == SalonPortalModules.ProductTypeId && i.PackageGroupId.HasValue))
                result[item.PackageGroupId!.Value] = item.MonthlyPrice;
        }

        return result;
    }

    public async Task<object?> GetPeriodDetailAsync(int periodId)
    {
        var period = await _periodEs.GetAllQueryable()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == periodId);

        if (period == null) return null;

        var ccItems = period.Items.Where(i => i.ProductTypeId == PortalModules.ProductTypeId).OrderBy(i => i.ServiceId).ToList();
        var slnItems = period.Items.Where(i => i.ProductTypeId == SalonPortalModules.ProductTypeId && !i.PackageGroupId.HasValue).OrderBy(i => i.ServiceId).ToList();
        var pkgItems = period.Items.Where(i => i.ProductTypeId == SalonPortalModules.ProductTypeId && i.PackageGroupId.HasValue).OrderBy(i => i.PackageGroupId).ToList();

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
            salonPackages = pkgItems.Select(i => new { i.Id, packageGroupId = i.PackageGroupId!.Value, name = i.ServiceName, i.MonthlyPrice, i.PreviousPrice }),
            operatorUnitPrice = 0m // CC operatör birim fiyatı - TODO
        };
    }

    public async Task<(object? Result, string? Error)> CreatePeriodAsync(string name, DateTime startDate, DateTime endDate)
    {
        var utcStart = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        var utcEnd = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

        // Duplicate kontrol: ayni isim + ayni baslangic tarihi
        var existing = await _periodEs.GetAllQueryable()
            .AnyAsync(p => p.Name == name && p.StartDate == utcStart);
        if (existing) return (null, "Aynı ad ve başlangıç tarihi ile dönem zaten mevcut.");

        // Onceki donemi bul (items dolu olanlardan)
        var previousPeriod = await _periodEs.GetAllQueryable()
            .Where(p => p.Items.Any())
            .OrderByDescending(p => p.StartDate)
            .Include(p => p.Items)
            .FirstOrDefaultAsync();

        var period = new ServicePricingPeriod
        {
            Name = name,
            StartDate = utcStart,
            EndDate = utcEnd,
            StatusId = 3 // Taslak
        };

        if (previousPeriod != null)
        {
            // Onceki donemin fiyatlarini kopyala (hem modul hem paket kalemleri)
            foreach (var prev in previousPeriod.Items)
            {
                period.Items.Add(new ServicePricingItem
                {
                    ProductTypeId = prev.ProductTypeId,
                    ServiceId = prev.ServiceId,
                    PackageGroupId = prev.PackageGroupId,
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
                    ProductTypeId = SalonPortalModules.ProductTypeId,
                    ServiceId = mod.Id,
                    ServiceName = mod.Description ?? mod.SystemName,
                    MonthlyPrice = 0
                });
            }
            // Salon paketleri — Temel Paket (groupId=0, tum salonlar icin zorunlu) + opsiyonel gruplar
            period.Items.Add(new ServicePricingItem
            {
                ProductTypeId = SalonPortalModules.ProductTypeId,
                ServiceId = 0,
                PackageGroupId = 0,
                ServiceName = "Temel Paket",
                MonthlyPrice = 1700m
            });
            foreach (var pkg in SalonModuleGroups.All)
            {
                period.Items.Add(new ServicePricingItem
                {
                    ProductTypeId = SalonPortalModules.ProductTypeId,
                    ServiceId = 0,
                    PackageGroupId = pkg.Id,
                    ServiceName = pkg.Description,
                    MonthlyPrice = pkg.MonthlyPrice
                });
            }
        }

        _periodEs.Add(period);
        await _uow.SaveChangesAsync(); // period + items tek transaction

        return (new { period.Id, period.Name, itemCount = period.Items.Count }, null);
    }

    public async Task<(bool Success, string? Error)> UpdateItemPriceAsync(int itemId, decimal newPrice)
    {
        var item = await _itemEs.GetByIdAsync(itemId);
        if (item == null) return (false, "Kalem bulunamadı.");
        item.MonthlyPrice = newPrice;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(int Updated, string? Error)> BulkAdjustAsync(int periodId, int? productTypeId, string adjustType, decimal value)
    {
        var items = await _itemEs.GetAllQueryable()
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
        var currentActive = await _periodEs.GetAllQueryable().Where(p => p.StatusId == 1).ToListAsync();
        foreach (var p in currentActive) p.StatusId = 2;

        var period = await _periodEs.GetByIdAsync(periodId);
        if (period == null) return (false, "Dönem bulunamadı.");
        period.StatusId = 1;

        // Aktif fiyatlari ModulePricing tablosuna yansit (sadece modul satirlari; paket satirlari ServiceId=0 olur, senkronize edilmez)
        var items = await _itemEs.GetAllQueryable().Where(i => i.PeriodId == periodId && i.ProductTypeId == SalonPortalModules.ProductTypeId && !i.PackageGroupId.HasValue).ToListAsync();
        foreach (var item in items)
        {
            var pricing = await _modulePricingEs.GetByModuleIdAsync(item.ServiceId);
            if (pricing != null)
            {
                pricing.MonthlyPrice = item.MonthlyPrice;
                pricing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _modulePricingEs.Add(new ModulePricing { ModuleId = item.ServiceId, MonthlyPrice = item.MonthlyPrice });
            }
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeletePeriodAsync(int periodId)
    {
        var period = await _periodEs.GetByIdAsync(periodId);
        if (period == null) return (false, "Dönem bulunamadı.");
        if (period.StatusId == 1) return (false, "Aktif dönem silinemez.");
        _periodEs.Remove(period);
        await _uow.SaveChangesAsync();
        return (true, null);
    }
}
