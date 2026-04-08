using System.Security.Claims;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Controllers;

/// <summary>
/// Platform son kullanici API'si.
/// Salon uyelik, randevu, profil, sadakat islemleri.
/// </summary>
[ApiController]
[Route("api/platform")]
[Authorize(Roles = "PlatformUser")]
public class PlatformController : ControllerBase
{
    private readonly AppDbContext _db;

    public PlatformController(AppDbContext db) => _db = db;

    // ═══ SALON ÜYELİK ═══

    /// <summary>Üye olduğum salonlar</summary>
    [HttpGet("salons")]
    public async Task<ActionResult<List<PlatformSalonDto>>> GetMySalons()
    {
        var userId = GetPlatformUserId();
        var salons = await _db.PlatformUserSalons
            .Where(s => s.PlatformUserId == userId && s.IsActive)
            .Include(s => s.Customer)
            .Select(s => new PlatformSalonDto
            {
                Id = s.Id,
                CustomerId = s.CustomerId,
                SalonName = s.Customer.Name,
                LogoUrl = _db.SlnSalonProfiles.Where(p => p.CustomerId == s.CustomerId).Select(p => p.LogoUrl).FirstOrDefault(),
                City = _db.SlnSalonProfiles.Where(p => p.CustomerId == s.CustomerId).Select(p => p.City).FirstOrDefault(),
                District = _db.SlnSalonProfiles.Where(p => p.CustomerId == s.CustomerId).Select(p => p.District).FirstOrDefault(),
                IsFavorite = s.IsFavorite,
                JoinedAt = s.JoinedAt
            })
            .ToListAsync();

        return Ok(salons);
    }

    /// <summary>Salona üye ol</summary>
    [HttpPost("salons/join")]
    public async Task<ActionResult> JoinSalon([FromBody] PlatformJoinSalonDto dto)
    {
        var userId = GetPlatformUserId();

        // Zaten üye mi?
        var exists = await _db.PlatformUserSalons
            .AnyAsync(s => s.PlatformUserId == userId && s.CustomerId == dto.CustomerId);
        if (exists)
            return BadRequest(new { message = "Bu salona zaten üyesiniz." });

        // Salon var mı?
        var salon = await _db.Customers.FindAsync(dto.CustomerId);
        if (salon == null || !salon.IsActive)
            return BadRequest(new { message = "Salon bulunamadı." });

        // Platform user bilgilerini al
        var platformUser = await _db.PlatformUsers.FindAsync(userId);
        if (platformUser == null) return Unauthorized();

        // SlnClient oluştur (salonun kendi müşteri kartı)
        var slnClient = new SlnClient
        {
            CustomerId = dto.CustomerId,
            FullName = platformUser.FullName,
            Phone = platformUser.Phone,
            Email = platformUser.Email
        };
        _db.SlnClients.Add(slnClient);
        await _db.SaveChangesAsync();

        // Bağlantı oluştur
        var link = new PlatformUserSalon
        {
            PlatformUserId = userId,
            CustomerId = dto.CustomerId,
            SlnClientId = slnClient.Id,
            IsActive = true
        };
        _db.PlatformUserSalons.Add(link);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Salona üye oldunuz." });
    }

    /// <summary>Salon üyeliğinden ayrıl</summary>
    [HttpDelete("salons/{customerId}")]
    public async Task<ActionResult> LeaveSalon(int customerId)
    {
        var userId = GetPlatformUserId();
        var link = await _db.PlatformUserSalons
            .FirstOrDefaultAsync(s => s.PlatformUserId == userId && s.CustomerId == customerId);

        if (link == null) return NotFound();
        link.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Salon üyeliğiniz sonlandırıldı." });
    }

    /// <summary>Favori toggle</summary>
    [HttpPost("salons/{customerId}/favorite")]
    public async Task<ActionResult> ToggleFavorite(int customerId)
    {
        var userId = GetPlatformUserId();
        var link = await _db.PlatformUserSalons
            .FirstOrDefaultAsync(s => s.PlatformUserId == userId && s.CustomerId == customerId);

        if (link == null) return NotFound();
        link.IsFavorite = !link.IsFavorite;
        await _db.SaveChangesAsync();

        return Ok(new { isFavorite = link.IsFavorite });
    }

