using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnScannerFactory : ISlnScannerFactory
{
    private const string TokenPrefix = "slnscan:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ISlnProductFactory _productFactory;
    private readonly ISlnGiftCardFactory _giftCardFactory;
    private readonly ISlnBranchEntityService _branches;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnLoyaltyPackagePurchaseEntityService _loyaltyPurchases;
    private readonly ISlnClientMembershipEntityService _memberships;
    private readonly ISlnProductEntityService _products;
    private readonly ISlnGiftCardEntityService _giftCards;
    private readonly PaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public SlnScannerFactory(
        ISlnProductFactory productFactory,
        ISlnGiftCardFactory giftCardFactory,
        ISlnBranchEntityService branches,
        ISlnClientEntityService clients,
        ISlnAppointmentEntityService appointments,
        ISlnLoyaltyPackagePurchaseEntityService loyaltyPurchases,
        ISlnClientMembershipEntityService memberships,
        ISlnProductEntityService products,
        ISlnGiftCardEntityService giftCards,
        PaymentService paymentService,
        IConfiguration configuration)
    {
        _productFactory = productFactory;
        _giftCardFactory = giftCardFactory;
        _branches = branches;
        _clients = clients;
        _appointments = appointments;
        _loyaltyPurchases = loyaltyPurchases;
        _memberships = memberships;
        _products = products;
        _giftCards = giftCards;
        _paymentService = paymentService;
        _configuration = configuration;
    }

    public async Task<SlnScanResolveDto> ResolvePublicAsync(SlnScanResolveRequest request)
    {
        var raw = NormalizeRaw(request.Code);
        if (string.IsNullOrWhiteSpace(raw))
            return NotFound(raw, "empty", "Okunacak kod bos.");

        var publicLink = TryResolvePublicRoute(raw);
        if (publicLink == null)
            return NotFound(raw, "unknown", "QR/link desteklenen bir Salon linki degil.");

        await EnrichBranchBySlugAsync(publicLink);
        return publicLink;
    }

    public async Task<SlnScanResolveDto> ResolveSalonAsync(SlnScanResolveRequest request, int customerId, int? claimBranchId)
    {
        var raw = NormalizeRaw(request.Code);
        if (string.IsNullOrWhiteSpace(raw))
            return NotFound(raw, "empty", "Okunacak kod bos.");

        var requestedBranchId = claimBranchId ?? request.BranchId;
        var tokenPayload = TryReadToken(raw, out var tokenError);
        if (tokenPayload != null)
            return await ResolveTokenPayloadAsync(tokenPayload, raw, customerId, claimBranchId);

        if (tokenError != null)
            return NotFound(raw, "scanToken", tokenError);

        var publicLink = TryResolvePublicRoute(raw);
        if (publicLink != null)
        {
            await EnrichBranchBySlugAsync(publicLink, customerId);
            return publicLink;
        }

        var prefixResult = await TryResolveKnownPrefixAsync(raw, customerId, requestedBranchId);
        if (prefixResult != null)
            return prefixResult;

        var context = (request.Context ?? string.Empty).Trim().ToLowerInvariant();
        var wantsGiftCard = context.Contains("gift") || context.Contains("hediye");
        var wantsProduct = context.Contains("product") || context.Contains("urun") || context.Contains("barcode") || context.Contains("barkod") || context.Contains("stock") || context.Contains("sale");

        if (wantsGiftCard)
        {
            var gift = await ResolveGiftCardAsync(raw, customerId, requestedBranchId);
            if (gift.Found) return gift;
        }

        var product = await ResolveProductBarcodeAsync(raw, customerId, requestedBranchId);
        if (product.Found || wantsProduct || product.ScanType == "productBarcodeAmbiguous")
            return product;

        if (!wantsGiftCard)
        {
            var gift = await ResolveGiftCardAsync(raw, customerId, requestedBranchId);
            if (gift.Found) return gift;
        }

        return NotFound(raw, "unknown", "Kod bu salon icin urun, hediye karti veya desteklenen QR olarak cozulmedi.");
    }

    public async Task<SlnScanTokenDto> CreateTokenAsync(SlnScanTokenCreateRequest request, int customerId, int? claimBranchId, bool isSalonOwner)
    {
        var targetType = NormalizeTargetType(request.TargetType);
        if (string.IsNullOrWhiteSpace(targetType))
            throw new InvalidOperationException("TargetType zorunludur.");

        var branchId = isSalonOwner ? request.BranchId : claimBranchId;
        var payload = new ScanTokenPayload
        {
            V = 1,
            Type = targetType,
            CustomerId = customerId,
            BranchId = branchId,
            TargetId = request.TargetId,
            Slug = string.IsNullOrWhiteSpace(request.Slug) ? null : request.Slug.Trim(),
            Exp = NormalizeExpiry(request.ExpiresAt),
            ReturnPath = NormalizeReturnPath(request.ReturnPath)
        };

        var webUrl = await ValidateAndCompletePayloadAsync(payload, claimBranchId, isSalonOwner);
        var token = SignPayload(payload);
        return new SlnScanTokenDto
        {
            Token = token,
            DeepLink = "corplynk-salon://scan/" + Uri.EscapeDataString(token),
            WebUrl = webUrl,
            ExpiresAt = payload.Exp
        };
    }

    private async Task<SlnScanResolveDto> ResolveProductBarcodeAsync(string raw, int customerId, int? branchId)
    {
        var products = await _productFactory.GetProductsByBarcodeAsync(raw, customerId, branchId);
        var product = PickSingleBarcodeProduct(products, branchId);
        if (product != null)
        {
            return new SlnScanResolveDto
            {
                Found = true,
                ScanType = "productBarcode",
                Action = "addProduct",
                RawValue = raw,
                NormalizedValue = NormalizeRaw(raw),
                CustomerId = customerId,
                BranchId = product.BranchId ?? branchId,
                Product = product
            };
        }

        if (products.Count > 0)
        {
            return new SlnScanResolveDto
            {
                Found = false,
                ScanType = "productBarcodeAmbiguous",
                Action = "chooseProduct",
                RawValue = raw,
                NormalizedValue = NormalizeRaw(raw),
                CustomerId = customerId,
                BranchId = branchId,
                Message = "Bu barkod birden fazla urunle eslesiyor.",
                Metadata =
                {
                    ["count"] = products.Count.ToString(),
                    ["productIds"] = string.Join(",", products.Select(p => p.Id))
                }
            };
        }

        return NotFound(raw, "productBarcode", "Barkodla eslesen urun bulunamadi.");
    }

    private async Task<SlnScanResolveDto> ResolveGiftCardAsync(string raw, int customerId, int? branchId)
    {
        var code = StripKnownPrefix(raw, "gift:", "gift-card:", "hediye:", "hediye-karti:");
        var card = await _giftCardFactory.GetGiftCardByCodeAsync(code, customerId, branchId);
        if (card == null)
            return NotFound(raw, "giftCard", "Hediye karti bulunamadi.");

        return new SlnScanResolveDto
        {
            Found = true,
            ScanType = "giftCard",
            Action = "openGiftCard",
            RawValue = raw,
            NormalizedValue = code.Trim().ToUpperInvariant(),
            CustomerId = customerId,
            BranchId = card.BranchId ?? branchId,
            GiftCard = card
        };
    }

    private async Task<SlnScanResolveDto?> TryResolveKnownPrefixAsync(string raw, int customerId, int? branchId)
    {
        var value = raw.Trim();
        if (TryReadPrefixedInt(value, out var appointmentId, "appointment:", "appt:", "randevu:"))
            return await ResolveAppointmentAsync(appointmentId, raw, customerId, branchId);

        if (TryReadPrefixedInt(value, out var packageId, "package:", "client-package:", "pkg:", "seans:"))
            return await ResolveClientPackageAsync(packageId, raw, customerId, branchId);

        if (TryReadPrefixedInt(value, out var membershipId, "membership:", "member:", "uyelik:"))
            return await ResolveMembershipAsync(membershipId, raw, customerId, branchId);

        if (TryReadPrefixedGuid(value, out var clientUid, "client:", "clt:", "musteri:") || Guid.TryParse(value, out clientUid))
            return await ResolveClientAsync(clientUid, raw, customerId, branchId);

        return null;
    }

    private async Task<SlnScanResolveDto> ResolveTokenPayloadAsync(ScanTokenPayload payload, string raw, int customerId, int? claimBranchId)
    {
        if (payload.CustomerId != customerId)
            return NotFound(raw, "scanToken", "Bu QR farkli bir salona ait.");

        if (payload.Exp.HasValue && payload.Exp.Value < DateTime.UtcNow)
            return NotFound(raw, "scanToken", "Bu QR kodun suresi dolmus.");

        var branchId = claimBranchId ?? payload.BranchId;
        return payload.Type switch
        {
            "branch" or "booking" => await ResolveBranchTokenAsync(payload, raw, customerId, claimBranchId),
            "product" => payload.TargetId.HasValue
                ? await ResolveProductTokenAsync(payload.TargetId.Value, raw, customerId, branchId)
                : NotFound(raw, "scanToken", "Urun token hedefi eksik."),
            "giftCard" => payload.TargetId.HasValue
                ? await ResolveGiftCardTokenAsync(payload.TargetId.Value, raw, customerId, branchId)
                : NotFound(raw, "scanToken", "Hediye karti token hedefi eksik."),
            "client" => payload.TargetId.HasValue
                ? await ResolveClientByIdAsync(payload.TargetId.Value, raw, customerId, branchId)
                : NotFound(raw, "scanToken", "Musteri token hedefi eksik."),
            "appointment" => payload.TargetId.HasValue
                ? await ResolveAppointmentAsync(payload.TargetId.Value, raw, customerId, branchId)
                : NotFound(raw, "scanToken", "Randevu token hedefi eksik."),
            "clientPackage" => payload.TargetId.HasValue
                ? await ResolveClientPackageAsync(payload.TargetId.Value, raw, customerId, branchId)
                : NotFound(raw, "scanToken", "Seans paketi token hedefi eksik."),
            "membership" => payload.TargetId.HasValue
                ? await ResolveMembershipAsync(payload.TargetId.Value, raw, customerId, branchId)
                : NotFound(raw, "scanToken", "Uyelik token hedefi eksik."),
            _ => NotFound(raw, "scanToken", "Desteklenmeyen QR token tipi.")
        };
    }

    private async Task<SlnScanResolveDto> ResolveBranchTokenAsync(ScanTokenPayload payload, string raw, int customerId, int? claimBranchId)
    {
        var query = _branches.GetAllQueryable().Where(b => b.CustomerId == customerId);
        if (payload.TargetId.HasValue)
            query = query.Where(b => b.Id == payload.TargetId.Value);
        else if (!string.IsNullOrWhiteSpace(payload.Slug))
        {
            var slug = payload.Slug.Trim().ToLowerInvariant();
            query = query.Where(b => b.Slug != null && b.Slug.ToLower() == slug);
        }
        else
            return NotFound(raw, "branchQr", "Sube hedefi eksik.");

        var branch = await query.FirstOrDefaultAsync();
        if (branch == null || !IsVisibleForBranchClaim(branch.Id, claimBranchId))
            return NotFound(raw, "branchQr", "Sube bulunamadi veya yetki disi.");

        return BranchResult(raw, branch, payload.Type == "booking" ? "openBooking" : "openProfile");
    }

    private async Task<SlnScanResolveDto> ResolveProductTokenAsync(int productId, string raw, int customerId, int? branchId)
    {
        var product = await _productFactory.GetProductAsync(productId, customerId, branchId);
        return product == null
            ? NotFound(raw, "productQr", "Urun bulunamadi veya yetki disi.")
            : new SlnScanResolveDto
            {
                Found = true,
                ScanType = "productQr",
                Action = "openProduct",
                RawValue = raw,
                CustomerId = customerId,
                BranchId = product.BranchId ?? branchId,
                Product = product
            };
    }

    private async Task<SlnScanResolveDto> ResolveGiftCardTokenAsync(int giftCardId, string raw, int customerId, int? branchId)
    {
        var card = await _giftCardFactory.GetGiftCardAsync(giftCardId, customerId, branchId);
        return card == null
            ? NotFound(raw, "giftCardQr", "Hediye karti bulunamadi.")
            : new SlnScanResolveDto
            {
                Found = true,
                ScanType = "giftCardQr",
                Action = "openGiftCard",
                RawValue = raw,
                CustomerId = customerId,
                BranchId = card.BranchId ?? branchId,
                GiftCard = card
            };
    }

    private async Task<SlnScanResolveDto> ResolveClientAsync(Guid uid, string raw, int customerId, int? branchId)
    {
        var client = await SalonBranchScope.ApplyToClients(
                _clients.GetAllQueryable().Where(c => c.CustomerId == customerId && c.Uid == uid),
                branchId)
            .FirstOrDefaultAsync();

        return client == null
            ? NotFound(raw, "clientQr", "Musteri bulunamadi veya yetki disi.")
            : ClientResult(raw, customerId, client);
    }

    private async Task<SlnScanResolveDto> ResolveClientByIdAsync(int clientId, string raw, int customerId, int? branchId)
    {
        var client = await SalonBranchScope.ApplyToClients(
                _clients.GetAllQueryable().Where(c => c.CustomerId == customerId && c.Id == clientId),
                branchId)
            .FirstOrDefaultAsync();

        return client == null
            ? NotFound(raw, "clientQr", "Musteri bulunamadi veya yetki disi.")
            : ClientResult(raw, customerId, client);
    }

    private async Task<SlnScanResolveDto> ResolveAppointmentAsync(int appointmentId, string raw, int customerId, int? branchId)
    {
        var appointment = await _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId && a.Id == appointmentId)
            .Include(a => a.SlnClient)
            .Include(a => a.Personnel).ThenInclude(p => p!.User)
            .Include(a => a.Branch)
            .Include(a => a.Combo)
            .Include(a => a.Service)
            .Include(a => a.Services).ThenInclude(s => s.SlnService)
            .FirstOrDefaultAsync();

        if (appointment == null || !IsVisibleForBranchScope(appointment.BranchId, branchId))
            return NotFound(raw, "appointmentQr", "Randevu bulunamadi veya yetki disi.");

        var paidAmount = await _paymentService.GetAppointmentPaidAmountAsync(appointment.Id);

        return new SlnScanResolveDto
        {
            Found = true,
            ScanType = "appointmentQr",
            Action = "openAppointment",
            RawValue = raw,
            CustomerId = customerId,
            BranchId = appointment.BranchId,
            BranchName = appointment.Branch?.Name,
            Client = appointment.SlnClient != null ? MapClient(appointment.SlnClient) : null,
            Appointment = MapAppointment(appointment, paidAmount)
        };
    }

    private async Task<SlnScanResolveDto> ResolveClientPackageAsync(int packageId, string raw, int customerId, int? branchId)
    {
        var package = await SalonBranchScope.ApplyToLoyaltyPackagePurchases(
                _loyaltyPurchases.GetAllQueryable().Where(p => p.CustomerId == customerId && p.Id == packageId),
                branchId)
            .Include(p => p.Offer).ThenInclude(o => o!.Service)
            .Include(p => p.SlnClient)
            .FirstOrDefaultAsync();

        if (package == null)
            return NotFound(raw, "clientPackageQr", "Seans paketi bulunamadi veya yetki disi.");

        return new SlnScanResolveDto
        {
            Found = true,
            ScanType = "clientPackageQr",
            Action = "openClientPackage",
            RawValue = raw,
            CustomerId = customerId,
            BranchId = package.BranchId ?? branchId,
            Client = package.SlnClient != null ? MapClient(package.SlnClient) : null,
            LoyaltyPackage = MapLoyaltyPurchase(package)
        };
    }

    private async Task<SlnScanResolveDto> ResolveMembershipAsync(int membershipId, string raw, int customerId, int? branchId)
    {
        var membership = await SalonBranchScope.ApplyToMemberships(
                _memberships.GetAllQueryable().Where(m => m.CustomerId == customerId && m.Id == membershipId),
                branchId)
            .Include(m => m.Plan)
            .Include(m => m.SlnClient)
            .FirstOrDefaultAsync();

        if (membership == null)
            return NotFound(raw, "membershipQr", "Uyelik bulunamadi veya yetki disi.");

        return new SlnScanResolveDto
        {
            Found = true,
            ScanType = "membershipQr",
            Action = "openMembership",
            RawValue = raw,
            CustomerId = customerId,
            BranchId = membership.Plan?.BranchId ?? branchId,
            Client = membership.SlnClient != null ? MapClient(membership.SlnClient) : null,
            Membership = MapMembership(membership)
        };
    }

    private async Task<string?> ValidateAndCompletePayloadAsync(ScanTokenPayload payload, int? claimBranchId, bool isSalonOwner)
    {
        switch (payload.Type)
        {
            case "branch":
            case "booking":
            {
                var branch = await FindBranchForTokenAsync(payload);
                if (branch == null) throw new InvalidOperationException("Sube bulunamadi.");
                if (!isSalonOwner && !IsVisibleForBranchClaim(branch.Id, claimBranchId)) throw new InvalidOperationException("Sube yetki disi.");
                payload.TargetId = branch.Id;
                payload.BranchId = branch.Id;
                payload.Slug = branch.Slug;
                return BuildSalonUrl(branch.Slug, payload.Type == "booking" ? "/book" : "");
            }
            case "client":
                await RequireClientAsync(payload.TargetId, payload.CustomerId, claimBranchId);
                return null;
            case "appointment":
                await RequireAppointmentAsync(payload.TargetId, payload.CustomerId, claimBranchId);
                return null;
            case "product":
                await RequireProductAsync(payload.TargetId, payload.CustomerId, claimBranchId);
                return null;
            case "giftCard":
                await RequireGiftCardAsync(payload.TargetId, payload.CustomerId, isSalonOwner ? payload.BranchId : claimBranchId);
                return null;
            case "clientPackage":
                await RequireClientPackageAsync(payload.TargetId, payload.CustomerId, claimBranchId);
                return null;
            case "membership":
                await RequireMembershipAsync(payload.TargetId, payload.CustomerId, claimBranchId);
                return null;
            default:
                throw new InvalidOperationException("Desteklenmeyen TargetType.");
        }
    }

    private async Task<SlnBranch?> FindBranchForTokenAsync(ScanTokenPayload payload)
    {
        var query = _branches.GetAllQueryable().Where(b => b.CustomerId == payload.CustomerId && b.IsActive);
        if (payload.TargetId.HasValue)
            query = query.Where(b => b.Id == payload.TargetId.Value);
        else if (!string.IsNullOrWhiteSpace(payload.Slug))
        {
            var slug = payload.Slug.Trim().ToLowerInvariant();
            query = query.Where(b => b.Slug != null && b.Slug.ToLower() == slug);
        }
        else if (payload.BranchId.HasValue)
            query = query.Where(b => b.Id == payload.BranchId.Value);
        else
            return null;

        return await query.FirstOrDefaultAsync();
    }

    private async Task RequireClientAsync(int? id, int customerId, int? branchId)
    {
        if (!id.HasValue) throw new InvalidOperationException("Musteri hedefi eksik.");
        var exists = await SalonBranchScope.ApplyToClients(
                _clients.GetAllQueryable().Where(c => c.Id == id.Value && c.CustomerId == customerId),
                branchId)
            .AnyAsync();
        if (!exists) throw new InvalidOperationException("Musteri bulunamadi veya yetki disi.");
    }

    private async Task RequireAppointmentAsync(int? id, int customerId, int? branchId)
    {
        if (!id.HasValue) throw new InvalidOperationException("Randevu hedefi eksik.");
        var appointment = await _appointments.GetAllQueryable()
            .Where(a => a.Id == id.Value && a.CustomerId == customerId)
            .Select(a => new { a.BranchId })
            .FirstOrDefaultAsync();
        if (appointment == null || !IsVisibleForBranchScope(appointment.BranchId, branchId))
            throw new InvalidOperationException("Randevu bulunamadi veya yetki disi.");
    }

    private async Task RequireProductAsync(int? id, int customerId, int? branchId)
    {
        if (!id.HasValue) throw new InvalidOperationException("Urun hedefi eksik.");
        var exists = await _products.GetAllQueryable()
            .AnyAsync(p => p.Id == id.Value && p.CustomerId == customerId && (!branchId.HasValue || p.BranchId == null || p.BranchId == branchId.Value));
        if (!exists) throw new InvalidOperationException("Urun bulunamadi veya yetki disi.");
    }

    private async Task RequireGiftCardAsync(int? id, int customerId, int? branchId)
    {
        if (!id.HasValue) throw new InvalidOperationException("Hediye karti hedefi eksik.");
        var exists = await SalonBranchScope.ApplyToGiftCards(
                _giftCards.GetAllQueryable().Where(g => g.Id == id.Value && g.CustomerId == customerId),
                branchId)
            .AnyAsync();
        if (!exists) throw new InvalidOperationException("Hediye karti bulunamadi veya yetki disi.");
    }

    private async Task RequireClientPackageAsync(int? id, int customerId, int? branchId)
    {
        if (!id.HasValue) throw new InvalidOperationException("Seans paketi hedefi eksik.");
        var exists = await SalonBranchScope.ApplyToLoyaltyPackagePurchases(
                _loyaltyPurchases.GetAllQueryable().Where(p => p.Id == id.Value && p.CustomerId == customerId),
                branchId)
            .AnyAsync();
        if (!exists) throw new InvalidOperationException("Seans paketi bulunamadi veya yetki disi.");
    }

    private async Task RequireMembershipAsync(int? id, int customerId, int? branchId)
    {
        if (!id.HasValue) throw new InvalidOperationException("Uyelik hedefi eksik.");
        var exists = await SalonBranchScope.ApplyToMemberships(
                _memberships.GetAllQueryable().Where(m => m.Id == id.Value && m.CustomerId == customerId),
                branchId)
            .AnyAsync();
        if (!exists) throw new InvalidOperationException("Uyelik bulunamadi veya yetki disi.");
    }

    private async Task EnrichBranchBySlugAsync(SlnScanResolveDto result, int? customerId = null)
    {
        if (string.IsNullOrWhiteSpace(result.Slug))
            return;

        var slug = result.Slug.Trim().ToLowerInvariant();
        var query = _branches.GetAllQueryable()
            .Where(b => b.Slug != null && b.Slug.ToLower() == slug && b.IsActive);

        if (customerId.HasValue)
            query = query.Where(b => b.CustomerId == customerId.Value);

        var branch = await query.FirstOrDefaultAsync();
        if (branch == null)
        {
            if (customerId.HasValue)
                result.Message = "Bu QR bu salon hesabina ait degil.";
            return;
        }

        result.Found = true;
        result.CustomerId = branch.CustomerId;
        result.BranchId = branch.Id;
        result.BranchName = branch.Name;
    }

    private static SlnScanResolveDto? TryResolvePublicRoute(string raw)
    {
        var value = raw.Trim();
        if (TryExtractSalonSlugFromUri(value, out var slug, out var bookingUrl, out var normalizedUrl))
        {
            return new SlnScanResolveDto
            {
                Found = true,
                ScanType = bookingUrl ? "publicBookingLink" : "publicSalonLink",
                Action = bookingUrl ? "openBooking" : "openProfile",
                RawValue = raw,
                NormalizedValue = slug,
                Slug = slug,
                Url = normalizedUrl
            };
        }

        var prefixed = StripKnownPrefix(value, "branch:", "sube:", "booking:", "book:", "salon:");
        if (!ReferenceEquals(prefixed, value) && IsSlugLike(prefixed))
        {
            return new SlnScanResolveDto
            {
                Found = true,
                ScanType = "publicBookingLink",
                Action = "openBooking",
                RawValue = raw,
                NormalizedValue = prefixed,
                Slug = prefixed
            };
        }

        return null;
    }

    private static bool TryExtractSalonSlugFromUri(string raw, out string slug, out bool bookingUrl, out string? normalizedUrl)
    {
        slug = string.Empty;
        bookingUrl = false;
        normalizedUrl = null;

        var candidate = raw.Trim();
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            var relative = candidate.Trim('/');
            var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2 && parts[0].Equals("salon", StringComparison.OrdinalIgnoreCase))
            {
                slug = parts[1];
                bookingUrl = parts.Length >= 3 && parts[2].Equals("book", StringComparison.OrdinalIgnoreCase);
                normalizedUrl = "/salon/" + slug + (bookingUrl ? "/book" : "");
                return IsSlugLike(slug);
            }
            return false;
        }

        if (uri.Scheme.Equals("corplynk-salon", StringComparison.OrdinalIgnoreCase))
        {
            var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (uri.Host.Equals("book", StringComparison.OrdinalIgnoreCase) && parts.Length >= 1)
            {
                slug = parts[0];
                bookingUrl = true;
            }
            else if (uri.Host.Equals("salon", StringComparison.OrdinalIgnoreCase) && parts.Length >= 1)
            {
                slug = parts[0];
                bookingUrl = parts.Length >= 2 && parts[1].Equals("book", StringComparison.OrdinalIgnoreCase);
            }

            normalizedUrl = string.IsNullOrWhiteSpace(slug) ? null : "/salon/" + slug + (bookingUrl ? "/book" : "");
            return IsSlugLike(slug);
        }

        var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pathParts.Length >= 2 && pathParts[0].Equals("salon", StringComparison.OrdinalIgnoreCase))
        {
            slug = pathParts[1];
            bookingUrl = pathParts.Length >= 3 && pathParts[2].Equals("book", StringComparison.OrdinalIgnoreCase);
            normalizedUrl = uri.GetLeftPart(UriPartial.Authority) + "/salon/" + slug + (bookingUrl ? "/book" : "");
            return IsSlugLike(slug);
        }

        return false;
    }

    private string SignPayload(ScanTokenPayload payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadBytes = Encoding.UTF8.GetBytes(json);
        var payloadPart = WebEncoders.Base64UrlEncode(payloadBytes);
        var signature = Sign(payloadPart);
        return TokenPrefix + payloadPart + "." + signature;
    }

    private ScanTokenPayload? TryReadToken(string raw, out string? error)
    {
        error = null;
        var token = ExtractToken(raw);
        if (token == null)
            return null;

        var body = token[TokenPrefix.Length..];
        var parts = body.Split('.', 2);
        if (parts.Length != 2)
        {
            error = "QR token formati gecersiz.";
            return null;
        }

        var expected = Sign(parts[0]);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(parts[1])))
        {
            error = "QR token imzasi gecersiz.";
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(parts[0]));
            return JsonSerializer.Deserialize<ScanTokenPayload>(json, JsonOptions);
        }
        catch
        {
            error = "QR token okunamadi.";
            return null;
        }
    }

    private static string? ExtractToken(string raw)
    {
        var value = raw.Trim();
        if (value.StartsWith(TokenPrefix, StringComparison.OrdinalIgnoreCase))
            return TokenPrefix + value[TokenPrefix.Length..];

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme.Equals("corplynk-salon", StringComparison.OrdinalIgnoreCase) && uri.Host.Equals("scan", StringComparison.OrdinalIgnoreCase))
        {
            var token = uri.AbsolutePath.Trim('/');
            return Uri.UnescapeDataString(token);
        }

        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        if (query.TryGetValue("scanToken", out var scanToken) || query.TryGetValue("token", out scanToken))
            return scanToken.ToString();

        return null;
    }

    private string Sign(string payloadPart)
    {
        var key = _configuration["Scanner:SigningKey"];
        if (string.IsNullOrWhiteSpace(key)) key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key)) key = _configuration["Encryption:Key"];
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Scanner signing key tanimli degil.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return WebEncoders.Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadPart)));
    }

    private string? BuildSalonUrl(string? slug, string suffix)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var baseUrl = _configuration["Salon:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = "https://sln.corplynk.com";
        return baseUrl.TrimEnd('/') + "/salon/" + slug.Trim('/') + suffix;
    }

    private static SlnScanResolveDto BranchResult(string raw, SlnBranch branch, string action) => new()
    {
        Found = true,
        ScanType = action == "openBooking" ? "publicBookingLink" : "publicSalonLink",
        Action = action,
        RawValue = raw,
        NormalizedValue = branch.Slug,
        Slug = branch.Slug,
        CustomerId = branch.CustomerId,
        BranchId = branch.Id,
        BranchName = branch.Name
    };

    private static SlnScanResolveDto ClientResult(string raw, int customerId, SlnClient client) => new()
    {
        Found = true,
        ScanType = "clientQr",
        Action = "openClient",
        RawValue = raw,
        CustomerId = customerId,
        BranchId = client.BranchId,
        Client = MapClient(client)
    };

    private static SlnClientDto MapClient(SlnClient c) => new()
    {
        Id = c.Id,
        Uid = c.Uid,
        BranchId = c.BranchId,
        FullName = c.FullName,
        Phone = c.Phone,
        Email = c.Email,
        GenderId = c.GenderId,
        BirthDate = c.BirthDate,
        HairColor = c.HairColor,
        IsFavorite = c.IsFavorite,
        CreatedAt = c.CreatedAt,
        HealthInfoRequiresReview = c.HealthInfoRequiresReview
    };

    private static SlnAppointmentDto MapAppointment(SlnAppointment a, decimal paidAmount = 0m)
    {
        var serviceIds = a.Services.Count > 0
            ? a.Services.OrderBy(s => s.SortOrder).Select(s => s.SlnServiceId).ToList()
            : a.ServiceId.HasValue ? [a.ServiceId.Value] : [];

        var serviceNames = a.Services.Count > 0
            ? a.Services.OrderBy(s => s.SortOrder).Select(s => s.SlnService?.Name ?? "").ToList()
            : a.Service != null ? [a.Service.Name] : [];

        return new SlnAppointmentDto
        {
            Id = a.Id,
            SlnClientId = a.SlnClientId,
            ClientName = a.SlnClient?.FullName ?? "",
            ClientPhone = a.SlnClient?.Phone,
            PersonnelId = a.PersonnelId,
            PersonnelName = a.Personnel?.User?.FullName ?? "",
            BranchId = a.BranchId,
            BranchName = a.Branch?.Name,
            ComboId = a.ComboId,
            ComboName = a.Combo?.Name,
            ServiceIds = serviceIds,
            ServiceNames = serviceNames,
            DurationMinutes = (int)(a.EndTime - a.StartTime).TotalMinutes,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            StatusId = a.StatusId,
            Notes = a.Notes,
            IsPrepaid = a.IsPrepaid,
            PrepaidAmount = a.PrepaidAmount,
            PaidAmount = paidAmount,
            DepositAmount = a.DepositAmount,
            ClientNoShowCount = a.SlnClient?.NoShowCount ?? 0,
            ClientIsBlacklisted = a.SlnClient?.IsBlacklisted ?? false
        };
    }

    private static SlnLoyaltyPackagePurchaseDto MapLoyaltyPurchase(SlnLoyaltyPackagePurchase p)
    {
        var price = p.SaleAmount > 0 ? p.SaleAmount : p.Offer?.Price ?? 0;
        return new SlnLoyaltyPackagePurchaseDto
        {
            Id = p.Id,
            OfferId = p.OfferId,
            ServiceId = p.Offer?.ServiceId ?? 0,
            BranchId = p.BranchId,
            SourceInvoiceId = p.SourceInvoiceId,
            SourceInvoiceItemId = p.SourceInvoiceItemId,
            OfferName = p.Offer?.Name ?? "",
            ServiceName = p.Offer?.Service?.Name ?? "",
            ClientName = p.SlnClient?.FullName,
            TotalSessions = p.TotalSessions,
            UsedSessions = p.UsedSessions,
            RemainingSessions = p.RemainingSessions,
            OfferPrice = p.Offer?.Price ?? 0,
            SaleAmount = price,
            PaidAmount = p.PaidAmount,
            BalanceAmount = Math.Max(0, price - p.PaidAmount),
            ExpiresAt = p.ExpiresAt,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        };
    }

    private static SlnClientMembershipDto MapMembership(SlnClientMembership m) => new()
    {
        Id = m.Id,
        PlanName = m.Plan?.Name ?? "",
        PlanColor = m.Plan?.Color,
        ClientName = m.SlnClient?.FullName ?? "",
        DiscountPercent = m.Plan?.DiscountPercent ?? 0,
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        CurrentPeriodStart = m.CurrentPeriodStart,
        CurrentPeriodEnd = m.CurrentPeriodEnd,
        PaidAmount = m.PaidAmount,
        StatusId = m.StatusId
    };

    private static SlnProductDto? PickSingleBarcodeProduct(List<SlnProductDto> products, int? branchId)
    {
        if (products.Count == 1) return products[0];
        if (!branchId.HasValue) return null;

        var branchProducts = products.Where(p => p.BranchId == branchId.Value).ToList();
        if (branchProducts.Count == 1) return branchProducts[0];

        var globalProducts = products.Where(p => !p.BranchId.HasValue).ToList();
        return branchProducts.Count == 0 && globalProducts.Count == 1 ? globalProducts[0] : null;
    }

    private static bool IsVisibleForBranchScope(int? targetBranchId, int? branchId)
        => !branchId.HasValue || !targetBranchId.HasValue || targetBranchId.Value == branchId.Value;

    private static bool IsVisibleForBranchClaim(int targetBranchId, int? claimBranchId)
        => !claimBranchId.HasValue || targetBranchId == claimBranchId.Value;

    private static string NormalizeRaw(string? code)
        => (code ?? string.Empty).Trim();

    private static string NormalizeTargetType(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant().Replace("_", "-");
        return normalized switch
        {
            "branch" or "sube" => "branch",
            "booking" or "book" or "randevu-link" => "booking",
            "product" or "urun" => "product",
            "gift" or "gift-card" or "hediye" or "hediye-karti" => "giftCard",
            "client" or "customer" or "musteri" => "client",
            "appointment" or "randevu" => "appointment",
            "package" or "client-package" or "seans" or "seans-paketi" => "clientPackage",
            "membership" or "member" or "uyelik" => "membership",
            _ => string.Empty
        };
    }

    private static DateTime? NormalizeExpiry(DateTime? expiresAt)
    {
        if (!expiresAt.HasValue) return null;
        var value = expiresAt.Value;
        return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }

    private static string? NormalizeReturnPath(string? returnPath)
    {
        if (string.IsNullOrWhiteSpace(returnPath)) return null;
        var value = returnPath.Trim();
        return value.StartsWith('/') && !value.StartsWith("//") ? value : null;
    }

    private static bool TryReadPrefixedInt(string raw, out int id, params string[] prefixes)
    {
        id = 0;
        foreach (var prefix in prefixes)
        {
            if (!raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            return int.TryParse(raw[prefix.Length..].Trim(), out id) && id > 0;
        }
        return false;
    }

    private static bool TryReadPrefixedGuid(string raw, out Guid id, params string[] prefixes)
    {
        id = Guid.Empty;
        foreach (var prefix in prefixes)
        {
            if (!raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            return Guid.TryParse(raw[prefix.Length..].Trim(), out id);
        }
        return false;
    }

    private static string StripKnownPrefix(string raw, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return raw[prefix.Length..].Trim();
        }
        return raw;
    }

    private static bool IsSlugLike(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && value.Length <= 120
            && value.All(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_');

    private static SlnScanResolveDto NotFound(string raw, string scanType, string message) => new()
    {
        Found = false,
        ScanType = scanType,
        Action = "none",
        RawValue = raw,
        NormalizedValue = NormalizeRaw(raw),
        Message = message
    };

    private sealed class ScanTokenPayload
    {
        public int V { get; set; }
        public string Type { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public int? BranchId { get; set; }
        public int? TargetId { get; set; }
        public string? Slug { get; set; }
        public DateTime? Exp { get; set; }
        public string? ReturnPath { get; set; }
    }
}
