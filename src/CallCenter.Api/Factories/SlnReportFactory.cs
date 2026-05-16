using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Shared.DTOs;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CallCenter.Api.Factories;

public class SlnReportFactory : ISlnReportFactory
{
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly ISlnInvoiceItemEntityService _invoiceItems;
    private readonly ISlnProductEntityService _products;
    private readonly ISlnSupplierEntityService _suppliers;
    private readonly ISlnExpenseEntityService _expenses;
    private readonly ISlnCashTransactionEntityService _cashTransactions;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnPersonnelCommissionEntityService _commissions;
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly ISlnBranchEntityService _branches;
    private readonly ISlnSalonProfileEntityService _profiles;
    private readonly ISlnStockBalanceService _stockBalances;
    private readonly ILogger<SlnReportFactory> _logger;

    private static readonly Dictionary<int, string> PaymentMethodNames = new()
    {
        { 1, "Nakit" },
        { 2, "Kredi Karti" },
        { 3, "Karma" },
        { 4, "Havale/EFT" },
        { 5, "Hediye Karti" }
    };

    public SlnReportFactory(
        ISlnInvoiceEntityService invoices,
        ISlnInvoiceItemEntityService invoiceItems,
        ISlnProductEntityService products,
        ISlnSupplierEntityService suppliers,
        ISlnExpenseEntityService expenses,
        ISlnCashTransactionEntityService cashTransactions,
        ISlnClientEntityService clients,
        ISlnPersonnelCommissionEntityService commissions,
        ISlnAppointmentEntityService appointments,
        ICustomerPersonnelEntityService personnel,
        ISlnBranchEntityService branches,
        ISlnSalonProfileEntityService profiles,
        ISlnStockBalanceService stockBalances,
        ILogger<SlnReportFactory> logger)
    {
        _invoices = invoices;
        _invoiceItems = invoiceItems;
        _products = products;
        _suppliers = suppliers;
        _expenses = expenses;
        _cashTransactions = cashTransactions;
        _clients = clients;
        _commissions = commissions;
        _appointments = appointments;
        _personnel = personnel;
        _branches = branches;
        _profiles = profiles;
        _stockBalances = stockBalances;
        _logger = logger;
    }

