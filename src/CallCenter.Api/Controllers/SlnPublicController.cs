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

    /// <summary>Slug ile salon profili getir (branch slug veya eski profile slug)</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<SlnSalonProfileDto>> GetBySlug(string slug)
    {
        // Oncelik: branch slug → sonra eski profile slug (geriye uyumluluk)
        var branch = await _db.SlnBranches
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);

        SlnSalonProfile? profile;
        int customerId;

        if (branch != null)
        {
            customerId = branch.CustomerId;
            profile = await _db.SlnSalonProfiles
                .FirstOrDefaultAsync(p => p.CustomerId == customerId && p.IsPublished);
        }
        else
        {
            profile = await _db.SlnSalonProfiles
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

            if (profile == null) return NotFound();
            customerId = profile.CustomerId;

            // Eski slug ile geldiyse merkez subeyi bul
            branch = await _db.SlnBranches
                .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);
        }

        if (profile == null) return NotFound();

        var categories = await _db.SlnServiceCategories
            .Where(c => c.CustomerId == customerId && c.IsActive)
            .Include(c => c.Services.Where(s => s.IsActive))
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return Ok(new SlnSalonProfileDto
        {
            Id = profile.Id,
            CustomerId = customerId,
            Slug = branch?.Slug ?? profile.Slug ?? "",
            SalonName = branch?.Customer?.Name ?? profile.Customer?.Name ?? "",
            Description = profile.Description,
            Address = branch?.Address ?? profile.Address,
            City = branch?.City ?? profile.City,
            District = branch?.District ?? profile.District,
            Phone = branch?.Phone ?? profile.Phone,
            Email = branch?.Email ?? profile.Email,
            Website = profile.Website,
            GoogleMapsUrl = branch?.GoogleMapsUrl ?? profile.GoogleMapsUrl,
            LogoUrl = profile.LogoUrl,
            CoverImageUrl = profile.CoverImageUrl,
            WorkingHoursJson = branch?.WorkingHoursJson ?? profile.WorkingHoursJson,
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
            Latitude = branch?.Latitude ?? profile.Latitude,
            Longitude = branch?.Longitude ?? profile.Longitude,
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

    /// <summary>Tum yayinlanmis salonlari listele (merkez sube bilgileriyle)</summary>
    [HttpGet]
    public async Task<ActionResult> GetAllPublished()
    {
        var profiles = await _db.SlnSalonProfiles
            .Where(p => p.IsPublished)
            .Include(p => p.Customer)
            .ToListAsync();

        var customerIds = profiles.Select(p => p.CustomerId).ToList();
        var hqBranches = await _db.SlnBranches
            .Where(b => customerIds.Contains(b.CustomerId) && b.IsHeadquarter && b.IsActive)
            .ToDictionaryAsync(b => b.CustomerId);

        var result = profiles.OrderBy(p => p.Customer?.Name).Select(p =>
        {
            hqBranches.TryGetValue(p.CustomerId, out var hq);
            return new
            {
                Slug = hq?.Slug ?? p.Slug ?? "",
                SalonName = p.Customer?.Name ?? "",
                City = hq?.City ?? p.City,
                District = hq?.District ?? p.District,
                p.LogoUrl,
                p.Description,
                Latitude = hq?.Latitude ?? p.Latitude,
                Longitude = hq?.Longitude ?? p.Longitude
            };
        }).ToList();

        return Ok(result);
    }

    /// <summary>Branch slug veya eski profile slug'dan customerId bul</summary>
    private async Task<int?> ResolveCustomerIdAsync(string slug)
    {
        var branch = await _db.SlnBranches.FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);
        if (branch != null) return branch.CustomerId;

        var profile = await _db.SlnSalonProfiles.FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        return profile?.CustomerId;
    }

    /// <summary>Salonun onaylanmis yorumlarini getir</summary>
    [HttpGet("{slug}/reviews")]
    public async Task<ActionResult> GetReviews(string slug)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return NotFound();

        var reviews = await _db.SlnReviews
            .Where(r => r.CustomerId == customerId.Value && r.StatusId == 2) // 2=Approved
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
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return NotFound();

        var team = await _db.Set<CustomerPersonnel>()
            .Where(p => p.CustomerId == customerId.Value && p.IsActive)
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

    /// <summary>Salonun subelerini getir (online randevu icin sube secimi)</summary>
    [HttpGet("{slug}/branches")]
    public async Task<ActionResult> GetBranches(string slug)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return NotFound();

        var branches = await _db.SlnBranches
            .Where(b => b.CustomerId == customerId.Value && b.IsActive)
            .OrderByDescending(b => b.IsHeadquarter)
            .ThenBy(b => b.Name)
            .Select(b => new
            {
                b.Id, b.Name, b.Address, b.City, b.District,
                b.Phone, b.IsHeadquarter, b.Latitude, b.Longitude,
                b.PhotoUrl, b.WorkingHoursJson
            })
            .ToListAsync();

        return Ok(branches);
    }

    /// <summary>Salonun aktif uyelik planlarini getir</summary>
    [HttpGet("{slug}/memberships")]
    public async Task<ActionResult> GetMembershipPlans(string slug)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return NotFound();

        var plans = await _db.SlnMembershipPlans
            .Where(p => p.CustomerId == customerId.Value && p.IsActive)
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
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return NotFound();
        var cid = customerId.Value;

        var plan = await _db.SlnMembershipPlans
            .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.CustomerId == cid && p.IsActive);
        if (plan == null) return BadRequest("Uyelik plani bulunamadi.");

        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
            return BadRequest("Ad ve telefon zorunludur.");

        // Musteri bul veya olustur
        var client = await _db.SlnClients
            .FirstOrDefaultAsync(c => c.Phone == dto.Phone && c.CustomerId == cid);

        if (client == null)
        {
            client = new SlnClient
            {
                CustomerId = cid,
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
                .AnyAsync(m => m.SlnClientId == client.Id && m.CustomerId == cid && m.StatusId == 1);
            if (existing)
                return BadRequest("Bu telefon numarasina ait zaten aktif bir uyelik bulunmaktadir.");
        }

        var now = DateTime.UtcNow;
        var membership = new SlnClientMembership
        {
            CustomerId = cid,
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
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return NotFound();
        var cid = customerId.Value;

        var service = await _db.SlnServices
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.CustomerId == cid && s.IsActive);
        if (service == null) return BadRequest("Hizmet bulunamadi");

        // Calisma saatleri (branch'ten al)
        var branch = await _db.SlnBranches.FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive)
            ?? await _db.SlnBranches.FirstOrDefaultAsync(b => b.CustomerId == cid && b.IsHeadquarter);

        var dayKey = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "mon", DayOfWeek.Tuesday => "tue", DayOfWeek.Wednesday => "wed",
            DayOfWeek.Thursday => "thu", DayOfWeek.Friday => "fri", DayOfWeek.Saturday => "sat",
            _ => "sun"
        };

        var openHour = 9; var closeHour = 19;
        var workingHoursJson = branch?.WorkingHoursJson;
        if (!string.IsNullOrEmpty(workingHoursJson))
        {
            try
            {
                var hours = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(workingHoursJson);
                if (hours != null && hours.TryGetValue(dayKey, out var val))
                {
                    if (val == "closed") return Ok(new List<object>());
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
            .Where(a => a.CustomerId == cid && a.StartTime >= dayStart && a.StartTime < dayEnd && a.StatusId != 4)
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
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return NotFound();
        var cid = customerId.Value;

        var service = await _db.SlnServices
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId && s.CustomerId == cid && s.IsActive);
        if (service == null) return BadRequest("Hizmet bulunamadi");

        // Musteri bul veya olustur
        var client = await _db.SlnClients
            .FirstOrDefaultAsync(c => c.Phone == dto.Phone && c.CustomerId == cid);

        if (client == null)
        {
            client = new SlnClient
            {
                CustomerId = cid,
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
            CustomerId = cid,
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
                .FirstOrDefaultAsync(p => p.CustomerId == cid && p.IsActive);
            appointment.PersonnelId = firstPersonnel?.Id ?? 0;
        }

        _db.SlnAppointments.Add(appointment);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, appointmentId = appointment.Id, message = "Randevunuz alindi. Salon tarafindan onaylanacaktir." });
    }
}