    // ═══ RANDEVU ═══

    /// <summary>Randevularım (gelecek + geçmiş)</summary>
    [HttpGet("appointments")]
    public async Task<ActionResult<List<PlatformAppointmentDto>>> GetMyAppointments([FromQuery] bool past = false)
    {
        var userId = GetPlatformUserId();
        var clientIds = await GetMyClientIds(userId);

        var now = DateTime.UtcNow;
        var baseQuery = _db.SlnAppointments
            .Include(a => a.Services).ThenInclude(s => s.SlnService)
            .Include(a => a.Personnel)
            .Where(a => clientIds.Contains(a.SlnClientId));

        var query = past
            ? baseQuery.Where(a => a.StartTime < now).OrderByDescending(a => a.StartTime)
            : baseQuery.Where(a => a.StartTime >= now).OrderBy(a => a.StartTime);

        var appointments = await query.Take(50).ToListAsync();

        var customerIds = appointments.Select(a => a.CustomerId).Distinct().ToList();
        var salonNames = await _db.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);
        var salonLogos = await _db.SlnSalonProfiles
            .Where(p => customerIds.Contains(p.CustomerId))
            .ToDictionaryAsync(p => p.CustomerId, p => p.LogoUrl);

        return Ok(appointments.Select(a => new PlatformAppointmentDto
        {
            Id = a.Id,
            SalonName = salonNames.GetValueOrDefault(a.CustomerId, "-"),
            SalonLogoUrl = salonLogos.GetValueOrDefault(a.CustomerId),
            AppointmentDate = a.StartTime.Date,
            StartTime = a.StartTime.TimeOfDay,
            EndTime = a.EndTime.TimeOfDay,
            PersonnelName = a.Personnel?.Title,
            ServiceNames = a.Services?.Select(s => s.SlnService?.Name ?? "-").ToList() ?? new(),
            TotalPrice = a.Services?.Sum(s => s.SlnService?.Price ?? 0) ?? 0,
            StatusId = a.StatusId
        }).ToList());
    }

    /// <summary>Randevu oluştur</summary>
    [HttpPost("appointments")]
    public async Task<ActionResult> CreateAppointment([FromBody] PlatformCreateAppointmentDto dto)
    {
        var userId = GetPlatformUserId();

        // Bu salona üye mi?
        var link = await _db.PlatformUserSalons
            .FirstOrDefaultAsync(s => s.PlatformUserId == userId && s.CustomerId == dto.CustomerId && s.IsActive);
        if (link == null || link.SlnClientId == null)
            return BadRequest(new { message = "Önce bu salona üye olmanız gerekiyor." });

        // Hizmetleri al, toplam süre ve fiyat hesapla
        var services = await _db.SlnServices
            .Where(s => dto.ServiceIds.Contains(s.Id) && s.CustomerId == dto.CustomerId)
            .ToListAsync();

        if (services.Count == 0)
            return BadRequest(new { message = "En az bir hizmet seçmelisiniz." });

        var totalDuration = services.Sum(s => s.DurationMinutes);
        var startDateTime = dto.Date.Date.Add(dto.StartTime);
        var endDateTime = startDateTime.AddMinutes(totalDuration);

        var appointment = new SlnAppointment
        {
            CustomerId = dto.CustomerId,
            SlnClientId = link.SlnClientId.Value,
            PersonnelId = dto.PersonnelId ?? 0,
            StartTime = startDateTime,
            EndTime = endDateTime,
            StatusId = 1, // Pending
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? "Platform" : $"{dto.Notes} [Platform]"
        };

        _db.SlnAppointments.Add(appointment);
        await _db.SaveChangesAsync();

        // Hizmetleri ekle
        var sortOrder = 0;
        foreach (var svc in services)
        {
            _db.SlnAppointmentServices.Add(new SlnAppointmentService
            {
                SlnAppointmentId = appointment.Id,
                SlnServiceId = svc.Id,
                SortOrder = sortOrder++
            });
        }
        await _db.SaveChangesAsync();

        return Ok(new { appointmentId = appointment.Id, message = "Randevu oluşturuldu." });
    }

    /// <summary>Randevu iptal</summary>
    [HttpDelete("appointments/{id}")]
    public async Task<ActionResult> CancelAppointment(int id)
    {
        var userId = GetPlatformUserId();
        var clientIds = await GetMyClientIds(userId);

        var appointment = await _db.SlnAppointments.FirstOrDefaultAsync(a => a.Id == id && clientIds.Contains(a.SlnClientId));
        if (appointment == null) return NotFound();

        if (appointment.StatusId >= 3) // Completed veya Cancelled
            return BadRequest(new { message = "Bu randevu iptal edilemez." });

        appointment.StatusId = 5; // Cancelled
        await _db.SaveChangesAsync();

        return Ok(new { message = "Randevu iptal edildi." });
    }

    // ═══ SADAKAt ═══

    /// <summary>Sadakat bilgilerim (salon bazlı)</summary>
    [HttpGet("loyalty")]
    public async Task<ActionResult<List<PlatformLoyaltyDto>>> GetMyLoyalty()
    {
        var userId = GetPlatformUserId();
        var links = await _db.PlatformUserSalons
            .Where(s => s.PlatformUserId == userId && s.IsActive && s.SlnClientId != null)
            .Include(s => s.Customer)
            .ToListAsync();

        var result = new List<PlatformLoyaltyDto>();

        foreach (var link in links)
        {
            var loyalty = await _db.SlnClientLoyalties
                .FirstOrDefaultAsync(l => l.SlnClientId == link.SlnClientId && l.CustomerId == link.CustomerId);

            var membership = await _db.SlnClientMemberships
                .Include(m => m.Plan)
                .FirstOrDefaultAsync(m => m.SlnClientId == link.SlnClientId && m.CustomerId == link.CustomerId && m.StatusId == 1);

            var giftCards = await _db.SlnGiftCards
                .Where(g => g.CustomerId == link.CustomerId && g.IsActive && g.RemainingBalance > 0
                    && g.RecipientPhone == _db.PlatformUsers.Where(u => u.Id == userId).Select(u => u.Phone).FirstOrDefault())
                .Select(g => new PlatformGiftCardDto
                {
                    Code = g.Code,
                    RemainingBalance = g.RemainingBalance,
                    OriginalAmount = g.OriginalAmount,
                    IsActive = g.IsActive
                })
                .ToListAsync();

            result.Add(new PlatformLoyaltyDto
            {
                SalonName = link.Customer.Name,
                CurrentPoints = loyalty?.CurrentBalance ?? 0,
                TotalEarned = loyalty?.TotalEarned ?? 0,
                MembershipPlanName = membership?.Plan?.Name,
                MembershipDiscount = membership?.Plan?.DiscountPercent,
                GiftCards = giftCards
            });
        }

        return Ok(result);
    }

    // ═══ SALON KEŞFET ═══

    /// <summary>Yayında olan salonları listele</summary>
    [HttpGet("discover")]
    [AllowAnonymous]
    public async Task<ActionResult> DiscoverSalons([FromQuery] string? city, [FromQuery] string? search, [FromQuery] int page = 1)
    {
        var query = _db.SlnSalonProfiles
            .Where(p => p.IsPublished)
            .Include(p => p.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.City != null && p.City.Contains(city));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Customer!.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));

        var total = await query.CountAsync();
        var salons = await query
            .OrderBy(p => p.Customer!.Name)
            .Skip((page - 1) * 20)
            .Take(20)
            .Select(p => new
            {
                customerId = p.CustomerId,
                name = p.Customer!.Name,
                slug = p.Slug,
                logoUrl = p.LogoUrl,
                coverImageUrl = p.CoverImageUrl,
                city = p.City,
                district = p.District,
                description = p.Description
            })
            .ToListAsync();

        return Ok(new { total, page, salons });
    }

    // ═══ HELPERS ═══

    private int GetPlatformUserId()
        => int.Parse(User.FindFirstValue("PlatformUserId") ?? "0");

    private async Task<List<int>> GetMyClientIds(int platformUserId)
        => await _db.PlatformUserSalons
            .Where(s => s.PlatformUserId == platformUserId && s.IsActive && s.SlnClientId != null)
            .Select(s => s.SlnClientId!.Value)
            .ToListAsync();
}