    public async Task<SlnKpiReportDto> GetKpiReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null)
    {
        var (start, end) = NormalizeRange(from, to);

        var invoiceQuery = _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId
                && i.StatusId != 3
                && i.InvoiceDate >= start
                && i.InvoiceDate < end);

        if (branchId.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.BranchId == branchId.Value);

        var invoices = await invoiceQuery
            .Select(i => new { i.Id, i.SlnClientId, i.NetAmount })
            .ToListAsync();

        var totalRevenue = invoices.Sum(i => i.NetAmount);
        var invoiceCount = invoices.Count;
        var activeClientIds = invoices
            .Where(i => i.SlnClientId.HasValue)
            .Select(i => i.SlnClientId!.Value)
            .Distinct()
            .ToList();

        var repeatClientCount = invoices
            .Where(i => i.SlnClientId.HasValue)
            .GroupBy(i => i.SlnClientId!.Value)
            .Count(g => g.Count() > 1);

        var lifetimeQuery = _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId
                && i.StatusId != 3
                && i.SlnClientId.HasValue);

        if (branchId.HasValue)
            lifetimeQuery = lifetimeQuery.Where(i => i.BranchId == branchId.Value);

        var lifetimeTotals = await lifetimeQuery
            .GroupBy(i => i.SlnClientId)
            .Select(g => g.Sum(i => i.NetAmount))
            .ToListAsync();

        var appointmentQuery = _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId
                && a.StatusId != 4
                && a.StatusId != 5
                && a.StartTime >= start
                && a.StartTime < end);

        if (branchId.HasValue)
            appointmentQuery = appointmentQuery.Where(a => a.BranchId == branchId.Value);

        var appointments = await appointmentQuery
            .Select(a => new
            {
                a.PersonnelId,
                a.StartTime,
                a.EndTime,
                a.StatusId
            })
            .ToListAsync();

        var staffQuery = _personnel.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && p.IsActive);

        if (branchId.HasValue)
            staffQuery = staffQuery.Where(p => p.BranchId == branchId.Value);

        var activeStaff = await staffQuery
            .Include(p => p.User)
            .Select(p => new
            {
                p.Id,
                p.BranchId,
                PersonnelName = p.User.FullName
            })
            .ToListAsync();

        var staffItemQuery = _invoiceItems.GetAllQueryable()
            .Where(it => it.Invoice != null
                && it.Invoice.CustomerId == customerId
                && it.Invoice.StatusId != 3
                && it.Invoice.InvoiceDate >= start
                && it.Invoice.InvoiceDate < end
                && it.PersonnelId.HasValue);

        if (branchId.HasValue)
            staffItemQuery = staffItemQuery.Where(it => it.Invoice!.BranchId == branchId.Value);

        var staffItems = await staffItemQuery
            .Select(it => new
            {
                PersonnelId = it.PersonnelId!.Value,
                it.ServiceId,
                it.LineTotal
            })
            .ToListAsync();

        var staffRevenue = staffItems
            .GroupBy(it => it.PersonnelId)
            .ToDictionary(g => g.Key, g => new
            {
                Revenue = g.Sum(it => it.LineTotal),
                ServiceCount = g.Count(it => it.ServiceId.HasValue)
            });

        var staffAppointments = appointments
            .GroupBy(a => a.PersonnelId)
            .ToDictionary(g => g.Key, g => new
            {
                AppointmentCount = g.Count(),
                CompletedAppointmentCount = g.Count(a => a.StatusId == 3),
                BookedHours = RoundHours(g.Sum(a => SafeMinutes(a.StartTime, a.EndTime)))
            });

        var bookedHours = RoundHours(appointments.Sum(a => SafeMinutes(a.StartTime, a.EndTime)));
        var capacityHours = await CalculateCapacityHoursAsync(customerId, branchId, start, end, activeStaff.Select(s => s.BranchId).ToList());
        var staffEfficiency = activeStaff
            .Select(staff =>
            {
                staffRevenue.TryGetValue(staff.Id, out var revenue);
                staffAppointments.TryGetValue(staff.Id, out var appt);
                var staffBookedHours = appt?.BookedHours ?? 0;
                var staffServiceCount = revenue?.ServiceCount ?? 0;

                return new SlnStaffEfficiencyDto
                {
                    PersonnelId = staff.Id,
                    PersonnelName = staff.PersonnelName,
                    ServiceCount = staffServiceCount,
                    AppointmentCount = appt?.AppointmentCount ?? 0,
                    CompletedAppointmentCount = appt?.CompletedAppointmentCount ?? 0,
                    BookedHours = staffBookedHours,
                    Revenue = revenue?.Revenue ?? 0,
                    RevenuePerBookedHour = staffBookedHours > 0 ? Math.Round((revenue?.Revenue ?? 0) / staffBookedHours, 2, MidpointRounding.AwayFromZero) : 0,
                    RevenuePerService = staffServiceCount > 0 ? Math.Round((revenue?.Revenue ?? 0) / staffServiceCount, 2, MidpointRounding.AwayFromZero) : 0
                };
            })
            .OrderByDescending(s => s.Revenue)
            .ThenByDescending(s => s.BookedHours)
            .Take(8)
            .ToList();

        return new SlnKpiReportDto
        {
            TotalRevenue = totalRevenue,
            InvoiceCount = invoiceCount,
            AverageTicket = invoiceCount > 0 ? Math.Round(totalRevenue / invoiceCount, 2, MidpointRounding.AwayFromZero) : 0,
            BookedHours = bookedHours,
            CapacityHours = capacityHours,
            OccupancyPercent = PercentOrZero(bookedHours, capacityHours),
            AppointmentCount = appointments.Count,
            CompletedAppointmentCount = appointments.Count(a => a.StatusId == 3),
            ActiveClientCount = activeClientIds.Count,
            RepeatClientCount = repeatClientCount,
            RepeatVisitRatePercent = PercentOrZero(repeatClientCount, activeClientIds.Count),
            AverageLifetimeValue = lifetimeTotals.Count > 0 ? Math.Round(lifetimeTotals.Average(), 2, MidpointRounding.AwayFromZero) : 0,
            PeriodSpendPerClient = activeClientIds.Count > 0 ? Math.Round(totalRevenue / activeClientIds.Count, 2, MidpointRounding.AwayFromZero) : 0,
            ActiveStaffCount = activeStaff.Count,
            RevenuePerActiveStaff = activeStaff.Count > 0 ? Math.Round(totalRevenue / activeStaff.Count, 2, MidpointRounding.AwayFromZero) : 0,
            RevenuePerBookedHour = bookedHours > 0 ? Math.Round(totalRevenue / bookedHours, 2, MidpointRounding.AwayFromZero) : 0,
            StaffEfficiency = staffEfficiency
        };
    }

    public async Task<SlnBranchComparisonReportDto> GetBranchComparisonReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null)
    {
        var (start, end) = NormalizeRange(from, to);
        var branches = await _branches.GetAllQueryable()
            .Where(b => b.CustomerId == customerId && b.IsActive)
            .Select(b => new { b.Id, b.Name, b.IsHeadquarter })
            .ToListAsync();

        var branchNames = branches.ToDictionary(b => b.Id, b => b.Name);

        var invoiceQuery = _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId
                && i.StatusId != 3
                && i.InvoiceDate >= start
                && i.InvoiceDate < end);

        if (branchId.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.BranchId == branchId.Value);

        var invoices = await invoiceQuery
            .Select(i => new { i.Id, i.BranchId, i.SlnClientId, i.NetAmount })
            .ToListAsync();

        var itemQuery = _invoiceItems.GetAllQueryable()
            .Where(it => it.Invoice != null
                && it.Invoice.CustomerId == customerId
                && it.Invoice.StatusId != 3
                && it.Invoice.InvoiceDate >= start
                && it.Invoice.InvoiceDate < end);

        if (branchId.HasValue)
            itemQuery = itemQuery.Where(it => it.Invoice!.BranchId == branchId.Value);

        var items = await itemQuery
            .Select(it => new
            {
                BranchId = it.Invoice!.BranchId,
                it.ServiceId,
                ServiceName = it.Service != null ? it.Service.Name : "",
                it.ProductId,
                ProductName = it.Product != null ? it.Product.Name : "",
                it.PersonnelId,
                PersonnelName = it.Personnel != null && it.Personnel.User != null ? it.Personnel.User.FullName : "",
                it.LineTotal
            })
            .ToListAsync();

        var appointmentQuery = _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId
                && a.StatusId != 4
                && a.StatusId != 5
                && a.StartTime >= start
                && a.StartTime < end);

        if (branchId.HasValue)
            appointmentQuery = appointmentQuery.Where(a => a.BranchId == branchId.Value);

        var appointments = await appointmentQuery
            .Select(a => new { a.BranchId, a.StatusId })
            .ToListAsync();

        var branchKeys = branches
            .Where(b => !branchId.HasValue || b.Id == branchId.Value)
            .OrderByDescending(b => b.IsHeadquarter)
            .ThenBy(b => b.Name)
            .Select(b => (int?)b.Id)
            .ToList();

        if (!branchId.HasValue && (branchKeys.Count == 0 || invoices.Any(i => i.BranchId == null) || items.Any(i => i.BranchId == null) || appointments.Any(a => a.BranchId == null)))
            branchKeys.Insert(0, null);

        foreach (var dataBranchId in invoices.Select(i => i.BranchId)
                     .Concat(items.Select(i => i.BranchId))
                     .Concat(appointments.Select(a => a.BranchId))
                     .Distinct())
        {
            if (!branchKeys.Contains(dataBranchId))
                branchKeys.Add(dataBranchId);
        }

        var totalRevenue = invoices.Sum(i => i.NetAmount);
        var rows = branchKeys
            .Select(key =>
            {
                var branchInvoices = invoices.Where(i => i.BranchId == key).ToList();
                var branchItems = items.Where(i => i.BranchId == key).ToList();
                var branchAppointments = appointments.Where(a => a.BranchId == key).ToList();
                var revenue = branchInvoices.Sum(i => i.NetAmount);

                return new SlnBranchComparisonRowDto
                {
                    BranchId = key,
                    BranchName = GetBranchName(key, branchNames),
                    TotalRevenue = revenue,
                    ServiceRevenue = branchItems.Where(i => i.ServiceId.HasValue).Sum(i => i.LineTotal),
                    ProductRevenue = branchItems.Where(i => i.ProductId.HasValue).Sum(i => i.LineTotal),
                    InvoiceCount = branchInvoices.Count,
                    AverageTicket = branchInvoices.Count > 0 ? Math.Round(revenue / branchInvoices.Count, 2, MidpointRounding.AwayFromZero) : 0,
                    AppointmentCount = branchAppointments.Count,
                    CompletedAppointmentCount = branchAppointments.Count(a => a.StatusId == 3),
                    ActiveClientCount = branchInvoices.Where(i => i.SlnClientId.HasValue).Select(i => i.SlnClientId!.Value).Distinct().Count(),
                    RevenueSharePercent = PercentOrZero(revenue, totalRevenue)
                };
            })
            .OrderByDescending(r => r.TotalRevenue)
            .ThenBy(r => r.BranchName)
            .ToList();

        return new SlnBranchComparisonReportDto
        {
            Branches = rows,
            Services = BuildBranchDimensionRows(
                items.Where(i => i.ServiceId.HasValue)
                    .Select(i => new BranchDimensionSource(i.BranchId, i.ServiceId!.Value, string.IsNullOrWhiteSpace(i.ServiceName) ? $"Hizmet #{i.ServiceId}" : i.ServiceName, i.LineTotal)),
                branchNames),
            Personnel = BuildBranchDimensionRows(
                items.Where(i => i.PersonnelId.HasValue)
                    .Select(i => new BranchDimensionSource(i.BranchId, i.PersonnelId!.Value, string.IsNullOrWhiteSpace(i.PersonnelName) ? $"Personel #{i.PersonnelId}" : i.PersonnelName, i.LineTotal)),
                branchNames),
            Products = BuildBranchDimensionRows(
                items.Where(i => i.ProductId.HasValue)
                    .Select(i => new BranchDimensionSource(i.BranchId, i.ProductId!.Value, string.IsNullOrWhiteSpace(i.ProductName) ? $"Urun #{i.ProductId}" : i.ProductName, i.LineTotal)),
                branchNames)
        };
    }

    public async Task<SlnSalesReportDto> GetSalesReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null)
    {
        var (start, end) = NormalizeRange(from, to);
        var query = _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId && i.StatusId != 3 && i.InvoiceDate >= start && i.InvoiceDate < end);

        if (branchId.HasValue)
            query = query.Where(i => i.BranchId == branchId.Value);

        var invoices = await query
            .Include(i => i.Items).ThenInclude(it => it.Service)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .ToListAsync();

        var totalRevenue = invoices.Sum(i => i.NetAmount);
        var totalInvoices = invoices.Count;

        decimal serviceRevenue = 0;
        decimal productRevenue = 0;
        foreach (var inv in invoices)
        {
            foreach (var item in inv.Items)
            {
                if (item.ServiceId.HasValue)
                    serviceRevenue += item.LineTotal;
                else if (item.ProductId.HasValue)
                    productRevenue += item.LineTotal;
            }
        }

        var dailySales = invoices
            .GroupBy(i => i.InvoiceDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new SlnDailySalesDto
            {
                Date = g.Key,
                Revenue = g.Sum(i => i.NetAmount),
                InvoiceCount = g.Count()
            })
            .ToList();

        var paymentBreakdown = invoices
            .GroupBy(i => i.PaymentMethodId)
            .Select(g => new SlnPaymentMethodSalesDto
            {
                PaymentMethodId = g.Key,
                PaymentMethodName = PaymentMethodNames.GetValueOrDefault(g.Key, "Diger"),
                Amount = g.Sum(i => i.NetAmount),
                Count = g.Count()
            })
            .ToList();

        return new SlnSalesReportDto
        {
            TotalRevenue = totalRevenue,
            TotalInvoices = totalInvoices,
            ServiceRevenue = serviceRevenue,
            ProductRevenue = productRevenue,
            AverageTicket = totalInvoices > 0 ? totalRevenue / totalInvoices : 0,
            DailySales = dailySales,
            PaymentMethodBreakdown = paymentBreakdown
        };
    }

    public async Task<SlnStaffReportDto> GetStaffReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null)
    {
        var (start, end) = NormalizeRange(from, to);
        var staffQuery = _invoiceItems.GetAllQueryable()
            .Where(it => it.Invoice != null && it.Invoice.CustomerId == customerId
                && it.Invoice.StatusId != 3
                && it.Invoice.InvoiceDate >= start && it.Invoice.InvoiceDate < end
                && it.PersonnelId.HasValue);

        if (branchId.HasValue)
            staffQuery = staffQuery.Where(it => it.Invoice!.BranchId == branchId.Value);

        var items = await staffQuery
            .Include(it => it.Personnel).ThenInclude(p => p!.User)
            .Include(it => it.Invoice)
            .ToListAsync();

        var staffGroups = items
            .GroupBy(it => it.PersonnelId!.Value)
            .Select(g =>
            {
                var first = g.First();
                return new SlnStaffPerformanceDto
                {
                    PersonnelId = g.Key,
                    PersonnelName = first.Personnel?.User?.FullName ?? "",
                    ServiceCount = g.Count(it => it.ServiceId.HasValue),
                    Revenue = g.Sum(it => it.LineTotal),
                    Commission = 0 // Prim hesabi asagida
                };
            })
            .ToList();

        // Prim hesabi: commission rate * invoice item lineTotal
        var personnelIds = staffGroups.Select(s => s.PersonnelId).ToList();
        var commissionRates = await _commissions.GetAllQueryable()
            .Where(c => personnelIds.Contains(c.PersonnelId))
            .ToListAsync();

        foreach (var staff in staffGroups)
        {
            var staffItems = items.Where(it => it.PersonnelId == staff.PersonnelId).ToList();
            decimal totalCommission = 0;

            foreach (var item in staffItems)
            {
                // Hizmet/urun bazli prim tanimini bul, yoksa genel tanimini kullan
                var rate = commissionRates.FirstOrDefault(c =>
                    c.PersonnelId == staff.PersonnelId &&
                    ((item.ServiceId.HasValue && c.ServiceId == item.ServiceId) ||
                     (item.ProductId.HasValue && c.ProductId == item.ProductId)))
                    ?? commissionRates.FirstOrDefault(c =>
                        c.PersonnelId == staff.PersonnelId && c.ServiceId == null && c.ProductId == null);

                if (rate != null)
                {
                    totalCommission += rate.IsPercentage
                        ? item.LineTotal * rate.Rate / 100
                        : rate.Rate;
                }
            }

            staff.Commission = totalCommission;
        }

        return new SlnStaffReportDto
        {
            Staff = staffGroups.OrderByDescending(s => s.Revenue).ToList()
        };
    }

    public async Task<SlnStockReportDto> GetStockReportAsync(int customerId, int? branchId = null)
    {
        var products = await _products.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && p.IsActive &&
                (!branchId.HasValue || p.BranchId == null || p.BranchId == branchId.Value))
            .Include(p => p.Category)
            .ToListAsync();

        var stockMap = await _stockBalances.GetStockQuantitiesAsync(customerId, products.Select(p => p.Id), branchId);

        var items = products.Select(p =>
        {
            var stockQuantity = stockMap.GetValueOrDefault(p.Id, ResolveStockFallback(branchId, p.StockQuantity));
            return new SlnStockItemDto
        {
            ProductId = p.Id,
            ProductName = p.Name,
            CategoryName = p.Category?.Name ?? "",
            StockQuantity = stockQuantity,
            MinStockLevel = p.MinStockLevel,
            PurchasePrice = p.PurchasePrice,
            SalePrice = p.SalePrice,
            TaxRate = p.TaxRate,
            StockValue = Math.Round(stockQuantity * p.PurchasePrice, 2, MidpointRounding.AwayFromZero),
            RetailValue = Math.Round(stockQuantity * p.SalePrice, 2, MidpointRounding.AwayFromZero),
            IsLowStock = p.MinStockLevel > 0 && stockQuantity <= p.MinStockLevel
        };
        }).Select(item =>
        {
            item.PotentialGrossProfit = item.RetailValue - item.StockValue;
            item.MarginPercent = PercentOrZero(item.PotentialGrossProfit, item.RetailValue);
            item.EstimatedVatAmount = VatFromVatIncluded(item.RetailValue, item.TaxRate);
            return item;
        }).ToList();

        var suppliers = await _suppliers.GetAllQueryable()
            .Where(s => s.CustomerId == customerId)
            .Include(s => s.Transactions)
            .ToListAsync();

        var supplierDebtBreakdown = suppliers
            .Select(s =>
            {
                var balance = s.Transactions.Sum(t => t.TransactionTypeId == 1 ? t.Amount : -t.Amount);
                return new SlnSupplierDebtBreakdownDto
                {
                    SupplierId = s.Id,
                    SupplierName = s.Name,
                    Balance = Math.Round(balance, 2, MidpointRounding.AwayFromZero),
                    LastTransactionDate = s.Transactions
                        .OrderByDescending(t => t.TransactionDate)
                        .Select(t => (DateTime?)t.TransactionDate)
                        .FirstOrDefault()
                };
            })
            .Where(s => s.Balance != 0)
            .OrderByDescending(s => s.Balance)
            .ToList();

        var taxBreakdown = items
            .GroupBy(i => i.TaxRate)
            .Select(g => new SlnStockTaxBreakdownDto
            {
                TaxRate = g.Key,
                ProductCount = g.Count(),
                StockValue = g.Sum(i => i.StockValue),
                RetailValue = g.Sum(i => i.RetailValue),
                EstimatedVatAmount = g.Sum(i => i.EstimatedVatAmount)
            })
            .OrderBy(t => t.TaxRate)
            .ToList();

        var totalStockValue = items.Sum(i => i.StockValue);
        var totalRetailValue = items.Sum(i => i.RetailValue);
        var potentialGrossProfit = totalRetailValue - totalStockValue;

        return new SlnStockReportDto
        {
            TotalProducts = products.Count,
            LowStockCount = items.Count(i => i.IsLowStock),
            TotalStockValue = totalStockValue,
            TotalRetailValue = totalRetailValue,
            PotentialGrossProfit = potentialGrossProfit,
            AverageMarginPercent = PercentOrZero(potentialGrossProfit, totalRetailValue),
            EstimatedVatTotal = items.Sum(i => i.EstimatedVatAmount),
            SupplierDebtTotal = supplierDebtBreakdown.Sum(s => s.Balance),
            TaxBreakdown = taxBreakdown,
            SupplierDebtBreakdown = supplierDebtBreakdown,
            Items = items.OrderBy(i => i.IsLowStock ? 0 : 1).ThenBy(i => i.ProductName).ToList()
        };
    }

    public async Task<SlnFinanceReportDto> GetFinanceReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null)
    {
        var (start, end) = NormalizeRange(from, to);
        var invoiceQuery = _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId && i.StatusId != 3 && i.InvoiceDate >= start && i.InvoiceDate < end);

        if (branchId.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.BranchId == branchId.Value);

        var invoices = await invoiceQuery
            .Select(i => new
            {
                i.Id,
                i.TotalAmount,
                i.DiscountAmount,
                i.NetAmount,
                i.TaxAmount,
                i.GrandTotal,
                i.PaymentMethodId
            })
            .ToListAsync();

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var invoiceItems = await _invoiceItems.GetAllQueryable()
            .Where(it => invoiceIds.Contains(it.InvoiceId))
            .Select(it => new
            {
                it.ServiceId,
                it.ProductId,
                it.LineTotal,
                it.TaxRate,
                it.TaxAmount
            })
            .ToListAsync();

        var totalIncome = invoices.Sum(i => i.NetAmount);
        var grossRevenue = invoices.Sum(i => i.TotalAmount);
        var discountTotal = invoices.Sum(i => i.DiscountAmount);
        var salesVatTotal = invoices.Sum(i => i.TaxAmount);
        var serviceRevenue = invoiceItems.Where(i => i.ServiceId.HasValue).Sum(i => i.LineTotal);
        var productRevenue = invoiceItems.Where(i => i.ProductId.HasValue).Sum(i => i.LineTotal);

        var paymentBreakdown = invoices
            .GroupBy(i => i.PaymentMethodId)
            .Select(g => new SlnPaymentMethodSalesDto
            {
                PaymentMethodId = g.Key,
                PaymentMethodName = PaymentMethodNames.GetValueOrDefault(g.Key, "Diger"),
                Amount = g.Sum(i => i.GrandTotal > 0 ? i.GrandTotal : i.NetAmount),
                Count = g.Count()
            })
            .OrderByDescending(p => p.Amount)
            .ToList();

        var taxBreakdown = invoiceItems
            .GroupBy(i => i.TaxRate)
            .Select(g => new SlnFinanceTaxBreakdownDto
            {
                TaxRate = g.Key,
                TaxableAmount = g.Sum(i => i.LineTotal),
                TaxAmount = g.Sum(i => i.TaxAmount),
                LineCount = g.Count()
            })
            .OrderBy(t => t.TaxRate)
            .ToList();

        var expenseQuery = _expenses.GetAllQueryable()
            .Where(e => e.CustomerId == customerId && e.ExpenseDate >= start && e.ExpenseDate < end);

        if (branchId.HasValue)
            expenseQuery = expenseQuery.Where(e => e.BranchId == branchId.Value);

        var expensesList = await expenseQuery
            .Include(e => e.Category)
            .ToListAsync();

        var totalExpense = expensesList.Sum(e => e.Amount);
        var expenseVatTotal = expensesList.Sum(e => e.TaxAmount);

        var expenseBreakdown = expensesList
            .GroupBy(e => e.Category?.Name ?? "Diger")
            .Select(g => new SlnExpenseCategoryBreakdownDto
            {
                CategoryName = g.Key,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(e => e.Amount)
            .ToList();

        var cashPeriodQuery = _cashTransactions.GetAllQueryable()
            .Where(t => t.Register != null
                && t.Register.CustomerId == customerId
                && t.CreatedAt >= start
                && t.CreatedAt < end);

        var cashBalanceQuery = _cashTransactions.GetAllQueryable()
            .Where(t => t.Register != null
                && t.Register.CustomerId == customerId
                && t.CreatedAt < end);

        if (branchId.HasValue)
        {
            cashPeriodQuery = cashPeriodQuery.Where(t => t.Register!.BranchId == branchId.Value);
            cashBalanceQuery = cashBalanceQuery.Where(t => t.Register!.BranchId == branchId.Value);
        }

        var cashTransactions = await cashPeriodQuery
            .Select(t => new { t.TransactionTypeId, t.Amount })
            .ToListAsync();
        var cashIncome = cashTransactions.Where(t => t.TransactionTypeId == 1).Sum(t => t.Amount);
        var cashExpense = cashTransactions.Where(t => t.TransactionTypeId == 2).Sum(t => t.Amount);

        var cashBalanceRows = await cashBalanceQuery
            .Select(t => new { t.TransactionTypeId, t.Amount })
            .ToListAsync();
        var cashBalance = cashBalanceRows.Sum(t => t.TransactionTypeId == 1 ? t.Amount : t.TransactionTypeId == 2 ? -t.Amount : 0);

        var products = await _products.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && p.IsActive &&
                (!branchId.HasValue || p.BranchId == null || p.BranchId == branchId.Value))
            .Select(p => new
            {
                p.Id,
                p.StockQuantity,
                p.PurchasePrice,
                p.SalePrice,
                p.TaxRate
            })
            .ToListAsync();

        var financeStockMap = await _stockBalances.GetStockQuantitiesAsync(customerId, products.Select(p => p.Id), branchId);
        var stockValue = products.Sum(p => ResolveStockQuantity(financeStockMap, p.Id, branchId, p.StockQuantity) * p.PurchasePrice);
        var retailStockValue = products.Sum(p => ResolveStockQuantity(financeStockMap, p.Id, branchId, p.StockQuantity) * p.SalePrice);
        var estimatedStockVat = products.Sum(p => VatFromVatIncluded(ResolveStockQuantity(financeStockMap, p.Id, branchId, p.StockQuantity) * p.SalePrice, p.TaxRate));

        return new SlnFinanceReportDto
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            NetProfit = totalIncome - totalExpense,
            InvoiceCount = invoices.Count,
            GrossRevenue = grossRevenue,
            DiscountTotal = discountTotal,
            ServiceRevenue = serviceRevenue,
            ProductRevenue = productRevenue,
            SalesVatTotal = salesVatTotal,
            ExpenseVatTotal = expenseVatTotal,
            VatPayable = salesVatTotal - expenseVatTotal,
            StockValue = stockValue,
            RetailStockValue = retailStockValue,
            EstimatedStockVat = estimatedStockVat,
            CashIncome = cashIncome,
            CashExpense = cashExpense,
            CashNet = cashIncome - cashExpense,
            CashBalance = cashBalance,
            PaymentMethodBreakdown = paymentBreakdown,
            TaxBreakdown = taxBreakdown,
            ExpenseBreakdown = expenseBreakdown
        };
    }

    public async Task<SlnClientReportDto> GetClientReportAsync(int customerId, DateTime from, DateTime to, int? branchId = null)
    {
        var (start, end) = NormalizeRange(from, to);
        var totalClients = await _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .CountAsync();

        var newClientsInPeriod = await _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId && c.CreatedAt >= start && c.CreatedAt < end)
            .CountAsync();

        // En degerli musteriler
        var clientInvoiceQuery = _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId && i.StatusId != 3 && i.SlnClientId.HasValue
                && i.InvoiceDate >= start && i.InvoiceDate < end);

        if (branchId.HasValue)
            clientInvoiceQuery = clientInvoiceQuery.Where(i => i.BranchId == branchId.Value);

        var clientStats = await clientInvoiceQuery
            .GroupBy(i => i.SlnClientId)
            .Select(g => new
            {
                ClientId = g.Key!.Value,
                VisitCount = g.Count(),
                TotalSpent = g.Sum(i => i.NetAmount),
                LastVisit = g.Max(i => i.InvoiceDate)
            })
            .OrderByDescending(s => s.TotalSpent)
            .Take(20)
            .ToListAsync();

        var topClientIds = clientStats.Select(s => s.ClientId).ToList();
        var clientNames = await _clients.GetAllQueryable()
            .Where(c => topClientIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.FullName);

        var topClients = clientStats.Select(s => new SlnTopClientDto
        {
            ClientId = s.ClientId,
            ClientName = clientNames.GetValueOrDefault(s.ClientId, ""),
            VisitCount = s.VisitCount,
            TotalSpent = s.TotalSpent,
            LastVisit = s.LastVisit
        }).ToList();

        // Ortalama ziyaret sikligi
        decimal avgFrequency = 0;
        if (clientStats.Count > 0)
            avgFrequency = (decimal)clientStats.Sum(s => s.VisitCount) / clientStats.Count;

        return new SlnClientReportDto
        {
            TotalClients = totalClients,
            NewClientsInPeriod = newClientsInPeriod,
            AverageVisitFrequency = avgFrequency,
            TopClients = topClients
        };
    }

    public async Task<byte[]> ExportSalonReportCsvAsync(int customerId, string report, DateTime from, DateTime to, int? branchId = null)
    {
        var reportKey = NormalizeReportKey(report);
        var sb = new StringBuilder();

        switch (reportKey)
        {
            case "kpis":
                AppendKpiCsv(sb, await GetKpiReportAsync(customerId, from, to, branchId));
                break;
            case "sales":
                AppendSalesCsv(sb, await GetSalesReportAsync(customerId, from, to, branchId));
                break;
            case "staff":
                AppendStaffCsv(sb, await GetStaffReportAsync(customerId, from, to, branchId));
                break;
            case "stock":
                AppendStockCsv(sb, await GetStockReportAsync(customerId, branchId));
                break;
            case "finance":
                AppendFinanceCsv(sb, await GetFinanceReportAsync(customerId, from, to, branchId));
                break;
            case "clients":
                AppendClientsCsv(sb, await GetClientReportAsync(customerId, from, to, branchId));
                break;
            case "branches":
                AppendBranchesCsv(sb, await GetBranchComparisonReportAsync(customerId, from, to, branchId));
                break;
            default:
                throw new ArgumentException("Desteklenmeyen rapor turu.", nameof(report));
        }

        return ToCsvBytes(sb.ToString());
    }

    public async Task<byte[]> ExportSalonReportExcelAsync(int customerId, string report, DateTime from, DateTime to, int? branchId = null)
    {
        var reportKey = NormalizeReportKey(report);
        using var workbook = new XLWorkbook();

        switch (reportKey)
        {
            case "kpis":
                AddKpiWorksheets(workbook, await GetKpiReportAsync(customerId, from, to, branchId));
                break;
            case "sales":
                AddSalesWorksheets(workbook, await GetSalesReportAsync(customerId, from, to, branchId));
                break;
            case "staff":
                AddStaffWorksheets(workbook, await GetStaffReportAsync(customerId, from, to, branchId));
                break;
            case "stock":
                AddStockWorksheets(workbook, await GetStockReportAsync(customerId, branchId));
                break;
            case "finance":
                AddFinanceWorksheets(workbook, await GetFinanceReportAsync(customerId, from, to, branchId));
                break;
            case "clients":
                AddClientsWorksheets(workbook, await GetClientReportAsync(customerId, from, to, branchId));
                break;
            case "branches":
                AddBranchesWorksheets(workbook, await GetBranchComparisonReportAsync(customerId, from, to, branchId));
                break;
            default:
                throw new ArgumentException("Desteklenmeyen rapor turu.", nameof(report));
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportSalonReportPdfAsync(int customerId, string report, DateTime from, DateTime to, int? branchId = null)
    {
        var reportKey = NormalizeReportKey(report);
        var csvBytes = await ExportSalonReportCsvAsync(customerId, reportKey, from, to, branchId);
        var csvText = Encoding.UTF8.GetString(csvBytes).TrimStart('\uFEFF');
        var title = $"Salon {ReportDisplayName(reportKey)} Raporu";
        var subtitle = $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy}";

        var lines = new List<string> { subtitle, "" };
        foreach (var rawLine in csvText.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var line = rawLine.Replace(';', ' ');
            if (string.IsNullOrWhiteSpace(line))
            {
                lines.Add("");
                continue;
            }

            foreach (var wrapped in WrapPdfLine(line, 105))
                lines.Add(wrapped);
        }

        return SimplePdfBuilder.Build(title, lines);
    }

    private static void AppendKpiCsv(StringBuilder sb, SlnKpiReportDto report)
    {
        AppendCsvSection(sb, "KPI Ozeti", ["Metrik", "Deger"], [
            new object?[] { "Toplam Gelir", report.TotalRevenue },
            new object?[] { "Adisyon Sayisi", report.InvoiceCount },
            new object?[] { "Ortalama Sepet", report.AverageTicket },
            new object?[] { "Dolu Saat", report.BookedHours },
            new object?[] { "Kapasite Saat", report.CapacityHours },
            new object?[] { "Doluluk (%)", report.OccupancyPercent },
            new object?[] { "Randevu", report.AppointmentCount },
            new object?[] { "Tamamlanan Randevu", report.CompletedAppointmentCount },
            new object?[] { "Aktif Musteri", report.ActiveClientCount },
            new object?[] { "Tekrar Eden Musteri", report.RepeatClientCount },
            new object?[] { "Tekrar Ziyaret (%)", report.RepeatVisitRatePercent },
            new object?[] { "Ortalama LTV", report.AverageLifetimeValue },
            new object?[] { "Donem Kisi Basi Harcama", report.PeriodSpendPerClient },
            new object?[] { "Aktif Personel", report.ActiveStaffCount },
            new object?[] { "Personel Basi Gelir", report.RevenuePerActiveStaff },
            new object?[] { "Saat Basi Gelir", report.RevenuePerBookedHour }
        ]);

        AppendCsvSection(sb, "Personel Verimliligi",
            ["Personel", "Hizmet", "Randevu", "Tamamlanan", "Saat", "Ciro", "Saat Basi", "Hizmet Basi"],
            report.StaffEfficiency.Select(s => new object?[]
            {
                s.PersonnelName, s.ServiceCount, s.AppointmentCount, s.CompletedAppointmentCount,
                s.BookedHours, s.Revenue, s.RevenuePerBookedHour, s.RevenuePerService
            }));
    }

    private static void AppendSalesCsv(StringBuilder sb, SlnSalesReportDto report)
    {
        AppendCsvSection(sb, "Satis Ozeti", ["Metrik", "Deger"], [
            new object?[] { "Toplam Gelir", report.TotalRevenue },
            new object?[] { "Adisyon Sayisi", report.TotalInvoices },
            new object?[] { "Hizmet Geliri", report.ServiceRevenue },
            new object?[] { "Urun Geliri", report.ProductRevenue },
            new object?[] { "Ortalama Sepet", report.AverageTicket }
        ]);

        AppendCsvSection(sb, "Gunluk Satis",
            ["Tarih", "Adisyon", "Gelir"],
            report.DailySales.Select(d => new object?[] { d.Date, d.InvoiceCount, d.Revenue }));

        AppendCsvSection(sb, "Odeme Yontemi",
            ["Yontem", "Adet", "Tutar"],
            report.PaymentMethodBreakdown.Select(p => new object?[] { p.PaymentMethodName, p.Count, p.Amount }));
    }

    private static void AppendStaffCsv(StringBuilder sb, SlnStaffReportDto report)
    {
        AppendCsvSection(sb, "Personel Performansi",
            ["Personel", "Hizmet Sayisi", "Ciro", "Prim"],
            report.Staff.Select(s => new object?[] { s.PersonnelName, s.ServiceCount, s.Revenue, s.Commission }));
    }

    private static void AppendStockCsv(StringBuilder sb, SlnStockReportDto report)
    {
        AppendCsvSection(sb, "Stok Ozeti", ["Metrik", "Deger"], [
            new object?[] { "Toplam Urun", report.TotalProducts },
            new object?[] { "Dusuk Stok", report.LowStockCount },
            new object?[] { "Stok Maliyeti", report.TotalStockValue },
            new object?[] { "Raf Satis Degeri", report.TotalRetailValue },
            new object?[] { "Tahmini Brut Kar", report.PotentialGrossProfit },
            new object?[] { "Ortalama Marj (%)", report.AverageMarginPercent },
            new object?[] { "Tahmini KDV", report.EstimatedVatTotal },
            new object?[] { "Tedarikci Borcu", report.SupplierDebtTotal }
        ]);

        AppendCsvSection(sb, "Urun Stok ve Marj",
            ["Urun", "Kategori", "Stok", "Min", "Alis", "Satis", "KDV", "Stok Degeri", "Satis Degeri", "Brut Kar", "Marj (%)", "Kritik"],
            report.Items.Select(i => new object?[]
            {
                i.ProductName, i.CategoryName, i.StockQuantity, i.MinStockLevel, i.PurchasePrice, i.SalePrice,
                i.TaxRate, i.StockValue, i.RetailValue, i.PotentialGrossProfit, i.MarginPercent, i.IsLowStock
            }));

        AppendCsvSection(sb, "KDV Kirilimi",
            ["KDV", "Urun", "Stok Degeri", "Satis Degeri", "Tahmini KDV"],
            report.TaxBreakdown.Select(t => new object?[] { t.TaxRate, t.ProductCount, t.StockValue, t.RetailValue, t.EstimatedVatAmount }));

        AppendCsvSection(sb, "Tedarikci Borclari",
            ["Tedarikci", "Bakiye", "Son Hareket"],
            report.SupplierDebtBreakdown.Select(s => new object?[] { s.SupplierName, s.Balance, s.LastTransactionDate }));
    }

    private static void AppendFinanceCsv(StringBuilder sb, SlnFinanceReportDto report)
    {
        AppendCsvSection(sb, "Finans Ozeti", ["Metrik", "Deger"], [
            new object?[] { "Toplam Gelir", report.TotalIncome },
            new object?[] { "Toplam Gider", report.TotalExpense },
            new object?[] { "Net Kar", report.NetProfit },
            new object?[] { "Fatura Sayisi", report.InvoiceCount },
            new object?[] { "Brut Satis", report.GrossRevenue },
            new object?[] { "Indirim", report.DiscountTotal },
            new object?[] { "Hizmet Geliri", report.ServiceRevenue },
            new object?[] { "Urun Geliri", report.ProductRevenue },
            new object?[] { "Satis KDV", report.SalesVatTotal },
            new object?[] { "Gider KDV", report.ExpenseVatTotal },
            new object?[] { "Odenecek KDV", report.VatPayable },
            new object?[] { "Stok Maliyeti", report.StockValue },
            new object?[] { "Raf Satis Degeri", report.RetailStockValue },
            new object?[] { "Stok Tahmini KDV", report.EstimatedStockVat },
            new object?[] { "Kasa Giris", report.CashIncome },
            new object?[] { "Kasa Cikis", report.CashExpense },
            new object?[] { "Kasa Net", report.CashNet },
            new object?[] { "Kasa Bakiyesi", report.CashBalance }
        ]);

        AppendCsvSection(sb, "Odeme Yontemi",
            ["Yontem", "Adet", "Tutar"],
            report.PaymentMethodBreakdown.Select(p => new object?[] { p.PaymentMethodName, p.Count, p.Amount }));

        AppendCsvSection(sb, "Satis KDV Kirilimi",
            ["KDV", "Satir", "Matrah", "KDV Tutar"],
            report.TaxBreakdown.Select(t => new object?[] { t.TaxRate, t.LineCount, t.TaxableAmount, t.TaxAmount }));

        AppendCsvSection(sb, "Masraf Dagilimi",
            ["Kategori", "Adet", "Tutar"],
            report.ExpenseBreakdown.Select(e => new object?[] { e.CategoryName, e.Count, e.Amount }));
    }

    private static void AppendClientsCsv(StringBuilder sb, SlnClientReportDto report)
    {
        AppendCsvSection(sb, "Musteri Ozeti", ["Metrik", "Deger"], [
            new object?[] { "Toplam Musteri", report.TotalClients },
            new object?[] { "Donemde Yeni", report.NewClientsInPeriod },
            new object?[] { "Ortalama Ziyaret Sikligi", report.AverageVisitFrequency }
        ]);

        AppendCsvSection(sb, "En Degerli Musteriler",
            ["Musteri", "Ziyaret", "Toplam Harcama", "Son Ziyaret"],
            report.TopClients.Select(c => new object?[] { c.ClientName, c.VisitCount, c.TotalSpent, c.LastVisit }));
    }

    private static void AppendBranchesCsv(StringBuilder sb, SlnBranchComparisonReportDto report)
    {
        AppendCsvSection(sb, "Sube Ozetleri",
            ["Sube", "Ciro", "Pay (%)", "Adisyon", "Ort. Sepet", "Hizmet", "Urun", "Randevu", "Tamamlanan", "Musteri"],
            report.Branches.Select(b => new object?[]
            {
                b.BranchName, b.TotalRevenue, b.RevenueSharePercent, b.InvoiceCount, b.AverageTicket,
                b.ServiceRevenue, b.ProductRevenue, b.AppointmentCount, b.CompletedAppointmentCount, b.ActiveClientCount
            }));

        AppendBranchDimensionCsv(sb, "Hizmet Boyutu", report.Services);
        AppendBranchDimensionCsv(sb, "Personel Boyutu", report.Personnel);
        AppendBranchDimensionCsv(sb, "Urun Boyutu", report.Products);
    }

    private static void AppendBranchDimensionCsv(StringBuilder sb, string title, IEnumerable<SlnBranchDimensionRowDto> rows)
        => AppendCsvSection(sb, title,
            ["Sube", "Boyut", "Adet", "Ciro"],
            rows.Select(r => new object?[] { r.BranchName, r.DimensionName, r.Count, r.Revenue }));

    private static void AddKpiWorksheets(XLWorkbook workbook, SlnKpiReportDto report)
    {
        AddWorksheet(workbook, "KPI Ozeti", ["Metrik", "Deger"], [
            new object?[] { "Toplam Gelir", report.TotalRevenue },
            new object?[] { "Adisyon Sayisi", report.InvoiceCount },
            new object?[] { "Ortalama Sepet", report.AverageTicket },
            new object?[] { "Dolu Saat", report.BookedHours },
            new object?[] { "Kapasite Saat", report.CapacityHours },
            new object?[] { "Doluluk (%)", report.OccupancyPercent },
            new object?[] { "Randevu", report.AppointmentCount },
            new object?[] { "Tamamlanan Randevu", report.CompletedAppointmentCount },
            new object?[] { "Aktif Musteri", report.ActiveClientCount },
            new object?[] { "Tekrar Eden Musteri", report.RepeatClientCount },
            new object?[] { "Tekrar Ziyaret (%)", report.RepeatVisitRatePercent },
            new object?[] { "Ortalama LTV", report.AverageLifetimeValue },
            new object?[] { "Donem Kisi Basi Harcama", report.PeriodSpendPerClient },
            new object?[] { "Aktif Personel", report.ActiveStaffCount },
            new object?[] { "Personel Basi Gelir", report.RevenuePerActiveStaff },
            new object?[] { "Saat Basi Gelir", report.RevenuePerBookedHour }
        ]);

        AddWorksheet(workbook, "Personel Verimliligi",
            ["Personel", "Hizmet", "Randevu", "Tamamlanan", "Saat", "Ciro", "Saat Basi", "Hizmet Basi"],
            report.StaffEfficiency.Select(s => new object?[]
            {
                s.PersonnelName, s.ServiceCount, s.AppointmentCount, s.CompletedAppointmentCount,
                s.BookedHours, s.Revenue, s.RevenuePerBookedHour, s.RevenuePerService
            }));
    }

    private static void AddSalesWorksheets(XLWorkbook workbook, SlnSalesReportDto report)
    {
        AddWorksheet(workbook, "Satis Ozeti", ["Metrik", "Deger"], [
            new object?[] { "Toplam Gelir", report.TotalRevenue },
            new object?[] { "Adisyon Sayisi", report.TotalInvoices },
            new object?[] { "Hizmet Geliri", report.ServiceRevenue },
            new object?[] { "Urun Geliri", report.ProductRevenue },
            new object?[] { "Ortalama Sepet", report.AverageTicket }
        ]);

        AddWorksheet(workbook, "Gunluk Satis",
            ["Tarih", "Adisyon", "Gelir"],
            report.DailySales.Select(d => new object?[] { d.Date, d.InvoiceCount, d.Revenue }));

        AddWorksheet(workbook, "Odeme Yontemi",
            ["Yontem", "Adet", "Tutar"],
            report.PaymentMethodBreakdown.Select(p => new object?[] { p.PaymentMethodName, p.Count, p.Amount }));
    }

    private static void AddStaffWorksheets(XLWorkbook workbook, SlnStaffReportDto report)
    {
        AddWorksheet(workbook, "Personel Performansi",
            ["Personel", "Hizmet Sayisi", "Ciro", "Prim"],
            report.Staff.Select(s => new object?[] { s.PersonnelName, s.ServiceCount, s.Revenue, s.Commission }));
    }

    private static void AddStockWorksheets(XLWorkbook workbook, SlnStockReportDto report)
    {
        AddWorksheet(workbook, "Stok Ozeti", ["Metrik", "Deger"], [
            new object?[] { "Toplam Urun", report.TotalProducts },
            new object?[] { "Dusuk Stok", report.LowStockCount },
            new object?[] { "Stok Maliyeti", report.TotalStockValue },
            new object?[] { "Raf Satis Degeri", report.TotalRetailValue },
            new object?[] { "Tahmini Brut Kar", report.PotentialGrossProfit },
            new object?[] { "Ortalama Marj (%)", report.AverageMarginPercent },
            new object?[] { "Tahmini KDV", report.EstimatedVatTotal },
            new object?[] { "Tedarikci Borcu", report.SupplierDebtTotal }
        ]);

        AddWorksheet(workbook, "Urun Stoklari",
            ["Urun", "Kategori", "Stok", "Min", "Alis", "Satis", "KDV", "Stok Degeri", "Satis Degeri", "Brut Kar", "Marj (%)", "Kritik"],
            report.Items.Select(i => new object?[]
            {
                i.ProductName, i.CategoryName, i.StockQuantity, i.MinStockLevel, i.PurchasePrice, i.SalePrice,
                i.TaxRate, i.StockValue, i.RetailValue, i.PotentialGrossProfit, i.MarginPercent, i.IsLowStock
            }));

        AddWorksheet(workbook, "KDV Kirilimi",
            ["KDV", "Urun", "Stok Degeri", "Satis Degeri", "Tahmini KDV"],
            report.TaxBreakdown.Select(t => new object?[] { t.TaxRate, t.ProductCount, t.StockValue, t.RetailValue, t.EstimatedVatAmount }));

        AddWorksheet(workbook, "Tedarikci Borclari",
            ["Tedarikci", "Bakiye", "Son Hareket"],
            report.SupplierDebtBreakdown.Select(s => new object?[] { s.SupplierName, s.Balance, s.LastTransactionDate }));
    }

    private static void AddFinanceWorksheets(XLWorkbook workbook, SlnFinanceReportDto report)
    {
        AddWorksheet(workbook, "Finans Ozeti", ["Metrik", "Deger"], [
            new object?[] { "Toplam Gelir", report.TotalIncome },
            new object?[] { "Toplam Gider", report.TotalExpense },
            new object?[] { "Net Kar", report.NetProfit },
            new object?[] { "Fatura Sayisi", report.InvoiceCount },
            new object?[] { "Brut Satis", report.GrossRevenue },
            new object?[] { "Indirim", report.DiscountTotal },
            new object?[] { "Hizmet Geliri", report.ServiceRevenue },
            new object?[] { "Urun Geliri", report.ProductRevenue },
            new object?[] { "Satis KDV", report.SalesVatTotal },
            new object?[] { "Gider KDV", report.ExpenseVatTotal },
            new object?[] { "Odenecek KDV", report.VatPayable },
            new object?[] { "Stok Maliyeti", report.StockValue },
            new object?[] { "Raf Satis Degeri", report.RetailStockValue },
            new object?[] { "Stok Tahmini KDV", report.EstimatedStockVat },
            new object?[] { "Kasa Giris", report.CashIncome },
            new object?[] { "Kasa Cikis", report.CashExpense },
            new object?[] { "Kasa Net", report.CashNet },
            new object?[] { "Kasa Bakiyesi", report.CashBalance }
        ]);

        AddWorksheet(workbook, "Odeme Yontemi",
            ["Yontem", "Adet", "Tutar"],
            report.PaymentMethodBreakdown.Select(p => new object?[] { p.PaymentMethodName, p.Count, p.Amount }));

        AddWorksheet(workbook, "Satis KDV Kirilimi",
            ["KDV", "Satir", "Matrah", "KDV Tutar"],
            report.TaxBreakdown.Select(t => new object?[] { t.TaxRate, t.LineCount, t.TaxableAmount, t.TaxAmount }));

        AddWorksheet(workbook, "Masraf Dagilimi",
            ["Kategori", "Adet", "Tutar"],
            report.ExpenseBreakdown.Select(e => new object?[] { e.CategoryName, e.Count, e.Amount }));
    }

    private static void AddClientsWorksheets(XLWorkbook workbook, SlnClientReportDto report)
    {
        AddWorksheet(workbook, "Musteri Ozeti", ["Metrik", "Deger"], [
            new object?[] { "Toplam Musteri", report.TotalClients },
            new object?[] { "Donemde Yeni", report.NewClientsInPeriod },
            new object?[] { "Ortalama Ziyaret Sikligi", report.AverageVisitFrequency }
        ]);

        AddWorksheet(workbook, "En Degerli Musteriler",
            ["Musteri", "Ziyaret", "Toplam Harcama", "Son Ziyaret"],
            report.TopClients.Select(c => new object?[] { c.ClientName, c.VisitCount, c.TotalSpent, c.LastVisit }));
    }

    private static void AddBranchesWorksheets(XLWorkbook workbook, SlnBranchComparisonReportDto report)
    {
        AddWorksheet(workbook, "Sube Ozetleri",
            ["Sube", "Ciro", "Pay (%)", "Adisyon", "Ort. Sepet", "Hizmet", "Urun", "Randevu", "Tamamlanan", "Musteri"],
            report.Branches.Select(b => new object?[]
            {
                b.BranchName, b.TotalRevenue, b.RevenueSharePercent, b.InvoiceCount, b.AverageTicket,
                b.ServiceRevenue, b.ProductRevenue, b.AppointmentCount, b.CompletedAppointmentCount, b.ActiveClientCount
            }));

        AddWorksheet(workbook, "Hizmet Boyutu",
            ["Sube", "Hizmet", "Adet", "Ciro"],
            report.Services.Select(r => new object?[] { r.BranchName, r.DimensionName, r.Count, r.Revenue }));

        AddWorksheet(workbook, "Personel Boyutu",
            ["Sube", "Personel", "Adet", "Ciro"],
            report.Personnel.Select(r => new object?[] { r.BranchName, r.DimensionName, r.Count, r.Revenue }));

        AddWorksheet(workbook, "Urun Boyutu",
            ["Sube", "Urun", "Adet", "Ciro"],
            report.Products.Select(r => new object?[] { r.BranchName, r.DimensionName, r.Count, r.Revenue }));
    }

    private static void AppendCsvSection(StringBuilder sb, string title, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
    {
        if (sb.Length > 0)
            sb.AppendLine();

        sb.AppendLine(title);
        AppendCsvRow(sb, headers);
        foreach (var row in rows)
            AppendCsvRow(sb, row);
    }

    private static void AppendCsvRow(StringBuilder sb, IReadOnlyList<object?> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
                sb.Append(';');

            sb.Append(EscapeCsvValue(FormatExportValue(values[i])));
        }

        sb.AppendLine();
    }

    private static string EscapeCsvValue(string value)
    {
        if (!value.Contains(';') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static byte[] ToCsvBytes(string text)
    {
        var bom = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(text);
        var result = new byte[bom.Length + content.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(content, 0, result, bom.Length, content.Length);
        return result;
    }

    private static void AddWorksheet(XLWorkbook workbook, string name, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
    {
        var ws = workbook.Worksheets.Add(name);

        for (var column = 0; column < headers.Count; column++)
        {
            var cell = ws.Cell(1, column + 1);
            cell.Value = headers[column];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var column = 0; column < row.Count; column++)
                SetCellValue(ws.Cell(rowIndex, column + 1), row[column]);

            rowIndex++;
        }

        if (rowIndex > 2)
            ws.Range(1, 1, rowIndex - 1, headers.Count).SetAutoFilter();

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = "";
                break;
            case DateTime date:
                cell.Value = date;
                cell.Style.DateFormat.Format = date.TimeOfDay == TimeSpan.Zero ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm";
                break;
            case DateTimeOffset date:
                cell.Value = date.DateTime;
                cell.Style.DateFormat.Format = date.TimeOfDay == TimeSpan.Zero ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm";
                break;
            case decimal number:
                cell.Value = Convert.ToDouble(number, CultureInfo.InvariantCulture);
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;
            case double number:
                cell.Value = number;
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;
            case float number:
                cell.Value = number;
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;
            case int number:
                cell.Value = number;
                break;
            case long number:
                cell.Value = number;
                break;
            case bool flag:
                cell.Value = flag ? "Evet" : "Hayir";
                break;
            default:
                cell.Value = Convert.ToString(value, ExportCulture) ?? "";
                break;
        }
    }

    private static string FormatExportValue(object? value)
        => value switch
        {
            null => "",
            DateTime date => date.TimeOfDay == TimeSpan.Zero
                ? date.ToString("dd.MM.yyyy", ExportCulture)
                : date.ToString("dd.MM.yyyy HH:mm", ExportCulture),
            DateTimeOffset date => date.TimeOfDay == TimeSpan.Zero
                ? date.ToString("dd.MM.yyyy", ExportCulture)
                : date.ToString("dd.MM.yyyy HH:mm", ExportCulture),
            decimal number => number.ToString("0.##", ExportCulture),
            double number => number.ToString("0.##", ExportCulture),
            float number => number.ToString("0.##", ExportCulture),
            bool flag => flag ? "Evet" : "Hayir",
            _ => Convert.ToString(value, ExportCulture) ?? ""
        };

    private static string NormalizeReportKey(string report)
    {
        var key = (report ?? "").Trim().ToLowerInvariant();
        return key switch
        {
            "kpi" or "overview" => "kpis",
            "sale" => "sales",
            "personnel" => "staff",
            "client" or "customers" => "clients",
            "branch" or "branch-comparison" => "branches",
            _ => key
        };
    }

    private static string ReportDisplayName(string reportKey)
        => NormalizeReportKey(reportKey) switch
        {
            "kpis" => "KPI",
            "sales" => "Satis",
            "staff" => "Personel",
            "stock" => "Stok",
            "finance" => "Finans",
            "clients" => "Musteri",
            "branches" => "Sube Karsilastirma",
            _ => reportKey
        };

    private static IEnumerable<string> WrapPdfLine(string line, int maxLen)
    {
        line = ToPdfSafeText(line);
        if (line.Length <= maxLen)
        {
            yield return line;
            yield break;
        }

        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        foreach (var word in words)
        {
            if (current.Length == 0)
            {
                current.Append(word);
                continue;
            }

            if (current.Length + word.Length + 1 > maxLen)
            {
                yield return current.ToString();
                current.Clear();
            }

            if (current.Length > 0) current.Append(' ');
            current.Append(word);
        }

        if (current.Length > 0)
            yield return current.ToString();
    }

    private static string ToPdfSafeText(string text)
    {
        var normalized = text
            .Replace('ç', 'c').Replace('Ç', 'C')
            .Replace('ğ', 'g').Replace('Ğ', 'G')
            .Replace('ı', 'i').Replace('İ', 'I')
            .Replace('ö', 'o').Replace('Ö', 'O')
            .Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ü', 'u').Replace('Ü', 'U')
            .Replace('₺', 'T')
            .Replace('—', '-')
            .Replace('·', '-');

        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
            sb.Append(ch is >= ' ' and <= '~' ? ch : '?');
        return sb.ToString();
    }

    private static class SimplePdfBuilder
    {
        public static byte[] Build(string title, IReadOnlyList<string> lines)
        {
            const int maxLinesPerPage = 44;
            var pages = lines
                .Select((line, index) => new { line, index })
                .GroupBy(x => x.index / maxLinesPerPage)
                .Select(g => g.Select(x => x.line).ToList())
                .DefaultIfEmpty(new List<string>())
                .ToList();

            var objects = new List<string>();
            var pageObjectNumbers = new List<int>();
            var catalogNo = AddObject(objects, "<< /Type /Catalog /Pages 2 0 R >>");
            var pagesNo = AddObject(objects, "");
            var fontNo = AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            foreach (var pageLines in pages)
            {
                var content = BuildPageContent(title, pageLines);
                var contentNo = AddObject(objects, $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
                var pageNo = AddObject(objects,
                    $"<< /Type /Page /Parent {pagesNo} 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontNo} 0 R >> >> /Contents {contentNo} 0 R >>");
                pageObjectNumbers.Add(pageNo);
            }

            objects[pagesNo - 1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectNumbers.Select(n => $"{n} 0 R"))}] /Count {pageObjectNumbers.Count} >>";

            var sb = new StringBuilder();
            sb.AppendLine("%PDF-1.4");
            var offsets = new List<int> { 0 };
            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
                sb.Append(i + 1).AppendLine(" 0 obj");
                sb.AppendLine(objects[i]);
                sb.AppendLine("endobj");
            }

            var xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString());
            sb.AppendLine("xref");
            sb.AppendLine($"0 {objects.Count + 1}");
            sb.AppendLine("0000000000 65535 f ");
            for (var i = 1; i < offsets.Count; i++)
                sb.AppendLine(offsets[i].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n ");
            sb.AppendLine("trailer");
            sb.AppendLine($"<< /Size {objects.Count + 1} /Root {catalogNo} 0 R >>");
            sb.AppendLine("startxref");
            sb.AppendLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("%%EOF");
            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        private static int AddObject(List<string> objects, string content)
        {
            objects.Add(content);
            return objects.Count;
        }

        private static string BuildPageContent(string title, IReadOnlyList<string> lines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BT");
            sb.AppendLine("/F1 16 Tf");
            sb.AppendLine("40 800 Td");
            sb.Append('(').Append(EscapePdfText(ToPdfSafeText(title))).AppendLine(") Tj");
            sb.AppendLine("/F1 9 Tf");
            sb.AppendLine("0 -22 Td");
            foreach (var line in lines)
            {
                sb.Append('(').Append(EscapePdfText(ToPdfSafeText(line))).AppendLine(") Tj");
                sb.AppendLine("0 -15 Td");
            }
            sb.AppendLine("ET");
            return sb.ToString();
        }

        private static string EscapePdfText(string text)
            => text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static readonly CultureInfo ExportCulture = CultureInfo.GetCultureInfo("tr-TR");

    private static (DateTime Start, DateTime EndExclusive) NormalizeRange(DateTime from, DateTime to)
    {
        var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var end = to.TimeOfDay == TimeSpan.Zero ? to.Date.AddDays(1) : to;
        if (end <= start)
            end = start.AddDays(1);

        return (start, DateTime.SpecifyKind(end, DateTimeKind.Utc));
    }

    private async Task<decimal> CalculateCapacityHoursAsync(
        int customerId,
        int? branchId,
        DateTime start,
        DateTime end,
        List<int?> staffBranchIds)
    {
        if (staffBranchIds.Count == 0)
            return 0;

        var branches = await _branches.GetAllQueryable()
            .Where(b => b.CustomerId == customerId && b.IsActive)
            .ToListAsync();

        var profileHours = await _profiles.GetAllQueryable()
            .Where(p => p.CustomerId == customerId)
            .Select(p => p.WorkingHoursJson)
            .FirstOrDefaultAsync();

        var defaultHours = branches.FirstOrDefault(b => b.IsHeadquarter)?.WorkingHoursJson ?? profileHours;
        var branchHours = branches.ToDictionary(b => b.Id, b => b.WorkingHoursJson);
        decimal total = 0;

        foreach (var staffBranchId in staffBranchIds)
        {
            var hoursJson = defaultHours;
            if (branchId.HasValue)
            {
                hoursJson = branchHours.GetValueOrDefault(branchId.Value) ?? defaultHours;
            }
            else if (staffBranchId.HasValue)
            {
                hoursJson = branchHours.GetValueOrDefault(staffBranchId.Value) ?? defaultHours;
            }

            total += WorkingHoursBetween(hoursJson, start, end);
        }

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal WorkingHoursBetween(string? workingHoursJson, DateTime start, DateTime end)
    {
        decimal total = 0;
        var current = start.Date;
        var endDate = end.Date;

        while (current < endDate)
        {
            total += WorkingHoursForDay(workingHoursJson, current.DayOfWeek);
            current = current.AddDays(1);
        }

        return total;
    }

    private static decimal WorkingHoursForDay(string? workingHoursJson, DayOfWeek dayOfWeek)
    {
        var value = GetWorkingHourValue(workingHoursJson, dayOfWeek);
        if (string.Equals(value, "closed", StringComparison.OrdinalIgnoreCase))
            return 0;

        if (string.IsNullOrWhiteSpace(value))
            value = "09:00-19:00";

        var parts = value.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !TimeSpan.TryParse(parts[0], out var open) || !TimeSpan.TryParse(parts[1], out var close))
            return 10;

        var minutes = (decimal)(close - open).TotalMinutes;
        return minutes > 0 ? Math.Round(minutes / 60, 2, MidpointRounding.AwayFromZero) : 0;
    }

    private static string? GetWorkingHourValue(string? workingHoursJson, DayOfWeek dayOfWeek)
    {
        if (string.IsNullOrWhiteSpace(workingHoursJson))
            return null;

        try
        {
            var hours = JsonSerializer.Deserialize<Dictionary<string, string>>(workingHoursJson);
            var dayKey = dayOfWeek switch
            {
                DayOfWeek.Monday => "mon",
                DayOfWeek.Tuesday => "tue",
                DayOfWeek.Wednesday => "wed",
                DayOfWeek.Thursday => "thu",
                DayOfWeek.Friday => "fri",
                DayOfWeek.Saturday => "sat",
                _ => "sun"
            };

            return hours != null && hours.TryGetValue(dayKey, out var value) ? value : null;
        }
        catch
        {
            return null;
        }
    }

    private static decimal SafeMinutes(DateTime start, DateTime end)
        => end > start ? (decimal)(end - start).TotalMinutes : 0;

    private static decimal RoundHours(decimal minutes)
        => Math.Round(minutes / 60, 2, MidpointRounding.AwayFromZero);

    private static List<SlnBranchDimensionRowDto> BuildBranchDimensionRows(
        IEnumerable<BranchDimensionSource> rows,
        Dictionary<int, string> branchNames)
        => rows
            .GroupBy(r => new { r.BranchId, r.DimensionId, r.DimensionName })
            .Select(g => new SlnBranchDimensionRowDto
            {
                BranchId = g.Key.BranchId,
                BranchName = GetBranchName(g.Key.BranchId, branchNames),
                DimensionId = g.Key.DimensionId,
                DimensionName = g.Key.DimensionName,
                Count = g.Count(),
                Revenue = g.Sum(r => r.Revenue)
            })
            .GroupBy(r => r.BranchId)
            .SelectMany(g => g.OrderByDescending(r => r.Revenue).ThenBy(r => r.DimensionName).Take(10))
            .OrderBy(r => r.BranchName)
            .ThenByDescending(r => r.Revenue)
            .ToList();

    private static string GetBranchName(int? branchId, Dictionary<int, string> branchNames)
        => branchId.HasValue
            ? branchNames.GetValueOrDefault(branchId.Value) ?? $"Sube #{branchId.Value}"
            : "Merkez / Atanmamis";

    private static decimal PercentOrZero(decimal numerator, decimal denominator)
        => denominator == 0 ? 0 : Math.Round(numerator / denominator * 100, 2, MidpointRounding.AwayFromZero);

    private static decimal ResolveStockQuantity(Dictionary<int, decimal> stockMap, int productId, int? branchId, decimal productTotalStock)
        => stockMap.GetValueOrDefault(productId, ResolveStockFallback(branchId, productTotalStock));

    private static decimal ResolveStockFallback(int? branchId, decimal productTotalStock)
        => branchId.HasValue ? 0m : productTotalStock;

    private static decimal VatFromVatIncluded(decimal grossAmount, decimal taxRate)
        => grossAmount <= 0 || taxRate <= 0
            ? 0
            : Math.Round(grossAmount * taxRate / (100 + taxRate), 2, MidpointRounding.AwayFromZero);

    private sealed record BranchDimensionSource(int? BranchId, int DimensionId, string DimensionName, decimal Revenue);
}
