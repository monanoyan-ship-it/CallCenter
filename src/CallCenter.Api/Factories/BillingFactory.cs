using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class BillingFactory : IBillingFactory
{
    private readonly IBillingPeriodEntityService _billingEs;
    private readonly ICustomerEntityService _customerEs;
    private readonly ICustomerPersonnelEntityService _personnelEs;
    private readonly ICustomerServiceSubscriptionEntityService _subscriptionEs;
    private readonly IServiceBillingItemEntityService _billingItemEs;
    private readonly IUnitOfWork _uow;

    public BillingFactory(
        IBillingPeriodEntityService billingEs,
        ICustomerEntityService customerEs,
        ICustomerPersonnelEntityService personnelEs,
        ICustomerServiceSubscriptionEntityService subscriptionEs,
        IServiceBillingItemEntityService billingItemEs,
        IUnitOfWork uow)
    {
        _billingEs = billingEs;
        _customerEs = customerEs;
        _personnelEs = personnelEs;
        _subscriptionEs = subscriptionEs;
        _billingItemEs = billingItemEs;
        _uow = uow;
    }

    public async Task<List<BillingPeriodDto>> GetByCustomerAsync(int customerId)
    {
        var periods = await _billingEs.GetAllQueryable()
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month)
            .Select(b => new BillingPeriodDto
            {
                Id = b.Id,
                CustomerId = b.CustomerId,
                CustomerName = b.Customer.Name,
                Year = b.Year,
                Month = b.Month,
                PeriodStartDate = b.PeriodStartDate,
                PeriodEndDate = b.PeriodEndDate,
                UserCount = b.UserCount,
                UnitPrice = b.UnitPrice,
                Amount = b.Amount,
                ServiceAmount = b.ServiceAmount,
                StatusId = b.StatusId,
                IsPaid = b.IsPaid,
                PaidAt = b.PaidAt,
                Notes = b.Notes
            })
            .ToListAsync();

        // StatusName map
        foreach (var p in periods)
            p.StatusName = BillingPeriodStatuses.GetById(p.StatusId)?.Description ?? "?";

        // Her donem icin hizmet kalemlerini yukle
        if (periods.Count > 0)
        {
            var billingItems = await _billingItemEs.GetAllQueryable()
                .Include(bi => bi.CustomerServiceSubscription)
                .Where(bi => bi.CustomerId == customerId)
                .ToListAsync();

            foreach (var period in periods)
            {
                var items = billingItems
                    .Where(bi => bi.Year == period.Year && bi.Month == period.Month)
                    .ToList();

                period.ServiceLines = items.Select(bi =>
                {
                    var svc = ServiceTypes.GetById(bi.CustomerServiceSubscription.ServiceTypeId);
                    return new BillingServiceLineDto
                    {
                        ServiceName = svc?.Description ?? "?",
                        ServiceCode = svc?.SystemName ?? "?",
                        MonthlyPrice = bi.Amount
                    };
                }).ToList();
            }
        }

        return periods;
    }

    public async Task<(bool Success, string? Error)> UpdatePeriodAsync(int periodId, BillingPeriodUpdateDto dto)
    {
        var period = await _billingEs.GetByIdAsync(periodId);
        if (period == null) return (false, "Faturalama donemi bulunamadi.");

        // StatusId verilmisse onu kullan
        if (dto.StatusId.HasValue)
        {
            period.StatusId = dto.StatusId.Value;
            period.IsPaid = dto.StatusId.Value == BillingPeriodStatuses.Ids.Paid;
            period.PaidAt = period.IsPaid ? DateTime.UtcNow : null;
        }
        else
        {
            // Geriye uyumluluk: IsPaid ile calis
            period.IsPaid = dto.IsPaid;
            period.PaidAt = dto.IsPaid ? DateTime.UtcNow : null;
            period.StatusId = dto.IsPaid ? BillingPeriodStatuses.Ids.Paid : BillingPeriodStatuses.Ids.Draft;
        }

        period.Notes = dto.Notes;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeletePeriodAsync(int periodId)
    {
        var period = await _billingEs.GetByIdAsync(periodId);
        if (period == null) return (false, "Faturalama donemi bulunamadi.");

        if (period.IsPaid)
            return (false, "Odemesi onaylanmis donem silinemez.");

        _billingEs.Remove(period);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(int Created, int Skipped, int SkippedNoAnchor, string? Error)> GenerateBulkAsync(int year, int month)
    {
        if (month < 1 || month > 12)
            return (0, 0, 0, "Gecersiz ay degeri.");
        if (year < 2020 || year > 2100)
            return (0, 0, 0, "Gecersiz yil degeri.");

        var activeCustomers = await _customerEs.GetAllQueryable()
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.MonthlyUnitPrice, c.BillingAnchorDay })
            .ToListAsync();

        var existingCustomerIds = await _billingEs.GetAllQueryable()
            .Where(b => b.Year == year && b.Month == month)
            .Select(b => b.CustomerId)
            .ToListAsync();

        // Aktif hizmet abonelikleri (tum musteriler icin)
        var activeSubscriptions = await _subscriptionEs.GetAllQueryable()
            .Where(s => s.StatusId == SubscriptionStatuses.Ids.Active && s.MonthlyPrice > 0)
            .ToListAsync();

        // Bu donem icin zaten olusturulmus hizmet faturalari
        var existingBillingItemKeys = await _billingItemEs.GetAllQueryable()
            .Where(b => b.Year == year && b.Month == month)
            .Select(b => b.CustomerServiceSubscriptionId)
            .ToListAsync();
        var existingBillingSet = new HashSet<int>(existingBillingItemKeys);

        var created = 0;
        var skipped = 0;
        var skippedNoAnchor = 0;

        foreach (var customer in activeCustomers)
        {
            if (existingCustomerIds.Contains(customer.Id))
            {
                skipped++;
                continue;
            }

            // BillingAnchorDay null ise atla
            if (!customer.BillingAnchorDay.HasValue)
            {
                skippedNoAnchor++;
                continue;
            }

            var startDay = customer.BillingAnchorDay.Value;
            var daysInMonth = DateTime.DaysInMonth(year, month);
            if (startDay > daysInMonth) startDay = daysInMonth;

            var periodStart = new DateTime(year, month, startDay, 0, 0, 0, DateTimeKind.Utc);

            // Donem bitis: bir sonraki ayin ayni gunu - 1 gun
            var nextMonth = month == 12 ? 1 : month + 1;
            var nextYear = month == 12 ? year + 1 : year;
            var daysInNextMonth = DateTime.DaysInMonth(nextYear, nextMonth);
            var endDay = Math.Min(startDay, daysInNextMonth);
            var periodEnd = new DateTime(nextYear, nextMonth, endDay, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);

            var userCount = await _personnelEs.GetAllQueryable()
                .CountAsync(p => p.CustomerId == customer.Id && p.IsActive
                    && p.CustomerRoleId == CustomerRoles.Ids.Operator);

            // Bu musterinin aktif ucretli hizmetleri
            var customerSubs = activeSubscriptions.Where(s => s.CustomerId == customer.Id).ToList();
            var serviceAmount = customerSubs.Sum(s => s.MonthlyPrice);

            _billingEs.Add(new CustomerBillingPeriod
            {
                CustomerId = customer.Id,
                Year = year,
                Month = month,
                PeriodStartDate = periodStart,
                PeriodEndDate = periodEnd,
                UserCount = userCount,
                UnitPrice = customer.MonthlyUnitPrice,
                Amount = userCount * customer.MonthlyUnitPrice,
                ServiceAmount = serviceAmount,
                StatusId = BillingPeriodStatuses.Ids.Draft,
                IsPaid = false,
                CreatedAt = DateTime.UtcNow
            });

            // Hizmet fatura kalemleri olustur
            foreach (var sub in customerSubs)
            {
                if (existingBillingSet.Contains(sub.Id)) continue;

                _billingItemEs.Add(new ServiceBillingItem
                {
                    CustomerId = customer.Id,
                    CustomerServiceSubscriptionId = sub.Id,
                    StatusId = BillingItemStatuses.Ids.Pending,
                    Year = year,
                    Month = month,
                    Amount = sub.MonthlyPrice,
                    IsPaid = false
                });
            }

            created++;
        }

        if (created > 0)
            await _uow.SaveChangesAsync();

        return (created, skipped, skippedNoAnchor, null);
    }

    public async Task<(bool IsBlocked, string? Reason)> IsCustomerBlockedByBillingAsync(int customerId)
    {
        var now = DateTime.UtcNow;
        var unpaidPeriods = await _billingEs.GetAllQueryable()
            .Where(b => b.CustomerId == customerId && b.StatusId != BillingPeriodStatuses.Ids.Paid)
            .Select(b => new { b.PeriodEndDate, b.Year, b.Month })
            .ToListAsync();

        foreach (var period in unpaidPeriods)
        {
            // PeriodEndDate + 7 gun gecmisse engelle
            var deadline = period.PeriodEndDate.AddDays(7);

            if (now > deadline)
                return (true, $"Odenmemis donem: {period.Month:00}/{period.Year}. Lutfen odeme yapiniz.");
        }

        return (false, null);
    }

    public async Task<(bool Success, string? Error)> CreateManualPeriodAsync(BillingPeriodCreateDto dto)
    {
        var customer = await _customerEs.GetByIdAsync(dto.CustomerId);
        if (customer == null) return (false, "Musteri bulunamadi.");

        var startDate = dto.PeriodStartDate;
        var year = startDate.Year;
        var month = startDate.Month;

        // Bu donem zaten var mi?
        var exists = await _billingEs.GetAllQueryable()
            .AnyAsync(b => b.CustomerId == dto.CustomerId && b.Year == year && b.Month == month);
        if (exists) return (false, $"{month:00}/{year} donemi zaten mevcut.");

        // BillingAnchorDay kaydet
        customer.BillingAnchorDay = startDate.Day;

        // Donem bitis: bir sonraki ayin ayni gunu - 1 gun
        var nextMonth = month == 12 ? 1 : month + 1;
        var nextYear = month == 12 ? year + 1 : year;
        var daysInNextMonth = DateTime.DaysInMonth(nextYear, nextMonth);
        var endDay = Math.Min(startDate.Day, daysInNextMonth);
        var periodEnd = new DateTime(nextYear, nextMonth, endDay, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);

        var userCount = await _personnelEs.GetAllQueryable()
            .CountAsync(p => p.CustomerId == dto.CustomerId && p.IsActive
                && p.CustomerRoleId == CustomerRoles.Ids.Operator);

        // Aktif ucretli hizmetler
        var customerSubs = await _subscriptionEs.GetAllQueryable()
            .Where(s => s.CustomerId == dto.CustomerId && s.StatusId == SubscriptionStatuses.Ids.Active && s.MonthlyPrice > 0)
            .ToListAsync();
        var serviceAmount = customerSubs.Sum(s => s.MonthlyPrice);

        var period = new CustomerBillingPeriod
        {
            CustomerId = dto.CustomerId,
            Year = year,
            Month = month,
            PeriodStartDate = new DateTime(year, month, startDate.Day, 0, 0, 0, DateTimeKind.Utc),
            PeriodEndDate = periodEnd,
            UserCount = userCount,
            UnitPrice = customer.MonthlyUnitPrice,
            Amount = userCount * customer.MonthlyUnitPrice,
            ServiceAmount = serviceAmount,
            StatusId = BillingPeriodStatuses.Ids.Draft,
            IsPaid = false,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };
        _billingEs.Add(period);

        // Hizmet fatura kalemleri
        foreach (var sub in customerSubs)
        {
            _billingItemEs.Add(new ServiceBillingItem
            {
                CustomerId = dto.CustomerId,
                CustomerServiceSubscriptionId = sub.Id,
                StatusId = BillingItemStatuses.Ids.Pending,
                Year = year,
                Month = month,
                Amount = sub.MonthlyPrice,
                IsPaid = false
            });
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<BillingReportDto>> GetBillingReportAsync(int? year, int? month, int? statusId)
    {
        var query = _billingEs.GetAllQueryable()
            .Include(b => b.Customer)
            .AsQueryable();

        if (year.HasValue)
            query = query.Where(b => b.Year == year.Value);
        if (month.HasValue)
            query = query.Where(b => b.Month == month.Value);
        if (statusId.HasValue)
            query = query.Where(b => b.StatusId == statusId.Value);

        var periods = await query
            .OrderBy(b => b.Customer.Name).ThenByDescending(b => b.Year).ThenByDescending(b => b.Month)
            .Select(b => new BillingReportDto
            {
                PeriodId = b.Id,
                CustomerId = b.CustomerId,
                CustomerName = b.Customer.Name,
                Year = b.Year,
                Month = b.Month,
                PeriodStartDate = b.PeriodStartDate,
                PeriodEndDate = b.PeriodEndDate,
                Amount = b.Amount,
                ServiceAmount = b.ServiceAmount,
                StatusId = b.StatusId
            })
            .ToListAsync();

        foreach (var p in periods)
            p.StatusName = BillingPeriodStatuses.GetById(p.StatusId)?.Description ?? "?";

        return periods;
    }
}
