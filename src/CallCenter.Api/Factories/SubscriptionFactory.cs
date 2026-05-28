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
    private readonly ServicePricingFactory _servicePricingFactory;
    private readonly IUnitOfWork _uow;

    private static readonly (int IntervalMonths, decimal DiscountPercent, string Name, int SortOrder)[] DefaultSalonPlans =
    [
        (1, 0m, "1 Aylik Abonelik", 1),
        (3, 10m, "3 Aylik Abonelik (%10 indirim)", 2),
        (6, 15m, "6 Aylik Abonelik (%15 indirim)", 3),
        (12, 20m, "12 Aylik Abonelik (%20 indirim)", 4)
    ];

    /// <summary>Plan indirimi uygulanmadan temel paket dönem tutarı (tahakkuk sıfırlanmasın).</summary>
    private async Task<decimal> ComputeSalonPlatformListBasePeriodAmountForIntervalAsync(int intervalMonths)
    {
        var prices = await _servicePricingFactory.GetActiveSalonPackagePricesAsync();
        var coreMonthly = prices.TryGetValue(SalonModuleGroups.Ids.Core, out var m) ? m : SalonModuleGroups.Core.MonthlyPrice;
        if (coreMonthly <= 0m)
            coreMonthly = SalonModuleGroups.Core.MonthlyPrice;
        return Math.Round(coreMonthly * intervalMonths, 2, MidpointRounding.AwayFromZero);
    }

    private Task<decimal> ComputeSalonPlatformListBasePeriodAmountAsync(CustomerSubscription sub) =>
        ComputeSalonPlatformListBasePeriodAmountForIntervalAsync(GetEffectiveBillingIntervalMonths(sub.Plan));

    /// <summary>
    /// Ücretli plan (PeriodPrice &gt; 0) iken hesaplanan toplam 0 ise — ör. plan indirimi %100 —
    /// temel paketi liste fiyatıyla doldurur; otomatik "ödendi" ve 0 TL hayalet satır oluşmasın.
    /// </summary>
    private async Task<(decimal Base, decimal Branch, decimal Module)> ApplyPaidPlanNonZeroFloorAsync(
        CustomerSubscription sub,
        decimal basePeriodAmount,
        decimal branchAmount,
        decimal moduleAmount)
    {
        if (sub.PeriodPrice <= 0m)
            return (basePeriodAmount, branchAmount, moduleAmount);

        var total = basePeriodAmount + branchAmount + moduleAmount;
        if (total > 0m)
            return (basePeriodAmount, branchAmount, moduleAmount);

        var listBase = await ComputeSalonPlatformListBasePeriodAmountAsync(sub);
        return (listBase, branchAmount, moduleAmount);
    }

    /// <summary>
    /// Ücretli planda veya manuel tahakkukda liste fiyatıyla dönem tutarı hâlâ 0 kalmasın (otomatik "ödendi" hayalet satır).
    /// </summary>
    private static (decimal Base, decimal Branch, decimal Module, decimal Total) CoerceSalonTotalsWithListBaseIfZero(
        decimal basePeriodAmount,
        decimal branchAmount,
        decimal moduleAmount,
        decimal listBaseNoDiscount)
    {
        var billingTotal = basePeriodAmount + branchAmount + moduleAmount;
        if (billingTotal > 0m || listBaseNoDiscount <= 0m)
            return (basePeriodAmount, branchAmount, moduleAmount, billingTotal);
        basePeriodAmount = listBaseNoDiscount;
        billingTotal = basePeriodAmount + branchAmount + moduleAmount;
        return (basePeriodAmount, branchAmount, moduleAmount, billingTotal);
    }

    /// <summary>
    /// Ücretli sözleşmede (<see cref="CustomerSubscription.PeriodPrice"/>) dönem toplamı bu tutarın altına düşmesin.
    /// Liste/fiyat dönemi hesabı 0 veya eksik çıksa bile tahakkuk satırı oluşsun; eksik kısım temel paket satırına eklenir.
    /// </summary>
    private static void ApplyPaidContractPeriodFloor(
        CustomerSubscription sub,
        ref decimal basePeriodAmount,
        ref decimal branchAmount,
        ref decimal moduleAmount)
    {
        if (sub.PeriodPrice <= 0m) return;
        var total = basePeriodAmount + branchAmount + moduleAmount;
        if (total >= sub.PeriodPrice) return;
        basePeriodAmount += sub.PeriodPrice - total;
    }

    /// <summary>Plan kaydı bozuksa (0 ay) tahakkuk sıfır ve otomatik "ödendi" oluşmasın diye en az 1 ay.</summary>
    private static int GetEffectiveBillingIntervalMonths(SubscriptionPlan plan)
    {
        var n = plan?.IntervalMonths ?? 0;
        return n > 0 ? n : 1;
    }

    private static decimal GetEffectiveDiscountPercent(CustomerSubscription sub) =>
        sub.DiscountPercentOverride ?? sub.Plan.DiscountPercent;

    private static readonly int[] PayableSubscriptionStatusIds =
    [
        SubscriptionStatuses.Ids.Active,
        SubscriptionStatuses.Ids.Suspended
    ];

    private static bool IsPayableSubscriptionStatus(int statusId) =>
        PayableSubscriptionStatusIds.Contains(statusId);

    /// <summary>
    /// Liste fiyatı / deneme (<see cref="CustomerSubscription.PeriodPrice"/>=0) modunda 2+ şubede Temel Paket tutarı
    /// yalnız şube satırında (N × Core brüt − eşik) hesaplanır; taban satırda Core tekrar eklenmez.
    /// Ücretli sözleşmede (PeriodPrice&gt;0 ve MonthlyPrice&gt;0) taban anlaşılmış tutar korunur.
    /// </summary>
    private static void ExcludeListCoreFromBaseWhenMultipleBranches(CustomerSubscription sub, int branchCount, ref decimal basePeriodAmount)
    {
        if (branchCount <= 1) return;
        if (sub.PeriodPrice > 0m && sub.MonthlyPrice > 0m) return;
        basePeriodAmount = 0m;
    }

    private static DateTime ResolveNextSalonAccrualStartUtc(CustomerSubscription sub, IReadOnlyDictionary<int, DateTime> lastSalonPeriodStartByCustomer)
    {
        var interval = GetEffectiveBillingIntervalMonths(sub.Plan);
        if (!lastSalonPeriodStartByCustomer.TryGetValue(sub.CustomerId, out var lastStart))
            return sub.StartDate;
        return lastStart.AddMonths(interval);
    }

    private async Task<Dictionary<int, DateTime>> BuildLastSalonPeriodStartMapAsync(IReadOnlyCollection<int> customerIds)
    {
        if (customerIds.Count == 0) return new Dictionary<int, DateTime>();

        var rows = await _billingPeriodEs.GetAllQueryable()
            .Where(p => customerIds.Contains(p.CustomerId) && p.BillingKindId == CustomerBillingKinds.SalonPlatform)
            .Select(p => new { p.CustomerId, p.PeriodStartDate })
            .ToListAsync();

        return rows
            .GroupBy(x => x.CustomerId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.PeriodStartDate));
    }

    public SubscriptionFactory(
        ISubscriptionPlanEntityService planEs,
        ICustomerSubscriptionEntityService subscriptionEs,
        ICustomerEntityService customerEs,
        IBillingPeriodEntityService billingPeriodEs,
        ISlnBranchEntityService branchEs,
        ICustomerPortalModuleEntityService moduleEs,
        ServicePricingFactory servicePricingFactory,
        IUnitOfWork uow)
    {
        _planEs = planEs;
        _subscriptionEs = subscriptionEs;
        _customerEs = customerEs;
        _billingPeriodEs = billingPeriodEs;
        _branchEs = branchEs;
        _moduleEs = moduleEs;
        _servicePricingFactory = servicePricingFactory;
        _uow = uow;
    }

    // ═══ PLAN YÖNETİMİ ═══

    public async Task<List<SubscriptionPlan>> GetPlansAsync()
    {
        await EnsureDefaultSalonPlansAsync();
        return await _planEs.GetAllQueryable().OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync();
    }

    private async Task EnsureDefaultSalonPlansAsync()
    {
        var plans = await _planEs.GetAllQueryable().OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync();
        var changed = false;

        foreach (var standard in DefaultSalonPlans)
        {
            var plan = plans.FirstOrDefault(p => p.IntervalMonths == standard.IntervalMonths);
            if (plan == null)
            {
                plan = new SubscriptionPlan
                {
                    Name = standard.Name,
                    IntervalMonths = standard.IntervalMonths,
                    DiscountPercent = standard.DiscountPercent,
                    SortOrder = standard.SortOrder,
                    IsActive = true
                };
                _planEs.Add(plan);
                plans.Add(plan);
                changed = true;
                continue;
            }

            if (ShouldNormalizePlanName(plan))
            {
                plan.Name = standard.Name;
                changed = true;
            }

            if (plan.DiscountPercent <= 0m && standard.DiscountPercent > 0m && ShouldNormalizePlanDiscount(plan))
            {
                plan.DiscountPercent = standard.DiscountPercent;
                changed = true;
            }

            if (plan.SortOrder <= 0)
            {
                plan.SortOrder = standard.SortOrder;
                changed = true;
            }
        }

        if (changed)
            await _uow.SaveChangesAsync();
    }

    private static bool ShouldNormalizePlanName(SubscriptionPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.Name))
            return true;

        var normalized = plan.Name.Trim().ToLowerInvariant();
        return normalized is "aylik" or "aylik plan" or "1 ay" or "1 aylik"
            or "3 aylik" or "3 ay" or "6 aylik" or "6 ay"
            or "yillik" or "12 aylik" or "12 ay";
    }

    private static bool ShouldNormalizePlanDiscount(SubscriptionPlan plan)
    {
        var normalized = (plan.Name ?? "").Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized)
            || normalized.Contains("indirim", StringComparison.OrdinalIgnoreCase)
            || normalized is "3 aylik" or "3 ay" or "6 aylik" or "6 ay" or "yillik" or "12 aylik" or "12 ay";
    }

    public async Task<SubscriptionPlan> CreatePlanAsync(string name, int intervalMonths, decimal discountPercent)
    {
        var plan = new SubscriptionPlan
        {
            Name = name,
            IntervalMonths = intervalMonths,
            DiscountPercent = discountPercent,
            SortOrder = await _planEs.GetAllQueryable().CountAsync() + 1
        };
        _planEs.Add(plan);
        await _uow.SaveChangesAsync();
        return plan;
    }

    public async Task<(bool Success, string? Error)> UpdatePlanAsync(int id, string name, int intervalMonths, decimal discountPercent, bool isActive)
    {
        var plan = await _planEs.GetByIdAsync(id);
        if (plan == null) return (false, "Plan bulunamadı.");
        plan.Name = name;
        plan.IntervalMonths = intervalMonths;
        plan.DiscountPercent = discountPercent;
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

        var list = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        var custIds = list.Select(s => s.CustomerId).Distinct().ToList();
        var lastMap = await BuildLastSalonPeriodStartMapAsync(custIds);

        return list.ConvertAll(s =>
        {
            var nextAccrual = ResolveNextSalonAccrualStartUtc(s, lastMap);
            return (object)new
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
                discountPercentOverride = s.DiscountPercentOverride,
                s.BillingDay,
                nextBillingDate = nextAccrual,
                s.StatusId,
                statusName = s.StatusId == 1 ? "Aktif" : s.StatusId == 2 ? "Askıda" : "İptal",
                s.PaymentGraceDays,
                s.CreatedAt
            };
        });
    }

    public async Task<(object? Result, string? Error)> CreateSubscriptionAsync(int customerId, int planId, DateTime startDate, decimal monthlyPrice, int? branchId = null, decimal? discountPercentOverride = null)
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

        decimal periodPrice;
        if (monthlyPrice > 0m)
            periodPrice = Math.Round(monthlyPrice * plan.IntervalMonths, 2, MidpointRounding.AwayFromZero);
        else
            periodPrice = 0m;

        var sub = new CustomerSubscription
        {
            CustomerId = customerId,
            PlanId = planId,
            BranchId = branchId,
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            MonthlyPrice = monthlyPrice,
            PeriodPrice = Math.Round(periodPrice, 2),
            DiscountPercentOverride = discountPercentOverride,
            BillingDay = startDate.Day,
            StatusId = 1
        };

        _subscriptionEs.Add(sub);

        // Customer'in BillingAnchorDay'ini ayarla
        customer.BillingAnchorDay = startDate.Day;

        await _uow.SaveChangesAsync();
        sub.Plan = plan;
        var lastMap = await BuildLastSalonPeriodStartMapAsync([customerId]);
        var nextAccrual = ResolveNextSalonAccrualStartUtc(sub, lastMap);
        await RefreshSubscriptionDisplayMonthlyPriceAsync(customerId, saveChanges: true);
        return (new { sub.Id, nextBillingDate = nextAccrual, sub.PeriodPrice }, null);
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
    /// Belirtilen ay icin tum aktif aboneliklerin tahakkuklarini olusturur.
    /// Sonraki kesim tarihi, son SalonPlatform tahakkukunun PeriodStartDate + plan araligi ile hesaplanir.
    /// </summary>
    public async Task<(int Created, int Skipped, int EligibleSubscriptions)> GenerateBillingForMonthAsync(int year, int month)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var activeSubscriptions = await _subscriptionEs.GetAllQueryable()
            .Where(s => s.StatusId == 1 && !s.Customer.IsTest)
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .ToListAsync();

        var custIds = activeSubscriptions.Select(s => s.CustomerId).Distinct().ToList();
        var lastMap = await BuildLastSalonPeriodStartMapAsync(custIds);

        var eligible = 0;
        int created = 0, skipped = 0;

        foreach (var sub in activeSubscriptions)
        {
            var next = ResolveNextSalonAccrualStartUtc(sub, lastMap);
            if (next < monthStart || next >= monthEnd) continue;

            eligible++;
            if (await TryCreateSalonBillingPeriodIfMissingAsync(sub, year, month, next)) created++;
            else skipped++;
        }

        if (created > 0)
            await _uow.SaveChangesAsync();

        return (created, skipped, eligible);
    }

    /// <summary>
    /// Müşteriler — toplu faturalama: aktif <see cref="CustomerSubscription"/> olmayan Salon müşterileri için
    /// seçilen yıl/ay + <see cref="Customer.BillingAnchorDay"/> (yoksa otomatik belirlenen gün, kayda yazılır) ile liste fiyatından aylık platform tahakkuku.
    /// </summary>
    public async Task<(int Created, int Skipped, int EligibleWithoutSubscription)> GenerateSalonPlatformBillingForMonthWithoutSubscriptionAsync(int year, int month)
    {
        if (month is < 1 or > 12) return (0, 0, 0);
        if (year is < 2020 or > 2100) return (0, 0, 0);

        var customers = await _customerEs.GetAllQueryable()
            .Where(c => c.IsActive && !c.IsTest)
            .Where(c => c.Products.Any(p => p.ProductTypeId == ProductTypes.Ids.Salon && p.IsActive))
            .Where(c => !c.Subscriptions.Any(s => s.StatusId == SubscriptionStatuses.Ids.Active))
            .Select(c => c.Id)
            .ToListAsync();

        var eligible = customers.Count;
        var created = 0;
        var skipped = 0;

        foreach (var customerId in customers)
        {
            if (await TryCreateSalonBillingPeriodForNonSubscriberAsync(customerId, year, month))
                created++;
            else
                skipped++;
        }

        if (created > 0)
            await _uow.SaveChangesAsync();

        return (created, skipped, eligible);
    }

    public async Task CreateInitialBillingPeriodForCustomerAsync(int customerId)
    {
        var sub = await _subscriptionEs.GetAllQueryable()
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && PayableSubscriptionStatusIds.Contains(s.StatusId));
        if (sub == null || sub.Customer.IsTest) return;

        var lastMap = await BuildLastSalonPeriodStartMapAsync([customerId]);
        var next = ResolveNextSalonAccrualStartUtc(sub, lastMap);
        if (!await TryCreateSalonBillingPeriodIfMissingAsync(sub, next.Year, next.Month, next)) return;

        await _uow.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? Error)> CreateManualSalonPlatformPeriodAsync(int customerId, DateTime periodStartDate,
        string? notes)
    {
        var customer = await _customerEs.GetByIdAsync(customerId);
        if (customer == null) return (false, "Müşteri bulunamadı.");
        if (customer.IsTest) return (false, "Test müşterisi için salon platform tahakkuku oluşturulmaz.");

        var start = DateTime.SpecifyKind(periodStartDate.Date, DateTimeKind.Utc);
        var year = start.Year;
        var month = start.Month;

        var sub = await _subscriptionEs.GetAllQueryable()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && PayableSubscriptionStatusIds.Contains(s.StatusId));
        if (sub == null) return (false, "Aktif abonelik yok.");
        var intervalMonths = GetEffectiveBillingIntervalMonths(sub.Plan);

        var salonExists = await _billingPeriodEs.GetAllQueryable()
            .AnyAsync(p => p.CustomerId == customerId && p.Year == year && p.Month == month
                && p.BillingKindId == CustomerBillingKinds.SalonPlatform);
        if (salonExists) return (false, $"{month:00}/{year} için salon platform tahakkuku zaten var.");

        customer.BillingAnchorDay = start.Day;

        var misTaggedCallCenterRow = await _billingPeriodEs.GetAllQueryable()
            .Include(p => p.ModuleLines)
            .FirstOrDefaultAsync(p => p.CustomerId == customerId && p.Year == year && p.Month == month
                && p.BillingKindId == CustomerBillingKinds.CallCenter);

        if (misTaggedCallCenterRow != null && !await HasActiveCallCenterProductAsync(customerId))
        {
            misTaggedCallCenterRow.BillingKindId = CustomerBillingKinds.SalonPlatform;
            await ApplySalonBillingPeriodTotalsAsync(misTaggedCallCenterRow, sub);
            if (!string.IsNullOrWhiteSpace(notes))
            {
                misTaggedCallCenterRow.Notes = string.IsNullOrWhiteSpace(misTaggedCallCenterRow.Notes)
                    ? notes
                    : misTaggedCallCenterRow.Notes + " | " + notes;
            }

            await _uow.SaveChangesAsync();
            return (true, null);
        }

        var (basePeriodAmount, branchAmount, moduleAmount, packageLines, branchCount) =
            await ComputeSalonBillingPeriodComponentsAsync(sub);
        (basePeriodAmount, branchAmount, moduleAmount) =
            await ApplyPaidPlanNonZeroFloorAsync(sub, basePeriodAmount, branchAmount, moduleAmount);

        // Yönetimden manuel tahakkuk: PeriodPrice henüz 0 kayıtlı kalsa bile liste fiyatıyla 0 TL + "ödendi" oluşmasın.
        var listBase = await ComputeSalonPlatformListBasePeriodAmountAsync(sub);
        var coerced = CoerceSalonTotalsWithListBaseIfZero(basePeriodAmount, branchAmount, moduleAmount, listBase);
        basePeriodAmount = coerced.Base;
        branchAmount = coerced.Branch;
        moduleAmount = coerced.Module;
        ApplyPaidContractPeriodFloor(sub, ref basePeriodAmount, ref branchAmount, ref moduleAmount);
        var billingTotal = basePeriodAmount + branchAmount + moduleAmount;

        if (sub.PeriodPrice > 0m && billingTotal <= 0m)
            return (false, "Platform tutarı 0 TL; PeriodPrice veya fiyat dönemini kontrol edin.");

        var period = new CustomerBillingPeriod
        {
            CustomerId = customerId,
            BillingKindId = CustomerBillingKinds.SalonPlatform,
            Year = year,
            Month = month,
            PeriodStartDate = start,
            PeriodEndDate = start.AddMonths(intervalMonths),
            UserCount = branchCount,
            UnitPrice = basePeriodAmount,
            Amount = basePeriodAmount + branchAmount,
            ServiceAmount = moduleAmount,
            StatusId = sub.PeriodPrice <= 0m && billingTotal <= 0m
                ? BillingPeriodStatuses.Ids.Paid
                : BillingPeriodStatuses.Ids.Draft,
            IsPaid = sub.PeriodPrice <= 0m && billingTotal <= 0m,
            PaidAt = sub.PeriodPrice <= 0m && billingTotal <= 0m ? DateTime.UtcNow : null,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        _billingPeriodEs.Add(period);
        foreach (var line in packageLines)
            period.ModuleLines.Add(line);

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Tahakkuk satiri yoksa olusturur. Sonraki kesim <paramref name="accrualStartUtc"/> ile verilir (persist edilen PeriodStartDate).
    /// Kayit persiste etmez — cagiran SaveChanges cagirmalidir.
    /// </summary>
    private async Task<bool> TryCreateSalonBillingPeriodIfMissingAsync(CustomerSubscription sub, int year, int month,
        DateTime accrualStartUtc)
    {
        accrualStartUtc = DateTime.SpecifyKind(accrualStartUtc, DateTimeKind.Utc);
        if (accrualStartUtc.Year != year || accrualStartUtc.Month != month) return false;

        var intervalMonths = GetEffectiveBillingIntervalMonths(sub.Plan);

        var alreadyExists = await _billingPeriodEs.GetAllQueryable()
            .AnyAsync(p => p.CustomerId == sub.CustomerId && p.Year == year && p.Month == month
                && p.BillingKindId == CustomerBillingKinds.SalonPlatform);

        if (alreadyExists) return false;

        var misTaggedCallCenterRow = await _billingPeriodEs.GetAllQueryable()
            .Include(p => p.ModuleLines)
            .FirstOrDefaultAsync(p => p.CustomerId == sub.CustomerId && p.Year == year && p.Month == month
                && p.BillingKindId == CustomerBillingKinds.CallCenter);

        if (misTaggedCallCenterRow != null && !await HasActiveCallCenterProductAsync(sub.CustomerId))
        {
            misTaggedCallCenterRow.BillingKindId = CustomerBillingKinds.SalonPlatform;
            await ApplySalonBillingPeriodTotalsAsync(misTaggedCallCenterRow, sub);
            await EnsureCustomerBillingAnchorDayFromAccrualAsync(sub.CustomerId, accrualStartUtc.Day);
            await _uow.SaveChangesAsync();
            return false;
        }

        await EnsureCustomerBillingAnchorDayFromAccrualAsync(sub.CustomerId, accrualStartUtc.Day);

        var (basePeriodAmount, branchAmount, moduleAmount, packageLines, branchCount) =
            await ComputeSalonBillingPeriodComponentsAsync(sub);
        (basePeriodAmount, branchAmount, moduleAmount) =
            await ApplyPaidPlanNonZeroFloorAsync(sub, basePeriodAmount, branchAmount, moduleAmount);

        var listBaseAuto = await ComputeSalonPlatformListBasePeriodAmountAsync(sub);
        var coercedAuto = CoerceSalonTotalsWithListBaseIfZero(basePeriodAmount, branchAmount, moduleAmount, listBaseAuto);
        basePeriodAmount = coercedAuto.Base;
        branchAmount = coercedAuto.Branch;
        moduleAmount = coercedAuto.Module;
        ApplyPaidContractPeriodFloor(sub, ref basePeriodAmount, ref branchAmount, ref moduleAmount);
        var billingTotal = basePeriodAmount + branchAmount + moduleAmount;

        // Deneme / PeriodPrice=0: gerçek borç yoksa otomatik kapat; ücretli planda hep taslak (gerçek tahsilat).
        var markSettled = sub.PeriodPrice <= 0m && billingTotal <= 0m;

        var period = new CustomerBillingPeriod
        {
            CustomerId = sub.CustomerId,
            BillingKindId = CustomerBillingKinds.SalonPlatform,
            Year = year,
            Month = month,
            PeriodStartDate = accrualStartUtc,
            PeriodEndDate = accrualStartUtc.AddMonths(intervalMonths),
            UserCount = branchCount,
            UnitPrice = basePeriodAmount,
            Amount = basePeriodAmount + branchAmount,
            ServiceAmount = moduleAmount,
            StatusId = markSettled ? BillingPeriodStatuses.Ids.Paid : BillingPeriodStatuses.Ids.Draft,
            IsPaid = markSettled,
            PaidAt = markSettled ? DateTime.UtcNow : null
        };

        _billingPeriodEs.Add(period);

        foreach (var line in packageLines)
            period.ModuleLines.Add(line);

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

    /// <summary>
    /// Ödenmemiş platform borcu: önce SalonPlatform satırı; yoksa yalnızca Salon müşterisinde
    /// yanlış türde kalmış CallCenter tahakkuku — tutar 0 olsa bile güncel kırılımla borç varsa dikkate alınır.
    /// </summary>
    private async Task<CustomerBillingPeriod?> GetOldestUnpaidSalonDebtPeriodAsync(int customerId)
    {
        var oldestSln = await _billingPeriodEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && p.BillingKindId == CustomerBillingKinds.SalonPlatform
                && !p.IsPaid && p.StatusId != BillingPeriodStatuses.Ids.Paid
                && p.Amount + p.ServiceAmount > 0m)
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .FirstOrDefaultAsync();
        if (oldestSln != null) return oldestSln;

        var br = await GetSalonBillingBreakdownForCustomerAsync(customerId);
        var effBr = (br?.PlatformAmount ?? 0m) + (br?.ModuleAmount ?? 0m);

        var oldestSlnZero = await _billingPeriodEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && p.BillingKindId == CustomerBillingKinds.SalonPlatform
                && !p.IsPaid && p.StatusId != BillingPeriodStatuses.Ids.Paid
                && p.Amount + p.ServiceAmount <= 0m)
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .FirstOrDefaultAsync();
        if (oldestSlnZero != null && effBr > 0m)
            return oldestSlnZero;

        if (await HasActiveCallCenterProductAsync(customerId)) return null;

        var oldestCc = await _billingPeriodEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && p.BillingKindId == CustomerBillingKinds.CallCenter
                && !p.IsPaid && p.StatusId != BillingPeriodStatuses.Ids.Paid)
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .FirstOrDefaultAsync();
        if (oldestCc == null) return null;

        if (effBr <= 0m) return null;

        return oldestCc;
    }

    private async Task<CustomerBillingPeriod?> EnsureSalonSubscriptionPeriodForPaymentAsync(CustomerSubscription sub)
    {
        var existing = await GetOldestUnpaidSalonDebtPeriodAsync(sub.CustomerId);
        if (existing != null) return existing;

        if (sub.PeriodPrice > 0m)
        {
            var hasAnySalonPeriod = await _billingPeriodEs.GetAllQueryable()
                .AnyAsync(p => p.CustomerId == sub.CustomerId && p.BillingKindId == CustomerBillingKinds.SalonPlatform);
            if (hasAnySalonPeriod) return null;
        }

        var customer = await _customerEs.GetByIdAsync(sub.CustomerId);
        if (customer == null || customer.IsTest) return null;

        var lastMap = await BuildLastSalonPeriodStartMapAsync([sub.CustomerId]);
        var next = ResolveNextSalonAccrualStartUtc(sub, lastMap);
        if (await TryCreateSalonBillingPeriodIfMissingAsync(sub, next.Year, next.Month, next))
            await _uow.SaveChangesAsync();

        return await GetOldestUnpaidSalonDebtPeriodAsync(sub.CustomerId);
    }

    /// <inheritdoc />
    public async Task<(CustomerBillingPeriod Period, decimal PayAmount)?> TryResolveSalonSubscriptionPaymentAsync(int customerId)
    {
        var sub = await _subscriptionEs.GetAllQueryable()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && PayableSubscriptionStatusIds.Contains(s.StatusId));

        var period = sub != null
            ? await EnsureSalonSubscriptionPeriodForPaymentAsync(sub)
            : await GetOldestUnpaidSalonDebtPeriodAsync(customerId);
        if (period == null) return null;

        var pay = period.Amount + period.ServiceAmount;
        if (pay <= 0m)
        {
            var br = await GetSalonBillingBreakdownForCustomerAsync(customerId);
            if (br.HasValue)
                pay = br.Value.PlatformAmount + br.Value.ModuleAmount;
            else
            {
                var (b, brAmt, mod, _, _) = await ComputeSalonBillingPeriodComponentsForNonSubscriberAsync(customerId);
                pay = b + brAmt + mod;
            }
        }

        if (pay <= 0m) return null;
        return (period, pay);
    }

    public async Task<object> GetMySubscriptionAsync(int customerId)
    {
        var sub = await GetCustomerSubscriptionsAsync(customerId);
        var activeSub = sub.FirstOrDefault();

        var activeEntity = await _subscriptionEs.GetAllQueryable()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && PayableSubscriptionStatusIds.Contains(s.StatusId));

        var salonOnly = !await HasActiveCallCenterProductAsync(customerId);
        var breakdown = await GetSalonBillingBreakdownForCustomerAsync(customerId);

        var unpaidRaw = await _billingPeriodEs.GetAllQueryable()
            .Where(p => p.CustomerId == customerId
                && !p.IsPaid && p.StatusId != BillingPeriodStatuses.Ids.Paid
                && (
                    p.BillingKindId == CustomerBillingKinds.SalonPlatform
                    || (salonOnly && p.BillingKindId == CustomerBillingKinds.CallCenter)
                ))
            .Include(p => p.ModuleLines)
            .OrderByDescending(p => p.PeriodStartDate)
            .ToListAsync();

        var unpaidBillings = unpaidRaw
            .Select(p =>
            {
                var amt = p.Amount;
                var svc = p.ServiceAmount;
                if (amt + svc <= 0m && breakdown.HasValue)
                {
                    amt = breakdown.Value.PlatformAmount;
                    svc = breakdown.Value.ModuleAmount;
                }

                return new
                {
                    p.Id,
                    p.Year,
                    p.Month,
                    Amount = amt,
                    ServiceAmount = svc,
                    total = amt + svc,
                    p.PeriodStartDate,
                    p.PeriodEndDate,
                    p.StatusId,
                    isUpcoming = false,
                    salonModuleLines = p.ModuleLines
                        .OrderBy(l => l.PackageGroupId ?? int.MaxValue)
                        .ThenBy(l => l.ModuleId ?? int.MaxValue)
                        .Select(l => new { l.PackageGroupId, l.ModuleId, l.ModuleDisplayName, l.MonthlyUnitPrice, l.LineAmount })
                        .ToList()
                };
            })
            .Where(x => x.total > 0m)
            .Cast<object>()
            .ToList();

        if (unpaidBillings.Count == 0 && activeEntity != null && activeEntity.PeriodPrice <= 0m)
        {
            var next = await GetNextSalonAccrualUtcAsync(customerId) ?? activeEntity.StartDate;
            var virtualBreakdown = await GetSalonBillingBreakdownForCustomerAsync(customerId);
            if (virtualBreakdown.HasValue)
            {
                var virtualTotal = virtualBreakdown.Value.PlatformAmount + virtualBreakdown.Value.ModuleAmount;
                if (virtualTotal > 0m)
                {
                    unpaidBillings.Add(new
                    {
                        Id = 0,
                        Year = next.Year,
                        Month = next.Month,
                        Amount = virtualBreakdown.Value.PlatformAmount,
                        ServiceAmount = virtualBreakdown.Value.ModuleAmount,
                        total = virtualTotal,
                        PeriodStartDate = next,
                        PeriodEndDate = next.AddMonths(GetEffectiveBillingIntervalMonths(activeEntity.Plan)),
                        StatusId = BillingPeriodStatuses.Ids.Draft,
                        isUpcoming = true,
                        salonModuleLines = Array.Empty<object>()
                    });
                }
            }
        }

        return new { subscription = activeSub, unpaidBillings };
    }

    public async Task<object> GetSalonBannerAsync(int customerId)
    {
        var activeSub = await _subscriptionEs.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.StatusId == SubscriptionStatuses.Ids.Active);
        if (activeSub != null && activeSub.PeriodPrice <= 0m)
            return new { overdue = (object?)null, info = (object?)null };

        var now = DateTime.UtcNow;
        var oldestUnpaid = await GetOldestUnpaidSalonDebtPeriodAsync(customerId);

        object? overdue = null;
        object? info = null;

        if (oldestUnpaid != null)
        {
            var graceEnd = oldestUnpaid.PeriodStartDate.AddDays(PlatformBillingAccessPolicy.UnpaidGraceDaysAfterPeriodStart);
            if (graceEnd < now)
            {
                overdue = new
                {
                    message = $"Ödenmemiş platform tahakkukunuz bulunmaktadır ({oldestUnpaid.Year}/{oldestUnpaid.Month:D2}). Lütfen ödeme yapın."
                };
            }
            else
            {
                var daysLeft = (graceEnd.Date - now.Date).Days;
                info = new
                {
                    message = $"Ödenmemiş tahakkukunuz var. Son ödeme tarihi: {graceEnd:yyyy-MM-dd} ({Math.Max(0, daysLeft)} gün kaldı)."
                };
            }
        }

        return new { overdue, info };
    }

    public async Task<object> GetSalonPanelAccessAsync(int customerId)
    {
        var customer = await _customerEs.GetByIdAsync(customerId);
        if (customer?.IsTest == true)
            return new { canAccessPanel = true, hasActiveSubscription = true };

        var sub = await _subscriptionEs.GetAllQueryable()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && PayableSubscriptionStatusIds.Contains(s.StatusId));

        if (sub == null)
        {
            // Aktif abonelik kaydı yok; platform borcu + grace (banner ile aynı kural)
            var oldestUnpaid = await GetOldestUnpaidSalonDebtPeriodAsync(customerId);
            if (oldestUnpaid == null)
                return new { canAccessPanel = false, hasActiveSubscription = false };

            var graceEndNs = oldestUnpaid.PeriodStartDate.AddDays(PlatformBillingAccessPolicy.UnpaidGraceDaysAfterPeriodStart);
            var canNs = graceEndNs >= DateTime.UtcNow;
            return new { canAccessPanel = canNs, hasActiveSubscription = false };
        }

        var now = DateTime.UtcNow;
        bool canAccess;
        if (sub.PeriodPrice == 0m)
        {
            var lastMap = await BuildLastSalonPeriodStartMapAsync([customerId]);
            var next = ResolveNextSalonAccrualStartUtc(sub, lastMap);
            canAccess = next.AddDays(PlatformBillingAccessPolicy.TrialSuspensionGraceDays) >= now;
        }
        else
        {
            var oldestUnpaid = await GetOldestUnpaidSalonDebtPeriodAsync(customerId);

            if (oldestUnpaid == null)
                canAccess = true;
            else
            {
                var graceEnd = oldestUnpaid.PeriodStartDate.AddDays(PlatformBillingAccessPolicy.UnpaidGraceDaysAfterPeriodStart);
                canAccess = graceEnd >= now;
            }
        }

        return new { canAccessPanel = canAccess, hasActiveSubscription = sub.StatusId == SubscriptionStatuses.Ids.Active };
    }

    public async Task RefreshSubscriptionDisplayMonthlyPriceAsync(int customerId, bool saveChanges = true)
    {
        var subs = await _subscriptionEs.GetAllQueryable()
            .Include(s => s.Plan)
            .Where(s => s.CustomerId == customerId && s.StatusId == SubscriptionStatuses.Ids.Active)
            .ToListAsync();

        var changed = false;
        foreach (var sub in subs)
        {
            var interval = GetEffectiveBillingIntervalMonths(sub.Plan);

            var basePeriodAmount = await ComputeSalonPlatformBasePeriodAmountAsync(sub);
            var branchCount = await _branchEs.GetAllQueryable().CountAsync(b => b.CustomerId == customerId && b.IsActive);
            var branchAmount = await ComputeSalonBranchPeriodAmountAsync(sub, branchCount);
            ExcludeListCoreFromBaseWhenMultipleBranches(sub, branchCount, ref basePeriodAmount);
            var (_, moduleAmount) = await BuildSalonPackageLinesAsync(customerId, interval);

            var periodTotal = basePeriodAmount + branchAmount + moduleAmount;
            var displayMonthly = Math.Round(periodTotal / interval, 2, MidpointRounding.AwayFromZero);

            if (sub.MonthlyPrice != displayMonthly)
            {
                sub.MonthlyPrice = displayMonthly;
                changed = true;
            }
        }

        if (changed && saveChanges)
            await _uow.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<(decimal PlatformAmount, decimal ModuleAmount)?> GetSalonBillingBreakdownForCustomerAsync(int customerId)
    {
        var sub = await _subscriptionEs.GetAllQueryable()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && PayableSubscriptionStatusIds.Contains(s.StatusId));
        if (sub == null) return null;

        var (basePeriodAmount, branchAmount, moduleAmount, _, _) =
            await ComputeSalonBillingPeriodComponentsAsync(sub);
        (basePeriodAmount, branchAmount, moduleAmount) =
            await ApplyPaidPlanNonZeroFloorAsync(sub, basePeriodAmount, branchAmount, moduleAmount);

        var listBase = await ComputeSalonPlatformListBasePeriodAmountAsync(sub);
        var coerced = CoerceSalonTotalsWithListBaseIfZero(basePeriodAmount, branchAmount, moduleAmount, listBase);
        basePeriodAmount = coerced.Base;
        branchAmount = coerced.Branch;
        moduleAmount = coerced.Module;
        ApplyPaidContractPeriodFloor(sub, ref basePeriodAmount, ref branchAmount, ref moduleAmount);

        return (basePeriodAmount + branchAmount, moduleAmount);
    }

    /// <summary>
    /// Temel paket (Core) dönem tutarı.
    /// <see cref="CustomerSubscription.PeriodPrice"/>&gt;0 ve <see cref="CustomerSubscription.MonthlyPrice"/>&gt;0 ise anlaşılmış aylık×interval (yönetim sözleşmesi).
    /// Aksi halde aktif dönem Core × interval × (1 − plan indirimi). Not: PeriodPrice=0 iken MonthlyPrice yalnızca özettir (Refresh); tahakkükte dikkate alınmaz.
    /// </summary>
    private async Task<decimal> ComputeSalonPlatformBasePeriodAmountAsync(CustomerSubscription sub)
    {
        var prices = await _servicePricingFactory.GetActiveSalonPackagePricesAsync();
        var coreMonthlyList = prices.TryGetValue(SalonModuleGroups.Ids.Core, out var m) ? m : SalonModuleGroups.Core.MonthlyPrice;
        if (coreMonthlyList <= 0m)
            coreMonthlyList = SalonModuleGroups.Core.MonthlyPrice;

        var interval = GetEffectiveBillingIntervalMonths(sub.Plan);

        if (sub.PeriodPrice > 0m && sub.MonthlyPrice > 0m)
            return Math.Round(sub.MonthlyPrice * interval, 2, MidpointRounding.AwayFromZero);

        var disc = GetEffectiveDiscountPercent(sub);
        return Math.Round(coreMonthlyList * interval * (1 - disc / 100), 2,
            MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Çoklu şube: tüm aktif şubeler × aktif dönem Temel Paket (Core) aylık × interval brüt, sonra eşik indirimi.
    /// Tek şubede 0 (Temel Paket taban satırında). Liste fiyatında çoklu şubede Core yalnız burada — tabanda Core yok.
    /// </summary>
    private async Task<decimal> ComputeSalonBranchPeriodAmountForIntervalAsync(int branchCount, int intervalMonths)
    {
        if (branchCount <= 1) return 0m;

        var prices = await _servicePricingFactory.GetActiveSalonPackagePricesAsync();
        var coreMonthly = prices.TryGetValue(SalonModuleGroups.Ids.Core, out var m) ? m : SalonModuleGroups.Core.MonthlyPrice;
        if (coreMonthly <= 0m)
            coreMonthly = SalonModuleGroups.Core.MonthlyPrice;

        var gross = branchCount * coreMonthly * intervalMonths;
        var disc = await _servicePricingFactory.GetActiveBranchDiscountPercentForTotalBranchesAsync(branchCount);
        return Math.Round(gross * (1 - disc / 100), 2, MidpointRounding.AwayFromZero);
    }

    private Task<decimal> ComputeSalonBranchPeriodAmountAsync(CustomerSubscription sub, int branchCount) =>
        ComputeSalonBranchPeriodAmountForIntervalAsync(branchCount, GetEffectiveBillingIntervalMonths(sub.Plan));

    private async Task<(decimal BasePeriodAmount, decimal BranchAmount, decimal ModuleAmount, List<CustomerBillingPeriodModuleLine> Lines, int BranchCount)>
        ComputeSalonBillingPeriodComponentsAsync(CustomerSubscription sub)
    {
        var basePeriodAmount = await ComputeSalonPlatformBasePeriodAmountAsync(sub);
        var interval = GetEffectiveBillingIntervalMonths(sub.Plan);
        var (lines, moduleAmount) = await BuildSalonPackageLinesAsync(sub.CustomerId, interval);
        var branchCount = await _branchEs.GetAllQueryable().CountAsync(b => b.CustomerId == sub.CustomerId && b.IsActive);
        var branchAmount = await ComputeSalonBranchPeriodAmountAsync(sub, branchCount);
        ExcludeListCoreFromBaseWhenMultipleBranches(sub, branchCount, ref basePeriodAmount);
        return (basePeriodAmount, branchAmount, moduleAmount, lines, branchCount);
    }

    private async Task ApplySalonBillingPeriodTotalsAsync(CustomerBillingPeriod period, CustomerSubscription sub)
    {
        period.ModuleLines.Clear();
        var (basePeriodAmount, branchAmount, moduleAmount, lines, branchCount) =
            await ComputeSalonBillingPeriodComponentsAsync(sub);
        (basePeriodAmount, branchAmount, moduleAmount) =
            await ApplyPaidPlanNonZeroFloorAsync(sub, basePeriodAmount, branchAmount, moduleAmount);

        var listBaseApply = await ComputeSalonPlatformListBasePeriodAmountAsync(sub);
        var coercedApply = CoerceSalonTotalsWithListBaseIfZero(basePeriodAmount, branchAmount, moduleAmount, listBaseApply);
        basePeriodAmount = coercedApply.Base;
        branchAmount = coercedApply.Branch;
        moduleAmount = coercedApply.Module;
        ApplyPaidContractPeriodFloor(sub, ref basePeriodAmount, ref branchAmount, ref moduleAmount);
        var billingTotalApply = basePeriodAmount + branchAmount + moduleAmount;

        // Ücretli planda satırı otomatik "ödendi" yapma; DB tutarı 0 kalsa taslak kalsın.
        var markSettledApply = sub.PeriodPrice <= 0m && billingTotalApply <= 0m;

        period.UserCount = branchCount;
        period.UnitPrice = basePeriodAmount;
        period.Amount = basePeriodAmount + branchAmount;
        period.ServiceAmount = moduleAmount;
        period.StatusId = markSettledApply ? BillingPeriodStatuses.Ids.Paid : BillingPeriodStatuses.Ids.Draft;
        period.IsPaid = markSettledApply;
        period.PaidAt = markSettledApply ? DateTime.UtcNow : null;
        foreach (var line in lines)
            period.ModuleLines.Add(line);
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
            if (SalonModuleGroups.IsCrmServiceGroup(groupId.Value)) continue;

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

    private async Task<(decimal BasePeriodAmount, decimal BranchAmount, decimal ModuleAmount, List<CustomerBillingPeriodModuleLine> Lines, int BranchCount)>
        ComputeSalonBillingPeriodComponentsForNonSubscriberAsync(int customerId)
    {
        const int intervalMonths = 1;
        var basePeriodAmount = await ComputeSalonPlatformListBasePeriodAmountForIntervalAsync(intervalMonths);
        var (lines, moduleAmount) = await BuildSalonPackageLinesAsync(customerId, intervalMonths);
        var branchCount = await _branchEs.GetAllQueryable().CountAsync(b => b.CustomerId == customerId && b.IsActive);
        var branchAmount = await ComputeSalonBranchPeriodAmountForIntervalAsync(branchCount, intervalMonths);
        if (branchCount > 1)
            basePeriodAmount = 0m;
        return (basePeriodAmount, branchAmount, moduleAmount, lines, branchCount);
    }

    private async Task ApplySalonBillingPeriodTotalsForNonSubscriberAsync(CustomerBillingPeriod period, int customerId)
    {
        period.ModuleLines.Clear();
        var (basePeriodAmount, branchAmount, moduleAmount, lines, branchCount) =
            await ComputeSalonBillingPeriodComponentsForNonSubscriberAsync(customerId);
        var listBase = await ComputeSalonPlatformListBasePeriodAmountForIntervalAsync(1);
        var coerced = CoerceSalonTotalsWithListBaseIfZero(basePeriodAmount, branchAmount, moduleAmount, listBase);
        basePeriodAmount = coerced.Base;
        branchAmount = coerced.Branch;
        moduleAmount = coerced.Module;
        var billingTotal = basePeriodAmount + branchAmount + moduleAmount;
        var markSettled = billingTotal <= 0m;

        period.UserCount = branchCount;
        period.UnitPrice = basePeriodAmount;
        period.Amount = basePeriodAmount + branchAmount;
        period.ServiceAmount = moduleAmount;
        period.StatusId = markSettled ? BillingPeriodStatuses.Ids.Paid : BillingPeriodStatuses.Ids.Draft;
        period.IsPaid = markSettled;
        period.PaidAt = markSettled ? DateTime.UtcNow : null;
        foreach (var line in lines)
            period.ModuleLines.Add(line);
    }

    private async Task EnsureCustomerBillingAnchorDayFromAccrualAsync(int customerId, int accrualDay)
    {
        var customer = await _customerEs.GetByIdAsync(customerId);
        if (customer == null || customer.IsTest || customer.BillingAnchorDay.HasValue) return;
        customer.BillingAnchorDay = accrualDay;
    }

    /// <returns><c>true</c> yeni <see cref="CustomerBillingKinds.SalonPlatform"/> satırı eklendi.</returns>
    private async Task<bool> TryCreateSalonBillingPeriodForNonSubscriberAsync(int customerId, int year, int month)
    {
        var customer = await _customerEs.GetByIdAsync(customerId);
        if (customer == null || customer.IsTest) return false;

        var alreadyExists = await _billingPeriodEs.GetAllQueryable()
            .AnyAsync(p => p.CustomerId == customerId && p.Year == year && p.Month == month
                && p.BillingKindId == CustomerBillingKinds.SalonPlatform);
        if (alreadyExists) return false;

        var startDay = BillingAnchorDayResolver.ResolvePeriodStartDay(year, month, customer.BillingAnchorDay);
        var periodStart = new DateTime(year, month, startDay, 0, 0, 0, DateTimeKind.Utc);
        const int intervalMonths = 1;

        var misTaggedCallCenterRow = await _billingPeriodEs.GetAllQueryable()
            .Include(p => p.ModuleLines)
            .FirstOrDefaultAsync(p => p.CustomerId == customerId && p.Year == year && p.Month == month
                && p.BillingKindId == CustomerBillingKinds.CallCenter);

        if (misTaggedCallCenterRow != null && !await HasActiveCallCenterProductAsync(customerId))
        {
            misTaggedCallCenterRow.BillingKindId = CustomerBillingKinds.SalonPlatform;
            misTaggedCallCenterRow.PeriodStartDate = periodStart;
            misTaggedCallCenterRow.PeriodEndDate = periodStart.AddMonths(intervalMonths);
            await ApplySalonBillingPeriodTotalsForNonSubscriberAsync(misTaggedCallCenterRow, customerId);
            if (!customer.BillingAnchorDay.HasValue)
                customer.BillingAnchorDay = startDay;
            await _uow.SaveChangesAsync();
            return false;
        }

        var (basePeriodAmount, branchAmount, moduleAmount, packageLines, branchCount) =
            await ComputeSalonBillingPeriodComponentsForNonSubscriberAsync(customerId);
        var listBase = await ComputeSalonPlatformListBasePeriodAmountForIntervalAsync(1);
        var coerced = CoerceSalonTotalsWithListBaseIfZero(basePeriodAmount, branchAmount, moduleAmount, listBase);
        basePeriodAmount = coerced.Base;
        branchAmount = coerced.Branch;
        moduleAmount = coerced.Module;
        var billingTotal = basePeriodAmount + branchAmount + moduleAmount;
        var markSettled = billingTotal <= 0m;

        if (!customer.BillingAnchorDay.HasValue)
            customer.BillingAnchorDay = startDay;

        var period = new CustomerBillingPeriod
        {
            CustomerId = customerId,
            BillingKindId = CustomerBillingKinds.SalonPlatform,
            Year = year,
            Month = month,
            PeriodStartDate = periodStart,
            PeriodEndDate = periodStart.AddMonths(intervalMonths),
            UserCount = branchCount,
            UnitPrice = basePeriodAmount,
            Amount = basePeriodAmount + branchAmount,
            ServiceAmount = moduleAmount,
            StatusId = markSettled ? BillingPeriodStatuses.Ids.Paid : BillingPeriodStatuses.Ids.Draft,
            IsPaid = markSettled,
            PaidAt = markSettled ? DateTime.UtcNow : null
        };

        _billingPeriodEs.Add(period);
        foreach (var line in packageLines)
            period.ModuleLines.Add(line);

        return true;
    }

    private async Task<bool> HasActiveCallCenterProductAsync(int customerId) =>
        await _customerEs.GetAllQueryable()
            .Where(c => c.Id == customerId)
            .AnyAsync(c => c.Products.Any(p =>
                p.ProductTypeId == ProductTypes.Ids.CallCenter && p.IsActive));

    public async Task<DateTime?> GetNextSalonAccrualUtcAsync(int customerId)
    {
        var sub = await _subscriptionEs.GetAllQueryable()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.StatusId == SubscriptionStatuses.Ids.Active);
        if (sub == null) return null;
        var map = await BuildLastSalonPeriodStartMapAsync([customerId]);
        return ResolveNextSalonAccrualStartUtc(sub, map);
    }
}
