using System.Security.Claims;
using System.Text.RegularExpressions;
using CallCenter.Api.Services;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-profile")]
[Authorize]
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

        var profile = await _db.SlnSalonProfiles
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.CustomerId == cid);

        if (profile == null)
            return Ok(new { exists = false });

        return Ok(new SlnSalonProfileDto
        {
            Id = profile.Id,
            Slug = profile.Slug,
            SalonName = profile.Customer?.Name ?? "",
            Description = profile.Description,
            Address = profile.Address,
            City = profile.City,
            District = profile.District,
            Phone = profile.Phone,
            Email = profile.Email,
            Website = profile.Website,
            InstagramHandle = profile.InstagramHandle,
            FacebookUrl = profile.FacebookUrl,
            GoogleMapsUrl = profile.GoogleMapsUrl,
            LogoUrl = profile.LogoUrl,
            CoverImageUrl = profile.CoverImageUrl,
            WorkingHoursJson = profile.WorkingHoursJson,
            IsPublished = profile.IsPublished,
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
            Latitude = profile.Latitude,
            Longitude = profile.Longitude
        });
    }

    [HttpPost]
    public async Task<ActionResult> SaveProfile([FromBody] SlnSalonProfileUpdateDto dto)
    {
        var cid = GetCustomerId();
        if (cid == 0) return Unauthorized();

        var slug = GenerateSlug(dto.Slug);
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest("Gecerli bir slug gerekli");

        // Slug benzersizlik kontrolu
        var existing = await _db.SlnSalonProfiles.FirstOrDefaultAsync(p => p.Slug == slug && p.CustomerId != cid);
        if (existing != null) return BadRequest("Bu adres zaten kullaniliyor");

        var profile = await _db.SlnSalonProfiles.FirstOrDefaultAsync(p => p.CustomerId == cid);
        if (profile == null)
        {
            profile = new SlnSalonProfile { CustomerId = cid };
            _db.SlnSalonProfiles.Add(profile);
        }

        profile.Slug = slug;
        profile.Description = dto.Description;
        profile.Address = dto.Address;
        profile.City = dto.City;
        profile.District = dto.District;
        profile.Phone = dto.Phone;
        profile.Email = dto.Email;
        profile.Website = dto.Website;
        profile.InstagramHandle = dto.InstagramHandle;
        profile.FacebookUrl = dto.FacebookUrl;
        profile.GoogleMapsUrl = dto.GoogleMapsUrl;
        profile.WorkingHoursJson = dto.WorkingHoursJson;
        profile.IsPublished = dto.IsPublished;
        profile.UpdatedAt = DateTime.UtcNow;

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
        // Turkce karakter donusumu
        slug = slug.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                   .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s-]+", "-").Trim('-');
        return slug;
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
