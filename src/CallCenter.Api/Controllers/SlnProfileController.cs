using System.Security.Claims;
using System.Text.RegularExpressions;
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

    public SlnProfileController(AppDbContext db) => _db = db;

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
            IsPublished = profile.IsPublished
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
