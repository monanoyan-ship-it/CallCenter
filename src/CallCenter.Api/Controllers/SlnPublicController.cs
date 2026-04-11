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
            CustomerId = profile.CustomerId,
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
            Longitude = profile.Longitude,
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
                p.Description,
                p.Latitude,
                p.Longitude
            }).ToListAsync();

        return Ok(profiles);
    }

    /// <summary>Salonun onaylanmis yorumlarini getir</summary>
    [HttpGet("{slug}/reviews")]
    public async Task<ActionResult> GetReviews(string slug)
    {
        var profile = await _db.SlnSalonProfiles
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (profile == null) return NotFound();

        var reviews = await _db.SlnReviews
            .Where(r => r.CustomerId == profile.CustomerId && r.StatusId == 2) // 2=Approved
            .OrderByDescending(r => r.CreatedAt)
            .Take(20)
            .Select(r => new
            {
                r.ClientName, r.Rating, r.Comment, r.SourceId, r.CreatedAt
            })
            .ToListAsync();

        var stats = new
        {
            totalCount = reviews.Count,
            averageRating = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0
        };

        return Ok(new { reviews, stats });
    }

    /// <summary>Salonun ekibini getir (aktif personel)</summary>
    [HttpGet("{slug}/team")]
    public async Task<ActionResult> GetTeam(string slug)
    {
        var profile = await _db.SlnSalonProfiles
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (profile == null) return NotFound();

        var team = await _db.Set<CustomerPersonnel>()
            .Where(p => p.CustomerId == profile.CustomerId && p.IsActive)
            .Include(p => p.User)
            .OrderBy(p => p.CustomerRoleId)
            .Select(p => new
            {
                name = p.User.FullName,
                title = p.Title,
                specialty = p.Specialty,
                photoUrl = p.PhotoUrl,
                roleId = p.CustomerRoleId
            })
            .ToListAsync();

        return Ok(team);
    }

    /// <summary>Salonun aktif uyelik planlarini getir</summary>
    [HttpGet("{slug}/memberships")]
    public async Task<ActionResult> GetMembershipPlans(string slug)
    {
        var profile = await _db.SlnSalonProfiles
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (profile == null) return NotFound();

        var plans = await _db.SlnMembershipPlans
            .Where(p => p.CustomerId == profile.CustomerId && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new
            {
                p.Id, p.Name, p.Description, p.IconClass, p.Color,
                p.DurationType, p.DurationDays, p.Price, p.DiscountPercent, p.PriorityBooking
            })
            .ToListAsync();

        return Ok(plans);
    }

    /// <summary>Online uyelik basvurusu (auth gerekmez)</summary>
    [HttpPost("{slug}/membership-signup")]
    public async Task<ActionResult> MembershipSignup(string slug, [FromBody] SlnMembershipSignupDto dto)
    {
        var profile = await _db.SlnSalonProfiles
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (profile == null) return NotFound();

        var plan = await _db.SlnMembershipPlans
            .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.CustomerId == profile.CustomerId && p.IsActive);
        if (plan == null) return BadRequest("Uyelik plani bulunamadi.");

        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
            return BadRequest("Ad ve telefon zorunludur.");

        // Musteri bul veya olustur
        var client = await _db.SlnClients
            .FirstOrDefaultAsync(c => c.Phone == dto.Phone && c.CustomerId == profile.CustomerId);

        if (client == null)
        {
            client = new SlnClient
            {
                CustomerId = profile.CustomerId,
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email
            };
            _db.SlnClients.Add(client);
            await _db.SaveChangesAsync();
        }
        else
        {
            // Mevcut musterinin zaten aktif uyeligi var mi?
            var existing = await _db.SlnClientMemberships
                .AnyAsync(m => m.SlnClientId == client.Id && m.CustomerId == profile.CustomerId && m.StatusId == 1);
            if (existing)
                return BadRequest("Bu telefon numarasina ait zaten aktif bir uyelik bulunmaktadir.");
        }

        var now = DateTime.UtcNow;
        var membership = new SlnClientMembership
        {
            CustomerId = profile.CustomerId,
            PlanId = plan.Id,
            SlnClientId = client.Id,
            StartDate = now,
            CurrentPeriodStart = plan.DurationType == 1 ? now : null,
            CurrentPeriodEnd = plan.DurationType == 1 ? now.AddDays(plan.DurationDays) : null,
            EndDate = plan.DurationType == 1 ? now.AddDays(plan.DurationDays) : null,
            PaidAmount = plan.Price,
            StatusId = 1
        };

        _db.SlnClientMemberships.Add(membership);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Uyelik basvurunuz alinmistir. Salonumuz sizinle iletisime gececektir." });
    }

    /// <summary>Belirli salon + tarih + hizmet icin musait saatleri getir</summary>
    [HttpGet("{slug}/available-slots")]
    public async Task<ActionResult> GetAvailableSlots(string slug, [FromQuery] int serviceId, [FromQuery] DateTime date)
    {
        var profile = await _db.SlnSalonProfiles
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (profile == null) return NotFound();

        var service = await _db.SlnServices
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.CustomerId == profile.CustomerId && s.IsActive);
        if (service == null) return BadRequest("Hizmet bulunamadi");

        // Calisma saatleri
        var dayKey = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "mon", DayOfWeek.Tuesday => "tue", DayOfWeek.Wednesday => "wed",
            DayOfWeek.Thursday => "thu", DayOfWeek.Friday => "fri", DayOfWeek.Saturday => "sat",
            _ => "sun"
        };

        var openHour = 9; var closeHour = 19;
        if (!string.IsNullOrEmpty(profile.WorkingHoursJson))
        {
            try
            {
                var hours = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(profile.WorkingHoursJson);
                if (hours != null && hours.TryGetValue(dayKey, out var val))
                {
                    var parts = val.Split('-');
                    if (parts.Length == 2)
                    {
                        openHour = int.Parse(parts[0].Split(':')[0]);
                        closeHour = int.Parse(parts[1].Split(':')[0]);
                    }
                }
            }
            catch { }
        }

        // Mevcut randevulari al
        var dayStart = date.Date;
        var dayEnd = date.Date.AddDays(1);
        var existingAppointments = await _db.SlnAppointments
            .Where(a => a.CustomerId == profile.CustomerId && a.StartTime >= dayStart && a.StartTime < dayEnd && a.StatusId != 4)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        // Musait slotlari hesapla (30 dk aralikla)
        var slots = new List<object>();
        var slotDuration = service.DurationMinutes;
        for (var hour = openHour; hour < closeHour; hour++)
        {
            for (var min = 0; min < 60; min += 30)
            {
                var slotStart = date.Date.AddHours(hour).AddMinutes(min);
                var slotEnd = slotStart.AddMinutes(slotDuration);

                if (slotEnd > date.Date.AddHours(closeHour)) break;
                if (slotStart < DateTime.UtcNow) continue;

                var hasConflict = existingAppointments.Any(a => slotStart < a.EndTime && slotEnd > a.StartTime);
                if (!hasConflict)
                {
                    slots.Add(new { startTime = slotStart, endTime = slotEnd });
                }
            }
        }

        return Ok(slots);
    }

    /// <summary>Online randevu olustur (auth gerekmez)</summary>
    [HttpPost("{slug}/book")]
    public async Task<ActionResult> BookAppointment(string slug, [FromBody] SlnOnlineBookingDto dto)
    {
        var profile = await _db.SlnSalonProfiles
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (profile == null) return NotFound();

        var service = await _db.SlnServices
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId && s.CustomerId == profile.CustomerId && s.IsActive);
        if (service == null) return BadRequest("Hizmet bulunamadi");

        // Musteri bul veya olustur
        var client = await _db.SlnClients
            .FirstOrDefaultAsync(c => c.Phone == dto.Phone && c.CustomerId == profile.CustomerId);

        if (client == null)
        {
            client = new SlnClient
            {
                CustomerId = profile.CustomerId,
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email
            };
            _db.SlnClients.Add(client);
            await _db.SaveChangesAsync();
        }

        // Randevu olustur (StatusId=1: Planlanmis, onay bekliyor)
        var appointment = new SlnAppointment
        {
            CustomerId = profile.CustomerId,
            SlnClientId = client.Id,
            PersonnelId = dto.PersonnelId ?? 0,
            ServiceId = dto.ServiceId,
            StartTime = dto.StartTime,
            EndTime = dto.StartTime.AddMinutes(service.DurationMinutes),
            StatusId = 1,
            Notes = dto.Notes
        };

        // PersonnelId 0 ise ilk musait personeli ata
        if (appointment.PersonnelId == 0)
        {
            var firstPersonnel = await _db.Set<CustomerPersonnel>()
                .FirstOrDefaultAsync(p => p.CustomerId == profile.CustomerId && p.IsActive);
            appointment.PersonnelId = firstPersonnel?.Id ?? 0;
        }

        _db.SlnAppointments.Add(appointment);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, appointmentId = appointment.Id, message = "Randevunuz alindi. Salon tarafindan onaylanacaktir." });
    }
}
