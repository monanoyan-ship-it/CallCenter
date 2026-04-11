using CallCenter.Api.Services.Payment;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

/// <summary>
/// Online odeme servisi. Aktif PlatformPaymentConfig uzerinden gateway ile odeme yapar.
/// Config yoksa veya gateway tanimli degilse hata dondurur.
/// </summary>
public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly PaymentGatewayFactory _gatewayFactory;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext db, PaymentGatewayFactory gatewayFactory, ILogger<PaymentService> logger)
    {
        _db = db;
        _gatewayFactory = gatewayFactory;
        _logger = logger;
    }

    /// <summary>Salon adisyon odemesi baslatir (PlatformUser odiyor)</summary>
    public async Task<PaymentResult> ProcessInvoicePaymentAsync(int customerId, int invoiceId, int platformUserId, PaymentCardInfo card)
    {
        var invoice = await _db.SlnInvoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.CustomerId == customerId);
        if (invoice == null) return PaymentResult.Fail("Adisyon bulunamadi.");

        var amount = invoice.TotalAmount;
        if (amount <= 0) return PaymentResult.Fail("Odenecek tutar 0.");

        var tx = new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.SalonAdisyon,
            CustomerId = customerId,
            PlatformUserId = platformUserId,
            InvoiceId = invoiceId,
            Amount = amount,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            InstallmentCount = card.Installment,
            CardLastFour = card.CardNumber?.Length >= 4 ? card.CardNumber[^4..] : null
        };

        var gatewayResult = await ExecutePaymentAsync(tx, card);

        if (gatewayResult.Success)
        {
            invoice.StatusId = 3; // Paid
            _logger.LogInformation("Adisyon odemesi basarili: InvoiceId={InvoiceId}, Amount={Amount}, TxId={TxId}",
                invoiceId, amount, tx.ProviderTransactionId);
        }
        else
        {
            _logger.LogWarning("Adisyon odemesi basarisiz: InvoiceId={InvoiceId}, Hata={Error}", invoiceId, gatewayResult.Error);
        }

        await _db.SaveChangesAsync();
        return gatewayResult;
    }

    /// <summary>Platform abonelik odemesi (firma faturasi)</summary>
    public async Task<PaymentResult> ProcessBillingPaymentAsync(int billingPeriodId, PaymentCardInfo card)
    {
        var period = await _db.CustomerBillingPeriods
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == billingPeriodId);

        if (period == null) return PaymentResult.Fail("Faturalama donemi bulunamadi.");
        if (period.IsPaid) return PaymentResult.Fail("Bu donem zaten odenmis.");

        var amount = period.Amount + period.ServiceAmount;

        var tx = new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.PlatformAbonelik,
            CustomerId = period.CustomerId,
            BillingPeriodId = billingPeriodId,
            Amount = amount,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            InstallmentCount = card.Installment,
            CardLastFour = card.CardNumber?.Length >= 4 ? card.CardNumber[^4..] : null
        };

        var gatewayResult = await ExecutePaymentAsync(tx, card);

        if (gatewayResult.Success)
        {
            period.StatusId = BillingPeriodStatuses.Ids.Paid;
            period.IsPaid = true;
            period.PaidAt = DateTime.UtcNow;
            period.PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti;
            _logger.LogInformation("Abonelik odemesi basarili: PeriodId={PeriodId}, Amount={Amount}", billingPeriodId, amount);
        }
        else
        {
            _logger.LogWarning("Abonelik odemesi basarisiz: PeriodId={PeriodId}, Hata={Error}", billingPeriodId, gatewayResult.Error);
        }

        await _db.SaveChangesAsync();
        return gatewayResult;
    }

    /// <summary>Modul satin alma odemesi (Salon admin odiyor)</summary>
    public async Task<PaymentResult> ProcessModulePurchaseAsync(int customerId, int moduleId, PaymentCardInfo card, string? buyerIp = null)
    {
        var pricing = await _db.ModulePricings.FirstOrDefaultAsync(p => p.ModuleId == moduleId);
        if (pricing == null) return PaymentResult.Fail("Modul fiyati tanimlanmamis.");
        if (pricing.MonthlyPrice <= 0) return PaymentResult.Fail("Bu modul ucretsizdir.");

        // Zaten aktif mi kontrol et
        var existing = await _db.CustomerPortalModules
            .FirstOrDefaultAsync(m => m.CustomerId == customerId && m.ModuleId == moduleId && m.IsActive);
        if (existing != null) return PaymentResult.Fail("Bu modul zaten aktif.");

        var tx = new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.ModulSatinAlma,
            CustomerId = customerId,
            ModuleId = moduleId,
            Amount = pricing.MonthlyPrice,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            InstallmentCount = card.Installment,
            CardLastFour = card.CardNumber?.Length >= 4 ? card.CardNumber[^4..] : null
        };

        var gatewayResult = await ExecutePaymentAsync(tx, card, buyerIp);

        if (gatewayResult.Success)
        {
            // Modulu aktif et
            var cpm = await _db.CustomerPortalModules
                .FirstOrDefaultAsync(m => m.CustomerId == customerId && m.ModuleId == moduleId);
            if (cpm != null)
            {
                cpm.IsActive = true;
                cpm.ActivatedAt = DateTime.UtcNow;
                cpm.DeactivatedAt = null;
            }
            else
            {
                _db.CustomerPortalModules.Add(new CustomerPortalModule
                {
                    CustomerId = customerId,
                    ModuleId = moduleId,
                    IsActive = true,
                    ActivatedAt = DateTime.UtcNow,
                    MonthlyPrice = pricing.MonthlyPrice
                });
            }

            _logger.LogInformation("Modul satin alma basarili: CustomerId={CustomerId}, ModuleId={ModuleId}, Amount={Amount}",
                customerId, moduleId, pricing.MonthlyPrice);
        }

        await _db.SaveChangesAsync();
        return gatewayResult;
    }

    /// <summary>Havale ile modul talebi (beklemede kayit olusturur)</summary>
    public async Task<PaymentResult> CreateHavaleRequestAsync(int customerId, int moduleId)
    {
        var pricing = await _db.ModulePricings.FirstOrDefaultAsync(p => p.ModuleId == moduleId);
        if (pricing == null) return PaymentResult.Fail("Modul fiyati tanimlanmamis.");

        var tx = new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.ModulSatinAlma,
            CustomerId = customerId,
            ModuleId = moduleId,
            Amount = pricing.MonthlyPrice,
            PaymentMethodId = BillingPaymentMethods.Ids.Havale,
            StatusId = PaymentStatuses.Ids.Beklemede,
            Provider = "Havale"
        };

        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Havale talebi olusturuldu: CustomerId={CustomerId}, ModuleId={ModuleId}, TxUid={TxUid}",
            customerId, moduleId, tx.Uid);

        return PaymentResult.Ok(tx.Uid, null);
    }

    /// <summary>Admin havale onaylar -> modulu aktif eder</summary>
    public async Task<PaymentResult> ConfirmHavaleAsync(Guid txUid)
    {
        var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.Uid == txUid);
        if (tx == null) return PaymentResult.Fail("Islem bulunamadi.");
        if (tx.StatusId != PaymentStatuses.Ids.Beklemede) return PaymentResult.Fail("Bu islem zaten islenmis.");
        if (tx.PaymentMethodId != BillingPaymentMethods.Ids.Havale) return PaymentResult.Fail("Bu bir havale islemi degil.");

        tx.StatusId = PaymentStatuses.Ids.Basarili;
        tx.CompletedAt = DateTime.UtcNow;

        // Modul satin almaysa modulu aktif et
        if (tx.PaymentTypeId == PaymentTypes.Ids.ModulSatinAlma && tx.ModuleId.HasValue && tx.CustomerId.HasValue)
        {
            var cpm = await _db.CustomerPortalModules
                .FirstOrDefaultAsync(m => m.CustomerId == tx.CustomerId && m.ModuleId == tx.ModuleId);
            if (cpm != null)
            {
                cpm.IsActive = true;
                cpm.ActivatedAt = DateTime.UtcNow;
                cpm.DeactivatedAt = null;
            }
            else
            {
                var pricing = await _db.ModulePricings.FirstOrDefaultAsync(p => p.ModuleId == tx.ModuleId);
                _db.CustomerPortalModules.Add(new CustomerPortalModule
                {
                    CustomerId = tx.CustomerId.Value,
                    ModuleId = tx.ModuleId.Value,
                    IsActive = true,
                    ActivatedAt = DateTime.UtcNow,
                    MonthlyPrice = pricing?.MonthlyPrice
                });
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Havale onaylandi: TxUid={TxUid}", txUid);
        return PaymentResult.Ok(tx.Uid, null);
    }

    /// <summary>Admin havale reddeder</summary>
    public async Task<PaymentResult> RejectHavaleAsync(Guid txUid, string? reason = null)
    {
        var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.Uid == txUid);
        if (tx == null) return PaymentResult.Fail("Islem bulunamadi.");
        if (tx.StatusId != PaymentStatuses.Ids.Beklemede) return PaymentResult.Fail("Bu islem zaten islenmis.");

        tx.StatusId = PaymentStatuses.Ids.Basarisiz;
        tx.ErrorMessage = reason ?? "Havale admin tarafindan reddedildi.";
        tx.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return PaymentResult.Ok(tx.Uid, null);
    }

    /// <summary>Bekleyen havale islemlerini listele</summary>
    public async Task<List<PaymentTransaction>> GetPendingHavaleAsync()
    {
        return await _db.PaymentTransactions
            .Include(t => t.Customer)
            .Where(t => t.PaymentMethodId == BillingPaymentMethods.Ids.Havale
                     && t.StatusId == PaymentStatuses.Ids.Beklemede)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Uyelik odemesi (PlatformUser odeme yapar)</summary>
    public async Task<PaymentResult> ProcessMembershipPaymentAsync(int planId, int slnClientId, int platformUserId, PaymentCardInfo card, string? buyerIp = null)
    {
        var plan = await _db.SlnMembershipPlans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
        if (plan == null) return PaymentResult.Fail("Uyelik plani bulunamadi.");
        if (plan.Price <= 0) return PaymentResult.Fail("Bu plan ucretsizdir.");

        var tx = new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.UyelikOdemesi,
            CustomerId = plan.CustomerId,
            PlatformUserId = platformUserId,
            Amount = plan.Price,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            InstallmentCount = card.Installment,
            CardLastFour = card.CardNumber?.Length >= 4 ? card.CardNumber[^4..] : null
        };

        var gatewayResult = await ExecutePaymentAsync(tx, card, buyerIp);

        if (gatewayResult.Success)
        {
            // Uyelik olustur
            var now = DateTime.UtcNow;
            _db.SlnClientMemberships.Add(new SlnClientMembership
            {
                CustomerId = plan.CustomerId,
                PlanId = planId,
                SlnClientId = slnClientId,
                StartDate = now,
                CurrentPeriodStart = plan.DurationType == 1 ? now : null,
                CurrentPeriodEnd = plan.DurationType == 1 ? now.AddDays(plan.DurationDays) : null,
                EndDate = plan.DurationType == 1 ? now.AddDays(plan.DurationDays) : null,
                PaidAmount = plan.Price,
                StatusId = 1 // Active
            });

            _logger.LogInformation("Uyelik odemesi basarili: PlanId={PlanId}, PlatformUserId={UserId}", planId, platformUserId);
        }

        await _db.SaveChangesAsync();
        return gatewayResult;
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

    // ─── Private: Gateway uzerinden odeme calistir ───

    private async Task<PaymentResult> ExecutePaymentAsync(PaymentTransaction tx, PaymentCardInfo card, string? buyerIp = null)
    {
        // Aktif gateway config'i al
        var config = await _db.PlatformPaymentConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (config == null)
        {
            tx.StatusId = PaymentStatuses.Ids.Basarisiz;
            tx.ErrorMessage = "Aktif odeme yapilandirmasi bulunamadi. Lutfen Management panelinden ayarlayin.";
            _db.PaymentTransactions.Add(tx);
            return PaymentResult.Fail(tx.ErrorMessage);
        }

        var provider = PaymentProviders.GetById(config.ProviderTypeId);
        tx.Provider = provider?.SystemName ?? "Unknown";

        try
        {
            var gateway = _gatewayFactory.Create(config);

            var request = new PaymentRequest
            {
                Amount = tx.Amount,
                Currency = tx.Currency,
                CardHolderName = card.CardHolderName,
                CardNumber = card.CardNumber,
                ExpireMonth = card.ExpireMonth,
                ExpireYear = card.ExpireYear,
                Cvc = card.Cvc,
                Installment = card.Installment,
                ConversationId = tx.Uid.ToString("N"),
                BuyerName = card.CardHolderName,
                BuyerIp = buyerIp,
                Description = $"Odeme #{tx.Uid:N}"
            };

            var result = await gateway.InitiatePaymentAsync(request);

            if (result.Success && result.IsCompleted)
            {
                tx.StatusId = PaymentStatuses.Ids.Basarili;
                tx.ProviderTransactionId = result.ProviderTransactionId;
                tx.ProviderPaymentId = result.ProviderPaymentId;
                tx.CompletedAt = DateTime.UtcNow;
                _db.PaymentTransactions.Add(tx);
                return PaymentResult.Ok(tx.Uid, tx.ProviderTransactionId);
            }
            else
            {
                tx.StatusId = PaymentStatuses.Ids.Basarisiz;
                tx.ErrorMessage = result.Error ?? "Odeme basarisiz.";
                _db.PaymentTransactions.Add(tx);
                return PaymentResult.Fail(tx.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            tx.StatusId = PaymentStatuses.Ids.Basarisiz;
            tx.ErrorMessage = $"Gateway hatasi: {ex.Message}";
            _db.PaymentTransactions.Add(tx);
            _logger.LogError(ex, "Odeme gateway hatasi: TxUid={TxUid}", tx.Uid);
            return PaymentResult.Fail(tx.ErrorMessage);
        }
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
