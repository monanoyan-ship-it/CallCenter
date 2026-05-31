using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnLoyaltyPackageFactory : ISlnLoyaltyPackageFactory
{
    private readonly ISlnLoyaltyPackageOfferEntityService _offerEs;
    private readonly ISlnLoyaltyPackagePurchaseEntityService _purchaseEs;
    private readonly ISlnLoyaltyPackageRedemptionEntityService _redemptionEs;
    private readonly ISlnClientEntityService _clients;
    private readonly IUnitOfWork _uow;

    public SlnLoyaltyPackageFactory(
        ISlnLoyaltyPackageOfferEntityService offerEs,
        ISlnLoyaltyPackagePurchaseEntityService purchaseEs,
        ISlnLoyaltyPackageRedemptionEntityService redemptionEs,
        ISlnClientEntityService clients,
        IUnitOfWork uow)
    {
        _offerEs = offerEs;
        _purchaseEs = purchaseEs;
        _redemptionEs = redemptionEs;
        _clients = clients;
        _uow = uow;
    }

    // ═══ Teklif Tanimlari (Offer) ═══

    public async Task<List<SlnLoyaltyPackageOfferDto>> GetOffersAsync(int customerId)
    {
        return await _offerEs.GetAllQueryable()
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.Service)
            .OrderBy(o => o.Name)
            .Select(o => new SlnLoyaltyPackageOfferDto
            {
                Id = o.Id,
                Name = o.Name,
                Description = o.Description,
                ServiceId = o.ServiceId,
                ServiceName = o.Service != null ? o.Service.Name : "",
                TotalSessions = o.TotalSessions,
                BonusSessions = o.BonusSessions,
                Price = o.Price,
                PricePerSession = o.TotalSessions > 0 ? Math.Round(o.Price / o.TotalSessions, 2) : 0,
                ValidDays = o.ValidDays,
                IsActive = o.IsActive
            }).ToListAsync();
    }

    public async Task<SlnLoyaltyPackageOfferDto> CreateOfferAsync(SlnLoyaltyPackageOfferCreateDto dto, int customerId)
    {
        var existing = await _offerEs.GetAllQueryable()
            .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.ServiceId == dto.ServiceId);
        if (existing != null)
        {
            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.TotalSessions = dto.TotalSessions;
            existing.BonusSessions = dto.BonusSessions;
            existing.Price = dto.Price;
            existing.ValidDays = dto.ValidDays;
            existing.IsActive = dto.IsActive;
            await _uow.SaveChangesAsync();
            return (await GetOffersAsync(customerId)).First(x => x.Id == existing.Id);
        }

        var offer = new SlnLoyaltyPackageOffer
        {
            CustomerId = customerId,
            Name = dto.Name,
            Description = dto.Description,
            ServiceId = dto.ServiceId,
            TotalSessions = dto.TotalSessions,
            BonusSessions = dto.BonusSessions,
            Price = dto.Price,
            ValidDays = dto.ValidDays,
            IsActive = dto.IsActive
        };
        _offerEs.Add(offer);
        await _uow.SaveChangesAsync();

        return (await GetOffersAsync(customerId)).First(x => x.Id == offer.Id);
    }

    public async Task<(bool Success, string? Error)> UpdateOfferAsync(int id, SlnLoyaltyPackageOfferCreateDto dto, int customerId)
    {
        var offer = await _offerEs.GetAllQueryable().FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);
        if (offer == null) return (false, "Sadakat paketi teklifi bulunamadi");

        var duplicateExists = await _offerEs.GetAllQueryable()
            .AnyAsync(o => o.Id != id && o.CustomerId == customerId && o.ServiceId == dto.ServiceId);
        if (duplicateExists) return (false, "Bu hizmet icin zaten sadakat paketi teklifi var");

        offer.Name = dto.Name;
        offer.Description = dto.Description;
        offer.ServiceId = dto.ServiceId;
        offer.TotalSessions = dto.TotalSessions;
        offer.BonusSessions = dto.BonusSessions;
        offer.Price = dto.Price;
        offer.ValidDays = dto.ValidDays;
        offer.IsActive = dto.IsActive;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteOfferAsync(int id, int customerId)
    {
        var offer = await _offerEs.GetAllQueryable().FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);
        if (offer == null) return (false, "Sadakat paketi teklifi bulunamadi");
        var hasSoldPurchases = await _purchaseEs.GetAllQueryable()
            .AnyAsync(p => p.CustomerId == customerId && p.OfferId == id);
        if (hasSoldPurchases) return (false, "Satilmis paket olan teklif silinemez. Pasife alin veya once satislari iptal edin.");
        _offerEs.Remove(offer);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══ Musteri Paketleri (Purchase) ═══

    public async Task<List<SlnLoyaltyPackagePurchaseDto>> GetPurchasesAsync(int customerId, int? clientId = null, int? branchId = null)
    {
        var query = SalonBranchScope.ApplyToLoyaltyPackagePurchases(
                _purchaseEs.GetAllQueryable().Where(p => p.CustomerId == customerId),
                branchId)
            .Include(p => p.Offer).ThenInclude(o => o!.Service)
            .Include(p => p.SlnClient)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(p => p.SlnClientId == clientId.Value);

        return await query.OrderByDescending(p => p.CreatedAt).Select(p => new SlnLoyaltyPackagePurchaseDto
        {
            Id = p.Id,
            OfferId = p.OfferId,
            ServiceId = p.Offer != null ? p.Offer.ServiceId : 0,
            BranchId = p.BranchId,
            SourceInvoiceId = p.SourceInvoiceId,
            SourceInvoiceItemId = p.SourceInvoiceItemId,
            OfferName = p.Offer != null ? p.Offer.Name : "",
            ServiceName = p.Offer != null && p.Offer.Service != null ? p.Offer.Service.Name : "",
            ClientName = p.SlnClient != null ? p.SlnClient.FullName : null,
            TotalSessions = p.TotalSessions,
            UsedSessions = p.UsedSessions,
            RemainingSessions = p.RemainingSessions,
            OfferPrice = p.Offer != null ? p.Offer.Price : 0,
            SaleAmount = p.SaleAmount > 0 ? p.SaleAmount : (p.Offer != null ? p.Offer.Price : 0),
            PaidAmount = p.PaidAmount,
            BalanceAmount = (p.SaleAmount > 0 ? p.SaleAmount : (p.Offer != null ? p.Offer.Price : 0)) > p.PaidAmount
                ? (p.SaleAmount > 0 ? p.SaleAmount : (p.Offer != null ? p.Offer.Price : 0)) - p.PaidAmount
                : 0,
            ExpiresAt = p.ExpiresAt,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        }).ToListAsync();
    }

    public async Task<(SlnLoyaltyPackagePurchaseDto? Purchase, string? Error)> SellPurchaseAsync(SlnLoyaltyPackagePurchaseSellDto dto, int userId, int customerId, int? branchId = null)
    {
        var offer = await _offerEs.GetAllQueryable()
            .FirstOrDefaultAsync(o => o.Id == dto.OfferId && o.CustomerId == customerId);
        if (offer == null) return (null, "Sadakat paketi teklifi bulunamadi");
        if (!offer.IsActive) return (null, "Sadakat paketi teklifi aktif degil");
        if (!dto.SlnClientId.HasValue) return (null, "Sadakat paketi satisi icin musteri secilmelidir");

        var created = await CreateLoyaltyPurchasesFromInvoiceAsync(
            customerId,
            dto.SlnClientId.Value,
            0,
            [new SlnLoyaltyPackageSaleLine(offer.ServiceId, offer.Price, 1)],
            userId,
            branchId);

        return created.Count > 0
            ? (created[0], null)
            : (null, "Sadakat paketi satisi kaydedilemedi");
    }

    public async Task<List<SlnLoyaltyPackagePurchaseDto>> CreateLoyaltyPurchasesFromInvoiceAsync(int customerId, int slnClientId, int invoiceId, IEnumerable<SlnLoyaltyPackageSaleLine> lines, int userId, int? branchId = null)
    {
        if (slnClientId <= 0)
            return [];

        var clientExists = await SalonBranchScope.ApplyToClients(
                _clients.GetAllQueryable().Where(c => c.Id == slnClientId && c.CustomerId == customerId),
                branchId)
            .AnyAsync();
        if (!clientExists)
            return [];

        var saleLines = lines
            .Where(l => l.ServiceId > 0 && l.Quantity > 0)
            .GroupBy(l => l.ServiceId)
            .Select(g => new
            {
                ServiceId = g.Key,
                Quantity = g.Sum(l => l.Quantity),
                PaidAmount = g.Sum(l => l.PaidAmount),
                InvoiceItemId = g.Select(l => l.InvoiceItemId).FirstOrDefault(id => id.HasValue)
            })
            .ToList();

        if (saleLines.Count == 0)
            return [];

        var serviceIds = saleLines.Select(l => l.ServiceId).Distinct().ToList();
        var offers = await _offerEs.GetAllQueryable()
            .Where(o => o.CustomerId == customerId
                && o.IsActive
                && o.TotalSessions > 0
                && serviceIds.Contains(o.ServiceId))
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        var created = new List<SlnLoyaltyPackagePurchase>();
        foreach (var line in saleLines)
        {
            var offer = offers.FirstOrDefault(o => o.ServiceId == line.ServiceId);
            if (offer == null)
                continue;

            var planCount = Math.Max(1, line.Quantity);
            var paidPerPlan = planCount > 0 ? Math.Round(line.PaidAmount / planCount, 2) : line.PaidAmount;
            for (var i = 0; i < planCount; i++)
            {
                var purchase = new SlnLoyaltyPackagePurchase
                {
                    CustomerId = customerId,
                    OfferId = offer.Id,
                    SlnClientId = slnClientId,
                    BranchId = branchId,
                    TotalSessions = offer.TotalSessions,
                    UsedSessions = 0,
                    RemainingSessions = offer.TotalSessions,
                    SaleAmount = offer.Price,
                    PaidAmount = paidPerPlan,
                    SourceInvoiceId = invoiceId > 0 ? invoiceId : null,
                    SourceInvoiceItemId = line.InvoiceItemId,
                    ExpiresAt = DateTime.UtcNow.AddDays(offer.ValidDays),
                    IsActive = true,
                    SoldByPersonnelId = userId
                };
                _purchaseEs.Add(purchase);
                created.Add(purchase);
            }
        }

        if (created.Count == 0)
            return [];

        await _uow.SaveChangesAsync();

        var ids = created.Select(p => p.Id).ToList();
        return (await GetPurchasesAsync(customerId, slnClientId, branchId))
            .Where(p => ids.Contains(p.Id))
            .ToList();
    }

    public async Task<(bool Success, string? Error)> RedeemSessionAsync(SlnLoyaltyPackageRedeemDto dto, int userId, int customerId, int? branchId = null)
    {
        return await RecordRedemptionAsync(customerId, dto.PurchaseId, null, null, userId, dto.Notes, branchId);
    }

    public async Task<List<SlnLoyaltyPackageRedemptionDto>> GetRedemptionHistoryAsync(int customerId, int? purchaseId = null, int? branchId = null)
    {
        var scopedPurchaseIds = SalonBranchScope.ApplyToLoyaltyPackagePurchases(
                _purchaseEs.GetAllQueryable().Where(p => p.CustomerId == customerId),
                branchId)
            .Select(p => p.Id);

        var query = _redemptionEs.GetAllQueryable()
            .Where(r => scopedPurchaseIds.Contains(r.PurchaseId));

        if (purchaseId.HasValue)
            query = query.Where(r => r.PurchaseId == purchaseId.Value);

        return await query
            .Include(r => r.Purchase).ThenInclude(p => p!.Offer).ThenInclude(o => o!.Service)
            .Include(r => r.Purchase).ThenInclude(p => p!.SlnClient)
            .Include(r => r.Personnel).ThenInclude(p => p!.User)
            .OrderByDescending(r => r.UsedAt)
            .Select(r => new SlnLoyaltyPackageRedemptionDto
            {
                Id = r.Id,
                PurchaseId = r.PurchaseId,
                InvoiceId = r.InvoiceId,
                InvoiceItemId = r.InvoiceItemId,
                ServiceId = r.ServiceId,
                SlnAppointmentId = r.SlnAppointmentId,
                OfferName = r.Purchase != null && r.Purchase.Offer != null ? r.Purchase.Offer.Name : "",
                ServiceName = r.Purchase != null && r.Purchase.Offer != null && r.Purchase.Offer.Service != null ? r.Purchase.Offer.Service.Name : "",
                ClientName = r.Purchase != null && r.Purchase.SlnClient != null ? r.Purchase.SlnClient.FullName : null,
                PersonnelName = r.Personnel != null && r.Personnel.User != null ? r.Personnel.User.FullName : null,
                Notes = r.Notes,
                UsedAt = r.UsedAt
            })
            .ToListAsync();
    }

    public async Task<List<SlnLoyaltyPackageBenefitDto>> GetUsablePurchasesAsync(int customerId, int slnClientId, IEnumerable<int> serviceIds, int? branchId = null)
    {
        var ids = serviceIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return [];

        var now = DateTime.UtcNow;
        return await SalonBranchScope.ApplyToLoyaltyPackagePurchases(
                _purchaseEs.GetAllQueryable()
                    .Where(p => p.CustomerId == customerId
                        && p.SlnClientId == slnClientId
                        && p.IsActive
                        && p.RemainingSessions > 0
                        && (!p.ExpiresAt.HasValue || p.ExpiresAt.Value >= now)),
                branchId)
            .Include(p => p.Offer)
            .Where(p => p.Offer != null && ids.Contains(p.Offer.ServiceId))
            .OrderBy(p => p.ExpiresAt.HasValue ? 0 : 1)
            .ThenBy(p => p.ExpiresAt)
            .ThenBy(p => p.Id)
            .Select(p => new SlnLoyaltyPackageBenefitDto
            {
                PurchaseId = p.Id,
                OfferId = p.OfferId,
                ServiceId = p.Offer!.ServiceId,
                OfferName = p.Offer.Name,
                RemainingSessions = p.RemainingSessions,
                ExpiresAt = p.ExpiresAt
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> RecordRedemptionAsync(int customerId, int purchaseId, int? serviceId, int? slnClientId, int userId, string? notes, int? branchId = null, int? invoiceId = null, int? invoiceItemId = null, int? appointmentId = null)
    {
        var purchase = await SalonBranchScope.ApplyToLoyaltyPackagePurchases(
                _purchaseEs.GetAllQueryable().Where(p => p.Id == purchaseId && p.CustomerId == customerId),
                branchId)
            .Include(p => p.Offer)
            .FirstOrDefaultAsync();
        if (purchase == null) return (false, "Sadakat paketi bulunamadi");
        if (slnClientId.HasValue && purchase.SlnClientId != slnClientId.Value) return (false, "Paket secili musteriye ait degil");
        if (serviceId.HasValue && purchase.Offer?.ServiceId != serviceId.Value) return (false, "Paket bu hizmet icin kullanilamaz");
        if (!purchase.IsActive) return (false, "Bu paket aktif degil");
        if (purchase.RemainingSessions <= 0) return (false, "Kalan seans yok");
        if (purchase.ExpiresAt.HasValue && purchase.ExpiresAt.Value < DateTime.UtcNow) return (false, "Paketin suresi dolmus");

        purchase.UsedSessions++;
        purchase.RemainingSessions--;
        if (purchase.RemainingSessions == 0) purchase.IsActive = false;

        _redemptionEs.Add(new SlnLoyaltyPackageRedemption
        {
            PurchaseId = purchase.Id,
            PersonnelId = userId > 0 ? userId : null,
            InvoiceId = invoiceId,
            InvoiceItemId = invoiceItemId,
            ServiceId = serviceId,
            SlnAppointmentId = appointmentId,
            Notes = notes
        });

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ReverseInvoiceRedemptionsAsync(int customerId, int invoiceId)
    {
        var redemptions = await _redemptionEs.GetAllQueryable()
            .Include(r => r.Purchase)
            .Where(r => r.Purchase != null
                && r.Purchase.CustomerId == customerId
                && (r.InvoiceId == invoiceId
                    || (r.Notes != null && r.Notes.Contains($"Invoice:{invoiceId}|"))))
            .ToListAsync();

        foreach (var redemption in redemptions)
        {
            var purchase = redemption.Purchase!;
            purchase.UsedSessions = Math.Max(0, purchase.UsedSessions - 1);
            purchase.RemainingSessions = Math.Min(purchase.TotalSessions, purchase.RemainingSessions + 1);
            if (purchase.RemainingSessions > 0 && (!purchase.ExpiresAt.HasValue || purchase.ExpiresAt.Value >= DateTime.UtcNow))
                purchase.IsActive = true;
            _redemptionEs.Remove(redemption);
        }

        if (redemptions.Count > 0)
            await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelPurchaseFromInvoiceAsync(int customerId, string? invoiceNotes)
    {
        var purchaseIds = TryReadNoteInts(invoiceNotes, "LoyaltyPackageSale:");
        var legacyIds = TryReadNoteInts(invoiceNotes, "SessionPlanSale:");
        purchaseIds.AddRange(legacyIds);
        var legacyPackageId = TryReadNoteInt(invoiceNotes, "PackageSale:");
        if (legacyPackageId.HasValue)
            purchaseIds.Add(legacyPackageId.Value);

        purchaseIds = purchaseIds.Distinct().ToList();
        if (purchaseIds.Count == 0)
            return (true, null);

        var purchases = await _purchaseEs.GetAllQueryable()
            .Where(p => purchaseIds.Contains(p.Id) && p.CustomerId == customerId)
            .ToListAsync();
        if (purchases.Count == 0)
            return (true, null);

        if (purchases.Any(p => p.UsedSessions > 0))
            return (false, "Kullanilmis sadakat paketi satisi iptal edilemez. Once manuel/pro-rata iade akisi uygulanmali.");

        foreach (var purchase in purchases)
        {
            purchase.IsActive = false;
            purchase.RemainingSessions = 0;
            purchase.PaidAmount = 0;
        }
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static List<int> TryReadNoteInts(string? notes, string prefix)
    {
        var values = new List<int>();
        if (string.IsNullOrWhiteSpace(notes))
            return values;

        var index = notes.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return values;

        var start = index + prefix.Length;
        var end = start;
        while (end < notes.Length && (char.IsDigit(notes[end]) || notes[end] == ','))
            end++;

        foreach (var part in notes[start..end].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var value))
                values.Add(value);
        }

        return values;
    }

    private static int? TryReadNoteInt(string? notes, string prefix)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return null;

        var index = notes.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var start = index + prefix.Length;
        var end = start;
        while (end < notes.Length && char.IsDigit(notes[end]))
            end++;

        return end > start && int.TryParse(notes[start..end], out var value) ? value : null;
    }
}
