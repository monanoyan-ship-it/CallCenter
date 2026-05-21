using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class ServicePricingFactory
{
    public const int CallCenterOperatorLicenseServiceId = 0;
    public const decimal InitialCallCenterOperatorMonthlyPrice = 700m;
    public const string CallCenterOperatorLicenseName = "Operator Lisansi";

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

        // Default: enum değerleri (Core Id=0 = Temel Paket 1700m dahil)
        foreach (var pkg in SalonModuleGroups.All) result[pkg.Id] = pkg.MonthlyPrice;

        if (activePeriod != null)
        {
            // Sifir DB degeri enum varsayilanini ezmesin (taslak/bos kalemler tahakkuku 0 yapmasin)
            foreach (var item in activePeriod.Items.Where(i => i.ProductTypeId == SalonPortalModules.ProductTypeId && i.PackageGroupId.HasValue))
            {
                if (item.MonthlyPrice > 0m && !IsLegacySalonPackagePlaceholder(item.PackageGroupId!.Value, item.MonthlyPrice))
                    result[item.PackageGroupId!.Value] = item.MonthlyPrice;
            }
        }

        return result;
    }

    /// <summary>CallCenter tahakkuku icin operator basina aylik birim fiyat.</summary>
    public async Task<decimal> GetActiveCallCenterOperatorUnitPriceAsync()
    {
        var (price, error) = await TryGetActiveCallCenterOperatorUnitPriceAsync();
        if (!price.HasValue)
            throw new InvalidOperationException(error ?? "CallCenter operator birim fiyati bulunamadi.");

        return price.Value;
    }

    public async Task<(decimal? Price, string? Error)> TryGetActiveCallCenterOperatorUnitPriceAsync()
    {
        var activePeriod = await _periodEs.GetAllQueryable()
            .Where(p => p.StatusId == 1)
            .OrderByDescending(p => p.StartDate)
            .Include(p => p.Items)
            .FirstOrDefaultAsync();

        if (activePeriod == null)
            return (null, "Aktif fiyat donemi bulunamadi. /Modules/PricingPeriods ekraninda bir fiyat donemi olusturup aktif edin.");

        var operatorItem = FindCallCenterOperatorLicenseItem(activePeriod.Items);
        if (operatorItem == null)
            return (null, "Aktif fiyat doneminde Operator Lisansi satiri bulunamadi. /Modules/PricingPeriods ekranindaki fiyat donemini kontrol edin.");

        if (operatorItem.MonthlyPrice <= 0m)
            return (null, "Aktif fiyat donemindeki Operator Lisansi fiyati 0 veya negatif olamaz. /Modules/PricingPeriods ekranindan duzeltin.");

        return (operatorItem.MonthlyPrice, null);
    }

    private static bool IsLegacySalonPackagePlaceholder(int packageGroupId, decimal monthlyPrice)
    {
        var group = SalonModuleGroups.GetById(packageGroupId);
        return monthlyPrice == 20m && group != null && group.MonthlyPrice != 20m;
    }

    /// <summary>
    /// Aktif dönemdeki toplam aktif şube sayısına göre <b>şube satırı</b> (tüm şubeler × Temel Paket brüt) üzerine uygulanacak indirim yüzdesi.
    /// Eşik yoksa veya eşleşme yoksa 0.
    /// </summary>
    public async Task<decimal> GetActiveBranchDiscountPercentForTotalBranchesAsync(int totalActiveBranches)
    {
        if (totalActiveBranches < 2) return 0m;

        var activePeriod = await _periodEs.GetAllQueryable()
            .AsNoTracking()
            .Where(p => p.StatusId == 1)
            .OrderByDescending(p => p.StartDate)
            .Include(p => p.BranchDiscountTiers)
            .FirstOrDefaultAsync();

        if (activePeriod == null || activePeriod.BranchDiscountTiers.Count == 0)
            return 0m;

        foreach (var tier in activePeriod.BranchDiscountTiers.OrderBy(t => t.SortOrder).ThenBy(t => t.MinBranches))
        {
            if (totalActiveBranches >= tier.MinBranches && totalActiveBranches <= tier.MaxBranches)
                return tier.DiscountPercent;
        }

        return 0m;
    }

    public async Task<object?> GetPeriodDetailAsync(int periodId)
    {
        var period = await _periodEs.GetAllQueryable()
            .Include(p => p.Items)
            .Include(p => p.BranchDiscountTiers)
            .FirstOrDefaultAsync(p => p.Id == periodId);

        if (period == null) return null;

        var operatorItem = FindCallCenterOperatorLicenseItem(period.Items);

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
            branchDiscountTiers = period.BranchDiscountTiers
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.MinBranches)
                .Select(t => new { t.Id, t.MinBranches, t.MaxBranches, t.DiscountPercent, t.SortOrder })
                .ToList(),
            operatorUnitPrice = operatorItem?.MonthlyPrice ?? 0m
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
            .Include(p => p.BranchDiscountTiers)
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

            EnsureCallCenterOperatorLicenseItem(period);

            foreach (var t in previousPeriod.BranchDiscountTiers.OrderBy(t => t.SortOrder))
            {
                period.BranchDiscountTiers.Add(new ServicePricingBranchDiscountTier
                {
                    MinBranches = t.MinBranches,
                    MaxBranches = t.MaxBranches,
                    DiscountPercent = t.DiscountPercent,
                    SortOrder = t.SortOrder
                });
            }
        }
        else
        {
            // Ilk donem — TypeDefinition'lardan tum hizmetleri yukle (fiyat 0)
            EnsureCallCenterOperatorLicenseItem(period);

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
            // Salon paketleri — tüm gruplar (Core=0 Temel Paket dahil)
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

        if (period.BranchDiscountTiers.Count == 0)
            SeedDefaultBranchDiscountTiers(period);

        _periodEs.Add(period);
        await _uow.SaveChangesAsync(); // period + items tek transaction

        return (new { period.Id, period.Name, itemCount = period.Items.Count }, null);
    }

    /// <summary>Yönetim: dönem şube indirim eşiklerini tamamen değiştirir (boş liste = indirim yok).</summary>
    public async Task<(bool Success, string? Error)> ReplaceBranchDiscountTiersAsync(int periodId, IReadOnlyList<BranchDiscountTierInput> tiers)
    {
        var period = await _periodEs.GetAllQueryable()
            .Include(p => p.BranchDiscountTiers)
            .FirstOrDefaultAsync(p => p.Id == periodId);
        if (period == null) return (false, "Dönem bulunamadı.");

        period.BranchDiscountTiers.Clear();
        var order = 1;
        foreach (var t in tiers.OrderBy(x => x.SortOrder).ThenBy(x => x.MinBranches))
        {
            if (t.MaxBranches < t.MinBranches)
                return (false, "Her satırda MaxBranches, MinBranches değerinden küçük olamaz.");
            if (t.DiscountPercent < 0 || t.DiscountPercent > 100)
                return (false, "İndirim yüzdesi 0–100 arasında olmalıdır.");
            period.BranchDiscountTiers.Add(new ServicePricingBranchDiscountTier
            {
                MinBranches = t.MinBranches,
                MaxBranches = t.MaxBranches,
                DiscountPercent = Math.Round(t.DiscountPercent, 2, MidpointRounding.AwayFromZero),
                SortOrder = t.SortOrder > 0 ? t.SortOrder : order
            });
            order++;
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static void SeedDefaultBranchDiscountTiers(ServicePricingPeriod period)
    {
        period.BranchDiscountTiers.Add(new ServicePricingBranchDiscountTier
            { MinBranches = 2, MaxBranches = 10, DiscountPercent = 10, SortOrder = 1 });
        period.BranchDiscountTiers.Add(new ServicePricingBranchDiscountTier
            { MinBranches = 11, MaxBranches = 20, DiscountPercent = 15, SortOrder = 2 });
        period.BranchDiscountTiers.Add(new ServicePricingBranchDiscountTier
            { MinBranches = 21, MaxBranches = 999, DiscountPercent = 20, SortOrder = 3 });
    }

    public async Task<(bool Success, string? Error)> UpdateItemPriceAsync(int itemId, decimal newPrice)
    {
        var item = await _itemEs.GetByIdAsync(itemId);
        if (item == null) return (false, "Kalem bulunamadı.");
        if (newPrice < 0m) return (false, "Fiyat negatif olamaz.");
        if (IsCallCenterOperatorLicenseItem(item) && newPrice <= 0m)
            return (false, "Operator Lisansi fiyati 0 veya negatif olamaz.");

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
            var nextPrice = adjustType == "percentage"
                ? Math.Round(item.MonthlyPrice * (1 + value / 100), 2)
                : Math.Max(0, item.MonthlyPrice + value);
            if (IsCallCenterOperatorLicenseItem(item) && nextPrice <= 0m)
                return (0, "Operator Lisansi fiyati 0 veya negatif olamaz.");

            if (adjustType == "percentage")
                item.MonthlyPrice = nextPrice;
            else
                item.MonthlyPrice = nextPrice;
        }

        await _uow.SaveChangesAsync();
        return (items.Count, null);
    }

    public async Task<(bool Success, string? Error)> ActivatePeriodAsync(int periodId)
    {
        var period = await _periodEs.GetAllQueryable()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == periodId);
        if (period == null) return (false, "Donem bulunamadi.");
        var operatorItem = FindCallCenterOperatorLicenseItem(period.Items);
        if (operatorItem == null)
            return (false, "Operator Lisansi satiri bulunamadi.");
        if (operatorItem.MonthlyPrice <= 0m)
            return (false, "Operator Lisansi fiyati 0 veya negatif olamaz.");

        // Mevcut aktif donemi gecmis yap
        var currentActive = await _periodEs.GetAllQueryable().Where(p => p.StatusId == 1 && p.Id != periodId).ToListAsync();
        foreach (var p in currentActive) p.StatusId = 2;

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

    private static bool IsCallCenterOperatorLicenseItem(ServicePricingItem item)
        => item.ProductTypeId == PortalModules.ProductTypeId
            && item.PackageGroupId == null
            && item.ServiceId == CallCenterOperatorLicenseServiceId;

    private static ServicePricingItem? FindCallCenterOperatorLicenseItem(IEnumerable<ServicePricingItem> items)
        => items
            .Where(IsCallCenterOperatorLicenseItem)
            .OrderByDescending(i => i.Id)
            .FirstOrDefault();

    private static ServicePricingItem EnsureCallCenterOperatorLicenseItem(ServicePricingPeriod period)
    {
        var existing = FindCallCenterOperatorLicenseItem(period.Items);
        if (existing != null)
            return existing;

        var item = new ServicePricingItem
        {
            ProductTypeId = PortalModules.ProductTypeId,
            ServiceId = CallCenterOperatorLicenseServiceId,
            ServiceName = CallCenterOperatorLicenseName,
            MonthlyPrice = InitialCallCenterOperatorMonthlyPrice,
            PreviousPrice = InitialCallCenterOperatorMonthlyPrice
        };
        period.Items.Add(item);
        return item;
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
