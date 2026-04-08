using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

/// <summary>
/// Online odeme servisi. Iyzico/Stripe entegrasyonu.
/// Simdlik kayit bazli — gercek entegrasyon sonra eklenir.
/// </summary>
public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext db, IConfiguration config, ILogger<PaymentService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    /// <summary>Salon adisyon odemesi baslatir</summary>
    public async Task<PaymentResult> ProcessInvoicePaymentAsync(int customerId, int invoiceId, int platformUserId, PaymentCardInfo card)
    {
        var invoice = await _db.SlnInvoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.CustomerId == customerId);
        if (invoice == null) return PaymentResult.Fail("Adisyon bulunamadı.");

        var amount = invoice.TotalAmount;
        if (amount <= 0) return PaymentResult.Fail("Ödenecek tutar 0.");

        var tx = new PaymentTransaction
        {
            PaymentTypeId = 1, // SalonAdisyon
            CustomerId = customerId,
            PlatformUserId = platformUserId,
            InvoiceId = invoiceId,
            Amount = amount,
            PaymentMethodId = 1, // KrediKarti
            Provider = "Iyzico",
            InstallmentCount = card.Installment,
            CardLastFour = card.CardNumber?.Length >= 4 ? card.CardNumber[^4..] : null
        };

        // TODO: Iyzico API entegrasyonu
        // Simdilik basarili kabul et
        tx.StatusId = 2; // Basarili
        tx.ProviderTransactionId = $"sim_{Guid.NewGuid():N}";
        tx.CompletedAt = DateTime.UtcNow;

        _db.PaymentTransactions.Add(tx);

        // Adisyonu odenmis olarak isaretle
        invoice.StatusId = 3; // Paid

        await _db.SaveChangesAsync();

        _logger.LogInformation("Adisyon odemesi basarili: InvoiceId={InvoiceId}, Amount={Amount}, TxId={TxId}",
            invoiceId, amount, tx.ProviderTransactionId);

        return PaymentResult.Ok(tx.Uid, tx.ProviderTransactionId);
    }

    /// <summary>Platform abonelik odemesi (firma faturasi)</summary>
    public async Task<PaymentResult> ProcessBillingPaymentAsync(int billingPeriodId, PaymentCardInfo card)
    {
        var period = await _db.CustomerBillingPeriods
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == billingPeriodId);

        if (period == null) return PaymentResult.Fail("Faturalama dönemi bulunamadı.");
        if (period.IsPaid) return PaymentResult.Fail("Bu dönem zaten ödenmiş.");

        var amount = period.Amount + period.ServiceAmount;

        var tx = new PaymentTransaction
        {
            PaymentTypeId = 2, // PlatformAbonelik
            CustomerId = period.CustomerId,
            BillingPeriodId = billingPeriodId,
            Amount = amount,
            PaymentMethodId = 1, // KrediKarti
            Provider = "Iyzico",
            InstallmentCount = card.Installment,
            CardLastFour = card.CardNumber?.Length >= 4 ? card.CardNumber[^4..] : null
        };

        // TODO: Iyzico API entegrasyonu
        tx.StatusId = 2;
        tx.ProviderTransactionId = $"sim_{Guid.NewGuid():N}";
        tx.CompletedAt = DateTime.UtcNow;

        _db.PaymentTransactions.Add(tx);

        // Donemi odenmis olarak isaretle
        period.StatusId = 3; // Paid
        period.IsPaid = true;
        period.PaidAt = DateTime.UtcNow;
        period.PaymentMethodId = 3; // KrediKarti

        await _db.SaveChangesAsync();

        _logger.LogInformation("Abonelik odemesi basarili: PeriodId={PeriodId}, Amount={Amount}", billingPeriodId, amount);

        return PaymentResult.Ok(tx.Uid, tx.ProviderTransactionId);
    }

    /// <summary>Odeme gecmisi</summary>
    public async Task<List<PaymentTransaction>> GetTransactionsAsync(int? customerId = null, int? platformUserId = null, int page = 1, int pageSize = 20)
    {
        var query = _db.PaymentTransactions.AsQueryable();
        if (customerId.HasValue) query = query.Where(t => t.CustomerId == customerId);
        if (platformUserId.HasValue) query = query.Where(t => t.PlatformUserId == platformUserId);
        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}

public class PaymentCardInfo
{
    public string? CardHolderName { get; set; }
    public string? CardNumber { get; set; }
    public string? ExpireMonth { get; set; }
    public string? ExpireYear { get; set; }
    public string? Cvc { get; set; }
    public int Installment { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Guid? TransactionUid { get; set; }
    public string? ProviderTransactionId { get; set; }

    public static PaymentResult Ok(Guid uid, string? providerTxId) => new() { Success = true, TransactionUid = uid, ProviderTransactionId = providerTxId };
    public static PaymentResult Fail(string error) => new() { Success = false, Error = error };
}
