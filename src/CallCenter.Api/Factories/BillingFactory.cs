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
    private readonly ICustomerSubscriptionEntityService _planSubscriptionEs;
    private readonly IServiceBillingItemEntityService _billingItemEs;
    private readonly ICustomerProductEntityService _customerProductEs;
    private readonly ICustomerBillingPeriodModuleLineEntityService _billingPeriodModuleLineEs;
    private readonly ISubscriptionFactory _subscriptionFactory;
    private readonly IUnitOfWork _uow;

    public BillingFactory(
        IBillingPeriodEntityService billingEs,
        ICustomerEntityService customerEs,
        ICustomerPersonnelEntityService personnelEs,
        ICustomerServiceSubscriptionEntityService subscriptionEs,
        ICustomerSubscriptionEntityService planSubscriptionEs,
        IServiceBillingItemEntityService billingItemEs,
        ICustomerProductEntityService customerProductEs,
        ICustomerBillingPeriodModuleLineEntityService billingPeriodModuleLineEs,
        ISubscriptionFactory subscriptionFactory,
        IUnitOfWork uow)
    {
        _billingEs = billingEs;
        _customerEs = customerEs;
        _personnelEs = personnelEs;
        _subscriptionEs = subscriptionEs;
        _planSubscriptionEs = planSubscriptionEs;
        _billingItemEs = billingItemEs;
        _customerProductEs = customerProductEs;
        _billingPeriodModuleLineEs = billingPeriodModuleLineEs;
        _subscriptionFactory = subscriptionFactory;
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

        if (periods.Count > 0)
        {
            var periodIds = periods.Select(p => p.Id).ToList();
            var salonLines = await _billingPeriodModuleLineEs.GetAllQueryable()
                .Where(l => periodIds.Contains(l.CustomerBillingPeriodId))
                .ToListAsync();

            foreach (var period in periods)
            {
                period.SalonModuleLines = salonLines
                    .Where(l => l.CustomerBillingPeriodId == period.Id)
                    .OrderBy(l => l.PackageGroupId ?? int.MaxValue)
                    .ThenBy(l => l.ModuleId ?? int.MaxValue)
                    .Select(l => new BillingPeriodModuleLineDto
                    {
                        PackageGroupId = l.PackageGroupId,
                        ModuleId = l.ModuleId,
                        ModuleDisplayName = l.ModuleDisplayName,
                        MonthlyUnitPrice = l.MonthlyUnitPrice,
                        LineAmount = l.LineAmount
                    })
                    .ToList();
            }

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

        if (dto.PaymentMethodId.HasValue)
            period.PaymentMethodId = dto.PaymentMethodId.Value;

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

    public async Task<(int Created, int Skipped, int SkippedNoAnchor, int SkippedSalonPlatform, int PlatformTahakkukCreated, int PlatformTahakkukSkipped, string? Error)> GenerateBulkAsync(int year, int month)
    {
        if (month < 1 || month > 12)
            return (0, 0, 0, 0, 0, 0, "Gecersiz ay degeri.");
        if (year < 2020 || year > 2100)
            return (0, 0, 0, 0, 0, 0, "Gecersiz yil degeri.");

        var activeCustomers = await _customerEs.GetAllQueryable()
            .Where(c => c.IsActive && !c.IsTest) // Test musterileri atla
            .Select(c => new { c.Id, c.BillingAnchorDay, c.MaxUsers })
            .ToListAsync();

        var salonPlatformCustomerIds = await _customerProductEs.GetAllQueryable()
            .Where(cp => cp.IsActive && cp.ProductTypeId == ProductTypes.Ids.Salon)
            .Select(cp => cp.CustomerId)
            .Distinct()
            .ToListAsync();
        var salonPlatformSet = salonPlatformCustomerIds.ToHashSet();

        // Urun bazli fiyatlari topla
        var productPrices = await _customerProductEs.GetAllQueryable()
            .Where(cp => cp.IsActive)
            .GroupBy(cp => cp.CustomerId)
            .Select(g => new { CustomerId = g.Key, TotalMonthlyPrice = g.Sum(cp => cp.MonthlyPrice) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.TotalMonthlyPrice);

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

        // Salon kayit vb.: trial abonelik BillingDay ile gelir ama BillingAnchorDay bos kalabiliyordu
        var planAnchorByCustomer = await _planSubscriptionEs.GetAllQueryable()
            .Where(s => s.StatusId == SubscriptionStatuses.Ids.Active)
            .GroupBy(s => s.CustomerId)
            .Select(g => new { CustomerId = g.Key, Day = g.Min(x => x.BillingDay) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.Day);

        var created = 0;
        var skipped = 0;
        var skippedNoAnchor = 0;
        var skippedSalonPlatform = 0;

        foreach (var customer in activeCustomers)
        {
            if (existingCustomerIds.Contains(customer.Id))
            {
                skipped++;
                continue;
            }

            if (salonPlatformSet.Contains(customer.Id))
            {
                skippedSalonPlatform++;
                continue;
            }

            var anchorDay = customer.BillingAnchorDay
                ?? (planAnchorByCustomer.TryGetValue(customer.Id, out var d) ? d : (int?)null);
            if (!anchorDay.HasValue)
            {
                skippedNoAnchor++;
                continue;
            }

            var startDay = anchorDay.Value;
            var daysInMonth = DateTime.DaysInMonth(year, month);
            if (startDay > daysInMonth) startDay = daysInMonth;

            var periodStart = new DateTime(year, month, startDay, 0, 0, 0, DateTimeKind.Utc);

            // Donem bitis: bir sonraki ayin ayni gunu - 1 gun
            var nextMonth = month == 12 ? 1 : month + 1;
            var nextYear = month == 12 ? year + 1 : year;
            var daysInNextMonth = DateTime.DaysInMonth(nextYear, nextMonth);
            var endDay = Math.Min(startDay, daysInNextMonth);
            var periodEnd = new DateTime(nextYear, nextMonth, endDay, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);

            // MaxUsers > 0: izin verilen sayi uzerinden tahakkuk
            // MaxUsers == 0: sinirsiz → aktif Operator sayisi uzerinden tahakkuk
            int userCount;
            if (customer.MaxUsers > 0)
            {
                userCount = customer.MaxUsers;
            }
            else
            {
                userCount = await _personnelEs.GetAllQueryable()
                    .CountAsync(p => p.CustomerId == customer.Id && p.IsActive
                        && p.CustomerRoleId == CustomerRoles.Ids.Operator);
            }

            // Bu musterinin aktif ucretli hizmetleri
            var customerSubs = activeSubscriptions.Where(s => s.CustomerId == customer.Id).ToList();
            var serviceAmount = customerSubs.Sum(s => s.MonthlyPrice);
            var productAmount = userCount * productPrices.GetValueOrDefault(customer.Id, 0m);
            var totalAmount = productAmount + serviceAmount;

            // BUG2.12 fix: 0 TL tahakkuklar da kayit olustur, otomatik Paid isaretle ki salon panel engellenmesin
            _billingEs.Add(new CustomerBillingPeriod
            {
                CustomerId = customer.Id,
                Year = year,
                Month = month,
                PeriodStartDate = periodStart,
                PeriodEndDate = periodEnd,
                UserCount = userCount,
                UnitPrice = productPrices.GetValueOrDefault(customer.Id, 0m),
                Amount = productAmount,
                ServiceAmount = serviceAmount,
                StatusId = totalAmount <= 0 ? BillingPeriodStatuses.Ids.Paid : BillingPeriodStatuses.Ids.Draft,
                IsPaid = totalAmount <= 0,
                PaidAt = totalAmount <= 0 ? DateTime.UtcNow : null,
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

        var (platformCreated, platformSkipped) = await _subscriptionFactory.GenerateBillingForMonthAsync(year, month);

        return (created, skipped, skippedNoAnchor, skippedSalonPlatform, platformCreated, platformSkipped, null);
    }

    public async Task<(bool IsBlocked, string? Reason)> IsCustomerBlockedByBillingAsync(int customerId)
    {
        var now = DateTime.UtcNow;
        var unpaidPeriods = await _billingEs.GetAllQueryable()
            .Where(b => b.CustomerId == customerId && b.StatusId != BillingPeriodStatuses.Ids.Paid
                && b.Amount + b.ServiceAmount > 0m)
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

        // MaxUsers > 0: izin verilen sayi uzerinden tahakkuk
        // MaxUsers == 0: sinirsiz → aktif Operator sayisi uzerinden tahakkuk
        int userCount;
        if (customer.MaxUsers > 0)
        {
            userCount = customer.MaxUsers;
        }
        else
        {
            userCount = await _personnelEs.GetAllQueryable()
                .CountAsync(p => p.CustomerId == dto.CustomerId && p.IsActive
                    && p.CustomerRoleId == CustomerRoles.Ids.Operator);
        }

        // Urun bazli fiyatlari topla
        var totalMonthlyPrice = await _customerProductEs.GetAllQueryable()
            .Where(cp => cp.CustomerId == dto.CustomerId && cp.IsActive)
            .SumAsync(cp => cp.MonthlyPrice);

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
            UnitPrice = totalMonthlyPrice,
            Amount = userCount * totalMonthlyPrice,
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

    public async Task<List<BillingReportDto>> GetBillingReportAsync(int? year, int? month, int? statusId, int? productTypeId = null)
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

        // Urun tipine gore filtrele (CC=1, Salon=2)
        if (productTypeId.HasValue)
        {
            var customerIds = await _customerProductEs.GetAllQueryable()
                .Where(cp => cp.ProductTypeId == productTypeId.Value && cp.IsActive)
                .Select(cp => cp.CustomerId)
                .Distinct()
                .ToListAsync();
            query = query.Where(b => customerIds.Contains(b.CustomerId));
        }

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
                StatusId = b.StatusId,
                PaymentMethodId = b.PaymentMethodId,
                IsPaid = b.IsPaid,
                PaidAt = b.PaidAt
            })
            .ToListAsync();

        foreach (var p in periods)
        {
            p.StatusName = BillingPeriodStatuses.GetById(p.StatusId)?.Description ?? "?";
            if (p.PaymentMethodId.HasValue)
                p.PaymentMethodName = BillingPaymentMethods.GetById(p.PaymentMethodId.Value)?.Description;
        }

        return periods;
    }

    public async Task<BillingTahakkukDetailDto?> GetBillingTahakkukDetailAsync(int periodId)
    {
        var period = await _billingEs.GetAllQueryable()
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == periodId);

        if (period == null) return null;

        var customerId = period.CustomerId;
        var products = await _customerProductEs.GetAllQueryable()
            .Where(cp => cp.CustomerId == customerId && cp.IsActive)
            .OrderBy(cp => cp.ProductTypeId)
            .ThenBy(cp => cp.Id)
            .ToListAsync();

        var productLines = products.Select(cp =>
        {
            var label = ProductTypes.GetById(cp.ProductTypeId)?.Description ?? $"Urun #{cp.ProductTypeId}";
            var lineAmt = Math.Round(period.UserCount * cp.MonthlyPrice, 2, MidpointRounding.AwayFromZero);
            return new BillingTahakkukProductLineDto
            {
                CustomerProductId = cp.Id,
                ProductTypeId = cp.ProductTypeId,
                ProductLabel = label,
                MonthlyUnitPrice = cp.MonthlyPrice,
                LineAmount = lineAmt
            };
        }).ToList();

        var items = await _billingItemEs.GetAllQueryable()
            .Include(bi => bi.CustomerServiceSubscription)
            .Where(bi => bi.CustomerId == customerId && bi.Year == period.Year && bi.Month == period.Month)
            .OrderBy(bi => bi.CustomerServiceSubscription.ServiceTypeId)
            .ToListAsync();

        var serviceLines = items.Select(bi =>
        {
            var svc = ServiceTypes.GetById(bi.CustomerServiceSubscription.ServiceTypeId);
            return new BillingServiceLineDto
            {
                ServiceName = svc?.Description ?? "?",
                ServiceCode = svc?.SystemName ?? "?",
                MonthlyPrice = bi.Amount
            };
        }).ToList();

        var unitSum = products.Sum(p => p.MonthlyPrice);

        return new BillingTahakkukDetailDto
        {
            PeriodId = period.Id,
            CustomerId = customerId,
            CustomerName = period.Customer.Name,
            Year = period.Year,
            Month = period.Month,
            PeriodStartDate = period.PeriodStartDate,
            PeriodEndDate = period.PeriodEndDate,
            UserCount = period.UserCount,
            UnitPriceSum = unitSum,
            OperatorAmount = period.Amount,
            ServiceAmount = period.ServiceAmount,
            ProductLines = productLines,
            ServiceLines = serviceLines
        };
    }
}
