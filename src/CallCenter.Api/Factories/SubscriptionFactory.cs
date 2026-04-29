using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SubscriptionFactory : ISubscriptionFactory
{
    private readonly ISubscriptionPlanEntityService _planEs;
    private readonly ICustomerSubscriptionEntityService _subscriptionEs;
    private readonly ICustomerEntityService _customerEs;
    private readonly IBillingPeriodEntityService _billingPeriodEs;
    private readonly ISlnBranchEntityService _branchEs;
    private readonly ICustomerPortalModuleEntityService _moduleEs;
    private readonly ICustomerProductEntityService _customerProductEs;
    private readonly ServicePricingFactory _servicePricingFactory;
    private readonly IUnitOfWork _uow;

    public SubscriptionFactory(
        ISubscriptionPlanEntityService planEs,
        ICustomerSubscriptionEntityService subscriptionEs,
        ICustomerEntityService customerEs,
        IBillingPeriodEntityService billingPeriodEs,
        ISlnBranchEntityService branchEs,
        ICustomerPortalModuleEntityService moduleEs,
        ICustomerProductEntityService customerProductEs,
        ServicePricingFactory servicePricingFactory,
        IUnitOfWork uow)
    {
        _planEs = planEs;
        _subscriptionEs = subscriptionEs;
        _customerEs = customerEs;
        _billingPeriodEs = billingPeriodEs;
        _branchEs = branchEs;
        _moduleEs = moduleEs;
        _customerProductEs = customerProductEs;
        _servicePricingFactory = servicePricingFactory;
        _uow = uow;
    }

    // ═══ PLAN YÖNETİMİ ═══

    public async Task<List<SubscriptionPlan>> GetPlansAsync()
        => await _planEs.GetAllQueryable().OrderBy(p => p.SortOrder).ToListAsync();

    public async Task<SubscriptionPlan> CreatePlanAsync(string name, int intervalMonths, decimal discountPercent, decimal branchPrice)
    {
        var plan = new SubscriptionPlan
        {
            Name = name,
            IntervalMonths = intervalMonths,
            DiscountPercent = discountPercent,
            BranchPrice = branchPrice,
            SortOrder = await _planEs.GetAllQueryable().CountAsync() + 1
        };
        _planEs.Add(plan);
        await _uow.SaveChangesAsync();
        return plan;
    }

    public async Task<(bool Success, string? Error)> UpdatePlanAsync(int id, string name, int intervalMonths, decimal discountPercent, decimal branchPrice, bool isActive)
    {
        var plan = await _planEs.GetByIdAsync(id);
        if (plan == null) return (false, "Plan bulunamadı.");
        plan.Name = name;
        plan.IntervalMonths = intervalMonths;
        plan.DiscountPercent = discountPercent;
        plan.BranchPrice = branchPrice;
        plan.IsActive = isActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeletePlanAsync(int id)
    {
        var plan = await _planEs.GetByIdAsync(id);
        if (plan == null) return (false, "Plan bulunamadı.");
        var hasSubscribers = await _subscriptionEs.GetAllQueryable().AnyAsync(s => s.PlanId == id && s.StatusId == 1);
        if (hasSubscribers) return (false, "Aktif abonesi olan plan silinemez.");
        _planEs.Remove(plan);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ MÜŞTERİ ABONELİĞİ ═══

    public async Task<List<object>> GetCustomerSubscriptionsAsync(int? customerId = null)
    {
        var query = _subscriptionEs.GetAllQueryable()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .Include(s => s.Branch)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);

        return await query.OrderByDescending(s => s.CreatedAt)
            .Select(s => (object)new
            {
                s.Id,
                s.CustomerId,
                customerName = s.Customer.Name,
                planId = s.PlanId,
                planName = s.Plan.Name,
                intervalMonths = s.Plan.IntervalMonths,
                branchId = s.BranchId,
                branchName = s.Branch != null ? s.Branch.Name : null,
                s.StartDate,
                s.MonthlyPrice,
                s.PeriodPrice,
                s.BillingDay,
                s.NextBillingDate,
                s.StatusId,
                statusName = s.StatusId == 1 ? "Aktif" : s.StatusId == 2 ? "Askıda" : "İptal",
                s.PaymentGraceDays,
                s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<(object? Result, string? Error)> CreateSubscriptionAsync(int customerId, int planId, DateTime startDate, decimal monthlyPrice, int? branchId = null)
    {
        var customer = await _customerEs.GetByIdAsync(customerId);
        if (customer == null) return (null, "Müşteri bulunamadı.");

        var plan = await _planEs.GetByIdAsync(planId);
        if (plan == null) return (null, "Plan bulunamadı.");

        // Aktif abonelik var mi kontrol (ayni sube bazinda; branchId null = firma genel)
        var existing = await _subscriptionEs.GetAllQueryable()
            .AnyAsync(s => s.CustomerId == customerId && s.StatusId == 1 && s.BranchId == branchId);
        if (existing)
            return (null, branchId.HasValue
                ? "Bu şubenin zaten aktif aboneliği var."
                : "Bu müşterinin zaten aktif firma-geneli aboneliği var.");

        var periodPrice = monthlyPrice * plan.IntervalMonths * (1 - plan.DiscountPercent / 100);

        var sub = new CustomerSubscription
        {
            CustomerId = customerId,
            PlanId = planId,
            BranchId = branchId,
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            MonthlyPrice = monthlyPrice,
            PeriodPrice = Math.Round(periodPrice, 2),
            BillingDay = startDate.Day,
            NextBillingDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            StatusId = 1
        };

        _subscriptionEs.Add(sub);

        // Customer'in BillingAnchorDay'ini ayarla
        customer.BillingAnchorDay = startDate.Day;

        await _uow.SaveChangesAsync();
        return (new { sub.Id, sub.NextBillingDate, sub.PeriodPrice }, null);
    }

    public async Task<(bool Success, string? Error)> CancelSubscriptionAsync(int subscriptionId)
    {
        var sub = await _subscriptionEs.GetByIdAsync(subscriptionId);
        if (sub == null) return (false, "Abonelik bulunamadı.");
        sub.StatusId = 3;
        sub.CancelledAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ TAHAKKUK OLUŞTURMA ═══

    /// <summary>
    /// Secilen takvim ayi (yil/ay) ve abonelik <see cref="CustomerSubscription.BillingDay"/> ile platform tahakkuku keser.
    /// Tutar: <see cref="ResolveCorePlatformPeriodAmountAsync"/> (PeriodPrice yoksa aktif urun aylikleri x donem).
    /// Aktif veya askidaki <see cref="CustomerSubscription"/> (plan) — odeme gecikince hosted servis askiya alir; toplu kesim yine de
    /// yeni donem uretebilmeli (iptal edilmemis musteriler). CC hizmet aboneligi degil.
    /// Mevcut donem: yalnizca gercekten odenmis (+tutarli) kayit korunur; CC Draft / sahte Paid silinip yeniden kesilir.
    /// </summary>
    public async Task<(int Created, int Skipped)> GenerateBillingForMonthAsync(int year, int month)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 2100)
            return (0, 0);

        var rawSubs = await _subscriptionEs.GetAllQueryable()
            .Where(s => s.StatusId == SubscriptionStatuses.Ids.Active || s.StatusId == SubscriptionStatuses.Ids.Suspended)
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .ToListAsync();

        var subs = rawSubs
            .Where(s => s is { Customer: not null, Customer.IsTest: false, Plan: not null })
            .GroupBy(s => s.CustomerId)
            .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
            .ToList();

        var created = 0;
        var skipped = 0;

        foreach (var sub in subs)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var day = Math.Clamp(sub.BillingDay, 1, daysInMonth);
            var periodStart = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

            var existing = await _billingPeriodEs.GetAllQueryable()
                .FirstOrDefaultAsync(p => p.CustomerId == sub.CustomerId && p.Year == year && p.Month == month);

            if (existing != null)
            {
                var tot = existing.Amount + existing.ServiceAmount;
                // Gercekten kapanmis fatura — dokunma
                if (existing.IsPaid && tot > 0m)
                {
                    skipped++;
                    continue;
                }

                _billingPeriodEs.Remove(existing);
                await _uow.SaveChangesAsync();
            }

            if (await TryAppendBillingPeriodForSubscriptionAsync(sub, periodStart))
                created++;
            else
                skipped++;
        }

        if (created > 0)
            await _uow.SaveChangesAsync();

        return (created, skipped);
    }

    /// <inheritdoc />
    public async Task CreateInitialBillingPeriodForCustomerAsync(int customerId)
    {
        var customer = await _customerEs.GetByIdAsync(customerId);
        if (customer?.IsTest == true) return;

        var hasPeriod = await _billingPeriodEs.GetAllQueryable()
            .AnyAsync(p => p.CustomerId == customerId);
        if (hasPeriod) return;

        var sub = await _subscriptionEs.GetAllQueryable()
            .Include(s => s.Plan)
            .Where(s => s.CustomerId == customerId && s.StatusId == SubscriptionStatuses.Ids.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
        if (sub?.Plan == null) return;

        if (!await TryAppendBillingPeriodForSubscriptionAsync(sub))
            return;

        await _uow.SaveChangesAsync();
    }

    /// <summary>
    /// Platform donemi ana tutari: <see cref="CustomerSubscription.PeriodPrice"/> sozlesme (varsa). 0 ise salon CC kesiminden
    /// muaf oldugu icin aktif <see cref="CustomerProduct.MonthlyPrice"/> toplaminin donem karsiligi kullanilir.
    /// </summary>
    private async Task<decimal> ResolveCorePlatformPeriodAmountAsync(CustomerSubscription sub)
    {
        if (sub.Plan == null) return 0m;
        if (sub.PeriodPrice > 0m) return sub.PeriodPrice;
        var im = Math.Max(1, sub.Plan.IntervalMonths);
        var productMonthlySum = await _customerProductEs.GetAllQueryable()
            .Where(cp => cp.CustomerId == sub.CustomerId && cp.IsActive)
            .SumAsync(cp => cp.MonthlyPrice);
        return productMonthlySum * im;
    }

    /// <summary>Aylik job ve salon ilk kayit icin ortak: tek donem ekler, abonelik NextBillingDate ilerletilir.</summary>
    /// <param name="explicitPeriodStartUtc">Doluysa tahakkuk baslangici (ve donem yil/ay); Manager takvim ayi kesimi. Bos ise <see cref="CustomerSubscription.NextBillingDate"/>.</param>
    private async Task<bool> TryAppendBillingPeriodForSubscriptionAsync(CustomerSubscription sub, DateTime? explicitPeriodStartUtc = null)
    {
        if (sub.Plan == null) return false;

        var start = explicitPeriodStartUtc.HasValue
            ? DateTime.SpecifyKind(explicitPeriodStartUtc.Value.Date, DateTimeKind.Utc)
            : DateTime.SpecifyKind(sub.NextBillingDate.Date, DateTimeKind.Utc);
        var year = start.Year;
        var month = start.Month;

        var alreadyExists = await _billingPeriodEs.GetAllQueryable()
            .AnyAsync(p => p.CustomerId == sub.CustomerId && p.Year == year && p.Month == month);
        if (alreadyExists) return false;

        var (packageLines, moduleAmount) = await BuildSalonPackageLinesAsync(sub.CustomerId, sub.Plan.IntervalMonths);

        var branchCount = await _branchEs.GetAllQueryable().CountAsync(b => b.CustomerId == sub.CustomerId && b.IsActive);
        var extraBranches = Math.Max(0, branchCount - 1);
        var branchAmount = extraBranches * sub.Plan.BranchPrice * sub.Plan.IntervalMonths;

        var corePlatformPeriodAmount = await ResolveCorePlatformPeriodAmountAsync(sub);

        var billingTotal = corePlatformPeriodAmount + branchAmount + moduleAmount;
        // 0 TL: CC bulk ile uyumlu (otomatik kapali). Pozitif tutar her zaman Tahakkuk.
        var autoWaive = billingTotal <= 0m;
        var period = new CustomerBillingPeriod
        {
            CustomerId = sub.CustomerId,
            Year = year,
            Month = month,
            PeriodStartDate = start,
            PeriodEndDate = start.AddMonths(sub.Plan.IntervalMonths),
            UserCount = branchCount,
            UnitPrice = corePlatformPeriodAmount,
            Amount = corePlatformPeriodAmount + branchAmount,
            ServiceAmount = moduleAmount,
            StatusId = autoWaive ? BillingPeriodStatuses.Ids.Paid : BillingPeriodStatuses.Ids.Draft,
            IsPaid = autoWaive,
            PaidAt = autoWaive ? DateTime.UtcNow : null
        };

        _billingPeriodEs.Add(period);

        foreach (var line in packageLines)
            period.ModuleLines.Add(line);

        sub.NextBillingDate = start.AddMonths(sub.Plan.IntervalMonths);

        await RefreshSubscriptionDisplayMonthlyPriceAsync(sub.CustomerId, saveChanges: false);

        return true;
    }

    public async Task<bool> HasActiveSubscriptionAsync(int customerId)
    {
        // Test musterileri icin kontrol devre disi — ucretsiz kullanabilirler
        var customer = await _customerEs.GetByIdAsync(customerId);
        if (customer?.IsTest == true) return true;

        return await _subscriptionEs.GetAllQueryable()
            .AnyAsync(s => s.CustomerId == customerId && s.StatusId == 1);
    }

    /// <inheritdoc />
    public async Task RefreshSubscriptionDisplayMonthlyPriceAsync(int customerId, bool saveChanges = true)
    {
        var sub = await _subscriptionEs.GetAllQueryable()
            .Include(s => s.Plan)
            .Where(s => s.CustomerId == customerId
                     && (s.StatusId == SubscriptionStatuses.Ids.Active || s.StatusId == SubscriptionStatuses.Ids.Suspended))
            .OrderBy(s => s.StatusId)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (sub?.Plan == null) return;

        var (_, moduleAmount) = await BuildSalonPackageLinesAsync(customerId, sub.Plan.IntervalMonths);
        var branchCount = await _branchEs.GetAllQueryable()
            .CountAsync(b => b.CustomerId == customerId && b.IsActive);
        var extraBranches = Math.Max(0, branchCount - 1);
        var branchAmount = extraBranches * sub.Plan.BranchPrice * sub.Plan.IntervalMonths;
        var core = await ResolveCorePlatformPeriodAmountAsync(sub);
        var full = core + branchAmount + moduleAmount;
        var im = Math.Max(1, sub.Plan.IntervalMonths);
        sub.MonthlyPrice = Math.Round(full / im, 2, MidpointRounding.AwayFromZero);

        if (saveChanges)
            await _uow.SaveChangesAsync();
    }

    /// <summary>
    /// Salon paneli erisimi: aktif abonelik veya odenmemis tahakkukda PeriodStartDate + 5 gun stiresinde.
    /// </summary>
    public async Task<object> GetSalonPanelAccessAsync(int customerId)
    {
        var customer = await _customerEs.GetByIdAsync(customerId);
        if (customer?.IsTest == true)
            return PanelAccessDto(true, true, false, null);

        var now = DateTime.UtcNow;
        var g = PlatformBillingAccessPolicy.UnpaidGraceDaysAfterPeriodStart;

        var hasActiveSubscription = await _subscriptionEs.GetAllQueryable()
            .AnyAsync(s => s.CustomerId == customerId && s.StatusId == SubscriptionStatuses.Ids.Active);
        if (hasActiveSubscription)
            return PanelAccessDto(true, true, false, null);

        var oldestUnpaid = await _billingPeriodEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && !p.IsPaid && p.StatusId != BillingPeriodStatuses.Ids.Paid
                && p.Amount + p.ServiceAmount > 0m)
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .FirstOrDefaultAsync();

        if (oldestUnpaid != null)
        {
            var graceEndsAt = oldestUnpaid.PeriodStartDate.AddDays(g);
            if (now <= graceEndsAt)
                return PanelAccessDto(false, true, true, graceEndsAt);
        }

        var suspendedSub = await _subscriptionEs.GetAllQueryable()
            .Where(s => s.CustomerId == customerId && s.StatusId == SubscriptionStatuses.Ids.Suspended)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (suspendedSub != null && oldestUnpaid == null)
        {
            var graceEndsAt = suspendedSub.NextBillingDate.AddDays(g);
            if (now <= graceEndsAt)
                return PanelAccessDto(false, true, true, graceEndsAt);
        }

        // Yeni kayit / henuz tahakkuk kesilmedi: Salon urunu var, borc yoksa kilit yok (odeme yapilacak fatura da yoktur)
        if (oldestUnpaid == null)
        {
            var suspendedPastGrace = suspendedSub != null && now > suspendedSub.NextBillingDate.AddDays(g);
            if (!suspendedPastGrace)
            {
                var hasSalonProduct = await _customerProductEs.GetAllQueryable()
                    .AnyAsync(cp => cp.CustomerId == customerId && cp.IsActive && cp.ProductTypeId == ProductTypes.Ids.Salon);
                if (hasSalonProduct)
                    return PanelAccessDto(false, true, false, null);
            }
        }

        return PanelAccessDto(false, false, false, null);

        static object PanelAccessDto(bool hasActiveSub, bool canAccess, bool inGrace, DateTime? graceEndsAt)
            => new
            {
                hasActiveSubscription = hasActiveSub,
                canAccessPanel = canAccess,
                inGracePeriod = inGrace,
                graceEndsAt,
                hasActive = hasActiveSub
            };
    }

    public async Task<object> GetMySubscriptionAsync(int customerId)
    {
        var sub = await GetCustomerSubscriptionsAsync(customerId);
        var activeSub = sub.FirstOrDefault();

        // Odenmemis tahakkuklar
        var unpaidRaw = await _billingPeriodEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && !p.IsPaid && p.StatusId != 3
                && p.Amount + p.ServiceAmount > 0m)
            .Include(p => p.ModuleLines)
            .OrderByDescending(p => p.PeriodStartDate)
            .ToListAsync();

        var unpaidBillings = unpaidRaw.Select(p => new
        {
            p.Id,
            p.Year,
            p.Month,
            p.Amount,
            p.ServiceAmount,
            total = p.Amount + p.ServiceAmount,
            p.PeriodStartDate,
            p.PeriodEndDate,
            p.StatusId,
            salonModuleLines = p.ModuleLines
                .OrderBy(l => l.PackageGroupId ?? int.MaxValue)
                .ThenBy(l => l.ModuleId ?? int.MaxValue)
                .Select(l => new { l.PackageGroupId, l.ModuleId, l.ModuleDisplayName, l.MonthlyUnitPrice, l.LineAmount })
                .ToList()
        }).ToList();

        return new { subscription = activeSub, unpaidBillings };
    }

    public async Task<object> GetSalonBannerAsync(int customerId)
    {
        var customer = await _customerEs.GetByIdAsync(customerId);
        if (customer?.IsTest == true)
            return new { trial = (object?)null, overdue = (object?)null, info = (object?)null };

        var now = DateTime.UtcNow;

        object? overdue = null;
        var graceDaysBanner = PlatformBillingAccessPolicy.UnpaidGraceDaysAfterPeriodStart;
        var unpaidPeriods = await _billingPeriodEs.GetAllQueryable()
            .Where(b => b.CustomerId == customerId && !b.IsPaid && b.StatusId != BillingPeriodStatuses.Ids.Paid)
            .Select(b => new { b.Id, b.PeriodStartDate, b.Year, b.Month, b.Amount, b.ServiceAmount })
            .ToListAsync();

        var payableUnpaid = unpaidPeriods
            .Where(p => p.Amount + p.ServiceAmount > 0m)
            .ToList();

        foreach (var period in payableUnpaid
                     .OrderBy(p => p.Year)
                     .ThenBy(p => p.Month))
        {
            var graceEnd = period.PeriodStartDate.AddDays(graceDaysBanner);
            if (now <= graceEnd) continue;

            overdue = new
            {
                periodId = period.Id,
                period.Month,
                period.Year,
                deadline = graceEnd,
                message = $"Ödenmemiş dönem: {period.Month:00}/{period.Year}. {graceDaysBanner} gün ödeme süresi ({graceEnd:dd.MM.yyyy}) doldu. Tahakkuku Modüller üzerinden ödeyebilirsiniz."
            };
            break;
        }

        object? info = null;
        if (overdue == null && payableUnpaid.Count > 0)
        {
            var oldestPayable = payableUnpaid
                .OrderBy(p => p.Year)
                .ThenBy(p => p.Month)
                .First();
            var graceEnd = oldestPayable.PeriodStartDate.AddDays(graceDaysBanner);
            if (now <= graceEnd)
            {
                var total = oldestPayable.Amount + oldestPayable.ServiceAmount;
                var daysLeft = Math.Max(0, (graceEnd.Date - now.Date).Days);
                info = new
                {
                    message = $"Ödenmemiş platform tahakkukunuz: {oldestPayable.Month:00}/{oldestPayable.Year} — {total:N2} TL. Ödeme süresi {graceEnd:dd.MM.yyyy} tarihine kadar ({daysLeft} gün). Modüller sayfasından ödeyebilirsiniz."
                };
            }
        }

        return new { trial = (object?)null, overdue, info };
    }

    /// <summary>
    /// Aktif (varsayilan olmayan) modullerden türetilen <b>tekillenmiş paket gruplari</b> icin tahakkuk satirlari.
    /// Aylik birim: <see cref="ServicePricingFactory.GetActiveSalonPackagePricesAsync"/> (aktif donem + enum varsayilan).
    /// Temel paket (Core) — onceki davranisla uyumlu: ServiceAmount icine alinmaz (PeriodPrice tarafinda ele alinir).
    /// </summary>
    private async Task<(List<CustomerBillingPeriodModuleLine> Lines, decimal ServiceTotal)> BuildSalonPackageLinesAsync(int customerId, int intervalMonths)
    {
        var packagePrices = await _servicePricingFactory.GetActiveSalonPackagePricesAsync();
        var modules = await _moduleEs.GetAllQueryable()
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .ToListAsync();

        var extraGroupIds = new HashSet<int>();
        foreach (var mod in modules)
        {
            var groupId = SalonModuleGroups.GetGroupId(mod.ModuleId);
            if (!groupId.HasValue) continue;
            if (groupId.Value == SalonModuleGroups.Ids.Core) continue;

            var def = SalonPortalModules.GetById(mod.ModuleId);
            if (def?.IsDefault == true) continue;

            extraGroupIds.Add(groupId.Value);
        }

        var lines = new List<CustomerBillingPeriodModuleLine>();
        foreach (var groupId in extraGroupIds.OrderBy(g => g))
        {
            var group = SalonModuleGroups.GetById(groupId);
            if (group == null) continue;

            var monthlyUnit = packagePrices.TryGetValue(groupId, out var p) ? p : group.MonthlyPrice;
            var lineAmount = Math.Round(monthlyUnit * intervalMonths, 2, MidpointRounding.AwayFromZero);
            lines.Add(new CustomerBillingPeriodModuleLine
            {
                PackageGroupId = groupId,
                ModuleId = null,
                CustomerPortalModuleId = null,
                ModuleDisplayName = group.Description ?? group.SystemName,
                MonthlyUnitPrice = monthlyUnit,
                LineAmount = lineAmount
            });
        }

        var total = lines.Sum(l => l.LineAmount);
        return (lines, total);
    }
}
