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

    /// <summary>Online randevu on odemesi/depozito (musteri kendi karti ile, salon API uzerinden)</summary>
    public async Task<PaymentResult> ProcessAppointmentDepositAsync(int customerId, decimal amount, PaymentCardInfo card, string? buyerIp = null)
    {
        if (amount <= 0) return PaymentResult.Fail("Tutar 0.");

        var tx = new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.RandevuOnOdemesi,
            CustomerId = customerId,
            Amount = amount,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            InstallmentCount = card.Installment,
            CardLastFour = card.CardNumber?.Length >= 4 ? card.CardNumber[^4..] : null
        };

        var gatewayResult = await ExecutePaymentAsync(tx, card, buyerIp);

        if (gatewayResult.Success)
        {
            _logger.LogInformation("Randevu on odemesi basarili: CustomerId={CustomerId}, Amount={Amount}, TxUid={TxUid}",
                customerId, amount, tx.Uid);
        }

        await _db.SaveChangesAsync();
        return gatewayResult;
    }

    public async Task<PaymentTransaction?> GetTransactionByTokenAsync(string token)
    {
        return await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.ProviderTransactionId == token);
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

    public async Task<object?> GetPackagePreviewAsync(int customerId, int packageGroupId)
    {
        var group = SalonModuleGroups.GetById(packageGroupId);
        if (group == null) return null;

        var alreadyActive = await _db.CustomerPortalModules
            .AnyAsync(m => m.CustomerId == customerId && m.IsActive
                && SalonModuleGroups.GetModuleIds(packageGroupId).Contains(m.ModuleId));
        if (alreadyActive) return new { error = "Bu paket zaten aktif." };

        var monthlyPrice = await GetActivePackagePriceAsync(packageGroupId) ?? group.MonthlyPrice;

        var subscription = await _db.CustomerSubscriptions
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.StatusId == 1);
        var nextBilling = subscription?.NextBillingDate ?? DateTime.UtcNow.AddDays(30);

        var daysUntilBilling = Math.Max(1, (int)Math.Ceiling((nextBilling - DateTime.UtcNow).TotalDays));
        if (daysUntilBilling > 30) daysUntilBilling = 30;
        var dailyRate = monthlyPrice / 30m;
        var proRataAmount = Math.Round(dailyRate * daysUntilBilling, 2);

        return new
        {
            packageGroupId = group.Id,
            packageName = group.Description,
            monthlyPrice,
            proRata = new { days = daysUntilBilling, dailyRate = Math.Round(dailyRate, 2), amount = proRataAmount },
            nextBillingDate = nextBilling.ToString("yyyy-MM-dd"),
            totalNow = proRataAmount
        };
    }

    private async Task<decimal?> GetActivePackagePriceAsync(int packageGroupId)
    {
        var activePeriod = await _db.ServicePricingPeriods
            .Where(p => p.StatusId == 1)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();
        if (activePeriod == null) return null;

        var item = await _db.ServicePricingItems
            .FirstOrDefaultAsync(i => i.PeriodId == activePeriod.Id && i.PackageGroupId == packageGroupId);
        return item?.MonthlyPrice;
    }

    public async Task<CheckoutFormResult> InitPackageCheckoutAsync(int customerId, int packageGroupId, string callbackUrl, string? buyerIp = null)
    {
        var group = SalonModuleGroups.GetById(packageGroupId);
        if (group == null) return CheckoutFormResult.Fail("Paket bulunamadi.");

        var alreadyActive = await _db.CustomerPortalModules
            .AnyAsync(m => m.CustomerId == customerId && m.IsActive
                && SalonModuleGroups.GetModuleIds(packageGroupId).Contains(m.ModuleId));
        if (alreadyActive) return CheckoutFormResult.Fail("Bu paket zaten aktif.");

        var monthlyPrice = await GetActivePackagePriceAsync(packageGroupId) ?? group.MonthlyPrice;

        var subscription = await _db.CustomerSubscriptions
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.StatusId == 1);
        var nextBilling = subscription?.NextBillingDate ?? DateTime.UtcNow.AddDays(30);
        var daysUntilBilling = Math.Max(1, (int)Math.Ceiling((nextBilling - DateTime.UtcNow).TotalDays));
        if (daysUntilBilling > 30) daysUntilBilling = 30;
        var proRataAmount = Math.Round(monthlyPrice / 30m * daysUntilBilling, 2);

        if (proRataAmount <= 0) return CheckoutFormResult.Fail("Odenecek tutar 0.");

        var config = await _db.PlatformPaymentConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (config == null) return CheckoutFormResult.Fail("Aktif odeme yapilandirmasi bulunamadi.");

        var tx = new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.ModulSatinAlma,
            CustomerId = customerId,
            ModuleId = packageGroupId,
            Amount = proRataAmount,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            StatusId = PaymentStatuses.Ids.Beklemede,
            Provider = PaymentProviders.GetById(config.ProviderTypeId)?.SystemName ?? "Iyzico",
            Notes = $"PackageGroup:{packageGroupId}|ProRata:{daysUntilBilling}gun"
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        try
        {
            var gateway = _gatewayFactory.Create(config);
            if (gateway is not IyzicoGateway iyzicoGw)
                return CheckoutFormResult.Fail("Checkout form sadece Iyzico destekler.");

            var customer = subscription?.Customer ?? await _db.Customers.FindAsync(customerId);
            var req = new CheckoutFormRequest
            {
                Amount = proRataAmount,
                ConversationId = tx.Uid.ToString("N"),
                CallbackUrl = callbackUrl,
                BuyerId = customerId.ToString(),
                BuyerName = customer?.Name ?? "Musteri",
                BuyerEmail = customer?.Email ?? "noreply@corplynk.com",
                BuyerIp = buyerIp,
                Description = $"{group.Description} - {daysUntilBilling} gunluk kist hesap"
            };

            var result = await iyzicoGw.InitCheckoutFormAsync(req);
            if (result.Success)
            {
                tx.ProviderTransactionId = result.Token;
                await _db.SaveChangesAsync();
            }
            return result;
        }
        catch (Exception ex)
        {
            tx.StatusId = PaymentStatuses.Ids.Basarisiz;
            tx.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync();
            return CheckoutFormResult.Fail($"Checkout form hatasi: {ex.Message}");
        }
    }

    public async Task<CheckoutFormResult> InitSubscriptionCheckoutAsync(int customerId, string callbackUrl, string? buyerIp = null)
    {
        var subscription = await _db.CustomerSubscriptions
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.StatusId == 1);
        if (subscription == null) return CheckoutFormResult.Fail("Aktif abonelik bulunamadi.");

        var unpaidPeriod = await _db.CustomerBillingPeriods
            .Where(p => p.CustomerId == customerId && !p.IsPaid && p.StatusId != BillingPeriodStatuses.Ids.Paid)
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .FirstOrDefaultAsync();
        if (unpaidPeriod == null) return CheckoutFormResult.Fail("Odenmemis donem bulunamadi.");

        var amount = unpaidPeriod.Amount + unpaidPeriod.ServiceAmount;
        if (amount <= 0) return CheckoutFormResult.Fail("Odenecek tutar 0.");

        var config = await _db.PlatformPaymentConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (config == null) return CheckoutFormResult.Fail("Aktif odeme yapilandirmasi bulunamadi.");

        var tx = new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.PlatformAbonelik,
            CustomerId = customerId,
            BillingPeriodId = unpaidPeriod.Id,
            Amount = amount,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            StatusId = PaymentStatuses.Ids.Beklemede,
            Provider = PaymentProviders.GetById(config.ProviderTypeId)?.SystemName ?? "Iyzico"
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        try
        {
            var gateway = _gatewayFactory.Create(config);
            if (gateway is not IyzicoGateway iyzicoGw)
                return CheckoutFormResult.Fail("Checkout form sadece Iyzico destekler.");

            var customer = subscription.Customer;
            var req = new CheckoutFormRequest
            {
                Amount = amount,
                ConversationId = tx.Uid.ToString("N"),
                CallbackUrl = callbackUrl,
                BuyerId = customerId.ToString(),
                BuyerName = customer?.Name ?? "Musteri",
                BuyerEmail = customer?.Email ?? "noreply@corplynk.com",
                BuyerIp = buyerIp,
                Description = $"Abonelik {unpaidPeriod.Year}/{unpaidPeriod.Month:D2}"
            };

            var result = await iyzicoGw.InitCheckoutFormAsync(req);

            if (result.Success)
            {
                tx.ProviderTransactionId = result.Token;
                await _db.SaveChangesAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            tx.StatusId = PaymentStatuses.Ids.Basarisiz;
            tx.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync();
            return CheckoutFormResult.Fail($"Checkout form hatasi: {ex.Message}");
        }
    }

    public async Task<PaymentResult> CompleteCheckoutAsync(string token)
    {
        var config = await _db.PlatformPaymentConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (config == null) return PaymentResult.Fail("Aktif odeme yapilandirmasi bulunamadi.");

        var gateway = _gatewayFactory.Create(config);
        if (gateway is not IyzicoGateway iyzicoGw)
            return PaymentResult.Fail("Checkout dogrulama sadece Iyzico destekler.");

        var verifyResult = await iyzicoGw.VerifyCheckoutFormAsync(token);

        var tx = await _db.PaymentTransactions
            .FirstOrDefaultAsync(t => t.ProviderTransactionId == token && t.StatusId == PaymentStatuses.Ids.Beklemede);

        if (tx == null) return PaymentResult.Fail("Bekleyen islem bulunamadi.");

        if (verifyResult.Success)
        {
            tx.StatusId = PaymentStatuses.Ids.Basarili;
            tx.ProviderPaymentId = verifyResult.ProviderPaymentId;
            tx.CompletedAt = DateTime.UtcNow;

            if (tx.BillingPeriodId.HasValue)
            {
                var period = await _db.CustomerBillingPeriods.FindAsync(tx.BillingPeriodId.Value);
                if (period != null)
                {
                    period.StatusId = BillingPeriodStatuses.Ids.Paid;
                    period.IsPaid = true;
                    period.PaidAt = DateTime.UtcNow;
                    period.PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti;
                }
            }

            // Paket satin alma ise modulleri aktif et
            if (tx.PaymentTypeId == PaymentTypes.Ids.ModulSatinAlma && tx.Notes?.StartsWith("PackageGroup:") == true)
            {
                var pgId = int.Parse(tx.Notes.Split('|')[0].Split(':')[1]);
                var moduleIds = SalonModuleGroups.GetModuleIds(pgId);
                var group = SalonModuleGroups.GetById(pgId);
                foreach (var moduleId in moduleIds)
                {
                    var cpm = await _db.CustomerPortalModules
                        .FirstOrDefaultAsync(m => m.CustomerId == tx.CustomerId && m.ModuleId == moduleId);
                    if (cpm != null)
                    {
                        cpm.IsActive = true;
                        cpm.ActivatedAt = DateTime.UtcNow;
                        cpm.DeactivatedAt = null;
                        cpm.MonthlyPrice = group?.MonthlyPrice;
                    }
                    else
                    {
                        _db.CustomerPortalModules.Add(new CustomerPortalModule
                        {
                            CustomerId = tx.CustomerId!.Value,
                            ModuleId = moduleId,
                            IsActive = true,
                            ActivatedAt = DateTime.UtcNow,
                            MonthlyPrice = group?.MonthlyPrice
                        });
                    }
                }
            }

            // Randevu depozitosu ise randevuyu onayli yap (StatusId 6 = AwaitingPayment → 2 = Confirmed)
            if (tx.PaymentTypeId == PaymentTypes.Ids.RandevuOnOdemesi && tx.Notes?.StartsWith("Appointment:") == true)
            {
                var parts = tx.Notes.Split('|');
                if (parts.Length > 0 && int.TryParse(parts[0].Replace("Appointment:", ""), out var aptId))
                {
                    var apt = await _db.SlnAppointments.FindAsync(aptId);
                    if (apt != null && apt.StatusId == 6)
                    {
                        apt.StatusId = 2; // Confirmed — depozito ödendiyse onay gerekmez
                        apt.IsPrepaid = true;
                        apt.PrepaidAmount = tx.Amount;
                        apt.PaymentTransactionId = tx.Id;
                    }
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Checkout odeme basarili: TxUid={TxUid}, PaymentId={PaymentId}", tx.Uid, verifyResult.ProviderPaymentId);
            return PaymentResult.Ok(tx.Uid, verifyResult.ProviderTransactionId);
        }
        else
        {
            tx.StatusId = PaymentStatuses.Ids.Basarisiz;
            tx.ErrorMessage = verifyResult.Error;
            tx.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogWarning("Checkout odeme basarisiz: TxUid={TxUid}, Hata={Error}", tx.Uid, verifyResult.Error);
            return PaymentResult.Fail(verifyResult.Error ?? "Odeme basarisiz");
        }
    }

    /// <summary>Online randevu depozitosu icin Iyzico Checkout Form baslatir (3DS).</summary>
    public async Task<CheckoutFormResult> InitBookingDepositCheckoutAsync(
        int customerId, int appointmentId, string slug, decimal amount,
        string buyerFullName, string buyerEmail, string callbackUrl, string? buyerIp = null)
    {
        var config = await _db.PlatformPaymentConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (config == null) return CheckoutFormResult.Fail("Aktif odeme yapilandirmasi bulunamadi.");

        var tx = new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.RandevuOnOdemesi,
            CustomerId = customerId,
            Amount = amount,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            StatusId = PaymentStatuses.Ids.Beklemede,
            Provider = PaymentProviders.GetById(config.ProviderTypeId)?.SystemName ?? "Iyzico",
            Notes = $"Appointment:{appointmentId}|Slug:{slug}"
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        try
        {
            var gateway = _gatewayFactory.Create(config);
            if (gateway is not IyzicoGateway iyzicoGw)
                return CheckoutFormResult.Fail("Checkout form sadece Iyzico destekler.");

            var req = new CheckoutFormRequest
            {
                Amount = amount,
                ConversationId = tx.Uid.ToString("N"),
                CallbackUrl = callbackUrl,
                BuyerId = $"apt-{appointmentId}",
                BuyerName = string.IsNullOrWhiteSpace(buyerFullName) ? "Musteri" : buyerFullName,
                BuyerEmail = string.IsNullOrWhiteSpace(buyerEmail) ? "noreply@corplynk.com" : buyerEmail,
                BuyerIp = buyerIp,
                Description = $"Randevu Depozitosu - {amount:N2} TL"
            };

            var result = await iyzicoGw.InitCheckoutFormAsync(req);
            if (result.Success)
            {
                tx.ProviderTransactionId = result.Token;
                await _db.SaveChangesAsync();
            }
            return result;
        }
        catch (Exception ex)
        {
            tx.StatusId = PaymentStatuses.Ids.Basarisiz;
            tx.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync();
            return CheckoutFormResult.Fail($"Checkout form hatasi: {ex.Message}");
        }
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
                BuyerName = card.BuyerFullName ?? card.CardHolderName,
                BuyerEmail = card.BuyerEmail,
                BuyerPhone = card.BuyerPhone,
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
    // Iyzico fraud kontrolu icin alici bilgileri
    public string? BuyerFullName { get; set; }
    public string? BuyerEmail { get; set; }
    public string? BuyerPhone { get; set; }
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
