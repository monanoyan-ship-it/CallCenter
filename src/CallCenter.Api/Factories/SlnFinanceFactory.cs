using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services.Interfaces;
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
    private readonly ISlnStockMovementEntityService _stockMovements;
    private readonly ISlnPersonnelCommissionEntityService _personnelCommissions;
    private readonly ISlnCashClosingEntityService _cashClosings;
    private readonly ISlnCashOpeningEntityService _cashOpenings;
    private readonly ISlnClientLedgerEntityService _clientLedgers;
    private readonly ISlnInvoiceRefundEntityService _invoiceRefunds;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnBranchEntityService _branches;
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnAppointmentFactory _appointmentFactory;
    private readonly ISlnMembershipFactory _memberships;
    private readonly ISlnServiceSessionFactory _serviceSessions;
    private readonly ISlnLoyaltyPackageFactory _loyaltyPackages;
    private readonly ISlnLoyaltyProgramFactory _loyaltyPrograms;
    private readonly ISlnLoyaltyFactory _loyaltyPoints;
    private readonly ISlnGiftCardFactory _giftCards;
    private readonly ISlnStockBalanceService _stockBalances;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnFinanceFactory> _logger;
    private const int GiftCardPaymentMethodId = 5;

    public SlnFinanceFactory(
        ISlnInvoiceEntityService invoices,
        ISlnInvoiceItemEntityService invoiceItems,
        ISlnCashRegisterEntityService cashRegisters,
        ISlnCashTransactionEntityService cashTransactions,
        ISlnExpenseCategoryEntityService expenseCategories,
        ISlnExpenseEntityService expenses,
        ISlnProductEntityService products,
        ISlnStockMovementEntityService stockMovements,
        ISlnPersonnelCommissionEntityService personnelCommissions,
        ISlnCashClosingEntityService cashClosings,
        ISlnCashOpeningEntityService cashOpenings,
        ISlnClientLedgerEntityService clientLedgers,
        ISlnInvoiceRefundEntityService invoiceRefunds,
        ISlnClientEntityService clients,
        ISlnBranchEntityService branches,
        ISlnAppointmentEntityService appointments,
        ISlnAppointmentFactory appointmentFactory,
        ISlnMembershipFactory memberships,
        ISlnServiceSessionFactory serviceSessions,
        ISlnLoyaltyPackageFactory loyaltyPackages,
        ISlnLoyaltyProgramFactory loyaltyPrograms,
        ISlnLoyaltyFactory loyaltyPoints,
        ISlnGiftCardFactory giftCards,
        ISlnStockBalanceService stockBalances,
        IUnitOfWork uow,
        ILogger<SlnFinanceFactory> logger)
    {
        _invoices = invoices;
        _invoiceItems = invoiceItems;
        _cashRegisters = cashRegisters;
        _cashTransactions = cashTransactions;
        _expenseCategories = expenseCategories;
        _expenses = expenses;
        _products = products;
        _stockMovements = stockMovements;
        _personnelCommissions = personnelCommissions;
        _cashClosings = cashClosings;
        _cashOpenings = cashOpenings;
        _clientLedgers = clientLedgers;
        _invoiceRefunds = invoiceRefunds;
        _clients = clients;
        _branches = branches;
        _appointments = appointments;
        _appointmentFactory = appointmentFactory;
        _memberships = memberships;
        _serviceSessions = serviceSessions;
        _loyaltyPackages = loyaltyPackages;
        _loyaltyPrograms = loyaltyPrograms;
        _loyaltyPoints = loyaltyPoints;
        _giftCards = giftCards;
        _stockBalances = stockBalances;
        _uow = uow;
        _logger = logger;
    }

    // ═══ Adisyon (Invoice) ═══

    public async Task<List<SlnInvoiceDto>> GetInvoicesAsync(
        int customerId,
        DateTime? from,
        DateTime? to,
        int? statusId = null,
        int? branchId = null,
        int? slnClientId = null)
    {
        var query = _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId);

        if (branchId.HasValue)
            query = query.Where(i => i.BranchId == branchId.Value);

        if (slnClientId.HasValue)
            query = query.Where(i => i.SlnClientId == slnClientId.Value);

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            query = query.Where(i => i.InvoiceDate >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            query = query.Where(i => i.InvoiceDate <= toUtc);
        }

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

    public async Task<SlnInvoiceDto?> GetInvoiceAsync(int invoiceId, int customerId, int? branchId = null)
    {
        var query = _invoices.GetAllQueryable()
            .Where(i => i.Id == invoiceId && i.CustomerId == customerId);

        if (branchId.HasValue)
            query = query.Where(i => i.BranchId == branchId.Value);

        var invoice = await query
            .Include(i => i.SlnClient)
            .Include(i => i.Personnel).ThenInclude(p => p!.User)
            .Include(i => i.Items).ThenInclude(it => it.Service)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .Include(i => i.Items).ThenInclude(it => it.Personnel).ThenInclude(p => p!.User)
            .FirstOrDefaultAsync();

        return invoice != null ? MapInvoiceToDto(invoice) : null;
    }

    public async Task<(SlnInvoiceDto? Invoice, string? Error)> CreateInvoiceAsync(SlnInvoiceCreateDto dto, int userId, int customerId, int? branchId = null)
    {
        if (dto.Items.Count == 0)
            return (null, "Adisyonda en az bir kalem olmali");

        SlnAppointment? linkedAppointment = null;
        if (dto.SlnAppointmentId.HasValue)
        {
            linkedAppointment = await _appointments.GetAllQueryable()
                .FirstOrDefaultAsync(a => a.Id == dto.SlnAppointmentId.Value && a.CustomerId == customerId);
            if (linkedAppointment == null)
                return (null, "Randevu bulunamadi");
            if (linkedAppointment.StatusId == 4 || linkedAppointment.StatusId == 5)
                return (null, "Iptal veya gelmedi durumundaki randevu adisyona baglanamaz");
            if (branchId.HasValue && linkedAppointment.BranchId.HasValue && linkedAppointment.BranchId.Value != branchId.Value)
                return (null, "Randevu bu sube icin uygun degil");
            if (dto.SlnClientId.HasValue && dto.SlnClientId.Value != linkedAppointment.SlnClientId)
                return (null, "Randevu musterisi ile adisyon musterisi eslesmiyor");

            dto.SlnClientId ??= linkedAppointment.SlnClientId;
            branchId ??= linkedAppointment.BranchId;
            if (dto.PrepaidAmount <= 0 && linkedAppointment.IsPrepaid && linkedAppointment.PrepaidAmount > 0)
                dto.PrepaidAmount = linkedAppointment.PrepaidAmount;
            if (dto.PrepaidAmount > 0 && (!linkedAppointment.IsPrepaid || linkedAppointment.PrepaidAmount <= 0))
                return (null, "Randevuda kayitli on odeme bulunamadi");
            if (dto.PrepaidAmount > linkedAppointment.PrepaidAmount)
                return (null, "Adisyon on odemesi randevuda kayitli on odemeyi asamaz");

            var existingInvoice = await _invoices.GetAllQueryable()
                .FirstOrDefaultAsync(i => i.CustomerId == customerId
                    && i.SlnAppointmentId == dto.SlnAppointmentId.Value
                    && i.StatusId != 3);
            if (existingInvoice != null)
                return (null, $"Bu randevu icin adisyon zaten olusturulmus: {existingInvoice.InvoiceNo}");
        }
        else if (dto.PrepaidAmount > 0)
        {
            return (null, "On odeme sadece randevuya bagli adisyonda kullanilabilir");
        }

        // Fatura numarasi olustur
        var today = DateTime.UtcNow;
        var todayCount = await _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId && i.InvoiceDate.Date == today.Date)
            .CountAsync();

        var invoiceNo = $"SLN-{today:yyyyMMdd}-{(todayCount + 1):D4}";

        var invoice = new SlnInvoice
        {
            CustomerId = customerId,
            BranchId = branchId,
            SlnClientId = dto.SlnClientId,
            SlnAppointmentId = dto.SlnAppointmentId,
            InvoiceNo = invoiceNo,
            InvoiceDate = today,
            PaymentMethodId = dto.PaymentMethodId,
            PosDeviceId = dto.PosDeviceId,
            PersonnelId = userId > 0 ? userId : null, // userId = CustomerPersonnelId
            DiscountAmount = dto.DiscountAmount,
            PrepaidAmount = dto.PrepaidAmount,
            TipAmount = dto.TipAmount,
            Notes = dto.Notes
        };

        decimal totalAmount = 0;
        var items = new List<SlnInvoiceItem>();
        var membershipUsageRecords = new List<(int MembershipId, int ServiceId)>();
        var packageUsageRecords = new List<(int ClientPackageId, int ServiceId, SlnInvoiceItem Item)>();
        var serviceSessionSaleItems = new List<SlnInvoiceItem>();
        var membershipBenefitLookup = new Dictionary<int, ServiceMembershipBenefit>();
        var packageBenefitLookup = new Dictionary<int, SlnLoyaltyPackageBenefitDto>();

        if (dto.Items.Any(i => i.UseMembershipBenefit && i.UsePackageSession))
            return (null, "Ayni kalemde uyelik hakki ve paket seansi birlikte kullanilamaz");

        var membershipBenefitItems = dto.Items
            .Where(i => i.UseMembershipBenefit)
            .ToList();
        var packageSessionItems = dto.Items
            .Where(i => i.UsePackageSession)
            .ToList();

        if (packageSessionItems.Count > 0)
        {
            if (!dto.SlnClientId.HasValue)
                return (null, "Paket seansi kullanimi icin musteri secilmelidir");

            if (packageSessionItems.Any(i => !i.ServiceId.HasValue || i.ProductId.HasValue || !i.LoyaltyPackagePurchaseId.HasValue))
                return (null, "Paket seansi sadece paketle eslesen hizmet kalemlerinde kullanilabilir");

            var packageServiceIds = packageSessionItems
                .Select(i => i.ServiceId!.Value)
                .Distinct()
                .ToList();

            var benefits = await _loyaltyPackages.GetUsablePurchasesAsync(customerId, dto.SlnClientId.Value, packageServiceIds, branchId);
            packageBenefitLookup = benefits.ToDictionary(b => b.PurchaseId);

            foreach (var group in packageSessionItems.GroupBy(i => new { PurchaseId = i.LoyaltyPackagePurchaseId!.Value, ServiceId = i.ServiceId!.Value }))
            {
                if (!packageBenefitLookup.TryGetValue(group.Key.PurchaseId, out var benefit)
                    || benefit.ServiceId != group.Key.ServiceId
                    || benefit.RemainingSessions <= 0)
                {
                    return (null, "Kullanilabilir paket seansi bulunamadi veya tukenmis");
                }

                var requestedQuantity = group.Sum(i => i.Quantity);
                var requestedCount = (int)requestedQuantity;
                if (requestedQuantity != requestedCount || requestedCount <= 0)
                    return (null, "Paket seansi kullaniminda miktar tam sayi olmali");

                if (requestedCount > benefit.RemainingSessions)
                    return (null, $"Paket seansi yetersiz. Kalan: {benefit.RemainingSessions}");
            }
        }

        if (membershipBenefitItems.Count > 0)
        {
            if (!dto.SlnClientId.HasValue)
                return (null, "Uyelik hakki kullanimi icin musteri secilmelidir");

            if (membershipBenefitItems.Any(i => !i.ServiceId.HasValue || i.ProductId.HasValue))
                return (null, "Uyelik hakki sadece hizmet kalemlerinde kullanilabilir");

            var membershipServiceIds = membershipBenefitItems
                .Select(i => i.ServiceId!.Value)
                .Distinct()
                .ToList();

            var benefits = await _memberships.CheckBenefitsAsync(customerId, dto.SlnClientId.Value, membershipServiceIds, branchId);
            membershipBenefitLookup = benefits.ToDictionary(b => b.ServiceId);

            foreach (var group in membershipBenefitItems.GroupBy(i => i.ServiceId!.Value))
            {
                if (!membershipBenefitLookup.TryGetValue(group.Key, out var benefit)
                    || !benefit.HasFreeBenefit
                    || benefit.RemainingFree <= 0)
                {
                    return (null, "Uyelik ucretsiz hakki bulunamadi veya tukenmis");
                }

                var requestedQuantity = group.Sum(i => i.Quantity);
                var requestedCount = (int)requestedQuantity;
                if (requestedQuantity != requestedCount || requestedCount <= 0)
                    return (null, "Uyelik ucretsiz hak kullaniminda miktar tam sayi olmali");

                if (requestedCount > benefit.RemainingFree)
                    return (null, $"Uyelik ucretsiz hakki yetersiz. Kalan: {benefit.RemainingFree}");
            }
        }

        await using var transaction = await _uow.BeginTransactionAsync();

        foreach (var itemDto in dto.Items)
        {
            if (itemDto.Quantity <= 0)
                return (null, "Kalem miktari 0'dan buyuk olmali");

            var unitPrice = itemDto.UnitPrice;
            if (itemDto.UsePackageSession)
            {
                if (!itemDto.ServiceId.HasValue || !itemDto.LoyaltyPackagePurchaseId.HasValue)
                    return (null, "Paket seansi icin hizmet ve paket bilgisi zorunludur");

                if (!packageBenefitLookup.TryGetValue(itemDto.LoyaltyPackagePurchaseId.Value, out var packageBenefit)
                    || packageBenefit.ServiceId != itemDto.ServiceId.Value
                    || packageBenefit.RemainingSessions <= 0)
                {
                    return (null, "Kullanilabilir paket seansi bulunamadi veya tukenmis");
                }

                var usageCount = (int)itemDto.Quantity;
                if (itemDto.Quantity != usageCount || usageCount <= 0)
                    return (null, "Paket seansi kullaniminda miktar tam sayi olmali");

                if (usageCount > packageBenefit.RemainingSessions)
                    return (null, $"Paket seansi yetersiz. Kalan: {packageBenefit.RemainingSessions}");

            }
            else if (itemDto.UseMembershipBenefit)
            {
                if (!itemDto.ServiceId.HasValue || !itemDto.MembershipId.HasValue)
                    return (null, "Uyelik hakki icin hizmet ve uyelik bilgisi zorunludur");

                if (!membershipBenefitLookup.TryGetValue(itemDto.ServiceId.Value, out var benefit)
                    || benefit.MembershipId != itemDto.MembershipId
                    || !benefit.HasFreeBenefit
                    || benefit.RemainingFree <= 0)
                {
                    return (null, "Uyelik ucretsiz hakki bulunamadi veya tukenmis");
                }

                var usageCount = (int)itemDto.Quantity;
                if (itemDto.Quantity != usageCount || usageCount <= 0)
                    return (null, "Uyelik ucretsiz hak kullaniminda miktar tam sayi olmali");

                if (usageCount > benefit.RemainingFree)
                    return (null, $"Uyelik ucretsiz hakki yetersiz. Kalan: {benefit.RemainingFree}");

                unitPrice = 0;
                for (var i = 0; i < usageCount; i++)
                    membershipUsageRecords.Add((itemDto.MembershipId.Value, itemDto.ServiceId.Value));
            }

            var lineDiscount = unitPrice == 0 ? 0 : itemDto.DiscountAmount;
            var lineTotal = Math.Max(0, (itemDto.Quantity * unitPrice) - lineDiscount);
            totalAmount += lineTotal;

            var invoiceItem = new SlnInvoiceItem
            {
                ServiceId = itemDto.ServiceId,
                ProductId = itemDto.ProductId,
                LoyaltyPackagePurchaseId = itemDto.UsePackageSession ? itemDto.LoyaltyPackagePurchaseId : null,
                IsSessionUsage = itemDto.UsePackageSession,
                PersonnelId = itemDto.PersonnelId,
                Quantity = itemDto.Quantity,
                UnitPrice = unitPrice,
                DiscountAmount = lineDiscount,
                LineTotal = lineTotal
            };
            items.Add(invoiceItem);

            if (itemDto.UsePackageSession && itemDto.LoyaltyPackagePurchaseId.HasValue && itemDto.ServiceId.HasValue)
            {
                var usageCount = (int)itemDto.Quantity;
                for (var i = 0; i < usageCount; i++)
                    packageUsageRecords.Add((itemDto.LoyaltyPackagePurchaseId.Value, itemDto.ServiceId.Value, invoiceItem));
            }

            if (dto.SlnClientId.HasValue
                && itemDto.ServiceId.HasValue
                && !itemDto.ProductId.HasValue
                && !itemDto.UseMembershipBenefit
                && !itemDto.UsePackageSession)
            {
                serviceSessionSaleItems.Add(invoiceItem);
            }

            // Urun satisinda stok dusur
            if (itemDto.ProductId.HasValue)
            {
                var product = await _products.GetAllQueryable()
                    .FirstOrDefaultAsync(p => p.Id == itemDto.ProductId.Value && p.CustomerId == customerId &&
                        (p.BranchId == null || p.BranchId == branchId));

                if (product == null)
                    return (null, "Urun bulunamadi");

                var availableStock = await _stockBalances.GetStockQuantityAsync(customerId, product.Id, branchId, product.StockQuantity);
                if (availableStock < itemDto.Quantity)
                    return (null, $"Yetersiz stok: {product.Name} (Mevcut: {availableStock:0.##} {product.Unit})");

                var (stockOk, stockError) = await _stockBalances.AdjustStockAsync(
                    product, customerId, branchId, -itemDto.Quantity, preventNegative: true);
                if (!stockOk) return (null, stockError);
                await _stockBalances.SyncProductTotalAsync(product, customerId);
                _stockMovements.Add(new SlnStockMovement
                {
                    CustomerId = customerId,
                    BranchId = branchId,
                    ProductId = product.Id,
                    MovementTypeId = 2,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    Notes = $"Adisyon: {invoiceNo}",
                    CreatedByPersonnelId = userId > 0 ? userId : null
                });
            }

            var materialConsumptions = itemDto.MaterialConsumptions ?? [];
            if (materialConsumptions.Count > 0)
            {
                if (!itemDto.ServiceId.HasValue || itemDto.ProductId.HasValue)
                    return (null, "Malzeme tuketimi sadece hizmet kalemlerinde kullanilabilir");

                var movementNotePrefix = dto.SlnAppointmentId.HasValue
                    ? $"Randevu:{dto.SlnAppointmentId.Value} | Adisyon:{invoiceNo}"
                    : $"Adisyon:{invoiceNo}";

                var (materialsOk, materialsError) = await ConsumeServiceMaterialsAsync(
                    materialConsumptions,
                    customerId,
                    branchId,
                    movementNotePrefix,
                    itemDto.PersonnelId ?? userId,
                    itemDto.ServiceId.Value);
                if (!materialsOk) return (null, materialsError);
            }
        }

        var subtotalAfterDiscount = Math.Max(0, totalAmount - dto.DiscountAmount);
        if (dto.PrepaidAmount > subtotalAfterDiscount)
            return (null, "On odeme adisyon tutarini asamaz");

        invoice.TotalAmount = totalAmount;
        // BUG.A2: bahsis opsiyonel olarak NetAmount'a eklenir
        invoice.NetAmount = subtotalAfterDiscount - dto.PrepaidAmount + (dto.IncludeTipInTotal ? dto.TipAmount : 0);
        invoice.StatusId = 2; // Paid

        SlnGiftCardDto? giftCardPayment = null;
        if (dto.PaymentMethodId == GiftCardPaymentMethodId && invoice.NetAmount > 0)
        {
            var giftCardCode = (dto.GiftCardCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(giftCardCode))
                return (null, "Hediye karti ile odeme icin kart kodu girilmelidir");

            giftCardPayment = await _giftCards.GetGiftCardByCodeAsync(giftCardCode, customerId, branchId);
            if (giftCardPayment == null)
                return (null, "Hediye karti bulunamadi veya aktif degil");
            if (giftCardPayment.ExpiresAt.HasValue && giftCardPayment.ExpiresAt.Value < DateTime.UtcNow)
                return (null, "Hediye kartinin suresi dolmus");
            if (giftCardPayment.RemainingBalance < invoice.NetAmount)
                return (null, $"Hediye karti bakiyesi yetersiz. Kalan: {giftCardPayment.RemainingBalance:N2} TL");
        }

        _invoices.Add(invoice);
        await _uow.SaveChangesAsync();

        // Item'lari invoice'a bagla
        foreach (var item in items)
        {
            item.InvoiceId = invoice.Id;
            _invoiceItems.Add(item);
        }
        await _uow.SaveChangesAsync();

        foreach (var usage in membershipUsageRecords)
            await _memberships.RecordUsageAsync(customerId, usage.MembershipId, usage.ServiceId);

        foreach (var usage in packageUsageRecords)
        {
            var notes = $"Invoice:{invoice.Id}|InvoiceNo:{invoiceNo}|Service:{usage.ServiceId}";
            var (success, error) = await _loyaltyPackages.RecordRedemptionAsync(customerId, usage.ClientPackageId, usage.ServiceId, dto.SlnClientId, userId, notes, branchId, invoice.Id, usage.Item.Id, dto.SlnAppointmentId);
            if (!success)
            {
                _logger.LogWarning("Paket seansi kaydedilemedi: InvoiceId={InvoiceId}, ClientPackageId={ClientPackageId}, Error={Error}", invoice.Id, usage.ClientPackageId, error);
                return (null, error ?? "Paket seansi kaydedilemedi");
            }
        }

        if (giftCardPayment != null)
        {
            var (success, error) = await _giftCards.RedeemGiftCardAsync(new SlnGiftCardRedeemDto
            {
                Code = giftCardPayment.Code,
                Amount = invoice.NetAmount,
                InvoiceId = invoice.Id
            }, customerId, branchId);
            if (!success)
                return (null, error ?? "Hediye karti odemesi kaydedilemedi");
        }

        if (dto.SlnClientId.HasValue && serviceSessionSaleItems.Count > 0)
        {
            var serviceSessionSaleLines = serviceSessionSaleItems
                .Where(i => i.ServiceId.HasValue)
                .Select(i =>
                {
                    var quantityAsInt = i.Quantity == Math.Truncate(i.Quantity)
                        ? (int)i.Quantity
                        : 1;
                    return new SlnServiceSessionPlanSaleLine(i.ServiceId!.Value, i.LineTotal, i.LineTotal, Math.Max(1, quantityAsInt), i.Id);
                })
                .ToList();

            var createdPlans = await _serviceSessions.CreatePlansFromInvoiceAsync(
                customerId,
                dto.SlnClientId.Value,
                invoice.Id,
                serviceSessionSaleLines,
                userId,
                branchId);

            if (createdPlans.Count > 0)
            {
                var saleNote = "ServiceSessionPlanSale:" + string.Join(",", createdPlans.Select(p => p.Id));
                invoice.Notes = string.IsNullOrWhiteSpace(invoice.Notes)
                    ? saleNote
                    : $"{invoice.Notes}|{saleNote}";
                await _uow.SaveChangesAsync();
            }
        }

        // Sadakat Programi (D — punch card): bu adisyondaki ziyaretleri say
        if (dto.SlnClientId.HasValue)
        {
            var earnServiceIds = items
                .Where(i => i.ServiceId.HasValue
                    && !i.IsSessionUsage
                    && i.UnitPrice > 0
                    && !i.LineTotal.Equals(0m))
                .SelectMany(i =>
                {
                    var qty = i.Quantity == Math.Truncate(i.Quantity) ? (int)i.Quantity : 1;
                    return Enumerable.Repeat(i.ServiceId!.Value, Math.Max(1, qty));
                })
                .ToList();

            if (earnServiceIds.Count > 0)
            {
                await _loyaltyPrograms.EarnFromInvoiceItemsAsync(
                    customerId,
                    dto.SlnClientId.Value,
                    branchId,
                    earnServiceIds);
            }
        }

        // Sadakat Programi (D) odul kullanimi: cart kalemleri uzerinden reward'i invoiceItem ile bagla
        if (dto.SlnClientId.HasValue)
        {
            for (var idx = 0; idx < dto.Items.Count && idx < items.Count; idx++)
            {
                var itemDto = dto.Items[idx];
                if (!itemDto.LoyaltyRewardId.HasValue) continue;
                var (rOk, rErr) = await _loyaltyPrograms.ApplyRewardAsync(customerId, itemDto.LoyaltyRewardId.Value, items[idx].Id);
                if (!rOk)
                {
                    _logger.LogWarning("Loyalty reward apply failed for invoice {InvoiceId} item {ItemId} reward {RewardId}: {Error}",
                        invoice.Id, items[idx].Id, itemDto.LoyaltyRewardId.Value, rErr);
                }
            }
        }

        // Sadakat Puani (C — TL bazli): puanla odeme, NetAmount'tan TL karsiligi indirim
        if (dto.SlnClientId.HasValue && dto.LoyaltyPointsToRedeem.HasValue && dto.LoyaltyPointsToRedeem.Value > 0)
        {
            var (tlValue, lpErr) = await _loyaltyPoints.RedeemForInvoiceAsync(
                customerId,
                dto.SlnClientId.Value,
                dto.LoyaltyPointsToRedeem.Value,
                invoice.Id,
                branchId);
            if (lpErr != null)
            {
                return (null, lpErr);
            }
            if (tlValue > 0)
            {
                var applied = Math.Min(tlValue, invoice.NetAmount);
                invoice.DiscountAmount += applied;
                invoice.NetAmount = Math.Max(0m, invoice.NetAmount - applied);
                var lpNote = $"LoyaltyPoints:{dto.LoyaltyPointsToRedeem.Value}|LoyaltyTL:{applied:F2}";
                invoice.Notes = string.IsNullOrWhiteSpace(invoice.Notes) ? lpNote : $"{invoice.Notes}|{lpNote}";
                await _uow.SaveChangesAsync();
            }
        }

        // Kasaya gelir hareketi yaz; sube adisyonu merkezin ya da baska subenin kasasina dusmemeli.
        if (invoice.NetAmount > 0 && invoice.PaymentMethodId != GiftCardPaymentMethodId)
        {
            var register = await ResolveCashRegisterForInvoiceAsync(customerId, branchId);

            if (register != null)
            {
                _cashTransactions.Add(new SlnCashTransaction
                {
                    RegisterId = register.Id,
                    TransactionTypeId = 1, // Income
                    Amount = invoice.NetAmount,
                    PaymentMethodId = dto.PaymentMethodId,
                    RelatedInvoiceId = invoice.Id,
                    CreatedByPersonnelId = userId > 0 ? userId : null,
                    Description = $"Adisyon: {invoiceNo}"
                });
                await _uow.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Adisyon icin aktif kasa yok: CustomerId={CustomerId}, InvoiceNo={InvoiceNo}", customerId, invoiceNo);
            }
        }

        if (dto.SlnAppointmentId.HasValue && linkedAppointment?.StatusId != 3)
        {
            var (statusSuccess, statusError, _) = await _appointmentFactory.UpdateStatusAsync(
                dto.SlnAppointmentId.Value,
                3,
                customerId,
                branchId);
            if (!statusSuccess)
                return (null, statusError ?? "Randevu tamamlanamadi");
        }

        await transaction.CommitAsync();

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

    private async Task<(bool Success, string? Error)> ConsumeServiceMaterialsAsync(
        List<SlnInvoiceMaterialConsumptionDto> materials,
        int customerId,
        int? branchId,
        string movementNotePrefix,
        int personnelId,
        int serviceId)
    {
        var validMaterials = materials
            .Where(m => m.ProductId > 0 && m.Quantity > 0)
            .GroupBy(m => m.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                Unit = g.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Unit))?.Unit,
                Notes = string.Join("; ", g.Select(x => x.Notes).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct())
            })
            .ToList();

        if (validMaterials.Count == 0) return (true, null);

        var productIds = validMaterials.Select(m => m.ProductId).ToList();
        var products = await _products.GetAllQueryable()
            .Where(p => p.CustomerId == customerId
                     && productIds.Contains(p.Id)
                     && (p.BranchId == null || p.BranchId == branchId))
            .ToDictionaryAsync(p => p.Id);

        foreach (var material in validMaterials)
        {
            if (!products.TryGetValue(material.ProductId, out var product))
                return (false, "Malzeme urunu bulunamadi");

            var availableStock = await _stockBalances.GetStockQuantityAsync(customerId, product.Id, branchId, product.StockQuantity);
            if (availableStock < material.Quantity)
                return (false, $"Yetersiz stok: {product.Name} (Mevcut: {availableStock:0.##} {product.Unit})");
        }

        foreach (var material in validMaterials)
        {
            var product = products[material.ProductId];
            var (stockOk, stockError) = await _stockBalances.AdjustStockAsync(
                product, customerId, branchId, -material.Quantity, preventNegative: true);
            if (!stockOk) return (false, stockError);

            await _stockBalances.SyncProductTotalAsync(product, customerId);
            _stockMovements.Add(new SlnStockMovement
            {
                CustomerId = customerId,
                BranchId = branchId,
                ProductId = product.Id,
                MovementTypeId = 3,
                Quantity = material.Quantity,
                UnitPrice = product.PurchasePrice,
                Notes = $"{movementNotePrefix} | Hizmet:{serviceId}" + (string.IsNullOrWhiteSpace(material.Notes) ? "" : $" | {material.Notes}"),
                CreatedByPersonnelId = personnelId > 0 ? personnelId : null
            });
        }

        return (true, null);
    }

    private async Task<SlnCashRegister?> ResolveCashRegisterForInvoiceAsync(int customerId, int? branchId)
    {
        if (!branchId.HasValue)
        {
            var hq = await _branches.GetAllQueryable()
                .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter && b.IsActive);
            branchId = hq?.Id;
        }

        var registerQuery = _cashRegisters.GetAllQueryable()
            .Where(r => r.CustomerId == customerId);

        var register = branchId.HasValue
            ? await registerQuery.FirstOrDefaultAsync(r => r.BranchId == branchId.Value && r.IsActive)
            : await registerQuery.FirstOrDefaultAsync(r => r.BranchId == null && r.IsActive);

        if (register != null)
            return register;

        string registerName;
        if (branchId.HasValue)
        {
            var branch = await _branches.GetAllQueryable()
                .FirstOrDefaultAsync(b => b.Id == branchId.Value && b.CustomerId == customerId && b.IsActive);
            if (branch == null)
                return null;

            registerName = $"{branch.Name} Kasasi";
        }
        else
        {
            registerName = "Kasa";
        }

        register = new SlnCashRegister
        {
            CustomerId = customerId,
            BranchId = branchId,
            Name = registerName,
            IsActive = true
        };
        _cashRegisters.Add(register);
        await _uow.SaveChangesAsync();

        return register;
    }

    public async Task<(bool Success, string? Error)> CancelInvoiceAsync(int invoiceId, int customerId, int? branchId = null)
    {
        var query = _invoices.GetAllQueryable()
            .Where(i => i.Id == invoiceId && i.CustomerId == customerId);

        if (branchId.HasValue)
            query = query.Where(i => i.BranchId == branchId.Value);

        var invoice = await query
            .Include(i => i.Items)
            .FirstOrDefaultAsync();

        if (invoice == null) return (false, "Adisyon bulunamadi");
        if (invoice.StatusId == 3) return (false, "Adisyon zaten iptal edilmis");

        var (packageSaleCancelled, packageSaleError) = await _loyaltyPackages.CancelPurchaseFromInvoiceAsync(customerId, invoice.Notes);
        if (!packageSaleCancelled) return (false, packageSaleError);

        var (packageUsagesReversed, packageUsageError) = await _loyaltyPackages.ReverseInvoiceRedemptionsAsync(customerId, invoice.Id);
        if (!packageUsagesReversed) return (false, packageUsageError);

        var (giftCardSaleCancelled, giftCardSaleError) = await _giftCards.CancelGiftCardSaleFromInvoiceAsync(customerId, invoice.Notes);
        if (!giftCardSaleCancelled) return (false, giftCardSaleError);

        var (giftCardRedemptionsReversed, giftCardRedemptionError) = await _giftCards.ReverseInvoiceRedemptionsAsync(customerId, invoice.Id);
        if (!giftCardRedemptionsReversed) return (false, giftCardRedemptionError);

        invoice.StatusId = 3; // Cancelled

        // Urun satislarinda stogu geri ekle
        foreach (var item in invoice.Items.Where(it => it.ProductId.HasValue))
        {
            var product = await _products.GetAllQueryable()
                .FirstOrDefaultAsync(p => p.Id == item.ProductId!.Value && p.CustomerId == customerId &&
                    (p.BranchId == null || p.BranchId == invoice.BranchId));
            if (product != null)
            {
                var (stockOk, stockError) = await _stockBalances.AdjustStockAsync(
                    product, customerId, invoice.BranchId, item.Quantity, preventNegative: false);
                if (!stockOk) return (false, stockError);
                await _stockBalances.SyncProductTotalAsync(product, customerId);
                _stockMovements.Add(new SlnStockMovement
                {
                    CustomerId = customerId,
                    BranchId = invoice.BranchId,
                    ProductId = product.Id,
                    MovementTypeId = 5,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Notes = $"Adisyon iptal: {invoice.InvoiceNo}",
                    CreatedByPersonnelId = invoice.PersonnelId
                });
            }
        }

        var materialMovements = await _stockMovements.GetAllQueryable()
            .Where(m => m.CustomerId == customerId
                     && m.MovementTypeId == 3
                     && m.Notes != null
                     && m.Notes.Contains($"Adisyon:{invoice.InvoiceNo}"))
            .ToListAsync();

        foreach (var movement in materialMovements)
        {
            var product = await _products.GetAllQueryable()
                .FirstOrDefaultAsync(p => p.Id == movement.ProductId && p.CustomerId == customerId &&
                    (p.BranchId == null || p.BranchId == invoice.BranchId));
            if (product == null) continue;

            var (stockOk, stockError) = await _stockBalances.AdjustStockAsync(
                product, customerId, invoice.BranchId, movement.Quantity, preventNegative: false);
            if (!stockOk) return (false, stockError);

            await _stockBalances.SyncProductTotalAsync(product, customerId);
            _stockMovements.Add(new SlnStockMovement
            {
                CustomerId = customerId,
                BranchId = invoice.BranchId,
                ProductId = product.Id,
                MovementTypeId = 5,
                Quantity = movement.Quantity,
                UnitPrice = movement.UnitPrice,
                Notes = $"Adisyon iptal malzeme iade: {invoice.InvoiceNo}",
                CreatedByPersonnelId = invoice.PersonnelId
            });
        }

        await _uow.SaveChangesAsync();

        _logger.LogInformation("Adisyon iptal edildi: {InvoiceId}", invoiceId);
        return (true, null);
    }

    // ═══ Kasa ═══

    public async Task<List<object>> GetCashRegistersAsync(int customerId, int? branchId = null)
    {
        var query = _cashRegisters.GetAllQueryable()
            .Where(r => r.CustomerId == customerId)
            .Include(r => r.Branch)
            .AsQueryable();

        if (branchId.HasValue)
            query = query.Where(r => r.BranchId == branchId.Value);

        var registers = await query.ToListAsync();

        var result = new List<object>();
        foreach (var r in registers)
        {
            var transactions = await _cashTransactions.GetAllQueryable()
                .Where(t => t.RegisterId == r.Id)
                .ToListAsync();
            var income = transactions.Where(t => t.TransactionTypeId == 1).Sum(t => t.Amount);
            var expense = transactions.Where(t => t.TransactionTypeId == 2).Sum(t => t.Amount);
            var balance = income - expense;

            // BUG3.2: Kasa tipi — Branch navigation'dan, HQ ise "Merkez", degilse sube adi, yoksa "Firma geneli"
            string typeName;
            if (r.Branch == null) typeName = "Firma geneli";
            else if (r.Branch.IsHeadquarter) typeName = "Merkez";
            else typeName = "Sube";

            result.Add(new
            {
                r.Id,
                r.Name,
                r.IsActive,
                balance,
                r.BranchId,
                branchName = r.Branch?.Name,
                isHeadquarter = r.Branch?.IsHeadquarter ?? false,
                typeName
            });
        }

        return result;
    }

    public async Task<(bool Success, string? Error)> UpdateCashRegisterAsync(int registerId, string name, int? branchId, bool isActive, int customerId, int? branchScopeId = null)
    {
        var register = await _cashRegisters.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.Id == registerId && r.CustomerId == customerId);
        if (register == null) return (false, "Kasa bulunamadi");
        if (branchScopeId.HasValue && register.BranchId != branchScopeId.Value)
            return (false, "Bu kasa icin yetkiniz yok");

        if (branchScopeId.HasValue)
            branchId = branchScopeId.Value;

        if (!string.IsNullOrWhiteSpace(name)) register.Name = name.Trim();
        if (branchId.HasValue)
        {
            var branchExists = await _branches.GetAllQueryable()
                .AnyAsync(b => b.Id == branchId.Value && b.CustomerId == customerId);
            if (!branchExists) return (false, "Gecersiz sube");
            register.BranchId = branchId.Value;
        }
        register.IsActive = isActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>BranchId null olan eski kasalari firmanin merkez subesine tasir</summary>
    public async Task<object> NormalizeCashRegisterBranchesAsync(int customerId)
    {
        var orphan = await _cashRegisters.GetAllQueryable()
            .Where(r => r.CustomerId == customerId && r.BranchId == null)
            .ToListAsync();
        if (orphan.Count == 0) return new { updated = 0, note = "Normalize edilecek kasa yok." };

        var hq = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter && b.IsActive);
        if (hq == null) return new { updated = 0, note = "Merkez sube bulunamadi — once merkez sube tanimlayin." };

        foreach (var r in orphan) r.BranchId = hq.Id;
        await _uow.SaveChangesAsync();
        return new { updated = orphan.Count, hqBranchId = hq.Id, hqName = hq.Name };
    }

    public async Task<object> CreateCashRegisterAsync(string name, int customerId, int? branchId = null)
    {
        // BranchId bos gelirse merkez subeye ata (eski davranisa duserken "firma geneli" yerine merkeze bagla)
        if (!branchId.HasValue)
        {
            var hq = await _branches.GetAllQueryable()
                .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter && b.IsActive);
            branchId = hq?.Id;
        }

        var register = new SlnCashRegister
        {
            CustomerId = customerId,
            BranchId = branchId,
            Name = name
        };

        _cashRegisters.Add(register);
        await _uow.SaveChangesAsync();

        return new { register.Id, register.Name, register.IsActive, register.BranchId };
    }

    public async Task<List<SlnCashTransactionDto>> GetCashTransactionsAsync(int registerId, int customerId, DateTime? from, DateTime? to, int? branchId = null)
    {
        // Kasanin bu firmaya ait oldugunu dogrula
        var registerQuery = _cashRegisters.GetAllQueryable()
            .Where(r => r.Id == registerId && r.CustomerId == customerId);

        if (branchId.HasValue)
            registerQuery = registerQuery.Where(r => r.BranchId == branchId.Value);

        var register = await registerQuery.FirstOrDefaultAsync();

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
        int paymentMethodId, int userId, int customerId, int? branchId = null)
    {
        var registerQuery = _cashRegisters.GetAllQueryable()
            .Where(r => r.Id == registerId && r.CustomerId == customerId);

        if (branchId.HasValue)
            registerQuery = registerQuery.Where(r => r.BranchId == branchId.Value);

        var register = await registerQuery.FirstOrDefaultAsync();

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

    public async Task<List<SlnExpenseDto>> GetExpensesAsync(int customerId, DateTime? from, DateTime? to, int? categoryId = null, int? branchId = null)
    {
        var query = _expenses.GetAllQueryable()
            .Where(e => e.CustomerId == customerId);

        if (branchId.HasValue)
            query = query.Where(e => e.BranchId == branchId.Value);

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            query = query.Where(e => e.ExpenseDate >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            query = query.Where(e => e.ExpenseDate <= toUtc);
        }

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
            PaymentMethodId = e.PaymentMethodId,
            StatusId = e.StatusId
        }).ToList();
    }

    public async Task<SlnExpenseDto> CreateExpenseAsync(SlnExpenseCreateDto dto, int userId, int customerId, int? branchId = null)
    {
        var expense = new SlnExpense
        {
            CustomerId = customerId,
            BranchId = branchId,
            CategoryId = dto.CategoryId,
            Amount = dto.Amount,
            ExpenseDate = dto.ExpenseDate,
            Description = dto.Description,
            PaymentMethodId = dto.PaymentMethodId,
            StatusId = 1,
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
            PaymentMethodId = expense.PaymentMethodId,
            StatusId = expense.StatusId
        };
    }

    public async Task<(SlnExpenseDto? Expense, string? Error)> UpdateExpenseAsync(
        int expenseId,
        SlnExpenseUpdateDto dto,
        int userId,
        int customerId,
        int? branchId = null)
    {
        var query = _expenses.GetAllQueryable()
            .Include(e => e.Category)
            .Where(e => e.Id == expenseId && e.CustomerId == customerId);

        if (branchId.HasValue)
            query = query.Where(e => e.BranchId == branchId.Value);

        var expense = await query.FirstOrDefaultAsync();
        if (expense == null) return (null, "Masraf bulunamadi");

        if (dto.CategoryId.HasValue)
        {
            if (dto.CategoryId.Value <= 0) return (null, "Kategori zorunludur");
            var categoryExists = await _expenseCategories.GetAllQueryable()
                .AnyAsync(c => c.Id == dto.CategoryId.Value && c.CustomerId == customerId);
            if (!categoryExists) return (null, "Kategori bulunamadi");
            expense.CategoryId = dto.CategoryId.Value;
        }

        if (dto.Amount.HasValue)
        {
            if (dto.Amount.Value <= 0) return (null, "Tutar sifirdan buyuk olmalidir");
            expense.Amount = dto.Amount.Value;
        }

        if (dto.ExpenseDate.HasValue)
            expense.ExpenseDate = dto.ExpenseDate.Value;

        if (dto.Description != null)
            expense.Description = dto.Description;

        if (dto.PaymentMethodId.HasValue)
            expense.PaymentMethodId = dto.PaymentMethodId.Value;

        if (dto.StatusId.HasValue)
        {
            if (dto.StatusId.Value is < 1 or > 3)
                return (null, "Gecersiz masraf durumu");

            var oldStatusId = expense.StatusId;
            expense.StatusId = dto.StatusId.Value;
            expense.ApprovedByPersonnelId = expense.StatusId == 2 && userId > 0
                ? userId
                : expense.ApprovedByPersonnelId;

            if (oldStatusId != 2 && expense.StatusId == 2)
            {
                var register = await ResolveCashRegisterForInvoiceAsync(customerId, expense.BranchId);
                if (register == null) return (null, "Aktif kasa bulunamadi");

                _cashTransactions.Add(new SlnCashTransaction
                {
                    RegisterId = register.Id,
                    TransactionTypeId = 2,
                    Amount = expense.Amount,
                    PaymentMethodId = expense.PaymentMethodId,
                    CreatedByPersonnelId = userId > 0 ? userId : null,
                    Description = $"Masraf #{expense.Id}: {expense.Description}"
                });
            }
        }

        await _uow.SaveChangesAsync();

        var categoryName = dto.CategoryId.HasValue
            ? await _expenseCategories.GetAllQueryable()
                .Where(c => c.Id == expense.CategoryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync()
            : expense.Category?.Name;

        return (new SlnExpenseDto
        {
            Id = expense.Id,
            CategoryName = categoryName ?? "",
            Amount = expense.Amount,
            ExpenseDate = expense.ExpenseDate,
            Description = expense.Description,
            PaymentMethodId = expense.PaymentMethodId,
            StatusId = expense.StatusId
        }, null);
    }

    public async Task<(bool Success, string? Error)> DeleteExpenseAsync(int expenseId, int customerId, int? branchId = null)
    {
        var query = _expenses.GetAllQueryable()
            .Where(e => e.Id == expenseId && e.CustomerId == customerId);

        if (branchId.HasValue)
            query = query.Where(e => e.BranchId == branchId.Value);

        var expense = await query.FirstOrDefaultAsync();

        if (expense == null) return (false, "Masraf bulunamadi");

        _expenses.Remove(expense);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static SlnInvoiceDto MapInvoiceToDto(SlnInvoice i) => new()
    {
        Id = i.Id,
        SlnAppointmentId = i.SlnAppointmentId,
        InvoiceNo = i.InvoiceNo,
        InvoiceDate = i.InvoiceDate,
        ClientName = i.SlnClient?.FullName,
        TotalAmount = i.TotalAmount,
        DiscountAmount = i.DiscountAmount,
        PrepaidAmount = i.PrepaidAmount,
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

    public async Task<List<SlnCashClosingDto>> GetCashClosingsAsync(int customerId, int? registerId, int? branchId = null)
    {
        var query = _cashClosings.GetAllQueryable()
            .Include(c => c.Register)
            .Include(c => c.ClosedByPersonnel).ThenInclude(p => p!.User)
            .Where(c => c.Register != null && c.Register.CustomerId == customerId);

        if (branchId.HasValue)
            query = query.Where(c => c.Register != null && c.Register.BranchId == branchId.Value);

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

    public async Task<(SlnCashClosingDto? Closing, string? Error)> CreateCashClosingAsync(SlnCashClosingCreateDto dto, int userId, int customerId, int? branchId = null)
    {
        var registerQuery = _cashRegisters.GetAllQueryable()
            .Where(r => r.Id == dto.RegisterId && r.CustomerId == customerId);

        if (branchId.HasValue)
            registerQuery = registerQuery.Where(r => r.BranchId == branchId.Value);

        var register = await registerQuery.FirstOrDefaultAsync();

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

        _cashClosings.Add(closing);
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

    public async Task<object> GetDailySummaryAsync(int registerId, int customerId, int? branchId = null)
    {
        var registerQuery = _cashRegisters.GetAllQueryable()
            .Where(r => r.Id == registerId && r.CustomerId == customerId);

        if (branchId.HasValue)
            registerQuery = registerQuery.Where(r => r.BranchId == branchId.Value);

        var register = await registerQuery.FirstOrDefaultAsync();

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

    public async Task<object> GetZReportAsync(int registerId, int customerId, DateTime? date = null, int? branchId = null)
    {
        var registerQuery = _cashRegisters.GetAllQueryable()
            .Where(r => r.Id == registerId && r.CustomerId == customerId);

        if (branchId.HasValue)
            registerQuery = registerQuery.Where(r => r.BranchId == branchId.Value);

        var register = await registerQuery.FirstOrDefaultAsync();
        if (register == null) return new { error = "Kasa bulunamadi" };

        var targetDate = (date ?? DateTime.UtcNow).Date;
        var nextDate = targetDate.AddDays(1);

        // Acilis bakiyesi
        var opening = await _cashOpenings.GetAllQueryable()
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
        var transferIncome = transactions.Where(t => t.TransactionTypeId == 1 && t.PaymentMethodId == 4).Sum(t => t.Amount);
        var totalIncome = transactions.Where(t => t.TransactionTypeId == 1).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.TransactionTypeId == 2).Sum(t => t.Amount);

        // Kapanis
        var closing = await _cashClosings.GetAllQueryable()
            .Where(c => c.RegisterId == registerId && c.ClosingDate >= targetDate && c.ClosingDate < nextDate)
            .FirstOrDefaultAsync();

        // Gun adisyonlari
        var invoices = await _invoices.GetAllQueryable()
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

    public async Task<(object? Result, string? Error)> CreateCashOpeningAsync(int registerId, int customerId, decimal? manualBalance, int personnelId, int? branchId = null)
    {
        var registerQuery = _cashRegisters.GetAllQueryable()
            .Where(r => r.Id == registerId && r.CustomerId == customerId);

        if (branchId.HasValue)
            registerQuery = registerQuery.Where(r => r.BranchId == branchId.Value);

        var register = await registerQuery.FirstOrDefaultAsync();
        if (register == null) return (null, "Kasa bulunamadi");

        var today = DateTime.UtcNow.Date;
        var exists = await _cashOpenings.GetAllQueryable().AnyAsync(o => o.RegisterId == registerId && o.OpeningDate >= today && o.OpeningDate < today.AddDays(1));
        if (exists) return (null, "Bugun zaten acilis yapilmis.");

        // Onceki kapanistan bakiye tasi
        decimal openingBalance = 0;
        bool isCarried = false;
        var lastClosing = await _cashClosings.GetAllQueryable()
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

        _cashOpenings.Add(opening);
        await _uow.SaveChangesAsync();

        return (new { opening.Id, opening.OpeningBalance, opening.IsCarriedForward }, null);
    }

    // ═══ MÜŞTERİ CARİ HESAP ═══

    public async Task<object> GetClientLedgerAsync(int customerId, int slnClientId, int? branchId = null)
    {
        var clientQuery = _clients.GetAllQueryable()
            .Where(c => c.Id == slnClientId && c.CustomerId == customerId);
        clientQuery = SalonBranchScope.ApplyToClients(clientQuery, branchId);

        if (!await clientQuery.AnyAsync())
            return new { balance = 0m, entries = Array.Empty<object>() };

        var entries = await _clientLedgers.GetAllQueryable()
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
        var lastEntry = await _clientLedgers.GetAllQueryable()
            .Where(l => l.CustomerId == customerId && l.SlnClientId == slnClientId)
            .OrderByDescending(l => l.TransactionDate)
            .FirstOrDefaultAsync();

        var runningBalance = lastEntry?.RunningBalance ?? 0;
        if (typeId == 1) runningBalance += amount;      // Borc
        else if (typeId == 2) runningBalance -= amount;  // Alacak (odeme)
        else if (typeId == 3) runningBalance -= amount;  // Iade

        _clientLedgers.Add(new SlnClientLedger
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

    public async Task<(object? Result, string? Error)> CreateRefundAsync(int customerId, int invoiceId, decimal refundAmount, int refundMethodId, string reason, int personnelId, int? branchId = null)
    {
        var query = _invoices.GetAllQueryable()
            .Where(i => i.Id == invoiceId && i.CustomerId == customerId);

        if (branchId.HasValue)
            query = query.Where(i => i.BranchId == branchId.Value);

        var invoice = await query
            .Include(i => i.Items).ThenInclude(item => item.Product)
            .FirstOrDefaultAsync();
        if (invoice == null) return (null, "Adisyon bulunamadi.");
        if (invoice.StatusId == 3) return (null, "Iptal edilmis adisyon icin iade yapilamaz.");

        var maxRefundable = invoice.GrandTotal > 0 ? invoice.GrandTotal : invoice.NetAmount;
        if (refundAmount > maxRefundable) return (null, "Iade tutari adisyon tutarini asamaz.");
        var isFullRefund = refundAmount >= maxRefundable;

        if (HasPackageSaleNote(invoice.Notes) && !isFullRefund)
            return (null, "Paket satisinda kismi iade desteklenmiyor. Paket hic kullanilmadiysa tam iade/iptal yapin.");

        if (HasGiftCardSaleNote(invoice.Notes) && !isFullRefund)
            return (null, "Hediye karti satisinda kismi iade desteklenmiyor. Kart kullanilmadiysa tam iade/iptal yapin.");

        if (!isFullRefund && await _giftCards.HasRedemptionForInvoiceAsync(customerId, invoiceId))
            return (null, "Hediye karti ile odenen adisyonda kismi iade desteklenmiyor. Tam iade yapin.");

        // Iade kaydi
        var refund = new SlnInvoiceRefund
        {
            InvoiceId = invoiceId,
            RefundAmount = refundAmount,
            RefundMethodId = refundMethodId,
            Reason = reason,
            PersonnelId = personnelId
        };
        _invoiceRefunds.Add(refund);

        // Tam iade ise adisyonu iptal et
        if (isFullRefund)
        {
            var (packageSaleCancelled, packageSaleError) = await _loyaltyPackages.CancelPurchaseFromInvoiceAsync(customerId, invoice.Notes);
            if (!packageSaleCancelled) return (null, packageSaleError);

            var (packageUsagesReversed, packageUsageError) = await _loyaltyPackages.ReverseInvoiceRedemptionsAsync(customerId, invoice.Id);
            if (!packageUsagesReversed) return (null, packageUsageError);

            var (giftCardSaleCancelled, giftCardSaleError) = await _giftCards.CancelGiftCardSaleFromInvoiceAsync(customerId, invoice.Notes);
            if (!giftCardSaleCancelled) return (null, giftCardSaleError);

            var (giftCardRedemptionsReversed, giftCardRedemptionError) = await _giftCards.ReverseInvoiceRedemptionsAsync(customerId, invoice.Id);
            if (!giftCardRedemptionsReversed) return (null, giftCardRedemptionError);

            invoice.StatusId = 3; // Cancelled

            // Urun stok geri yukle
            foreach (var item in invoice.Items.Where(i => i.ProductId != null && i.Product != null))
            {
                var (stockOk, stockError) = await _stockBalances.AdjustStockAsync(
                    item.Product!, customerId, invoice.BranchId, item.Quantity, preventNegative: false);
                if (!stockOk) return (null, stockError);
                await _stockBalances.SyncProductTotalAsync(item.Product!, customerId);
                _stockMovements.Add(new SlnStockMovement
                {
                    CustomerId = customerId,
                    BranchId = invoice.BranchId,
                    ProductId = item.Product!.Id,
                    MovementTypeId = 5,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Notes = $"Adisyon iade: {invoice.InvoiceNo}",
                    CreatedByPersonnelId = personnelId
                });
            }
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

    public async Task<object> GetStaffRevenueAsync(int customerId, DateTime startDate, DateTime endDate, int? branchId = null)
    {
        var query = _invoiceItems.GetAllQueryable()
            .Include(i => i.Invoice)
            .Include(i => i.Personnel).ThenInclude(p => p!.User)
            .Where(i => i.Invoice!.CustomerId == customerId
                     && i.Invoice.InvoiceDate >= startDate
                     && i.Invoice.InvoiceDate < endDate
                     && i.Invoice.StatusId != 3
                     && i.PersonnelId != null);

        if (branchId.HasValue)
            query = query.Where(i => i.Invoice!.BranchId == branchId.Value);

        var invoiceItems = await query.ToListAsync();

        var personnelIds = invoiceItems.Where(i => i.PersonnelId.HasValue).Select(i => i.PersonnelId!.Value).Distinct().ToList();
        var commissions = await _personnelCommissions.GetAllQueryable()
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

    public async Task<object> GetIncomeExpenseReportAsync(int customerId, DateTime startDate, DateTime endDate, int? branchId = null)
    {
        // Gelirler (adisyonlar)
        var invoiceQuery = _invoices.GetAllQueryable()
            .Where(i => i.CustomerId == customerId && i.InvoiceDate >= startDate && i.InvoiceDate < endDate && i.StatusId != 3);

        if (branchId.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.BranchId == branchId.Value);

        var invoices = await invoiceQuery.ToListAsync();

        // Giderler (masraflar)
        var expenseQuery = _expenses.GetAllQueryable()
            .Include(e => e.Category)
            .Where(e => e.CustomerId == customerId && e.ExpenseDate >= startDate && e.ExpenseDate < endDate && e.StatusId != 3);

        if (branchId.HasValue)
            expenseQuery = expenseQuery.Where(e => e.BranchId == branchId.Value);

        var expenses = await expenseQuery.ToListAsync();

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

    public async Task<object> GetTaxReportAsync(int customerId, DateTime startDate, DateTime endDate, int? branchId = null)
    {
        var taxQuery = _invoiceItems.GetAllQueryable()
            .Include(i => i.Invoice)
            .Where(i => i.Invoice!.CustomerId == customerId && i.Invoice.InvoiceDate >= startDate && i.Invoice.InvoiceDate < endDate && i.Invoice.StatusId != 3);

        if (branchId.HasValue)
            taxQuery = taxQuery.Where(i => i.Invoice!.BranchId == branchId.Value);

        var invoiceItems = await taxQuery.ToListAsync();

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

    private static bool HasPackageSaleNote(string? notes)
        => !string.IsNullOrWhiteSpace(notes)
            && (notes.Contains("PackageSale:", StringComparison.OrdinalIgnoreCase)
                || notes.Contains("SessionPlanSale:", StringComparison.OrdinalIgnoreCase));

    private static bool HasGiftCardSaleNote(string? notes)
        => !string.IsNullOrWhiteSpace(notes)
            && notes.Contains("GiftCardSale:", StringComparison.OrdinalIgnoreCase);
}
