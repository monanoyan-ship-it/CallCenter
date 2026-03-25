using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnReportFactory : ISlnReportFactory
{
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly ISlnInvoiceItemEntityService _invoiceItems;
    private readonly ISlnProductEntityService _products;
    private readonly ISlnExpenseEntityService _expenses;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnPersonnelCommissionEntityService _commissions;
    private readonly ILogger<SlnReportFactory> _logger;

    private static readonly Dictionary<int, string> PaymentMethodNames = new()
    {
        { 1, "Nakit" },
        { 2, "Kredi Karti" },
        { 3, "Banka Transferi" }
    };

    public SlnReportFactory(
        ISlnInvoiceEntityService invoices,
        ISlnInvoiceItemEntityService invoiceItems,
        ISlnProductEntityService products,
        ISlnExpenseEntityService expenses,
        ISlnClientEntityService clients,
        ISlnPersonnelCommissionEntityService commissions,
        ILogger<SlnReportFactory> logger)
    {
        _invoices = invoices;
        _invoiceItems = invoiceItems;
        _products = products;
        _expenses = expenses;
        _clients = clients;
        _commissions = commissions;
        _logger = logger;
    }

    public async Task<SlnSalesReportDto> GetSalesReportAsync(int customerId, DateTime from, DateTime to)
    {
        var invoices = await _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId && i.StatusId != 3 && i.InvoiceDate >= from && i.InvoiceDate <= to)
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

    public async Task<SlnStaffReportDto> GetStaffReportAsync(int customerId, DateTime from, DateTime to)
    {
        var items = await _invoiceItems.GetAllQueryable()
            .Where(it => it.Invoice != null && it.Invoice.CustomerId == customerId
                && it.Invoice.StatusId != 3
                && it.Invoice.InvoiceDate >= from && it.Invoice.InvoiceDate <= to
                && it.PersonnelId.HasValue)
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

    public async Task<SlnStockReportDto> GetStockReportAsync(int customerId)
    {
        var products = await _products.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && p.IsActive)
            .Include(p => p.Category)
            .ToListAsync();

        var items = products.Select(p => new SlnStockItemDto
        {
            ProductId = p.Id,
            ProductName = p.Name,
            CategoryName = p.Category?.Name ?? "",
            StockQuantity = p.StockQuantity,
            MinStockLevel = p.MinStockLevel,
            PurchasePrice = p.PurchasePrice,
            StockValue = p.StockQuantity * p.PurchasePrice,
            IsLowStock = p.StockQuantity <= p.MinStockLevel
        }).ToList();

        return new SlnStockReportDto
        {
            TotalProducts = products.Count,
            LowStockCount = items.Count(i => i.IsLowStock),
            TotalStockValue = items.Sum(i => i.StockValue),
            Items = items.OrderBy(i => i.IsLowStock ? 0 : 1).ThenBy(i => i.ProductName).ToList()
        };
    }

    public async Task<SlnFinanceReportDto> GetFinanceReportAsync(int customerId, DateTime from, DateTime to)
    {
        // Gelir
        var totalIncome = await _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId && i.StatusId != 3 && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .SumAsync(i => i.NetAmount);

        // Gider
        var expensesList = await _expenses.GetAllQueryable()
            .Where(e => e.CustomerId == customerId && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .Include(e => e.Category)
            .ToListAsync();

        var totalExpense = expensesList.Sum(e => e.Amount);

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

        return new SlnFinanceReportDto
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            NetProfit = totalIncome - totalExpense,
            ExpenseBreakdown = expenseBreakdown
        };
    }

    public async Task<SlnClientReportDto> GetClientReportAsync(int customerId, DateTime from, DateTime to)
    {
        var totalClients = await _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .CountAsync();

        var newClientsInPeriod = await _clients.GetAllQueryable()
            .Where(c => c.CustomerId == customerId && c.CreatedAt >= from && c.CreatedAt <= to)
            .CountAsync();

        // En degerli musteriler
        var clientStats = await _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId && i.StatusId != 3 && i.SlnClientId.HasValue
                && i.InvoiceDate >= from && i.InvoiceDate <= to)
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
}
