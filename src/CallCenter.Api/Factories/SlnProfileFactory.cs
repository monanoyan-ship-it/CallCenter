using System.Text.RegularExpressions;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnProfileFactory : ISlnProfileFactory
{
    private readonly ISlnSalonProfileEntityService _profiles;
    private readonly ICustomerEntityService _customers;
    private readonly ISlnBranchEntityService _branches;
    private readonly IUnitOfWork _uow;

    public SlnProfileFactory(
        ISlnSalonProfileEntityService profiles,
        ICustomerEntityService customers,
        ISlnBranchEntityService branches,
        IUnitOfWork uow)
    {
        _profiles = profiles;
        _customers = customers;
        _branches = branches;
        _uow = uow;
    }

    public async Task<SlnPaymentInfoDto?> GetPaymentInfoAsync(int customerId)
    {
        var profile = await _profiles.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.CustomerId == customerId);
        var customer = await _customers.GetByIdAsync(customerId);
        if (customer == null) return null;

        return new SlnPaymentInfoDto
        {
            SubMerchantType = profile?.IyzicoSubMerchantType,
            Iban = profile?.IyzicoIban,
            LegalCompanyTitle = profile?.IyzicoLegalCompanyTitle,
            TaxOffice = profile?.IyzicoTaxOffice,
            TaxNumber = profile?.IyzicoTaxNumber,
            IdentityNumber = profile?.IyzicoIdentityNumber,
            ContactName = profile?.IyzicoContactName,
            ContactSurname = profile?.IyzicoContactSurname,
            OnboardingStatus = profile?.IyzicoOnboardingStatus ?? 0,
            OnboardedAt = profile?.IyzicoOnboardedAt,
            OnboardingError = profile?.IyzicoOnboardingError,
            SubMerchantKey = !string.IsNullOrEmpty(profile?.IyzicoSubMerchantKey),
            CommissionPercent = customer.MarketplaceCommissionPercent,
            WithholdingPercent = customer.MarketplaceWithholdingPercent
        };
    }

    public async Task<object?> GetProfileAsync(int customerId)
    {
        var customer = await _customers.GetByIdAsync(customerId);
        if (customer == null) return null;

        var profile = await _profiles.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.CustomerId == customerId);

        if (profile == null)
            return new { exists = false, billingType = customer.BillingType };

        // Merkez sube bilgilerini al (public sayfa icin geriye uyumluluk)
        var hqBranch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);

        return new SlnSalonProfileDto
        {
            Id = profile.Id,
            CustomerId = profile.CustomerId,
            SalonName = customer.Name,
            BranchName = hqBranch?.Name,
            IsHeadquarter = hqBranch?.IsHeadquarter ?? true,
            Description = profile.Description,
            Website = profile.Website,
            InstagramHandle = profile.InstagramHandle,
            FacebookUrl = profile.FacebookUrl,
            LogoUrl = profile.LogoUrl,
            CoverImageUrl = profile.CoverImageUrl,
            FaviconUrl = profile.FaviconUrl,
            GalleryImagesJson = profile.GalleryImagesJson,
            IsPublished = profile.IsPublished,
            BillingType = customer.BillingType,
            ShowServices = profile.ShowServices,
            ShowMemberships = profile.ShowMemberships,
            ShowBooking = profile.ShowBooking,
            ShowHours = profile.ShowHours,
            ShowContact = profile.ShowContact,
            SectionOrderJson = profile.SectionOrderJson,
            ShowBanners = profile.ShowBanners,
            ShowTeam = profile.ShowTeam,
            ShowReviews = profile.ShowReviews,
            ShowMap = profile.ShowMap,
            BannersJson = profile.BannersJson,
            // Merkez subeden alinan alanlar
            Slug = hqBranch?.Slug ?? profile.Slug ?? "",
            Address = hqBranch?.Address ?? profile.Address,
            City = hqBranch?.City ?? profile.City,
            District = hqBranch?.District ?? profile.District,
            Phone = hqBranch?.Phone ?? profile.Phone,
            Email = hqBranch?.Email ?? profile.Email,
            GoogleMapsUrl = hqBranch?.GoogleMapsUrl ?? profile.GoogleMapsUrl,
            WorkingHoursJson = hqBranch?.WorkingHoursJson ?? profile.WorkingHoursJson,
            Latitude = hqBranch?.Latitude ?? profile.Latitude,
            Longitude = hqBranch?.Longitude ?? profile.Longitude
        };
    }

    public async Task<(bool Success, string? Error)> SaveProfileAsync(SlnSalonProfileUpdateDto dto, int customerId)
    {
        var customer = await _customers.GetByIdAsync(customerId);
        if (customer == null) return (false, "Musteri bulunamadi");

        var profile = await _profiles.GetAllQueryable().FirstOrDefaultAsync(p => p.CustomerId == customerId);
        if (profile == null)
        {
            profile = new SlnSalonProfile { CustomerId = customerId };
            _profiles.Add(profile);
        }

        profile.Description = dto.Description;
        profile.Website = dto.Website;
        profile.InstagramHandle = dto.InstagramHandle;
        profile.FacebookUrl = dto.FacebookUrl;
        profile.IsPublished = dto.IsPublished;
        profile.UpdatedAt = DateTime.UtcNow;

        // BillingType kaydet
        customer.BillingType = dto.BillingType;

        // Merkez sube yoksa otomatik olustur
        var hasAnyBranch = await _branches.GetAllQueryable().AnyAsync(b => b.CustomerId == customerId);
        if (!hasAnyBranch)
        {
            var hqBranch = new SlnBranch
            {
                CustomerId = customerId,
                Name = "Merkez",
                IsHeadquarter = true,
                IsActive = true,
                CompanyTitle = customer.Name,
                // Mevcut profildeki bilgileri merkez subeye tasi
                Address = profile.Address,
                City = profile.City,
                District = profile.District,
                Phone = profile.Phone,
                Email = profile.Email,
                WorkingHoursJson = profile.WorkingHoursJson,
                GoogleMapsUrl = profile.GoogleMapsUrl,
                Latitude = profile.Latitude,
                Longitude = profile.Longitude,
                Slug = GenerateSlug(customer.Name)
            };
            _branches.Add(hqBranch);
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SavePageSettingsAsync(SlnPageSettingsDto dto, int customerId)
    {
        var profile = await _profiles.GetAllQueryable().FirstOrDefaultAsync(p => p.CustomerId == customerId);
        if (profile == null) return (false, "Once salon profili olusturun.");

        profile.ShowServices = dto.ShowServices;
        profile.ShowMemberships = dto.ShowMemberships;
        profile.ShowBooking = dto.ShowBooking;
        profile.ShowHours = dto.ShowHours;
        profile.ShowContact = dto.ShowContact;
        profile.ShowBanners = dto.ShowBanners;
        profile.ShowTeam = dto.ShowTeam;
        profile.ShowReviews = dto.ShowReviews;
        profile.ShowMap = dto.ShowMap;
        profile.SectionOrderJson = dto.SectionOrderJson;
        profile.BannersJson = dto.BannersJson;
        profile.LogoUrl = dto.LogoUrl;
        profile.CoverImageUrl = dto.CoverImageUrl;
        profile.FaviconUrl = dto.FaviconUrl;
        profile.GalleryImagesJson = dto.GalleryImagesJson;
        profile.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var slug = input.ToLowerInvariant().Trim();
        slug = slug.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                   .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s-]+", "-").Trim('-');
        return slug;
    }
}
