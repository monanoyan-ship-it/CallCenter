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
    private readonly ICustomerProductEntityService _customerProductEs;
    private readonly ICustomerBillingPeriodModuleLineEntityService _billingPeriodModuleLineEs;
    private readonly IPaymentTransactionEntityService _paymentTransactionEs;
    private readonly ISubscriptionFactory _subscriptionFactory;
    private readonly ServicePricingFactory _servicePricingFactory;
    private readonly IUnitOfWork _uow;

    public BillingFactory(
        IBillingPeriodEntityService billingEs,
        ICustomerEntityService customerEs,
        ICustomerPersonnelEntityService personnelEs,
        ICustomerServiceSubscriptionEntityService subscriptionEs,
        IServiceBillingItemEntityService billingItemEs,
        ICustomerProductEntityService customerProductEs,
        ICustomerBillingPeriodModuleLineEntityService billingPeriodModuleLineEs,
        IPaymentTransactionEntityService paymentTransactionEs,
        ISubscriptionFactory subscriptionFactory,
        ServicePricingFactory servicePricingFactory,
        IUnitOfWork uow)
    {
        _billingEs = billingEs;
        _customerEs = customerEs;
        _personnelEs = personnelEs;
        _subscriptionEs = subscriptionEs;
        _billingItemEs = billingItemEs;
        _customerProductEs = customerProductEs;
        _billingPeriodModuleLineEs = billingPeriodModuleLineEs;
        _paymentTransactionEs = paymentTransactionEs;
        _subscriptionFactory = subscriptionFactory;
        _servicePricingFactory = servicePricingFactory;
        _uow = uow;
    }

    public async Task<List<BillingPeriodDto>> GetByCustomerAsync(int customerId)
    {
        var periods = await _billingEs.GetAllQueryable()
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month).ThenBy(b => b.BillingKindId)
            .Select(b => new BillingPeriodDto
            {
                Id = b.Id,
                CustomerId = b.CustomerId,
                CustomerName = b.Customer.Name,
                BillingKindId = b.BillingKindId,
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
        {
            p.StatusName = BillingPeriodStatuses.GetById(p.StatusId)?.Description ?? "?";
            p.BillingKindName = CustomerBillingKinds.GetDescription(p.BillingKindId);
        }

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
                if (period.BillingKindId != CustomerBillingKinds.CallCenter)
                    continue;

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

        if (period.StatusId != BillingPeriodStatuses.Ids.Draft
            || period.IsPaid
            || period.PaidAt.HasValue
            || period.PaymentMethodId.HasValue)
        {
            return (false, "Sadece fatura/odeme islemi baslamamis tahakkuk silinebilir.");
        }

        var hasPaymentTransaction = await _paymentTransactionEs.GetAllQueryable()
            .AnyAsync(t => t.BillingPeriodId == periodId);
        if (hasPaymentTransaction)
            return (false, "Bu tahakkuk icin odeme islemi olusturuldugu icin silinemez.");

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
            .Where(c => c.IsActive && !c.IsTest) // Test musterileri atla
            .Select(c => new { c.Id, c.BillingAnchorDay })
            .ToListAsync();

        var existingCustomerIds = await _billingEs.GetAllQueryable()
            .Where(b => b.Year == year && b.Month == month && b.BillingKindId == CustomerBillingKinds.CallCenter)
            .Select(b => b.CustomerId)
            .ToListAsync();

        // Aktif hizmet abonelikleri (tum musteriler icin)
        var activeSubscriptions = await _subscriptionEs.GetAllQueryable()
            .Where(s => s.StatusId == SubscriptionStatuses.Ids.Active && s.MonthlyPrice > 0)
            .ToListAsync();
        // CC donemi yalnizca CC urunu veya ucretli hizmet aboneligi olanlara:
        // Saf salon musterilerinde 0 TL CallCenter satiri + SalonPlatform tahakkuku ikili satir olusmasin.
        var customerIdsWithCcProduct = await _customerProductEs.GetAllQueryable()
            .Where(cp => cp.IsActive && cp.ProductTypeId == ProductTypes.Ids.CallCenter)
            .Select(cp => cp.CustomerId)
            .Distinct()
            .ToListAsync();
        var needsCallCenterBulk = new HashSet<int>(customerIdsWithCcProduct);
        foreach (var s in activeSubscriptions)
            needsCallCenterBulk.Add(s.CustomerId);

        decimal operatorUnitPrice = 0m;
        if (needsCallCenterBulk.Count > 0)
        {
            var (price, pricingError) = await _servicePricingFactory.TryGetActiveCallCenterOperatorUnitPriceAsync();
            if (!price.HasValue)
                return (0, 0, 0, pricingError);
            operatorUnitPrice = price.Value;
        }

        // Bu donem icin zaten olusturulmus hizmet faturalari
        var existingBillingItemKeys = await _billingItemEs.GetAllQueryable()
            .Where(b => b.Year == year && b.Month == month)
            .Select(b => b.CustomerServiceSubscriptionId)
            .ToListAsync();
        var existingBillingSet = new HashSet<int>(existingBillingItemKeys);

        var created = 0;
        var skipped = 0;

        foreach (var customer in activeCustomers)
        {
            if (!needsCallCenterBulk.Contains(customer.Id))
                continue;

            if (existingCustomerIds.Contains(customer.Id))
            {
                skipped++;
                continue;
            }

            var startDay = BillingAnchorDayResolver.ResolvePeriodStartDay(year, month, customer.BillingAnchorDay);
            if (!customer.BillingAnchorDay.HasValue)
            {
                var cust = await _customerEs.GetByIdAsync(customer.Id);
                if (cust != null && !cust.IsTest)
                    cust.BillingAnchorDay = startDay;
            }

            var periodStart = new DateTime(year, month, startDay, 0, 0, 0, DateTimeKind.Utc);

            // Donem bitis: bir sonraki ayin ayni gunu - 1 gun
            var nextMonth = month == 12 ? 1 : month + 1;
            var nextYear = month == 12 ? year + 1 : year;
            var daysInNextMonth = DateTime.DaysInMonth(nextYear, nextMonth);
            var endDay = Math.Min(startDay, daysInNextMonth);
            var periodEnd = new DateTime(nextYear, nextMonth, endDay, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);

            // CallCenter tahakkuku aktif operator sayisina gore kesilir.
            var userCount = await _personnelEs.GetAllQueryable()
                .CountAsync(p => p.CustomerId == customer.Id && p.IsActive
                    && p.CustomerRoleId == CustomerRoles.Ids.Operator);

            // Bu musterinin aktif ucretli hizmetleri
            var customerSubs = activeSubscriptions.Where(s => s.CustomerId == customer.Id).ToList();
            var serviceAmount = customerSubs.Sum(s => s.MonthlyPrice);
            var productAmount = userCount * operatorUnitPrice;
            var totalAmount = productAmount + serviceAmount;

            // BUG2.12 fix: 0 TL tahakkuklar da kayit olustur, otomatik Paid isaretle ki salon panel engellenmesin
            _billingEs.Add(new CustomerBillingPeriod
            {
                CustomerId = customer.Id,
                BillingKindId = CustomerBillingKinds.CallCenter,
                Year = year,
                Month = month,
                PeriodStartDate = periodStart,
                PeriodEndDate = periodEnd,
                UserCount = userCount,
                UnitPrice = operatorUnitPrice,
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

        // Eskiden "anchor yok" sayacı; artık ilk tahakkukta gün otomatik atanır.
        return (created, skipped, 0, null);
    }

    public async Task<(bool IsBlocked, string? Reason)> IsCustomerBlockedByBillingAsync(int customerId)
    {
        var now = DateTime.UtcNow;
        var hasBillableCallCenterScope =
            await _customerProductEs.GetAllQueryable()
                .AnyAsync(cp => cp.CustomerId == customerId
                    && cp.ProductTypeId == ProductTypes.Ids.CallCenter
                    && cp.IsActive)
            || await _subscriptionEs.GetAllQueryable()
                .AnyAsync(s => s.CustomerId == customerId
                    && s.StatusId == SubscriptionStatuses.Ids.Active
                    && s.MonthlyPrice > 0m);

        if (!hasBillableCallCenterScope)
            return (false, null);

        var unpaidPeriods = await _billingEs.GetAllQueryable()
            .Where(b => b.CustomerId == customerId
                && b.BillingKindId == CustomerBillingKinds.CallCenter
                && !b.IsPaid
                && b.Amount + b.ServiceAmount > 0m
                && b.StatusId != BillingPeriodStatuses.Ids.Paid)
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

        var hasCcProduct = await _customerProductEs.GetAllQueryable()
            .AnyAsync(cp => cp.CustomerId == dto.CustomerId && cp.ProductTypeId == ProductTypes.Ids.CallCenter && cp.IsActive);
        var hasSalonProduct = await _customerProductEs.GetAllQueryable()
            .AnyAsync(cp => cp.CustomerId == dto.CustomerId && cp.ProductTypeId == ProductTypes.Ids.Salon && cp.IsActive);

        // Saf salon musterisi: CC turu manuel tahakkuk operatör x ürün ile 0 TL + yanlis "odendi" üretir
        if (hasSalonProduct && !hasCcProduct)
            return await _subscriptionFactory.CreateManualSalonPlatformPeriodAsync(dto.CustomerId, dto.PeriodStartDate, dto.Notes);

        var startDate = dto.PeriodStartDate;
        var year = startDate.Year;
        var month = startDate.Month;

        // Bu donem zaten var mi?
        var exists = await _billingEs.GetAllQueryable()
            .AnyAsync(b => b.CustomerId == dto.CustomerId && b.Year == year && b.Month == month
                && b.BillingKindId == CustomerBillingKinds.CallCenter);
        if (exists) return (false, $"{month:00}/{year} donemi zaten mevcut.");

        // BillingAnchorDay kaydet
        customer.BillingAnchorDay = startDate.Day;

        // Donem bitis: bir sonraki ayin ayni gunu - 1 gun
        var nextMonth = month == 12 ? 1 : month + 1;
        var nextYear = month == 12 ? year + 1 : year;
        var daysInNextMonth = DateTime.DaysInMonth(nextYear, nextMonth);
        var endDay = Math.Min(startDate.Day, daysInNextMonth);
        var periodEnd = new DateTime(nextYear, nextMonth, endDay, 0, 0, 0, DateTimeKind.Utc).AddDays(-1);

        // CallCenter tahakkuku aktif operator sayisina gore kesilir.
        var userCount = await _personnelEs.GetAllQueryable()
            .CountAsync(p => p.CustomerId == dto.CustomerId && p.IsActive
                && p.CustomerRoleId == CustomerRoles.Ids.Operator);
        var (operatorUnitPrice, pricingError) = await _servicePricingFactory.TryGetActiveCallCenterOperatorUnitPriceAsync();
        if (!operatorUnitPrice.HasValue)
            return (false, pricingError);

        // Aktif ucretli hizmetler
        var customerSubs = await _subscriptionEs.GetAllQueryable()
            .Where(s => s.CustomerId == dto.CustomerId && s.StatusId == SubscriptionStatuses.Ids.Active && s.MonthlyPrice > 0)
            .ToListAsync();
        var serviceAmount = customerSubs.Sum(s => s.MonthlyPrice);

        var period = new CustomerBillingPeriod
        {
            CustomerId = dto.CustomerId,
            BillingKindId = CustomerBillingKinds.CallCenter,
            Year = year,
            Month = month,
            PeriodStartDate = new DateTime(year, month, startDate.Day, 0, 0, 0, DateTimeKind.Utc),
            PeriodEndDate = periodEnd,
            UserCount = userCount,
            UnitPrice = operatorUnitPrice.Value,
            Amount = userCount * operatorUnitPrice.Value,
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

        HashSet<int> salonOnlyCustomerIds = new();

        // Urun tipine gore filtrele (CC=1, Salon=2) + tahakkuk turu.
        // Salon: BKind=SalonPlatform; ayrica migration oncesi/Modul satiri olmayan satirlarda kalan
        // CallCenter (1) turu — saf salon musterisinde CC urunu yoksa bu satirlar da salon tahakkuku sayilir.
        // Modul kalemi varsa tur yanlis etiketlense bile salon raporunda goster.
        if (productTypeId.HasValue)
        {
            var customerIdsList = await _customerProductEs.GetAllQueryable()
                .Where(cp => cp.ProductTypeId == productTypeId.Value && cp.IsActive)
                .Select(cp => cp.CustomerId)
                .Distinct()
                .ToListAsync();
            query = query.Where(b => customerIdsList.Contains(b.CustomerId));

            if (productTypeId.Value == ProductTypes.Ids.CallCenter)
            {
                query = query.Where(b => b.BillingKindId == CustomerBillingKinds.CallCenter);
            }
            else if (productTypeId.Value == ProductTypes.Ids.Salon)
            {
                var ccCustomerIds = await _customerProductEs.GetAllQueryable()
                    .Where(cp => cp.ProductTypeId == ProductTypes.Ids.CallCenter && cp.IsActive)
                    .Select(cp => cp.CustomerId)
                    .Distinct()
                    .ToListAsync();
                var hasCc = ccCustomerIds.ToHashSet();
                salonOnlyCustomerIds = customerIdsList.Where(id => !hasCc.Contains(id)).ToHashSet();

                query = query.Where(b =>
                    b.BillingKindId == CustomerBillingKinds.SalonPlatform
                    || b.ModuleLines.Any()
                    || (b.BillingKindId == CustomerBillingKinds.CallCenter && salonOnlyCustomerIds.Contains(b.CustomerId)));
            }
        }

        var periods = await query
            .OrderBy(b => b.Customer.Name).ThenByDescending(b => b.Year).ThenByDescending(b => b.Month)
            .Select(b => new BillingReportDto
            {
                PeriodId = b.Id,
                CustomerId = b.CustomerId,
                CustomerName = b.Customer.Name,
                BillingKindId = b.BillingKindId,
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

        // Saf salon musterilerinde CC turu satirlar toplu faturalama ile operatör x ürün hesaplanir;
        // operatör 0 iken tutarlar 0 kalir. Raporda salon platform kırılımını göster.
        if (productTypeId == ProductTypes.Ids.Salon && salonOnlyCustomerIds.Count > 0)
        {
            var idsForBreakdown = periods
                .Where(p => p.BillingKindId == CustomerBillingKinds.CallCenter
                    && salonOnlyCustomerIds.Contains(p.CustomerId))
                .Select(p => p.CustomerId)
                .Distinct()
                .ToList();
            var breakdownByCustomer = new Dictionary<int, (decimal PlatformAmount, decimal ModuleAmount)?>();
            foreach (var cid in idsForBreakdown)
                breakdownByCustomer[cid] = await _subscriptionFactory.GetSalonBillingBreakdownForCustomerAsync(cid);

            foreach (var p in periods)
            {
                if (p.BillingKindId != CustomerBillingKinds.CallCenter
                    || !salonOnlyCustomerIds.Contains(p.CustomerId))
                    continue;
                if (breakdownByCustomer.TryGetValue(p.CustomerId, out var br) && br.HasValue)
                {
                    p.Amount = br.Value.PlatformAmount;
                    p.ServiceAmount = br.Value.ModuleAmount;
                }
            }
        }

        foreach (var p in periods)
        {
            p.StatusName = BillingPeriodStatuses.GetById(p.StatusId)?.Description ?? "?";
            p.BillingKindName = CustomerBillingKinds.GetDescription(p.BillingKindId);
            if (p.PaymentMethodId.HasValue)
                p.PaymentMethodName = BillingPaymentMethods.GetById(p.PaymentMethodId.Value)?.Description;
        }

        return periods;
    }

    public async Task<BillingTahakkukDetailDto?> GetBillingTahakkukDetailAsync(int periodId)
    {
        var period = await _billingEs.GetAllQueryable()
            .Include(b => b.Customer)
            .Include(b => b.ModuleLines)
            .FirstOrDefaultAsync(b => b.Id == periodId);

        if (period == null) return null;

        var customerId = period.CustomerId;

        var hasCcProduct = await _customerProductEs.GetAllQueryable()
            .AnyAsync(cp => cp.CustomerId == customerId && cp.ProductTypeId == ProductTypes.Ids.CallCenter && cp.IsActive);

        // Salon tahakkuk ekrani: tur yanlis (1) kalmis veya sadece modul satirlari olan kayitlar
        var useSalonTahakkukLayout = period.BillingKindId == CustomerBillingKinds.SalonPlatform
            || period.ModuleLines.Count > 0
            || (!hasCcProduct && period.BillingKindId == CustomerBillingKinds.CallCenter);

        if (useSalonTahakkukLayout)
        {
            return new BillingTahakkukDetailDto
            {
                PeriodId = period.Id,
                CustomerId = customerId,
                CustomerName = period.Customer.Name,
                BillingKindId = period.BillingKindId,
                Year = period.Year,
                Month = period.Month,
                PeriodStartDate = period.PeriodStartDate,
                PeriodEndDate = period.PeriodEndDate,
                UserCount = period.UserCount,
                UnitPriceSum = 0,
                OperatorAmount = period.Amount,
                ServiceAmount = period.ServiceAmount,
                SalonModuleLines = period.ModuleLines
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
                    .ToList()
            };
        }

        var activeOperatorUnitPrice = 0m;
        if (period.UnitPrice <= 0m && (period.UserCount <= 0 || period.Amount <= 0m))
        {
            var (price, _) = await _servicePricingFactory.TryGetActiveCallCenterOperatorUnitPriceAsync();
            activeOperatorUnitPrice = price ?? 0m;
        }

        var operatorUnitPrice = period.UnitPrice > 0m
            ? period.UnitPrice
            : period.UserCount > 0
                ? Math.Round(period.Amount / period.UserCount, 2, MidpointRounding.AwayFromZero)
                : activeOperatorUnitPrice;
        var productLines = new List<BillingTahakkukProductLineDto>();
        if (period.UserCount > 0 || period.Amount > 0m)
        {
            productLines.Add(new BillingTahakkukProductLineDto
            {
                ProductTypeId = ProductTypes.Ids.CallCenter,
                ProductLabel = ServicePricingFactory.CallCenterOperatorLicenseName,
                MonthlyUnitPrice = operatorUnitPrice,
                LineAmount = period.Amount
            });
        }

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

        return new BillingTahakkukDetailDto
        {
            PeriodId = period.Id,
            CustomerId = customerId,
            CustomerName = period.Customer.Name,
            BillingKindId = period.BillingKindId,
            Year = period.Year,
            Month = period.Month,
            PeriodStartDate = period.PeriodStartDate,
            PeriodEndDate = period.PeriodEndDate,
            UserCount = period.UserCount,
            UnitPriceSum = operatorUnitPrice,
            OperatorAmount = period.Amount,
            ServiceAmount = period.ServiceAmount,
            ProductLines = productLines,
            ServiceLines = serviceLines
        };
    }
}
