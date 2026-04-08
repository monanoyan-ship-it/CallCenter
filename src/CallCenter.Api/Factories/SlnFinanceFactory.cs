using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnFinanceFactory : ISlnFinanceFactory
{
    private readonly ISlnInvoiceEntityService _invoices;
    private readonly ISlnInvoiceItemEntityService _invoiceItems;
    private readonly ISlnCashRegisterEntityService _cashRegisters;
    private readonly ISlnCashTransactionEntityService _cashTransactions;
    private readonly ISlnExpenseCategoryEntityService _expenseCategories;
    private readonly ISlnExpenseEntityService _expenses;
    private readonly ISlnProductEntityService _products;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnFinanceFactory> _logger;
    private readonly AppDbContext _db;

    public SlnFinanceFactory(
        ISlnInvoiceEntityService invoices,
        ISlnInvoiceItemEntityService invoiceItems,
        ISlnCashRegisterEntityService cashRegisters,
        ISlnCashTransactionEntityService cashTransactions,
        ISlnExpenseCategoryEntityService expenseCategories,
        ISlnExpenseEntityService expenses,
        ISlnProductEntityService products,
        IUnitOfWork uow,
        ILogger<SlnFinanceFactory> logger,
        AppDbContext db)
    {
        _invoices = invoices;
        _invoiceItems = invoiceItems;
        _cashRegisters = cashRegisters;
        _cashTransactions = cashTransactions;
        _expenseCategories = expenseCategories;
        _expenses = expenses;
        _products = products;
        _uow = uow;
        _logger = logger;
        _db = db;
    }

    // ═══ Adisyon (Invoice) ═══

    public async Task<List<SlnInvoiceDto>> GetInvoicesAsync(int customerId, DateTime? from, DateTime? to, int? statusId = null)
    {
        var query = _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId);

        if (from.HasValue)
            query = query.Where(i => i.InvoiceDate >= from.Value);

        if (to.HasValue)
            query = query.Where(i => i.InvoiceDate <= to.Value);

        if (statusId.HasValue)
            query = query.Where(i => i.StatusId == statusId.Value);

        var invoices = await query
            .Include(i => i.SlnClient)
            .Include(i => i.Personnel).ThenInclude(p => p!.User)
            .Include(i => i.Items).ThenInclude(it => it.Service)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .Include(i => i.Items).ThenInclude(it => it.Personnel).ThenInclude(p => p!.User)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        return invoices.Select(MapInvoiceToDto).ToList();
    }

    public async Task<SlnInvoiceDto?> GetInvoiceAsync(int invoiceId, int customerId)
    {
        var invoice = await _invoices.GetAllQueryable()
            .Include(i => i.SlnClient)
            .Include(i => i.Personnel).ThenInclude(p => p!.User)
            .Include(i => i.Items).ThenInclude(it => it.Service)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .Include(i => i.Items).ThenInclude(it => it.Personnel).ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CustomerId == customerId);

        return invoice != null ? MapInvoiceToDto(invoice) : null;
    }

    public async Task<(SlnInvoiceDto? Invoice, string? Error)> CreateInvoiceAsync(SlnInvoiceCreateDto dto, int userId, int customerId)
    {
        if (dto.Items.Count == 0)
            return (null, "Adisyonda en az bir kalem olmali");

        // Fatura numarasi olustur
        var today = DateTime.UtcNow;
        var todayCount = await _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId && i.InvoiceDate.Date == today.Date)
            .CountAsync();

        var invoiceNo = $"SLN-{today:yyyyMMdd}-{(todayCount + 1):D4}";

        var invoice = new SlnInvoice
        {
            CustomerId = customerId,
            SlnClientId = dto.SlnClientId,
            InvoiceNo = invoiceNo,
            InvoiceDate = today,
            PaymentMethodId = dto.PaymentMethodId,
            PosDeviceId = dto.PosDeviceId,
            PersonnelId = userId,
            DiscountAmount = dto.DiscountAmount,
            TipAmount = dto.TipAmount,
            Notes = dto.Notes
        };

        decimal totalAmount = 0;
        var items = new List<SlnInvoiceItem>();

        foreach (var itemDto in dto.Items)
        {
            var lineTotal = (itemDto.Quantity * itemDto.UnitPrice) - itemDto.DiscountAmount;
            totalAmount += lineTotal;

            items.Add(new SlnInvoiceItem
            {
                ServiceId = itemDto.ServiceId,
                ProductId = itemDto.ProductId,
                PersonnelId = itemDto.PersonnelId,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                DiscountAmount = itemDto.DiscountAmount,
                LineTotal = lineTotal
            });

            // Urun satisinda stok dusur
            if (itemDto.ProductId.HasValue)
            {
                var product = await _products.GetByIdAsync(itemDto.ProductId.Value);
                if (product != null)
                    product.StockQuantity -= itemDto.Quantity;
            }
        }

        invoice.TotalAmount = totalAmount;
        invoice.NetAmount = totalAmount - dto.DiscountAmount;
        invoice.StatusId = 2; // Paid

        _invoices.Add(invoice);
        await _uow.SaveChangesAsync();

        // Item'lari invoice'a bagla
        foreach (var item in items)
        {
            item.InvoiceId = invoice.Id;
            _invoiceItems.Add(item);
        }
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni adisyon olusturuldu: {InvoiceNo} - {NetAmount:C}", invoiceNo, invoice.NetAmount);

        // Include'li tekrar cek
        var created = await _invoices.GetAllQueryable()
            .Include(i => i.SlnClient)
            .Include(i => i.Personnel).ThenInclude(p => p!.User)
            .Include(i => i.Items).ThenInclude(it => it.Service)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .Include(i => i.Items).ThenInclude(it => it.Personnel).ThenInclude(p => p!.User)
            .FirstAsync(i => i.Id == invoice.Id);

        return (MapInvoiceToDto(created), null);
    }

    public async Task<(bool Success, string? Error)> CancelInvoiceAsync(int invoiceId, int customerId)
    {
        var invoice = await _invoices.GetAllQueryable()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CustomerId == customerId);

        if (invoice == null) return (false, "Adisyon bulunamadi");
        if (invoice.StatusId == 3) return (false, "Adisyon zaten iptal edilmis");

        invoice.StatusId = 3; // Cancelled

        // Urun satislarinda stogu geri ekle
        foreach (var item in invoice.Items.Where(it => it.ProductId.HasValue))
        {
            var product = await _products.GetByIdAsync(item.ProductId!.Value);
            if (product != null)
                product.StockQuantity += item.Quantity;
        }

        await _uow.SaveChangesAsync();

        _logger.LogInformation("Adisyon iptal edildi: {InvoiceId}", invoiceId);
        return (true, null);
    }

    // ═══ Kasa ═══

    public async Task<List<object>> GetCashRegistersAsync(int customerId)
    {
        var registers = await _cashRegisters.GetAllQueryable()
            .Where(r => r.CustomerId == customerId)
            .ToListAsync();

        return registers.Select(r => (object)new { r.Id, r.Name, r.IsActive }).ToList();
    }

    public async Task<object> CreateCashRegisterAsync(string name, int customerId)
    {
        var register = new SlnCashRegister
        {
            CustomerId = customerId,
            Name = name
        };

        _cashRegisters.Add(register);
        await _uow.SaveChangesAsync();

        return new { register.Id, register.Name, register.IsActive };
    }

    public async Task<List<SlnCashTransactionDto>> GetCashTransactionsAsync(int registerId, int customerId, DateTime? from, DateTime? to)
    {
        // Kasanin bu firmaya ait oldugunu dogrula
        var register = await _cashRegisters.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == registerId && r.CustomerId == customerId);

        if (register == null) return [];

        var query = _cashTransactions.GetAllQueryable()
            .Where(t => t.RegisterId == registerId);

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);

        var transactions = await query
            .Include(t => t.Register)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return transactions.Select(t => new SlnCashTransactionDto
        {
            Id = t.Id,
            RegisterName = t.Register?.Name ?? "",
            TransactionTypeId = t.TransactionTypeId,
            Amount = t.Amount,
            Description = t.Description,
            PaymentMethodId = t.PaymentMethodId,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    public async Task<(SlnCashTransactionDto? Transaction, string? Error)> AddCashTransactionAsync(
        int registerId, int transactionTypeId, decimal amount, string description,
        int paymentMethodId, int userId, int customerId)
    {
        var register = await _cashRegisters.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == registerId && r.CustomerId == customerId);

        if (register == null) return (null, "Kasa bulunamadi");

        var transaction = new SlnCashTransaction
        {
            RegisterId = registerId,
            TransactionTypeId = transactionTypeId,
            Amount = amount,
            Description = description,
            PaymentMethodId = paymentMethodId,
            CreatedByPersonnelId = userId
        };

        _cashTransactions.Add(transaction);
        await _uow.SaveChangesAsync();

        return (new SlnCashTransactionDto
        {
            Id = transaction.Id,
            RegisterName = register.Name,
            TransactionTypeId = transaction.TransactionTypeId,
            Amount = transaction.Amount,
            Description = transaction.Description,
            PaymentMethodId = transaction.PaymentMethodId,
            CreatedAt = transaction.CreatedAt
        }, null);
    }

    // ═══ Masraf ═══

    public async Task<List<object>> GetExpenseCategoriesAsync(int customerId)
    {
        var categories = await _expenseCategories.GetAllQueryable()
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories.Select(c => (object)new { c.Id, c.Name, c.IsSystem }).ToList();
    }

    public async Task<object> CreateExpenseCategoryAsync(string name, int customerId)
    {
        var category = new SlnExpenseCategory
        {
            CustomerId = customerId,
            Name = name
        };

        _expenseCategories.Add(category);
        await _uow.SaveChangesAsync();

        return new { category.Id, category.Name, category.IsSystem };
    }

    public async Task<List<SlnExpenseDto>> GetExpensesAsync(int customerId, DateTime? from, DateTime? to, int? categoryId = null)
    {
        var query = _expenses.GetAllQueryable()
            .Where(e => e.CustomerId == customerId);

        if (from.HasValue)
            query = query.Where(e => e.ExpenseDate >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.ExpenseDate <= to.Value);

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        var expenses = await query
            .Include(e => e.Category)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

        return expenses.Select(e => new SlnExpenseDto
        {
            Id = e.Id,
            CategoryName = e.Category?.Name ?? "",
            Amount = e.Amount,
            ExpenseDate = e.ExpenseDate,
            Description = e.Description,
            PaymentMethodId = e.PaymentMethodId
        }).ToList();
    }

    public async Task<SlnExpenseDto> CreateExpenseAsync(SlnExpenseCreateDto dto, int userId, int customerId)
    {
        var expense = new SlnExpense
        {
            CustomerId = customerId,
            CategoryId = dto.CategoryId,
            Amount = dto.Amount,
            ExpenseDate = dto.ExpenseDate,
            Description = dto.Description,
            PaymentMethodId = dto.PaymentMethodId,
            CreatedByPersonnelId = userId
        };

        _expenses.Add(expense);
        await _uow.SaveChangesAsync();

        var category = await _expenseCategories.GetByIdAsync(dto.CategoryId);

        return new SlnExpenseDto
        {
            Id = expense.Id,
            CategoryName = category?.Name ?? "",
            Amount = expense.Amount,
            ExpenseDate = expense.ExpenseDate,
            Description = expense.Description,
            PaymentMethodId = expense.PaymentMethodId
        };
    }

    public async Task<(bool Success, string? Error)> DeleteExpenseAsync(int expenseId, int customerId)
    {
        var expense = await _expenses.GetAllQueryable()
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.CustomerId == customerId);

        if (expense == null) return (false, "Masraf bulunamadi");

        _expenses.Remove(expense);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static SlnInvoiceDto MapInvoiceToDto(SlnInvoice i) => new()
    {
        Id = i.Id,
        InvoiceNo = i.InvoiceNo,
        InvoiceDate = i.InvoiceDate,
        ClientName = i.SlnClient?.FullName,
        TotalAmount = i.TotalAmount,
        DiscountAmount = i.DiscountAmount,
        NetAmount = i.NetAmount,
        PaymentMethodId = i.PaymentMethodId,
        PersonnelName = i.Personnel?.User?.FullName,
        StatusId = i.StatusId,
        TipAmount = i.TipAmount,
        Items = i.Items.Select(it => new SlnInvoiceItemDto
        {
            Id = it.Id,
            ItemName = it.Service?.Name ?? it.Product?.Name ?? "",
            PersonnelName = it.Personnel?.User?.FullName,
            Quantity = it.Quantity,
            UnitPrice = it.UnitPrice,
            DiscountAmount = it.DiscountAmount,
            LineTotal = it.LineTotal
        }).ToList()
    };

    // ═══ Gun Sonu Kasa Kapama ═══

    public async Task<List<SlnCashClosingDto>> GetCashClosingsAsync(int customerId, int? registerId)
    {
        var query = _db.SlnCashClosings
            .Include(c => c.Register)
            .Include(c => c.ClosedByPersonnel).ThenInclude(p => p!.User)
            .Where(c => c.Register != null && c.Register.CustomerId == customerId);

        if (registerId.HasValue)
            query = query.Where(c => c.RegisterId == registerId.Value);

        return await query.OrderByDescending(c => c.ClosingDate).Select(c => new SlnCashClosingDto
        {
            Id = c.Id,
            RegisterId = c.RegisterId,
            RegisterName = c.Register != null ? c.Register.Name : "",
            ClosingDate = c.ClosingDate,
            SystemTotal = c.SystemTotal,
            CountedTotal = c.CountedTotal,
            Difference = c.Difference,
            Notes = c.Notes,
            ClosedByName = c.ClosedByPersonnel != null && c.ClosedByPersonnel.User != null ? c.ClosedByPersonnel.User.FullName : null,
            CreatedAt = c.CreatedAt
        }).ToListAsync();
    }

    public async Task<(SlnCashClosingDto? Closing, string? Error)> CreateCashClosingAsync(SlnCashClosingCreateDto dto, int userId, int customerId)
    {
        var register = await _cashRegisters.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == dto.RegisterId && r.CustomerId == customerId);

        if (register == null) return (null, "Kasa bulunamadi");

        // Gunun sistem toplami
        var today = DateTime.UtcNow.Date;
        var transactions = await _cashTransactions.GetAllQueryable()
            .Where(t => t.RegisterId == dto.RegisterId && t.CreatedAt >= today)
            .ToListAsync();

        decimal systemTotal = 0;
        foreach (var t in transactions)
        {
            if (t.TransactionTypeId == 1) // Gelir
                systemTotal += t.Amount;
            else if (t.TransactionTypeId == 2) // Gider
                systemTotal -= t.Amount;
        }

        var closing = new SlnCashClosing
        {
            RegisterId = dto.RegisterId,
            ClosingDate = DateTime.UtcNow,
            SystemTotal = systemTotal,
            CountedTotal = dto.CountedTotal,
            Difference = dto.CountedTotal - systemTotal,
            Notes = dto.Notes,
            ClosedByPersonnelId = userId
        };

        _db.SlnCashClosings.Add(closing);
        await _uow.SaveChangesAsync();

        return (new SlnCashClosingDto
        {
            Id = closing.Id,
            RegisterId = closing.RegisterId,
            RegisterName = register.Name,
            ClosingDate = closing.ClosingDate,
            SystemTotal = closing.SystemTotal,
            CountedTotal = closing.CountedTotal,
            Difference = closing.Difference,
            Notes = closing.Notes,
            CreatedAt = closing.CreatedAt
        }, null);
    }

    public async Task<object> GetDailySummaryAsync(int registerId, int customerId)
    {
        var register = await _cashRegisters.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == registerId && r.CustomerId == customerId);

        if (register == null) return new { error = "Kasa bulunamadi" };

        var today = DateTime.UtcNow.Date;
        var transactions = await _cashTransactions.GetAllQueryable()
            .Where(t => t.RegisterId == registerId && t.CreatedAt >= today)
            .ToListAsync();

        var income = transactions.Where(t => t.TransactionTypeId == 1).Sum(t => t.Amount);
        var expense = transactions.Where(t => t.TransactionTypeId == 2).Sum(t => t.Amount);

        return new
        {
            registerName = register.Name,
            income,
            expense,
            net = income - expense,
            transactionCount = transactions.Count
        };
    }

    // ═══ Z RAPORU ═══

    public async Task<object> GetZReportAsync(int registerId, int customerId, DateTime? date = null)
    {
        var register = await _cashRegisters.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == registerId && r.CustomerId == customerId);
        if (register == null) return new { error = "Kasa bulunamadi" };

        var targetDate = (date ?? DateTime.UtcNow).Date;
        var nextDate = targetDate.AddDays(1);

        // Acilis bakiyesi
        var opening = await _db.SlnCashOpenings
            .Where(o => o.RegisterId == registerId && o.OpeningDate >= targetDate && o.OpeningDate < nextDate)
            .FirstOrDefaultAsync();

        // Gun icindeki islemler
        var transactions = await _cashTransactions.GetAllQueryable()
            .Where(t => t.RegisterId == registerId && t.CreatedAt >= targetDate && t.CreatedAt < nextDate)
            .ToListAsync();

        // Odeme yontemi bazli
        var cashIncome = transactions.Where(t => t.TransactionTypeId == 1 && t.PaymentMethodId == 1).Sum(t => t.Amount);
        var cashExpense = transactions.Where(t => t.TransactionTypeId == 2 && t.PaymentMethodId == 1).Sum(t => t.Amount);
        var ccIncome = transactions.Where(t => t.TransactionTypeId == 1 && t.PaymentMethodId == 2).Sum(t => t.Amount);
        var ccExpense = transactions.Where(t => t.TransactionTypeId == 2 && t.PaymentMethodId == 2).Sum(t => t.Amount);
        var transferIncome = transactions.Where(t => t.TransactionTypeId == 1 && t.PaymentMethodId == 3).Sum(t => t.Amount);
        var totalIncome = transactions.Where(t => t.TransactionTypeId == 1).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.TransactionTypeId == 2).Sum(t => t.Amount);

        // Kapanis
        var closing = await _db.SlnCashClosings
            .Where(c => c.RegisterId == registerId && c.ClosingDate >= targetDate && c.ClosingDate < nextDate)
            .FirstOrDefaultAsync();

        // Gun adisyonlari
        var invoices = await _db.SlnInvoices
            .Where(i => i.CustomerId == customerId && i.InvoiceDate >= targetDate && i.InvoiceDate < nextDate && i.StatusId != 3)
            .ToListAsync();

        return new
        {
            date = targetDate,
            registerName = register.Name,
            openingBalance = opening?.OpeningBalance ?? 0,
            cashIncome, cashExpense, cashNet = cashIncome - cashExpense,
            ccIncome, ccExpense, ccNet = ccIncome - ccExpense,
            transferIncome,
            totalIncome, totalExpense, netTotal = totalIncome - totalExpense,
            closingSystemTotal = closing?.SystemTotal,
            closingCountedTotal = closing?.CountedTotal,
            closingDifference = closing?.Difference,
            isClosed = closing != null,
            invoiceCount = invoices.Count,
            invoiceTotal = invoices.Sum(i => i.GrandTotal > 0 ? i.GrandTotal : i.NetAmount),
            taxTotal = invoices.Sum(i => i.TaxAmount),
            transactionCount = transactions.Count
        };
    }

    // ═══ KASA ACILIS ═══

    public async Task<(object? Result, string? Error)> CreateCashOpeningAsync(int registerId, int customerId, decimal? manualBalance, int personnelId)
    {
        var register = await _cashRegisters.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == registerId && r.CustomerId == customerId);
        if (register == null) return (null, "Kasa bulunamadi");

        var today = DateTime.UtcNow.Date;
        var exists = await _db.SlnCashOpenings.AnyAsync(o => o.RegisterId == registerId && o.OpeningDate >= today && o.OpeningDate < today.AddDays(1));
        if (exists) return (null, "Bugun zaten acilis yapilmis.");

        // Onceki kapanistan bakiye tasi
        decimal openingBalance = 0;
        bool isCarried = false;
        var lastClosing = await _db.SlnCashClosings
            .Where(c => c.RegisterId == registerId)
            .OrderByDescending(c => c.ClosingDate)
            .FirstOrDefaultAsync();

        if (manualBalance.HasValue)
        {
            openingBalance = manualBalance.Value;
        }
        else if (lastClosing != null)
        {
            openingBalance = lastClosing.CountedTotal;
            isCarried = true;
        }

        var opening = new SlnCashOpening
        {
            RegisterId = registerId,
            OpeningDate = DateTime.UtcNow,
            OpeningBalance = openingBalance,
            IsCarriedForward = isCarried,
            OpenedByPersonnelId = personnelId
        };

        _db.SlnCashOpenings.Add(opening);
        await _uow.SaveChangesAsync();

        return (new { opening.Id, opening.OpeningBalance, opening.IsCarriedForward }, null);
    }

    // ═══ MÜŞTERİ CARİ HESAP ═══

    public async Task<object> GetClientLedgerAsync(int customerId, int slnClientId)
    {
        var entries = await _db.SlnClientLedgers
            .Where(l => l.CustomerId == customerId && l.SlnClientId == slnClientId)
            .OrderByDescending(l => l.TransactionDate)
            .Take(100)
            .ToListAsync();

        var balance = entries.FirstOrDefault()?.RunningBalance ?? 0;

        return new
        {
            balance,
            entries = entries.Select(e => new
            {
                e.Id,
                e.TransactionTypeId,
                typeName = e.TransactionTypeId == 1 ? "Borç" : e.TransactionTypeId == 2 ? "Alacak" : "İade",
                e.Amount,
                e.RunningBalance,
                e.InvoiceId,
                e.Description,
                e.TransactionDate
            })
        };
    }

    public async Task AddLedgerEntryAsync(int customerId, int slnClientId, int typeId, decimal amount, int? invoiceId, string? description)
    {
        var lastEntry = await _db.SlnClientLedgers
            .Where(l => l.CustomerId == customerId && l.SlnClientId == slnClientId)
            .OrderByDescending(l => l.TransactionDate)
            .FirstOrDefaultAsync();

        var runningBalance = lastEntry?.RunningBalance ?? 0;
        if (typeId == 1) runningBalance += amount;      // Borc
        else if (typeId == 2) runningBalance -= amount;  // Alacak (odeme)
        else if (typeId == 3) runningBalance -= amount;  // Iade

        _db.SlnClientLedgers.Add(new SlnClientLedger
        {
            CustomerId = customerId,
            SlnClientId = slnClientId,
            TransactionTypeId = typeId,
            Amount = amount,
            RunningBalance = runningBalance,
            InvoiceId = invoiceId,
            Description = description
        });
        await _uow.SaveChangesAsync();
    }

    // ═══ İADE ═══

    public async Task<(object? Result, string? Error)> CreateRefundAsync(int customerId, int invoiceId, decimal refundAmount, int refundMethodId, string reason, int personnelId)
    {
        var invoice = await _db.SlnInvoices
            .Include(i => i.Items).ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CustomerId == customerId);
        if (invoice == null) return (null, "Adisyon bulunamadi.");
        if (invoice.StatusId == 3) return (null, "Iptal edilmis adisyon icin iade yapilamaz.");

        var maxRefundable = invoice.GrandTotal > 0 ? invoice.GrandTotal : invoice.NetAmount;
        if (refundAmount > maxRefundable) return (null, "Iade tutari adisyon tutarini asamaz.");

        // Iade kaydi
        var refund = new SlnInvoiceRefund
        {
            InvoiceId = invoiceId,
            RefundAmount = refundAmount,
            RefundMethodId = refundMethodId,
            Reason = reason,
            PersonnelId = personnelId
        };
        _db.SlnInvoiceRefunds.Add(refund);

        // Tam iade ise adisyonu iptal et
        if (refundAmount >= maxRefundable)
        {
            invoice.StatusId = 3; // Cancelled

            // Urun stok geri yukle
            foreach (var item in invoice.Items.Where(i => i.ProductId != null && i.Product != null))
                item.Product!.StockQuantity += item.Quantity;
        }

        // Cari hesaba iade kaydi
        if (invoice.SlnClientId.HasValue)
        {
            await AddLedgerEntryAsync(customerId, invoice.SlnClientId.Value, 3, refundAmount, invoiceId, $"İade: {reason}");
        }

        await _uow.SaveChangesAsync();

        return (new { refund.Id, refund.RefundAmount, refund.Reason }, null);
    }

    // ═══ PERSONEL HASILAT ═══

    public async Task<object> GetStaffRevenueAsync(int customerId, DateTime startDate, DateTime endDate)
    {
        var invoiceItems = await _db.SlnInvoiceItems
            .Include(i => i.Invoice)
            .Include(i => i.Personnel).ThenInclude(p => p!.User)
            .Where(i => i.Invoice!.CustomerId == customerId
                     && i.Invoice.InvoiceDate >= startDate
                     && i.Invoice.InvoiceDate < endDate
                     && i.Invoice.StatusId != 3
                     && i.PersonnelId != null)
            .ToListAsync();

        var personnelIds = invoiceItems.Where(i => i.PersonnelId.HasValue).Select(i => i.PersonnelId!.Value).Distinct().ToList();
        var commissions = await _db.SlnPersonnelCommissions
            .Where(c => personnelIds.Contains(c.PersonnelId))
            .ToListAsync();

        var grouped = invoiceItems
            .GroupBy(i => i.PersonnelId)
            .Select(g =>
            {
                var personnel = g.First().Personnel;
                var totalRevenue = g.Sum(i => i.LineTotal);
                // Genel komisyon (ServiceId ve ProductId null olan)
                var commission = commissions.FirstOrDefault(c => c.PersonnelId == g.Key && c.ServiceId == null && c.ProductId == null);
                var commissionRate = commission != null && commission.IsPercentage ? commission.Rate : 0;
                var commissionAmount = commission != null && commission.IsPercentage
                    ? totalRevenue * commissionRate / 100
                    : (commission?.Rate ?? 0);

                return new
                {
                    personnelId = g.Key,
                    personnelName = personnel?.User?.FullName ?? personnel?.Title ?? "-",
                    serviceCount = g.Count(i => i.ServiceId != null),
                    productCount = g.Count(i => i.ProductId != null),
                    totalRevenue,
                    commissionRate,
                    commissionAmount,
                    netRevenue = totalRevenue - commissionAmount
                };
            })
            .OrderByDescending(x => x.totalRevenue)
            .ToList();

        return new
        {
            startDate, endDate,
            totalRevenue = grouped.Sum(g => g.totalRevenue),
            totalCommission = grouped.Sum(g => g.commissionAmount),
            staff = grouped
        };
    }

    // ═══ FİNANS RAPORLARI ═══

    public async Task<object> GetIncomeExpenseReportAsync(int customerId, DateTime startDate, DateTime endDate)
    {
        // Gelirler (adisyonlar)
        var invoices = await _db.SlnInvoices
            .Where(i => i.CustomerId == customerId && i.InvoiceDate >= startDate && i.InvoiceDate < endDate && i.StatusId != 3)
            .ToListAsync();

        // Giderler (masraflar)
        var expenses = await _db.SlnExpenses
            .Include(e => e.Category)
            .Where(e => e.CustomerId == customerId && e.ExpenseDate >= startDate && e.ExpenseDate < endDate && e.StatusId != 3)
            .ToListAsync();

        var totalIncome = invoices.Sum(i => i.GrandTotal > 0 ? i.GrandTotal : i.NetAmount);
        var totalTax = invoices.Sum(i => i.TaxAmount);
        var totalExpense = expenses.Sum(e => e.Amount);
        var totalExpenseTax = expenses.Sum(e => e.TaxAmount);

        // Kategoriye gore gider dagilimi
        var expenseByCategory = expenses
            .GroupBy(e => e.Category?.Name ?? "Diger")
            .Select(g => new { category = g.Key, amount = g.Sum(e => e.Amount) })
            .OrderByDescending(x => x.amount)
            .ToList();

        return new
        {
            startDate, endDate,
            income = new { total = totalIncome, tax = totalTax, net = totalIncome - totalTax, invoiceCount = invoices.Count },
            expense = new { total = totalExpense, tax = totalExpenseTax, net = totalExpense - totalExpenseTax, count = expenses.Count, byCategory = expenseByCategory },
            profit = totalIncome - totalExpense,
            profitAfterTax = (totalIncome - totalTax) - (totalExpense - totalExpenseTax)
        };
    }

    public async Task<object> GetTaxReportAsync(int customerId, DateTime startDate, DateTime endDate)
    {
        var invoiceItems = await _db.SlnInvoiceItems
            .Include(i => i.Invoice)
            .Where(i => i.Invoice!.CustomerId == customerId && i.Invoice.InvoiceDate >= startDate && i.Invoice.InvoiceDate < endDate && i.Invoice.StatusId != 3)
            .ToListAsync();

        var byRate = invoiceItems
            .GroupBy(i => i.TaxRate)
            .Select(g => new
            {
                taxRate = g.Key,
                lineCount = g.Count(),
                totalBeforeTax = g.Sum(i => i.LineTotal),
                totalTax = g.Sum(i => i.TaxAmount),
                totalWithTax = g.Sum(i => i.LineTotal + i.TaxAmount)
            })
            .OrderBy(x => x.taxRate)
            .ToList();

        return new
        {
            startDate, endDate,
            totalTax = byRate.Sum(x => x.totalTax),
            totalBeforeTax = byRate.Sum(x => x.totalBeforeTax),
            byRate
        };
    }
}
