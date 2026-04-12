using System.Security.Claims;
using System.Text.RegularExpressions;
using CallCenter.Api.Filters;
using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-profile")]
[Authorize]
[RequireModule(SalonPortalModules.Ids.SlnProfile)]
public class SlnProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GcsUploadService _gcs;

    public SlnProfileController(AppDbContext db, GcsUploadService gcs)
    {
        _db = db;
        _gcs = gcs;
    }

    [HttpGet]
    public async Task<ActionResult> GetProfile()
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var customer = await _db.Customers.FindAsync(cid);
        if (customer == null) return Unauthorized();

        var profile = await _db.SlnSalonProfiles
            .FirstOrDefaultAsync(p => p.CustomerId == cid);

        if (profile == null)
            return Ok(new { exists = false, billingType = customer.BillingType });

        // Merkez sube bilgilerini al (public sayfa icin geriye uyumluluk)
        var hqBranch = await _db.SlnBranches
            .FirstOrDefaultAsync(b => b.CustomerId == cid && b.IsHeadquarter);

        return Ok(new SlnSalonProfileDto
        {
            Id = profile.Id,
            CustomerId = profile.CustomerId,
            SalonName = customer.Name,
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
        });
    }

    [HttpPost]
    public async Task<ActionResult> SaveProfile([FromBody] SlnSalonProfileUpdateDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var customer = await _db.Customers.FindAsync(cid);
        if (customer == null) return Unauthorized();

        var profile = await _db.SlnSalonProfiles.FirstOrDefaultAsync(p => p.CustomerId == cid);
        if (profile == null)
        {
            profile = new SlnSalonProfile { CustomerId = cid };
            _db.SlnSalonProfiles.Add(profile);
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
        var hasAnyBranch = await _db.SlnBranches.AnyAsync(b => b.CustomerId == cid);
        if (!hasAnyBranch)
        {
            var hqBranch = new SlnBranch
            {
                CustomerId = cid,
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
            _db.SlnBranches.Add(hqBranch);
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("page-settings")]
    public async Task<ActionResult> SavePageSettings([FromBody] SlnPageSettingsDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var profile = await _db.SlnSalonProfiles.FirstOrDefaultAsync(p => p.CustomerId == cid);
        if (profile == null) return BadRequest("Once salon profili olusturun.");

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

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Gorsel yukle (banner, logo vb.). Multipart form-data.</summary>
    [HttpPost("upload-image")]
    [RequestSizeLimit(5_242_880)] // 5 MB
    public async Task<ActionResult> UploadImage(IFormFile file, [FromQuery] string type = "banner")
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        if (file == null || file.Length == 0) return BadRequest("Dosya secilmedi.");
        if (file.Length > 5_242_880) return BadRequest("Dosya 5 MB'dan buyuk olamaz.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Sadece JPEG, PNG ve WebP desteklenir.");

        var ext = file.ContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };

        var fileName = $"{type}-{Guid.NewGuid():N}{ext}";
        var path = $"salons/{cid}/{fileName}";

        using var stream = file.OpenReadStream();
        var (url, error) = await _gcs.UploadAsync(stream, path, file.ContentType);

        if (url == null) return BadRequest(error ?? "Yukleme hatasi.");
        return Ok(new { url, path });
    }

    /// <summary>Yuklenmis gorseli sil</summary>
    [HttpDelete("delete-image")]
    public async Task<ActionResult> DeleteImage([FromQuery] string path)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();
        if (string.IsNullOrEmpty(path) || !path.StartsWith($"salons/{cid}/"))
            return BadRequest("Gecersiz dosya yolu.");

        await _gcs.DeleteAsync(path);
        return Ok();
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

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
