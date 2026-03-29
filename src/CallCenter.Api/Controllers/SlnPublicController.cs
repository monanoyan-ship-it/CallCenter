using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

/// <summary>
/// Herkese acik salon profil endpoint'leri (auth gerekmez)
/// </summary>
[ApiController]
[Route("api/salon")]
public class SlnPublicController : ControllerBase
{
    private readonly AppDbContext _db;

    public SlnPublicController(AppDbContext db) => _db = db;

    /// <summary>Slug ile salon profili getir</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<SlnSalonProfileDto>> GetBySlug(string slug)
    {
        var profile = await _db.SlnSalonProfiles
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

        if (profile == null) return NotFound();

        var categories = await _db.SlnServiceCategories
            .Where(c => c.CustomerId == profile.CustomerId && c.IsActive)
            .Include(c => c.Services.Where(s => s.IsActive))
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

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
            ServiceCategories = categories.Select(c => new SlnServiceCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                IconClass = c.IconClass,
                Color = c.Color,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                Services = c.Services.OrderBy(s => s.SortOrder).Select(s => new SlnServiceDto
                {
                    Id = s.Id,
                    CategoryId = s.CategoryId,
                    CategoryName = c.Name,
                    Name = s.Name,
                    DurationMinutes = s.DurationMinutes,
                    Price = s.Price,
                    IsActive = s.IsActive
                }).ToList()
            }).ToList()
        });
    }

    /// <summary>Tum yayinlanmis salonlari listele</summary>
    [HttpGet]
    public async Task<ActionResult> GetAllPublished()
    {
        var profiles = await _db.SlnSalonProfiles
            .Where(p => p.IsPublished)
            .Include(p => p.Customer)
            .OrderBy(p => p.Customer!.Name)
            .Select(p => new
            {
                p.Slug,
                SalonName = p.Customer != null ? p.Customer.Name : "",
                p.City,
                p.District,
                p.LogoUrl,
                p.Description
            }).ToListAsync();

        return Ok(profiles);
    }
}
