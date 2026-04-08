using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SubscriptionFactory : ISubscriptionFactory
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;

    public SubscriptionFactory(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    // ═══ PLAN YÖNETİMİ ═══

    public async Task<List<SubscriptionPlan>> GetPlansAsync()
        => await _db.SubscriptionPlans.OrderBy(p => p.SortOrder).ToListAsync();

    public async Task<SubscriptionPlan> CreatePlanAsync(string name, int intervalMonths, decimal discountPercent)
    {
        var plan = new SubscriptionPlan
        {
            Name = name,
            IntervalMonths = intervalMonths,
            DiscountPercent = discountPercent,
            SortOrder = await _db.SubscriptionPlans.CountAsync() + 1
        };
        _db.SubscriptionPlans.Add(plan);
        await _uow.SaveChangesAsync();
        return plan;
    }

    public async Task<(bool Success, string? Error)> UpdatePlanAsync(int id, string name, int intervalMonths, decimal discountPercent, bool isActive)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(id);
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
        var plan = await _db.SubscriptionPlans.FindAsync(id);
        if (plan == null) return (false, "Plan bulunamadı.");
        var hasSubscribers = await _db.CustomerSubscriptions.AnyAsync(s => s.PlanId == id && s.StatusId == 1);
        if (hasSubscribers) return (false, "Aktif abonesi olan plan silinemez.");
        _db.SubscriptionPlans.Remove(plan);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ MÜŞTERİ ABONELİĞİ ═══

    public async Task<List<object>> GetCustomerSubscriptionsAsync(int? customerId = null)
    {
        var query = _db.CustomerSubscriptions
            .Include(s => s.Customer)
            .Include(s => s.Plan)
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

    public async Task<(object? Result, string? Error)> CreateSubscriptionAsync(int customerId, int planId, DateTime startDate, decimal monthlyPrice)
    {
        var customer = await _db.Customers.FindAsync(customerId);
        if (customer == null) return (null, "Müşteri bulunamadı.");

        var plan = await _db.SubscriptionPlans.FindAsync(planId);
        if (plan == null) return (null, "Plan bulunamadı.");

        // Aktif abonelik var mi kontrol
        var existing = await _db.CustomerSubscriptions.AnyAsync(s => s.CustomerId == customerId && s.StatusId == 1);
        if (existing) return (null, "Bu müşterinin zaten aktif aboneliği var.");

        var periodPrice = monthlyPrice * plan.IntervalMonths * (1 - plan.DiscountPercent / 100);

        var sub = new CustomerSubscription
        {
            CustomerId = customerId,
            PlanId = planId,
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            MonthlyPrice = monthlyPrice,
            PeriodPrice = Math.Round(periodPrice, 2),
            BillingDay = startDate.Day,
            NextBillingDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            StatusId = 1
        };

        _db.CustomerSubscriptions.Add(sub);

        // Customer'in BillingAnchorDay'ini ayarla
        customer.BillingAnchorDay = startDate.Day;

        await _uow.SaveChangesAsync();
        return (new { sub.Id, sub.NextBillingDate, sub.PeriodPrice }, null);
    }

    public async Task<(bool Success, string? Error)> CancelSubscriptionAsync(int subscriptionId)
    {
        var sub = await _db.CustomerSubscriptions.FindAsync(subscriptionId);
        if (sub == null) return (false, "Abonelik bulunamadı.");
        sub.StatusId = 3;
        sub.CancelledAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ TAHAKKUK OLUŞTURMA ═══

    /// <summary>
    /// Belirtilen ay icin tum aktif aboneliklerin tahakkuklarini olustur.
    /// Her aboneligin NextBillingDate'i bu ayin icinde mi kontrol eder.
    /// </summary>
    public async Task<(int Created, int Skipped)> GenerateBillingForMonthAsync(int year, int month)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var activeSubscriptions = await _db.CustomerSubscriptions
            .Where(s => s.StatusId == 1 && s.NextBillingDate >= monthStart && s.NextBillingDate < monthEnd)
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .ToListAsync();

        int created = 0, skipped = 0;

        foreach (var sub in activeSubscriptions)
        {
            // Bu donem icin zaten tahakkuk var mi?
            var alreadyExists = await _db.CustomerBillingPeriods
                .AnyAsync(p => p.CustomerId == sub.CustomerId && p.Year == year && p.Month == month);

            if (alreadyExists) { skipped++; continue; }

            // Aktif modullerin fiyatini hesapla
            var moduleAmount = await CalculateModuleAmountAsync(sub.CustomerId);

            var period = new CustomerBillingPeriod
            {
                CustomerId = sub.CustomerId,
                Year = year,
                Month = month,
                PeriodStartDate = sub.NextBillingDate,
                PeriodEndDate = sub.NextBillingDate.AddMonths(sub.Plan.IntervalMonths),
                UserCount = 0, // Salon'da user limiti yok
                UnitPrice = sub.PeriodPrice,
                Amount = sub.PeriodPrice,
                ServiceAmount = moduleAmount,
                StatusId = 1, // Draft
                IsPaid = false
            };

            _db.CustomerBillingPeriods.Add(period);

            // Sonraki tahakkuk tarihini guncelle
            sub.NextBillingDate = sub.NextBillingDate.AddMonths(sub.Plan.IntervalMonths);

            created++;
        }

        if (created > 0)
            await _uow.SaveChangesAsync();

        return (created, skipped);
    }

    /// <summary>Musterinin aktif opsiyonel modullerinin toplam aylik fiyati</summary>
    private async Task<decimal> CalculateModuleAmountAsync(int customerId)
    {
        var modules = await _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId && m.IsActive)
            .ToListAsync();

        var pricings = await _db.ModulePricings.ToListAsync();
        var pricingMap = pricings.ToDictionary(p => p.ModuleId);

        decimal total = 0;
        foreach (var mod in modules)
        {
            // Default modullerin fiyati 0
            var salonModule = CallCenter.Shared.Enums.SalonPortalModules.GetById(mod.ModuleId);
            if (salonModule?.IsDefault == true) continue;

            var effectivePrice = mod.MonthlyPrice ?? (pricingMap.TryGetValue(mod.ModuleId, out var p) ? p.MonthlyPrice : 0);
            total += effectivePrice;
        }

        return total;
    }
}
