using CallCenter.Api.Services.Payment;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Helpers;
using CallCenter.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Encodings.Web;

namespace CallCenter.Api.Services;

/// <summary>
/// Online odeme servisi. Aktif PlatformPaymentConfig uzerinden gateway ile odeme yapar.
/// Config yoksa veya gateway tanimli degilse hata dondurur.
/// </summary>
public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly PaymentGatewayFactory _gatewayFactory;
    private readonly ISubscriptionFactory _subscriptionFactory;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        AppDbContext db,
        PaymentGatewayFactory gatewayFactory,
        ISubscriptionFactory subscriptionFactory,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _gatewayFactory = gatewayFactory;
        _subscriptionFactory = subscriptionFactory;
        _logger = logger;
    }

    private async Task RefreshSalonSubscriptionDisplayMonthlySafeAsync(int customerId)
    {
        try
        {
            await _subscriptionFactory.RefreshSubscriptionDisplayMonthlyPriceAsync(customerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Salon abonelik ozet ayligi guncellenemedi: CustomerId={CustomerId}", customerId);
        }
    }

    private static int GetEffectiveSubscriptionIntervalMonths(SubscriptionPlan? plan)
    {
        var interval = plan?.IntervalMonths ?? 0;
        return interval > 0 ? interval : 1;
    }

    private async Task ActivateSalonSubscriptionAfterPlatformPaymentAsync(CustomerBillingPeriod period, decimal paidAmount)
    {
        if (period.BillingKindId != CustomerBillingKinds.SalonPlatform) return;

        var subscription = await _db.CustomerSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.CustomerId == period.CustomerId
                && (s.StatusId == SubscriptionStatuses.Ids.Active || s.StatusId == SubscriptionStatuses.Ids.Suspended))
            .OrderByDescending(s => s.StatusId == SubscriptionStatuses.Ids.Active)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
        if (subscription == null) return;

        if (subscription.PeriodPrice <= 0m)
        {
            var platformAmount = period.Amount > 0m
                ? period.Amount
                : Math.Max(0m, paidAmount - period.ServiceAmount);
            platformAmount = Math.Round(platformAmount, 2, MidpointRounding.AwayFromZero);

            if (platformAmount > 0m)
                subscription.PeriodPrice = platformAmount;

            if (period.UnitPrice > 0m)
            {
                var interval = GetEffectiveSubscriptionIntervalMonths(subscription.Plan);
                subscription.MonthlyPrice = Math.Round(period.UnitPrice / interval, 2, MidpointRounding.AwayFromZero);
            }

            subscription.StartDate = DateTime.SpecifyKind(period.PeriodStartDate, DateTimeKind.Utc);
            subscription.BillingDay = period.PeriodStartDate.Day;
        }

        subscription.StatusId = SubscriptionStatuses.Ids.Active;
        subscription.CancelledAt = null;
    }

    private static int? TryReadPackageGroupId(string? notes)
        => TryReadNoteInt(notes, "PackageGroup:");

    private static int? TryReadMembershipPlanId(string? notes)
        => TryReadNoteInt(notes, "MembershipPlan:");

    private static int? TryReadMembershipSlnClientId(string? notes)
        => TryReadNoteInt(notes, "SlnClient:");

    private static string? TryReadMembershipSlug(string? notes)
        => TryReadNoteValue(notes, "Slug:");

    private static string AddNoteValue(string? notes, string prefix, string value)
    {
        var part = prefix + value;
        return string.IsNullOrWhiteSpace(notes) ? part : notes + "|" + part;
    }

    private static string EncodeNoteValue(string value)
        => Uri.EscapeDataString(value);

    private static string DecodeNoteValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        try { return Uri.UnescapeDataString(value); }
        catch { return value; }
    }

    private static string AddEncodedNoteValue(string? notes, string prefix, string? value)
        => string.IsNullOrWhiteSpace(value) ? notes ?? string.Empty : AddNoteValue(notes, prefix, EncodeNoteValue(value.Trim()));

    private static string AddNoteEvent(string? notes, string kind, string detail)
        => AddEncodedNoteValue(notes, "Event:", $"{DateTime.UtcNow:O}~{kind}~{detail}");

    private async Task CancelPendingCheckoutAttemptsAsync(
        int customerId,
        int paymentTypeId,
        int? billingPeriodId = null,
        int? packageGroupId = null)
    {
        var query = _db.PaymentTransactions
            .Where(t => t.CustomerId == customerId
                     && t.PaymentTypeId == paymentTypeId
                     && t.StatusId == PaymentStatuses.Ids.Beklemede);

        if (billingPeriodId.HasValue)
            query = query.Where(t => t.BillingPeriodId == billingPeriodId.Value);

        var pending = await query.ToListAsync();
        foreach (var tx in pending)
        {
            if (packageGroupId.HasValue && TryReadPackageGroupId(tx.Notes) != packageGroupId.Value)
                continue;

            tx.StatusId = PaymentStatuses.Ids.Iptal;
            tx.CompletedAt = DateTime.UtcNow;
            tx.ErrorMessage = "Yeni odeme denemesi baslatildigi icin iptal edildi.";
            tx.Notes = AddNoteEvent(tx.Notes, "CheckoutSuperseded", tx.ErrorMessage);
        }
    }

    private static List<string> ReadDecodedNoteValues(string? notes, string prefix)
    {
        var values = new List<string>();
        if (string.IsNullOrWhiteSpace(notes)) return values;
        foreach (var part in notes.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                values.Add(DecodeNoteValue(part[prefix.Length..]));
        }

        return values;
    }

    private static int? TryReadNoteInt(string? notes, string prefix)
    {
        var value = TryReadNoteValue(notes, prefix);
        return int.TryParse(value, out var id) ? id : null;
    }

    private static string? TryReadNoteValue(string? notes, string prefix)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        foreach (var part in notes.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return part[prefix.Length..];
        }

        return null;
    }

    private static string? TryReadDecodedNoteValue(string? notes, string prefix)
    {
        var raw = TryReadNoteValue(notes, prefix);
        return raw == null ? null : DecodeNoteValue(raw);
    }

    private static string GetPaymentReceiptRoot()
        => Path.Combine(AppContext.BaseDirectory, "App_Data", "payment-receipts");

    private static string? GetAllowedReceiptExtension(string? fileName, string? contentType)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        if (ext is ".pdf" or ".jpg" or ".jpeg" or ".png" or ".webp")
            return ext;

        return (contentType ?? string.Empty).ToLowerInvariant() switch
        {
            "application/pdf" => ".pdf",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => null
        };
    }

    private static string GuessReceiptContentType(string? contentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
            return contentType;

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string SanitizeFileName(string? fileName, string fallback)
    {
        var safe = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? fallback : fileName);
        foreach (var ch in Path.GetInvalidFileNameChars())
            safe = safe.Replace(ch, '_');
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
    }

    private async Task<(string? RelativePath, string? OriginalFileName, string? ContentType, string? Error)> SaveHavaleReceiptAsync(
        int customerId,
        Guid transactionUid,
        string? fileName,
        string? contentType,
        string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return (null, null, null, null);

        var ext = GetAllowedReceiptExtension(fileName, contentType);
        if (ext == null)
            return (null, null, null, "Dekont dosyasi PDF, JPG, PNG veya WEBP olmalidir.");

        var payload = base64.Trim();
        var comma = payload.IndexOf(',');
        if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
            payload = payload[(comma + 1)..];

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(payload);
        }
        catch
        {
            return (null, null, null, "Dekont dosyasi okunamadi.");
        }

        if (bytes.Length == 0)
            return (null, null, null, "Dekont dosyasi bos.");
        if (bytes.Length > 5 * 1024 * 1024)
            return (null, null, null, "Dekont dosyasi en fazla 5 MB olabilir.");

        var originalName = SanitizeFileName(fileName, $"havale-dekont{ext}");
        var root = GetPaymentReceiptRoot();
        var customerFolder = Path.Combine(root, customerId.ToString());
        Directory.CreateDirectory(customerFolder);

        var storedName = transactionUid.ToString("N") + ext;
        var fullPath = Path.Combine(customerFolder, storedName);
        await File.WriteAllBytesAsync(fullPath, bytes);

        return ($"{customerId}/{storedName}", originalName, GuessReceiptContentType(contentType, ext), null);
    }

    private static (string? RelativePath, string? OriginalFileName, string? ContentType) GetHavaleReceiptInfo(PaymentTransaction tx)
    {
        return (
            TryReadDecodedNoteValue(tx.Notes, "HavaleReceiptPath:"),
            TryReadDecodedNoteValue(tx.Notes, "HavaleReceiptName:"),
            TryReadDecodedNoteValue(tx.Notes, "HavaleReceiptType:"));
    }

    /// <summary>Salon adisyon odemesi baslatir (PlatformUser odiyor)</summary>
    public async Task<PaymentResult> ProcessInvoicePaymentAsync(int customerId, int invoiceId, int platformUserId, PaymentCardInfo card)
    {
        var invoice = await _db.SlnInvoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.CustomerId == customerId);
        if (invoice == null) return PaymentResult.Fail("Adisyon bulunamadi.");

        // Tutari adisyondan al — randevu sirasinda yazandan degil, salonun guncelledigi son tutardan.
        var amount = invoice.TotalAmount;
        if (amount <= 0) return PaymentResult.Fail("Odenecek tutar 0.");

        // Mobil/musteri tahsilati: Pazaryeri kayit ZORUNLU — komisyon corplynk'a, kalan salonun sub-merchant hesabina.
        var split = await GetMarketplaceSplitAsync(customerId, amount, requireSplit: true);
        if (!split.Success)
            return PaymentResult.Fail(split.Error ?? "Salon Pazaryeri kaydi tamamlanmamis.");

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

        var gatewayResult = await ExecutePaymentAsync(tx, card,
            subMerchantKey: split.SubMerchantKey,
            subMerchantPrice: split.SubMerchantPrice);

        if (gatewayResult.Success)
        {
            invoice.StatusId = 3; // Paid
            tx.Notes = AddNoteEvent(tx.Notes, "MarketplaceSplit", $"Komisyon={split.CommissionAmount:F2} ({split.CommissionPercent}%), SubMerchantPrice={split.SubMerchantPrice:F2}");
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
        if (amount <= 0m && (period.BillingKindId == CustomerBillingKinds.SalonPlatform
            || period.BillingKindId == CustomerBillingKinds.CallCenter))
        {
            var br = await _subscriptionFactory.GetSalonBillingBreakdownForCustomerAsync(period.CustomerId);
            amount = (br?.PlatformAmount ?? 0m) + (br?.ModuleAmount ?? 0m);
        }

        if (amount <= 0m)
            return PaymentResult.Fail("Odenecek tutar sifir veya hesaplanamadi; tahakkuk tutarlari kontrol edin.");

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
            await ActivateSalonSubscriptionAfterPlatformPaymentAsync(period, amount);
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
        if (gatewayResult.Success)
            await RefreshSalonSubscriptionDisplayMonthlySafeAsync(customerId);
        return gatewayResult;
    }
    public async Task<PaymentResult> CreateHavaleRequestAsync(
        int customerId,
        int moduleId,
        string? referenceNote = null,
        string? receiptFileName = null,
        string? receiptContentType = null,
        string? receiptBase64 = null)
    {
        var pricing = await _db.ModulePricings.FirstOrDefaultAsync(p => p.ModuleId == moduleId);
        if (pricing == null) return PaymentResult.Fail("Modul fiyati tanimlanmamis.");
        if (pricing.MonthlyPrice <= 0) return PaymentResult.Fail("Bu modul ucretsizdir.");

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

        tx.Notes = AddNoteEvent(tx.Notes, "CustomerHavaleRequested", "Havale/EFT talebi olusturuldu.");
        tx.Notes = AddEncodedNoteValue(tx.Notes, "HavaleRef:", referenceNote);

        var receipt = await SaveHavaleReceiptAsync(
            customerId, tx.Uid, receiptFileName, receiptContentType, receiptBase64);
        if (!string.IsNullOrEmpty(receipt.Error))
            return PaymentResult.Fail(receipt.Error);

        if (!string.IsNullOrEmpty(receipt.RelativePath))
        {
            tx.Notes = AddEncodedNoteValue(tx.Notes, "HavaleReceiptPath:", receipt.RelativePath);
            tx.Notes = AddEncodedNoteValue(tx.Notes, "HavaleReceiptName:", receipt.OriginalFileName);
            tx.Notes = AddEncodedNoteValue(tx.Notes, "HavaleReceiptType:", receipt.ContentType);
            tx.Notes = AddNoteEvent(tx.Notes, "CustomerReceiptUploaded", "Havale dekontu yuklendi.");
        }

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
        tx.Notes = AddNoteEvent(tx.Notes, "AdminHavaleConfirmed", "Havale admin tarafindan onaylandi.");

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

        if (tx.PaymentTypeId == PaymentTypes.Ids.ModulSatinAlma && tx.CustomerId.HasValue)
            await RefreshSalonSubscriptionDisplayMonthlySafeAsync(tx.CustomerId.Value);

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
        tx.Notes = AddNoteEvent(tx.Notes, "AdminHavaleRejected", tx.ErrorMessage);

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

    public async Task<(byte[]? Bytes, string? FileName, string? ContentType, string? Error)> GetHavaleReceiptFileAsync(
        Guid transactionUid,
        int? customerId = null,
        bool allowAnyCustomer = false)
    {
        var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.Uid == transactionUid);
        if (tx == null) return (null, null, null, "Islem bulunamadi.");
        if (!allowAnyCustomer && tx.CustomerId != customerId)
            return (null, null, null, "Bu dekont icin yetkiniz yok.");

        var (relativePath, originalFileName, contentType) = GetHavaleReceiptInfo(tx);
        if (string.IsNullOrWhiteSpace(relativePath))
            return (null, null, null, "Bu havale icin yuklu dekont yok.");

        var root = Path.GetFullPath(GetPaymentReceiptRoot());
        var filePath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return (null, null, null, "Dekont yolu gecersiz.");
        if (!File.Exists(filePath))
            return (null, null, null, "Dekont dosyasi bulunamadi.");

        var bytes = await File.ReadAllBytesAsync(filePath);
        return (bytes, originalFileName ?? Path.GetFileName(filePath), contentType ?? "application/octet-stream", null);
    }

    public async Task<PaymentResult> RefundTransactionAsync(Guid transactionUid, decimal? amount = null, string? reason = null)
    {
        var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.Uid == transactionUid);
        if (tx == null) return PaymentResult.Fail("Islem bulunamadi.");
        if (tx.Amount <= 0) return PaymentResult.Fail("Bu islem iade edilemez.");
        if (tx.StatusId != PaymentStatuses.Ids.Basarili)
            return PaymentResult.Fail("Yalnizca basarili odemeler iade edilebilir.");

        var refundToken = $"RefundOf:{tx.Uid}";
        var alreadyRefunded = await _db.PaymentTransactions
            .Where(t => t.Notes != null
                     && t.Notes.Contains(refundToken)
                     && t.StatusId == PaymentStatuses.Ids.Basarili
                     && t.Amount < 0)
            .SumAsync(t => -t.Amount);

        var remaining = Math.Round(tx.Amount - alreadyRefunded, 2, MidpointRounding.AwayFromZero);
        if (remaining <= 0)
            return PaymentResult.Fail("Bu odeme zaten tamamen iade edilmis.");

        var refundAmount = Math.Round(amount ?? remaining, 2, MidpointRounding.AwayFromZero);
        if (refundAmount <= 0)
            return PaymentResult.Fail("Iade tutari 0'dan buyuk olmalidir.");
        if (refundAmount > remaining)
            return PaymentResult.Fail($"Iade tutari kalan iade edilebilir tutari asamaz: {remaining:N2} {tx.Currency}");

        string? refundReference;
        if (tx.PaymentMethodId == BillingPaymentMethods.Ids.Havale)
        {
            refundReference = "HAVALE-REFUND-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        }
        else
        {
            var providerReference = tx.ProviderPaymentId ?? tx.ProviderTransactionId;
            if (string.IsNullOrWhiteSpace(providerReference))
                return PaymentResult.Fail("Odeme saglayici referansi eksik, iade yapilamadi.");

            var config = await _db.PlatformPaymentConfigs.FirstOrDefaultAsync(c => c.IsActive);
            if (config == null) return PaymentResult.Fail("Aktif odeme yapilandirmasi bulunamadi.");

            var gateway = _gatewayFactory.Create(config);
            var refundResult = await gateway.RefundAsync(providerReference, refundAmount);
            if (!refundResult.Success)
            {
                tx.Notes = AddNoteEvent(tx.Notes, "AdminRefundFailed", refundResult.Error ?? "Iade basarisiz.");
                await _db.SaveChangesAsync();
                return PaymentResult.Fail(refundResult.Error ?? "Iade basarisiz.");
            }

            refundReference = refundResult.RefundId;
        }

        var refundTx = new PaymentTransaction
        {
            PaymentTypeId = tx.PaymentTypeId,
            CustomerId = tx.CustomerId,
            PlatformUserId = tx.PlatformUserId,
            InvoiceId = tx.InvoiceId,
            BillingPeriodId = tx.BillingPeriodId,
            ModuleId = tx.ModuleId,
            Amount = -refundAmount,
            Currency = tx.Currency,
            PaymentMethodId = tx.PaymentMethodId,
            StatusId = PaymentStatuses.Ids.Basarili,
            Provider = tx.Provider,
            ProviderTransactionId = refundReference,
            ProviderPaymentId = tx.ProviderPaymentId,
            CompletedAt = DateTime.UtcNow,
            Notes = AddNoteEvent($"RefundOf:{tx.Uid}", "AdminRefundSucceeded",
                $"{refundAmount:N2} {tx.Currency} iade edildi.")
        };
        refundTx.Notes = AddEncodedNoteValue(refundTx.Notes, "Reason:", reason);
        _db.PaymentTransactions.Add(refundTx);

        var totalRefunded = alreadyRefunded + refundAmount;
        tx.Notes = AddNoteEvent(tx.Notes, "AdminRefundRequested",
            $"{refundAmount:N2} {tx.Currency} iade islemi baslatildi.");
        if (totalRefunded >= tx.Amount)
            tx.StatusId = PaymentStatuses.Ids.Iade;

        await _db.SaveChangesAsync();
        return PaymentResult.Ok(refundTx.Uid, refundReference);
    }

    public async Task<PaymentResult> CancelTransactionAsync(Guid transactionUid, string? reason = null)
    {
        var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.Uid == transactionUid);
        if (tx == null) return PaymentResult.Fail("Islem bulunamadi.");
        if (tx.StatusId != PaymentStatuses.Ids.Beklemede)
            return PaymentResult.Fail("Yalnizca bekleyen islemler iptal edilebilir.");

        tx.StatusId = PaymentStatuses.Ids.Basarisiz;
        tx.ErrorMessage = string.IsNullOrWhiteSpace(reason) ? "Islem admin tarafindan iptal edildi." : reason.Trim();
        tx.CompletedAt = DateTime.UtcNow;
        tx.Notes = AddNoteEvent(tx.Notes, "AdminPaymentCancelled", tx.ErrorMessage);

        await _db.SaveChangesAsync();
        return PaymentResult.Ok(tx.Uid, tx.ProviderTransactionId);
    }

    public async Task<(object? Data, string? Error)> GetPaymentTimelineAsync(
        Guid transactionUid,
        int? customerId = null,
        int? platformUserId = null,
        bool allowAnyCustomer = false)
    {
        var tx = await _db.PaymentTransactions
            .Include(t => t.Customer)
            .Include(t => t.PlatformUser)
            .FirstOrDefaultAsync(t => t.Uid == transactionUid);
        if (tx == null) return (null, "Islem bulunamadi.");
        if (!allowAnyCustomer)
        {
            if (customerId.HasValue && tx.CustomerId != customerId)
                return (null, "Bu islem icin yetkiniz yok.");
            if (platformUserId.HasValue && tx.PlatformUserId != platformUserId)
                return (null, "Bu islem icin yetkiniz yok.");
        }

        var refundToken = $"RefundOf:{tx.Uid}";
        var refunds = await _db.PaymentTransactions
            .Where(t => t.Notes != null && t.Notes.Contains(refundToken))
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        var events = new List<object>();
        void AddEvent(DateTime at, string kind, string title, string? detail = null)
            => events.Add(new
            {
                at,
                kind,
                title,
                detail
            });

        AddEvent(tx.CreatedAt, "created", "Islem olusturuldu",
            $"{PaymentTypes.GetById(tx.PaymentTypeId)?.Description ?? "Odeme"} - {tx.Amount:N2} {tx.Currency}");

        if (!string.IsNullOrWhiteSpace(tx.ProviderTransactionId))
            AddEvent(tx.CreatedAt, "provider-reference", "Provider islem referansi alindi", tx.ProviderTransactionId);
        if (!string.IsNullOrWhiteSpace(tx.ProviderPaymentId))
            AddEvent(tx.CompletedAt ?? tx.CreatedAt, "provider-payment", "Provider odeme ID alindi", tx.ProviderPaymentId);

        foreach (var rawEvent in ReadDecodedNoteValues(tx.Notes, "Event:"))
        {
            var parts = rawEvent.Split('~', 3);
            var at = parts.Length > 0 && DateTime.TryParse(parts[0], out var parsedAt)
                ? parsedAt
                : tx.CreatedAt;
            var kind = parts.Length > 1 ? parts[1] : "note";
            var detail = parts.Length > 2 ? parts[2] : rawEvent;
            AddEvent(at, kind, PaymentEventTitle(kind), detail);
        }

        var havaleRef = TryReadDecodedNoteValue(tx.Notes, "HavaleRef:");
        if (!string.IsNullOrWhiteSpace(havaleRef))
            AddEvent(tx.CreatedAt, "havale-reference", "Havale aciklamasi girildi", havaleRef);

        var receipt = GetHavaleReceiptInfo(tx);
        if (!string.IsNullOrWhiteSpace(receipt.RelativePath))
            AddEvent(tx.CreatedAt, "receipt", "Havale dekontu yuklendi", receipt.OriginalFileName);

        if (tx.StatusId == PaymentStatuses.Ids.Basarili)
            AddEvent(tx.CompletedAt ?? tx.CreatedAt, "success", "Odeme basarili tamamlandi");
        else if (tx.StatusId == PaymentStatuses.Ids.Basarisiz)
            AddEvent(tx.CompletedAt ?? tx.CreatedAt, "failed", "Odeme basarisiz oldu", tx.ErrorMessage);
        else if (tx.StatusId == PaymentStatuses.Ids.Iade)
            AddEvent(tx.CompletedAt ?? tx.CreatedAt, "refunded", "Odeme tamamen iade edildi");

        foreach (var refund in refunds)
        {
            AddEvent(refund.CompletedAt ?? refund.CreatedAt, "refund",
                "Iade kaydi olustu", $"{Math.Abs(refund.Amount):N2} {refund.Currency} - {refund.ProviderTransactionId}");
        }

        var orderedEvents = events
            .OrderBy(e => (DateTime)e.GetType().GetProperty("at")!.GetValue(e)!)
            .ToList();

        return (new
        {
            payment = new
            {
                tx.Uid,
                tx.PaymentTypeId,
                PaymentType = PaymentTypes.GetById(tx.PaymentTypeId)?.Description,
                tx.Amount,
                tx.Currency,
                tx.PaymentMethodId,
                PaymentMethod = BillingPaymentMethods.GetById(tx.PaymentMethodId)?.Description,
                tx.StatusId,
                Status = PaymentStatuses.GetById(tx.StatusId)?.Description,
                tx.Provider,
                tx.ProviderTransactionId,
                tx.ProviderPaymentId,
                CustomerName = tx.Customer?.Name,
                PlatformUser = tx.PlatformUser?.FullName,
                tx.ErrorMessage,
                tx.CreatedAt,
                tx.CompletedAt
            },
            events = orderedEvents,
            refunds = refunds.Select(r => new
            {
                r.Uid,
                r.Amount,
                r.Currency,
                r.ProviderTransactionId,
                r.CreatedAt,
                r.CompletedAt,
                Reason = TryReadDecodedNoteValue(r.Notes, "Reason:")
            })
        }, null);
    }

    private static string PaymentEventTitle(string kind)
    {
        return kind switch
        {
            "CustomerHavaleRequested" => "Musteri havale bildirimi yapti",
            "CustomerReceiptUploaded" => "Dekont yuklendi",
            "AdminHavaleConfirmed" => "Admin havaleyi onayladi",
            "AdminHavaleRejected" => "Admin havaleyi reddetti",
            "AdminRefundRequested" => "Admin iade baslatti",
            "AdminRefundSucceeded" => "Iade basarili",
            "AdminRefundFailed" => "Iade basarisiz",
            "AdminPaymentCancelled" => "Admin islemi iptal etti",
            "ProviderCallbackSucceeded" => "Provider callback basarili",
            "ProviderCallbackFailed" => "Provider callback basarisiz",
            "GatewayPaymentSucceeded" => "Gateway odemesi basarili",
            "GatewayPaymentFailed" => "Gateway odemesi basarisiz",
            _ => "Islem notu"
        };
    }

    public async Task<object> GetPaymentReconciliationAsync(DateTime? from = null, DateTime? to = null, string? provider = null)
    {
        var start = DateTime.SpecifyKind(from?.Date ?? DateTime.UtcNow.Date.AddDays(-30), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind((to?.Date ?? DateTime.UtcNow.Date).AddDays(1), DateTimeKind.Utc);
        if (end <= start) end = start.AddDays(1);

        var query = _db.PaymentTransactions
            .Include(t => t.Customer)
            .Where(t => t.CreatedAt >= start && t.CreatedAt < end);
        if (!string.IsNullOrWhiteSpace(provider))
            query = query.Where(t => t.Provider != null && t.Provider.ToLower() == provider.ToLower());

        var txs = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        var duplicateRefs = txs
            .Where(t => !string.IsNullOrWhiteSpace(t.ProviderTransactionId))
            .GroupBy(t => new { t.Provider, t.ProviderTransactionId })
            .Where(g => g.Count() > 1)
            .Select(g => new { g.Key.Provider, g.Key.ProviderTransactionId, Count = g.Count() })
            .ToList();

        var refundOriginalUids = txs
            .Select(t => TryReadNoteValue(t.Notes, "RefundOf:"))
            .Select(v => Guid.TryParse(v, out var parsed) ? parsed : (Guid?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Distinct()
            .ToList();
        var existingRefundOriginals = refundOriginalUids.Count == 0
            ? new HashSet<Guid>()
            : (await _db.PaymentTransactions
                .Where(t => refundOriginalUids.Contains(t.Uid))
                .Select(t => t.Uid)
                .ToListAsync()).ToHashSet();

        object Row(PaymentTransaction t) => new
        {
            t.Uid,
            t.PaymentTypeId,
            PaymentType = PaymentTypes.GetById(t.PaymentTypeId)?.Description,
            t.CustomerId,
            CustomerName = t.Customer?.Name,
            t.Amount,
            t.Currency,
            t.PaymentMethodId,
            PaymentMethod = BillingPaymentMethods.GetById(t.PaymentMethodId)?.Description,
            t.StatusId,
            Status = PaymentStatuses.GetById(t.StatusId)?.Description,
            t.Provider,
            t.ProviderTransactionId,
            t.ProviderPaymentId,
            t.ErrorMessage,
            t.CreatedAt,
            t.CompletedAt
        };

        var successfulWithoutProviderRef = txs
            .Where(t => t.StatusId == PaymentStatuses.Ids.Basarili
                     && t.PaymentMethodId != BillingPaymentMethods.Ids.Havale
                     && string.IsNullOrWhiteSpace(t.ProviderTransactionId)
                     && string.IsNullOrWhiteSpace(t.ProviderPaymentId))
            .Take(50)
            .Select(Row)
            .ToList();

        var pendingOlderThan24Hours = txs
            .Where(t => t.StatusId == PaymentStatuses.Ids.Beklemede && t.CreatedAt < DateTime.UtcNow.AddHours(-24))
            .Take(50)
            .Select(Row)
            .ToList();

        var failedWithoutError = txs
            .Where(t => t.StatusId == PaymentStatuses.Ids.Basarisiz && string.IsNullOrWhiteSpace(t.ErrorMessage))
            .Take(50)
            .Select(Row)
            .ToList();

        var refundsWithoutOriginal = txs
            .Where(t => t.Amount < 0
                     && Guid.TryParse(TryReadNoteValue(t.Notes, "RefundOf:"), out var originalUid)
                     && !existingRefundOriginals.Contains(originalUid))
            .Take(50)
            .Select(Row)
            .ToList();

        return new
        {
            period = new { from = start, to = end.AddTicks(-1), provider },
            totals = new
            {
                count = txs.Count,
                successfulAmount = txs.Where(t => t.StatusId == PaymentStatuses.Ids.Basarili && t.Amount > 0).Sum(t => t.Amount),
                pendingAmount = txs.Where(t => t.StatusId == PaymentStatuses.Ids.Beklemede && t.Amount > 0).Sum(t => t.Amount),
                failedAmount = txs.Where(t => t.StatusId == PaymentStatuses.Ids.Basarisiz && t.Amount > 0).Sum(t => t.Amount),
                refundedAmount = txs.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount))
            },
            byProvider = txs
                .GroupBy(t => string.IsNullOrWhiteSpace(t.Provider) ? "Unknown" : t.Provider!)
                .Select(g => new
                {
                    provider = g.Key,
                    count = g.Count(),
                    successfulAmount = g.Where(t => t.StatusId == PaymentStatuses.Ids.Basarili && t.Amount > 0).Sum(t => t.Amount),
                    refundAmount = g.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount))
                })
                .OrderByDescending(x => x.successfulAmount)
                .ToList(),
            byStatus = txs
                .GroupBy(t => t.StatusId)
                .Select(g => new
                {
                    statusId = g.Key,
                    status = PaymentStatuses.GetById(g.Key)?.Description,
                    count = g.Count(),
                    amount = g.Sum(t => t.Amount)
                })
                .OrderBy(x => x.statusId)
                .ToList(),
            anomalies = new
            {
                successfulWithoutProviderRef,
                pendingOlderThan24Hours,
                failedWithoutError,
                refundsWithoutOriginal,
                duplicateProviderReferences = duplicateRefs
            }
        };
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

    /// <summary>Salon musteri uyeligi icin Iyzico Checkout Form baslatir (3DS).</summary>
    public async Task<CheckoutFormResult> InitMembershipCheckoutAsync(
        int planId,
        int slnClientId,
        int platformUserId,
        string callbackUrl,
        string? slug = null,
        string? buyerIp = null)
    {
        var plan = await _db.SlnMembershipPlans
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
        if (plan == null) return CheckoutFormResult.Fail("Uyelik plani bulunamadi.");
        if (plan.Price <= 0) return CheckoutFormResult.Fail("Bu plan ucretsizdir.");

        var slnClient = await _db.SlnClients
            .FirstOrDefaultAsync(c => c.Id == slnClientId && c.CustomerId == plan.CustomerId);
        if (slnClient == null) return CheckoutFormResult.Fail("Musteri kaydi bulunamadi.");

        var hasActiveMembership = await _db.SlnClientMemberships
            .AnyAsync(m => m.CustomerId == plan.CustomerId && m.SlnClientId == slnClientId && m.StatusId == 1);
        if (hasActiveMembership) return CheckoutFormResult.Fail("Bu musterinin zaten aktif bir uyeligi var.");

        var config = await _db.PlatformPaymentConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (config == null) return CheckoutFormResult.Fail("Aktif odeme yapilandirmasi bulunamadi.");

        var notes = $"MembershipPlan:{planId}|SlnClient:{slnClientId}";
        if (!string.IsNullOrWhiteSpace(slug))
            notes += $"|Slug:{slug}";

        var tx = new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.UyelikOdemesi,
            CustomerId = plan.CustomerId,
            PlatformUserId = platformUserId,
            Amount = plan.Price,
            PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti,
            StatusId = PaymentStatuses.Ids.Beklemede,
            Provider = PaymentProviders.GetById(config.ProviderTypeId)?.SystemName ?? "Iyzico",
            Notes = notes
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        try
        {
            var gateway = _gatewayFactory.Create(config);
            if (gateway is not IyzicoGateway iyzicoGw)
                return CheckoutFormResult.Fail("Checkout form sadece Iyzico destekler.");

            var platformUser = await _db.PlatformUsers.FindAsync(platformUserId);
            var buyerName = !string.IsNullOrWhiteSpace(platformUser?.FullName)
                ? platformUser.FullName
                : slnClient.FullName;
            var buyerEmail = !string.IsNullOrWhiteSpace(platformUser?.Email)
                ? platformUser.Email
                : slnClient.Email ?? "noreply@corplynk.com";

            // PS.7 — Pazaryeri split (opsiyonel): salon sub-merchant kayitliysa komisyon ayrismasi yapilir.
            // Kayitli degilse direkt-merchant akisi devam eder (eski davranis, kullanicidan ek aksiyon istemez).
            var split = await GetMarketplaceSplitAsync(plan.CustomerId, plan.Price);

            var req = new CheckoutFormRequest
            {
                Amount = plan.Price,
                ConversationId = tx.Uid.ToString("N"),
                CallbackUrl = callbackUrl,
                BuyerId = platformUserId.ToString(),
                BuyerName = buyerName,
                BuyerEmail = buyerEmail,
                BuyerIp = buyerIp,
                Description = $"Salon musteri uyeligi - {plan.Name}",
                SubMerchantKey = split.UseSplit ? split.SubMerchantKey : null,
                SubMerchantPrice = split.UseSplit ? split.SubMerchantPrice : null
            };

            var result = await iyzicoGw.InitCheckoutFormAsync(req);
            if (result.Success)
            {
                tx.ProviderTransactionId = result.Token;
                if (split.UseSplit)
                    tx.Notes = AddNoteEvent(tx.Notes, "MarketplaceSplit", $"Komisyon={split.CommissionAmount:F2} ({split.CommissionPercent}%), SubMerchantPrice={split.SubMerchantPrice:F2}");
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

    private async Task ActivateSalonCustomerMembershipAfterPaymentAsync(PaymentTransaction tx)
    {
        if (tx.CustomerId == null) return;

        var planId = TryReadMembershipPlanId(tx.Notes);
        var slnClientId = TryReadMembershipSlnClientId(tx.Notes);
        if (planId == null || slnClientId == null) return;

        var plan = await _db.SlnMembershipPlans
            .FirstOrDefaultAsync(p => p.Id == planId.Value && p.CustomerId == tx.CustomerId.Value && p.IsActive);
        if (plan == null) return;

        var slnClient = await _db.SlnClients
            .FirstOrDefaultAsync(c => c.Id == slnClientId.Value && c.CustomerId == tx.CustomerId.Value);
        if (slnClient == null) return;

        var now = DateTime.UtcNow;
        var membership = await _db.SlnClientMemberships
            .FirstOrDefaultAsync(m => m.CustomerId == tx.CustomerId.Value && m.SlnClientId == slnClientId.Value && m.StatusId == 1);

        if (membership == null)
        {
            _db.SlnClientMemberships.Add(new SlnClientMembership
            {
                CustomerId = tx.CustomerId.Value,
                PlanId = plan.Id,
                SlnClientId = slnClient.Id,
                StartDate = now,
                CurrentPeriodStart = plan.DurationType == 1 ? now : null,
                CurrentPeriodEnd = plan.DurationType == 1 ? now.AddDays(plan.DurationDays) : null,
                EndDate = plan.DurationType == 1 ? now.AddDays(plan.DurationDays) : null,
                PaidAmount = tx.Amount,
                StatusId = 1
            });
        }
        else
        {
            membership.PlanId = plan.Id;
            membership.PaidAmount = Math.Max(membership.PaidAmount, tx.Amount);
            membership.StatusId = 1;
        }

        if (tx.PlatformUserId.HasValue)
        {
            var link = await _db.PlatformUserSalons
                .FirstOrDefaultAsync(s => s.PlatformUserId == tx.PlatformUserId.Value && s.CustomerId == tx.CustomerId.Value);

            if (link == null)
            {
                _db.PlatformUserSalons.Add(new PlatformUserSalon
                {
                    PlatformUserId = tx.PlatformUserId.Value,
                    CustomerId = tx.CustomerId.Value,
                    SlnClientId = slnClient.Id,
                    IsActive = true
                });
            }
            else
            {
                link.IsActive = true;
                link.SlnClientId ??= slnClient.Id;
            }
        }
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
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.PaymentTransactions.AsQueryable();
        if (customerId.HasValue) query = query.Where(t => t.CustomerId == customerId);
        if (platformUserId.HasValue) query = query.Where(t => t.PlatformUserId == platformUserId);
        return await query
            .Include(t => t.Customer)
            .Include(t => t.PlatformUser)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>Platform kullanicisi icin odeme dekontu (HTML).</summary>
    public async Task<(byte[]? Bytes, string? FileName, string? Error)> GetPlatformUserReceiptHtmlAsync(Guid transactionUid, int platformUserId)
    {
        var tx = await _db.PaymentTransactions
            .Include(t => t.Customer)
            .Include(t => t.PlatformUser)
            .FirstOrDefaultAsync(t => t.Uid == transactionUid);

        if (tx == null) return (null, null, "İşlem bulunamadı.");
        if (tx.PlatformUserId != platformUserId) return (null, null, "Bu dekont için yetkiniz yok.");
        if (tx.StatusId != PaymentStatuses.Ids.Basarili && tx.StatusId != PaymentStatuses.Ids.Iade)
            return (null, null, "Yalnızca tamamlanan veya iade edilmiş işlemler için dekont indirilebilir.");

        var pu = tx.PlatformUser;
        var encoder = HtmlEncoder.Default;
        string E(string? s) => encoder.Encode(s ?? string.Empty);

        var typeLabel = PaymentTypes.GetById(tx.PaymentTypeId)?.Description ?? "Ödeme";
        var statusLabel = PaymentStatuses.GetById(tx.StatusId)?.Description ?? string.Empty;
        var when = (tx.CompletedAt ?? tx.CreatedAt).ToUniversalTime().ToString("yyyy-MM-dd HH:mm") + " UTC";
        var salonLine = tx.Customer != null
            ? $"<tr><th>İşletme</th><td>{E(tx.Customer.Name)}</td></tr>"
            : string.Empty;
        var cardLine = !string.IsNullOrEmpty(tx.CardLastFour)
            ? $"<tr><th>Kart</th><td>**** **** **** {E(tx.CardLastFour)}</td></tr>"
            : string.Empty;
        var refLine = !string.IsNullOrEmpty(tx.ProviderTransactionId)
            ? $"<tr><th>Referans</th><td>{E(tx.ProviderTransactionId)}</td></tr>"
            : string.Empty;

        var html = $@"<!DOCTYPE html>
<html lang=""tr"">
<head>
<meta charset=""utf-8""/>
<meta name=""viewport"" content=""width=device-width, initial-scale=1""/>
<title>Ödeme Dekontu</title>
<style>
body {{ font-family: system-ui, Segoe UI, sans-serif; margin: 2rem; color: #222; }}
h1 {{ font-size: 1.25rem; margin-bottom: 0.25rem; }}
.sub {{ color: #666; font-size: 0.9rem; margin-bottom: 1.5rem; }}
table {{ border-collapse: collapse; width: 100%; max-width: 520px; }}
th, td {{ text-align: left; padding: 0.5rem 0; border-bottom: 1px solid #eee; }}
th {{ width: 40%; color: #555; font-weight: 600; }}
.amount {{ font-size: 1.35rem; font-weight: 700; }}
footer {{ margin-top: 2rem; font-size: 0.8rem; color: #888; }}
@@media print {{ body {{ margin: 1cm; }} }}
</style>
</head>
<body>
<p><strong>CorpLynk Salon</strong></p>
<h1>Ödeme dekontu</h1>
<p class=""sub"">Bu belge bilgilendirme amaçlıdır; mali mevzuata uygun fatura yerine geçmez. Yazdır &gt; PDF olarak kaydedebilirsiniz.</p>
<table>
<tr><th>İşlem no</th><td>{tx.Uid}</td></tr>
<tr><th>Tarih</th><td>{when}</td></tr>
<tr><th>Ödeme türü</th><td>{E(typeLabel)}</td></tr>
{salonLine}
<tr><th>Ödeyen</th><td>{E(pu?.FullName)} — {E(pu?.Phone)}</td></tr>
<tr><th>Durum</th><td>{E(statusLabel)}</td></tr>
<tr><th>Tutar</th><td class=""amount"">{tx.Amount.ToString("N2")} {E(tx.Currency)}</td></tr>
{cardLine}
<tr><th>Ödeme altyapısı</th><td>{E(tx.Provider)}</td></tr>
{refLine}
</table>
<footer>© CorpLynk</footer>
</body>
</html>";

        var bytes = Encoding.UTF8.GetBytes(html);
        var fileName = $"corpLynk-dekont-{tx.Uid:N}.html";
        return (bytes, fileName, null);
    }

    /// <summary>Salon/management kullanicisi icin odeme dekontu (HTML).</summary>
    public async Task<(byte[]? Bytes, string? FileName, string? Error)> GetCustomerReceiptHtmlAsync(
        Guid transactionUid,
        int customerId,
        bool allowAnyCustomer = false)
    {
        var tx = await _db.PaymentTransactions
            .Include(t => t.Customer)
            .Include(t => t.PlatformUser)
            .FirstOrDefaultAsync(t => t.Uid == transactionUid);

        if (tx == null) return (null, null, "Islem bulunamadi.");
        if (!allowAnyCustomer && tx.CustomerId != customerId)
            return (null, null, "Bu dekont icin yetkiniz yok.");
        if (tx.StatusId != PaymentStatuses.Ids.Basarili && tx.StatusId != PaymentStatuses.Ids.Iade)
            return (null, null, "Yalnizca tamamlanan veya iade edilmis islemler icin dekont indirilebilir.");

        var encoder = HtmlEncoder.Default;
        string E(string? s) => encoder.Encode(s ?? string.Empty);

        var typeLabel = PaymentTypes.GetById(tx.PaymentTypeId)?.Description ?? "Odeme";
        var statusLabel = PaymentStatuses.GetById(tx.StatusId)?.Description ?? string.Empty;
        var methodLabel = BillingPaymentMethods.GetById(tx.PaymentMethodId)?.Description ?? string.Empty;
        var when = (tx.CompletedAt ?? tx.CreatedAt).ToUniversalTime().ToString("yyyy-MM-dd HH:mm") + " UTC";
        var salonLine = tx.Customer != null
            ? $"<tr><th>Isletme</th><td>{E(tx.Customer.Name)}</td></tr>"
            : string.Empty;
        var payerText = tx.PlatformUser != null
            ? $"{tx.PlatformUser.FullName} - {tx.PlatformUser.Phone}"
            : tx.Customer?.Name ?? string.Empty;
        var cardLine = !string.IsNullOrEmpty(tx.CardLastFour)
            ? $"<tr><th>Kart</th><td>**** **** **** {E(tx.CardLastFour)}</td></tr>"
            : string.Empty;
        var refLine = !string.IsNullOrEmpty(tx.ProviderTransactionId)
            ? $"<tr><th>Referans</th><td>{E(tx.ProviderTransactionId)}</td></tr>"
            : string.Empty;
        var paymentIdLine = !string.IsNullOrEmpty(tx.ProviderPaymentId)
            ? $"<tr><th>Provider odeme ID</th><td>{E(tx.ProviderPaymentId)}</td></tr>"
            : string.Empty;

        var html = $@"<!DOCTYPE html>
<html lang=""tr"">
<head>
<meta charset=""utf-8""/>
<meta name=""viewport"" content=""width=device-width, initial-scale=1""/>
<title>Odeme Dekontu</title>
<style>
body {{ font-family: system-ui, Segoe UI, sans-serif; margin: 2rem; color: #222; }}
h1 {{ font-size: 1.25rem; margin-bottom: 0.25rem; }}
.sub {{ color: #666; font-size: 0.9rem; margin-bottom: 1.5rem; }}
table {{ border-collapse: collapse; width: 100%; max-width: 560px; }}
th, td {{ text-align: left; padding: 0.5rem 0; border-bottom: 1px solid #eee; }}
th {{ width: 40%; color: #555; font-weight: 600; }}
.amount {{ font-size: 1.35rem; font-weight: 700; }}
footer {{ margin-top: 2rem; font-size: 0.8rem; color: #888; }}
@media print {{ body {{ margin: 1cm; }} }}
</style>
</head>
<body>
<p><strong>CorpLynk Salon</strong></p>
<h1>Odeme dekontu</h1>
<p class=""sub"">Bu belge bilgilendirme amaclidir; mali mevzuata uygun fatura yerine gecmez. Yazdir &gt; PDF olarak kaydedebilirsiniz.</p>
<table>
<tr><th>Islem no</th><td>{tx.Uid}</td></tr>
<tr><th>Tarih</th><td>{when}</td></tr>
<tr><th>Odeme turu</th><td>{E(typeLabel)}</td></tr>
{salonLine}
<tr><th>Odeyen</th><td>{E(payerText)}</td></tr>
<tr><th>Durum</th><td>{E(statusLabel)}</td></tr>
<tr><th>Tutar</th><td class=""amount"">{tx.Amount.ToString("N2")} {E(tx.Currency)}</td></tr>
<tr><th>Odeme yontemi</th><td>{E(methodLabel)}</td></tr>
{cardLine}
<tr><th>Odeme altyapisi</th><td>{E(tx.Provider)}</td></tr>
{refLine}
{paymentIdLine}
</table>
<footer>CorpLynk</footer>
</body>
</html>";

        var bytes = Encoding.UTF8.GetBytes(html);
        var fileName = $"corpLynk-dekont-{tx.Uid:N}.html";
        return (bytes, fileName, null);
    }

    public async Task<object?> GetPackagePreviewAsync(int customerId, int packageGroupId)
    {
        var group = SalonModuleGroups.GetById(packageGroupId);
        if (group == null) return null;

        var moduleIds = SalonModuleGroups.GetModuleIds(packageGroupId);
        if (moduleIds.Count == 0) return new { error = "Bu paket henuz satin almaya acik degil." };

        var activeIds = await _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId && m.IsActive && moduleIds.Contains(m.ModuleId))
            .Select(m => m.ModuleId)
            .ToListAsync();
        if (moduleIds.All(activeIds.Contains)) return new { error = "Bu paket zaten aktif." };

        var monthlyPrice = await GetActivePackagePriceAsync(packageGroupId) ?? group.MonthlyPrice;

        var nextBilling = await _subscriptionFactory.GetNextSalonAccrualUtcAsync(customerId) ?? DateTime.UtcNow.AddDays(30);

        var daysUntilBilling = Math.Max(1, (int)Math.Ceiling((nextBilling - DateTime.UtcNow).TotalDays));
        if (daysUntilBilling > 30) daysUntilBilling = 30;
        var dailyRate = monthlyPrice / 30m;
        var proRataAmount = Math.Round(dailyRate * daysUntilBilling, 2);

        return new
        {
            packageGroupId = group.Id,
            packageName = group.Description,
            monthlyPrice,
            recurringMonthlyAmount = monthlyPrice,
            currency = "TRY",
            taxIncluded = true,
            proRata = new { days = daysUntilBilling, dailyRate = Math.Round(dailyRate, 2), amount = proRataAmount },
            nextBillingDate = nextBilling.ToString("yyyy-MM-dd"),
            totalNow = proRataAmount,
            billingNote = "Fiyatlar KDV dahil aylik paket tutaridir. Bugun yalniz sonraki tahakkuk tarihine kadar olan kist donem tahsil edilir."
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
        if (item == null || item.MonthlyPrice <= 0m) return null;

        var group = SalonModuleGroups.GetById(packageGroupId);
        if (item.MonthlyPrice == 20m && group != null && group.MonthlyPrice != 20m)
            return null;

        return item.MonthlyPrice;
    }

    public async Task<CheckoutFormResult> InitPackageCheckoutAsync(int customerId, int packageGroupId, string callbackUrl, string? buyerIp = null)
    {
        var group = SalonModuleGroups.GetById(packageGroupId);
        if (group == null) return CheckoutFormResult.Fail("Paket bulunamadi.");

        var moduleIds = SalonModuleGroups.GetModuleIds(packageGroupId);
        if (moduleIds.Count == 0) return CheckoutFormResult.Fail("Bu paket henuz satin almaya acik degil.");

        var activeIds = await _db.CustomerPortalModules
            .Where(m => m.CustomerId == customerId && m.IsActive && moduleIds.Contains(m.ModuleId))
            .Select(m => m.ModuleId)
            .ToListAsync();
        if (moduleIds.All(activeIds.Contains)) return CheckoutFormResult.Fail("Bu paket zaten aktif.");

        var monthlyPrice = await GetActivePackagePriceAsync(packageGroupId) ?? group.MonthlyPrice;

        var subscription = await _db.CustomerSubscriptions
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.StatusId == 1);
        var nextBilling = await _subscriptionFactory.GetNextSalonAccrualUtcAsync(customerId) ?? DateTime.UtcNow.AddDays(30);
        var daysUntilBilling = Math.Max(1, (int)Math.Ceiling((nextBilling - DateTime.UtcNow).TotalDays));
        if (daysUntilBilling > 30) daysUntilBilling = 30;
        var proRataAmount = Math.Round(monthlyPrice / 30m * daysUntilBilling, 2);

        if (proRataAmount <= 0) return CheckoutFormResult.Fail("Odenecek tutar 0.");

        var config = await _db.PlatformPaymentConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (config == null) return CheckoutFormResult.Fail("Aktif odeme yapilandirmasi bulunamadi.");

        await CancelPendingCheckoutAttemptsAsync(customerId, PaymentTypes.Ids.ModulSatinAlma, packageGroupId: packageGroupId);

        var tx = new PaymentTransaction
        {
            PaymentTypeId = PaymentTypes.Ids.ModulSatinAlma,
            CustomerId = customerId,
            ModuleId = null,
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
        var resolved = await _subscriptionFactory.TryResolveSalonSubscriptionPaymentAsync(customerId);
        if (resolved == null) return CheckoutFormResult.Fail("Odenmemis donem bulunamadi.");

        var subscription = await _db.CustomerSubscriptions
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.StatusId == 1);
        var customer = subscription?.Customer ?? await _db.Customers.FindAsync(customerId);
        if (customer == null) return CheckoutFormResult.Fail("Musteri bulunamadi.");

        var unpaidPeriod = resolved.Value.Period;
        var amount = resolved.Value.PayAmount;

        var config = await _db.PlatformPaymentConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (config == null) return CheckoutFormResult.Fail("Aktif odeme yapilandirmasi bulunamadi.");

        await CancelPendingCheckoutAttemptsAsync(customerId, PaymentTypes.Ids.PlatformAbonelik, billingPeriodId: unpaidPeriod.Id);

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
            tx.Notes = AddNoteEvent(tx.Notes, "ProviderCallbackSucceeded", "Checkout callback dogrulamasi basarili.");

            if (tx.BillingPeriodId.HasValue)
            {
                var period = await _db.CustomerBillingPeriods.FindAsync(tx.BillingPeriodId.Value);
                if (period != null)
                {
                    period.StatusId = BillingPeriodStatuses.Ids.Paid;
                    period.IsPaid = true;
                    period.PaidAt = DateTime.UtcNow;
                    period.PaymentMethodId = BillingPaymentMethods.Ids.KrediKarti;
                    await ActivateSalonSubscriptionAfterPlatformPaymentAsync(period, tx.Amount);
                }
            }

            // Paket satin alma ise modulleri aktif et
            if (tx.PaymentTypeId == PaymentTypes.Ids.ModulSatinAlma && TryReadPackageGroupId(tx.Notes) is int pgId)
            {
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

            if (tx.PaymentTypeId == PaymentTypes.Ids.UyelikOdemesi && TryReadMembershipPlanId(tx.Notes).HasValue)
                await ActivateSalonCustomerMembershipAfterPaymentAsync(tx);

            // Randevu depozitosu ise randevuyu onayli yap (StatusId 6 = AwaitingPayment → 2 = Confirmed)
            if (tx.PaymentTypeId == PaymentTypes.Ids.RandevuOnOdemesi && tx.Notes?.StartsWith("Appointment:") == true)
            {
                var parts = tx.Notes.Split('|');
                if (parts.Length > 0 && int.TryParse(parts[0].Replace("Appointment:", ""), out var aptId))
                {
                    var apt = await _db.SlnAppointments
                        .Include(a => a.SlnClient)
                        .FirstOrDefaultAsync(a => a.Id == aptId);
                    if (apt != null && apt.StatusId == 6)
                    {
                        apt.StatusId = 2; // Confirmed — depozito ödendiyse onay gerekmez
                        apt.IsPrepaid = true;
                        apt.PrepaidAmount = tx.Amount;
                        apt.PaymentTransactionId = tx.Id;
                    }

                    if (tx.PlatformUserId == null && apt?.SlnClient?.Phone != null)
                    {
                        var norm = PhoneHelper.Normalize(apt.SlnClient.Phone);
                        if (!string.IsNullOrEmpty(norm))
                        {
                            var pu = await _db.PlatformUsers.FirstOrDefaultAsync(u => u.Phone == norm);
                            if (pu != null) tx.PlatformUserId = pu.Id;
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();
            if (tx.PaymentTypeId == PaymentTypes.Ids.ModulSatinAlma && tx.CustomerId.HasValue)
                await RefreshSalonSubscriptionDisplayMonthlySafeAsync(tx.CustomerId.Value);
            _logger.LogInformation("Checkout odeme basarili: TxUid={TxUid}, PaymentId={PaymentId}", tx.Uid, verifyResult.ProviderPaymentId);
            return PaymentResult.Ok(tx.Uid, verifyResult.ProviderTransactionId);
        }
        else
        {
            tx.StatusId = PaymentStatuses.Ids.Basarisiz;
            tx.ErrorMessage = verifyResult.Error;
            tx.CompletedAt = DateTime.UtcNow;
            tx.Notes = AddNoteEvent(tx.Notes, "ProviderCallbackFailed", verifyResult.Error ?? "Checkout dogrulamasi basarisiz.");
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

            // PS.6 — Pazaryeri split (opsiyonel): salon sub-merchant kayitliysa komisyon ayrisir, yoksa direkt-merchant.
            var split = await GetMarketplaceSplitAsync(customerId, amount);

            var req = new CheckoutFormRequest
            {
                Amount = amount,
                ConversationId = tx.Uid.ToString("N"),
                CallbackUrl = callbackUrl,
                BuyerId = $"apt-{appointmentId}",
                BuyerName = string.IsNullOrWhiteSpace(buyerFullName) ? "Musteri" : buyerFullName,
                BuyerEmail = string.IsNullOrWhiteSpace(buyerEmail) ? "noreply@corplynk.com" : buyerEmail,
                BuyerIp = buyerIp,
                Description = $"Randevu Depozitosu - {amount:N2} TL",
                SubMerchantKey = split.UseSplit ? split.SubMerchantKey : null,
                SubMerchantPrice = split.UseSplit ? split.SubMerchantPrice : null
            };

            var result = await iyzicoGw.InitCheckoutFormAsync(req);
            if (result.Success)
            {
                tx.ProviderTransactionId = result.Token;
                if (split.UseSplit)
                    tx.Notes = AddNoteEvent(tx.Notes, "MarketplaceSplit", $"Komisyon={split.CommissionAmount:F2} ({split.CommissionPercent}%), SubMerchantPrice={split.SubMerchantPrice:F2}");
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

    /// <summary>Randevu depozitosu iadesi. PaymentTransaction.ProviderPaymentId ile Iyzico'ya iade isteği gönderir.</summary>
    public async Task<(bool Success, string? Error)> RefundAppointmentDepositAsync(int appointmentId)
    {
        var apt = await _db.SlnAppointments.FindAsync(appointmentId);
        if (apt == null) return (false, "Randevu bulunamadi.");
        if (!apt.IsPrepaid || apt.PrepaidAmount <= 0) return (true, null); // Depozito yok, iade gerekmez

        var tx = apt.PaymentTransactionId.HasValue
            ? await _db.PaymentTransactions.FindAsync(apt.PaymentTransactionId.Value)
            : await _db.PaymentTransactions
                .Where(t => t.Notes != null && t.Notes.StartsWith($"Appointment:{appointmentId}|")
                         && t.StatusId == PaymentStatuses.Ids.Basarili)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

        if (tx == null) return (false, "Odeme kaydi bulunamadi.");

        var providerPaymentId = tx.ProviderPaymentId;
        if (string.IsNullOrEmpty(providerPaymentId))
            return (false, "Odeme saglayici referansi eksik, iade yapilamadi.");

        var config = await _db.PlatformPaymentConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (config == null) return (false, "Aktif odeme yapilandirmasi bulunamadi.");

        var gateway = _gatewayFactory.Create(config);
        var refundResult = await gateway.RefundAsync(providerPaymentId, apt.PrepaidAmount);

        if (refundResult.Success)
        {
            apt.DepositRefunded = true;
            var refundTx = new PaymentTransaction
            {
                PaymentTypeId = PaymentTypes.Ids.RandevuOnOdemesi,
                CustomerId = apt.CustomerId,
                Amount = -apt.PrepaidAmount,
                PaymentMethodId = tx.PaymentMethodId,
                StatusId = PaymentStatuses.Ids.Basarili,
                Provider = tx.Provider,
                ProviderTransactionId = refundResult.RefundId,
                ProviderPaymentId = tx.ProviderPaymentId,
                CompletedAt = DateTime.UtcNow,
                Notes = AddNoteEvent($"Refund:Appointment:{appointmentId}|RefundOf:{tx.Uid}", "AdminRefundSucceeded",
                    $"{apt.PrepaidAmount:N2} {tx.Currency} randevu depozitosu iade edildi.")
            };
            tx.Notes = AddNoteEvent(tx.Notes, "AdminRefundRequested",
                $"{apt.PrepaidAmount:N2} {tx.Currency} randevu depozitosu iade edildi.");
            _db.PaymentTransactions.Add(refundTx);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Depozito iadesi basarili: AptId={AptId}, Tutar={Amount}", appointmentId, apt.PrepaidAmount);
        }

        return (refundResult.Success, refundResult.Success ? null : refundResult.Error);
    }

    // ─── Private: Gateway uzerinden odeme calistir ───

    private async Task<PaymentResult> ExecutePaymentAsync(PaymentTransaction tx, PaymentCardInfo card, string? buyerIp = null, string? subMerchantKey = null, decimal? subMerchantPrice = null)
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
                Description = $"Odeme #{tx.Uid:N}",
                SubMerchantKey = subMerchantKey,
                SubMerchantPrice = subMerchantPrice
            };

            var result = await gateway.InitiatePaymentAsync(request);

            if (result.Success && result.IsCompleted)
            {
                tx.StatusId = PaymentStatuses.Ids.Basarili;
                tx.ProviderTransactionId = result.ProviderTransactionId;
                tx.ProviderPaymentId = result.ProviderPaymentId;
                tx.CompletedAt = DateTime.UtcNow;
                tx.Notes = AddNoteEvent(tx.Notes, "GatewayPaymentSucceeded", "Kart odemesi gateway tarafinda tamamlandi.");
                _db.PaymentTransactions.Add(tx);
                return PaymentResult.Ok(tx.Uid, tx.ProviderTransactionId);
            }
            else
            {
                tx.StatusId = PaymentStatuses.Ids.Basarisiz;
                tx.ErrorMessage = result.Error ?? "Odeme basarisiz.";
                tx.Notes = AddNoteEvent(tx.Notes, "GatewayPaymentFailed", tx.ErrorMessage);
                _db.PaymentTransactions.Add(tx);
                return PaymentResult.Fail(tx.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            tx.StatusId = PaymentStatuses.Ids.Basarisiz;
            tx.ErrorMessage = $"Gateway hatasi: {ex.Message}";
            tx.Notes = AddNoteEvent(tx.Notes, "GatewayPaymentFailed", tx.ErrorMessage);
            _db.PaymentTransactions.Add(tx);
            _logger.LogError(ex, "Odeme gateway hatasi: TxUid={TxUid}", tx.Uid);
            return PaymentResult.Fail(tx.ErrorMessage);
        }
    }

    /// <summary>
    /// PS.6/PS.7 yardimcisi: salonun sub-merchant onboarding durumunu ve komisyon hesabini doner.
    /// Pazaryeri kaydi opsiyonel — yoksa UseSplit=false, eski direkt-merchant akisi calisir.
    /// requireSplit=true verilirse (mobil tahsilat senaryosu) onboarding eksikse Success=false hata doner.
    /// </summary>
    public async Task<MarketplaceSplitContext> GetMarketplaceSplitAsync(int customerId, decimal grossAmount, bool requireSplit = false)
    {
        var profile = await _db.Set<SlnSalonProfile>()
            .FirstOrDefaultAsync(p => p.CustomerId == customerId);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId);

        var hasSubMerchant = profile != null
            && !string.IsNullOrEmpty(profile.IyzicoSubMerchantKey)
            && profile.IyzicoOnboardingStatus == 2;

        if (!hasSubMerchant)
        {
            if (requireSplit)
            {
                return new MarketplaceSplitContext
                {
                    Success = false,
                    UseSplit = false,
                    Error = "Online tahsilat icin once Odeme Bilgileri sayfasindan iyzico Pazaryeri kaydinizi tamamlayin."
                };
            }
            // Pazaryeri yok → eski direkt-merchant akisi (corplynk merchant'a gider, salona manuel havale).
            return new MarketplaceSplitContext { Success = true, UseSplit = false };
        }

        var commissionPct = customer?.MarketplaceCommissionPercent ?? 5m;
        var commission = Math.Round(grossAmount * commissionPct / 100m, 2, MidpointRounding.AwayFromZero);
        var subMerchantPrice = Math.Round(grossAmount - commission, 2, MidpointRounding.AwayFromZero);
        if (subMerchantPrice < 0m) subMerchantPrice = 0m;

        return new MarketplaceSplitContext
        {
            Success = true,
            UseSplit = true,
            SubMerchantKey = profile!.IyzicoSubMerchantKey,
            SubMerchantPrice = subMerchantPrice,
            CommissionAmount = commission,
            CommissionPercent = commissionPct
        };
    }

    /// <summary>
    /// PS.4 yardimcisi: salon onboarding'de iyzico'ya gonderilecek genel bilgileri (firma adi/email/telefon/adres) toplar.
    /// </summary>
    public async Task<SalonOnboardingContext?> GetCustomerOnboardingContextAsync(int customerId)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null) return null;

        var hqBranch = await _db.Set<SlnBranch>()
            .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);

        var profile = await _db.Set<SlnSalonProfile>()
            .FirstOrDefaultAsync(p => p.CustomerId == customerId);

        return new SalonOnboardingContext
        {
            Name = customer.Name,
            Email = customer.Email ?? hqBranch?.Email ?? profile?.Email ?? "noreply@corplynk.com",
            Phone = customer.Phone ?? hqBranch?.Phone ?? profile?.Phone ?? "",
            Address = hqBranch?.Address ?? profile?.Address ?? customer.Address ?? "Türkiye"
        };
    }

    /// <summary>
    /// PS.3 — Salonu iyzico Pazaryeri sub-merchant olarak kaydeder ve sonucu SlnSalonProfile'a yazar.
    /// </summary>
    public async Task<(bool Success, string? Error, string? SubMerchantKey)> OnboardSubMerchantAsync(int customerId, IyzicoSubMerchantOnboardRequest req)
    {
        var profile = await _db.Set<SlnSalonProfile>()
            .FirstOrDefaultAsync(p => p.CustomerId == customerId);
        if (profile == null)
            return (false, "Salon profili bulunamadı.", null);

        // Aktif iyzico config'i bul (Pazaryeri ayni hesaba bagli)
        var config = await _db.Set<PlatformPaymentConfig>()
            .Where(c => c.IsActive && c.ProviderTypeId == PaymentProviders.Ids.Iyzico)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();
        if (config == null)
            return (false, "Aktif iyzico ödeme yapılandırması bulunamadı (admin tanımlamalı).", null);

        var gateway = _gatewayFactory.Create(config) as IyzicoGateway;
        if (gateway == null)
            return (false, "iyzico gateway oluşturulamadı.", null);

        // External ID: corplynk customerId — idempotent
        if (string.IsNullOrEmpty(req.SubMerchantExternalId))
            req.SubMerchantExternalId = $"corplynk-salon-{customerId}";

        var result = await gateway.CreateSubMerchantAsync(req);
        if (!result.Success)
        {
            profile.IyzicoOnboardingStatus = 3; // Rejected
            profile.IyzicoOnboardingError = result.Error;
            await _db.SaveChangesAsync();
            return (false, result.Error, null);
        }

        // Profile'a yaz
        profile.IyzicoSubMerchantKey = result.SubMerchantKey;
        profile.IyzicoSubMerchantType = req.SubMerchantType;
        profile.IyzicoIban = req.Iban;
        profile.IyzicoLegalCompanyTitle = req.LegalCompanyTitle;
        profile.IyzicoTaxOffice = req.TaxOffice;
        profile.IyzicoTaxNumber = req.TaxNumber;
        profile.IyzicoIdentityNumber = req.IdentityNumber;
        profile.IyzicoContactName = req.ContactName;
        profile.IyzicoContactSurname = req.ContactSurname;
        profile.IyzicoOnboardingStatus = 2; // Approved (iyzico key dondurdu = aktif)
        profile.IyzicoOnboardedAt = DateTime.UtcNow;
        profile.IyzicoOnboardingError = null;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null, result.SubMerchantKey);
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

public class SalonOnboardingContext
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class MarketplaceSplitContext
{
    /// <summary>Akis devam etsin mi? false ise hata mesaji vardir (sadece requireSplit=true durumunda).</summary>
    public bool Success { get; set; }
    /// <summary>true ise CheckoutFormRequest'e SubMerchantKey/Price doldur; false ise direkt-merchant akisi.</summary>
    public bool UseSplit { get; set; }
    public string? Error { get; set; }
    public string? SubMerchantKey { get; set; }
    public decimal SubMerchantPrice { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CommissionPercent { get; set; }
}
