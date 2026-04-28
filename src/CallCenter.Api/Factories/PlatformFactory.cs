using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class PlatformFactory : IPlatformFactory
{
    private readonly IPlatformUserSalonEntityService _userSalonEs;
    private readonly IPlatformUserEntityService _platformUserEs;
    private readonly ICustomerEntityService _customerEs;
    private readonly ISlnClientEntityService _clientEs;
    private readonly ISlnAppointmentEntityService _appointmentEs;
    private readonly ISlnAppointmentServiceEntityService _appointmentServiceEs;
    private readonly ISlnServiceEntityService _serviceEs;
    private readonly ISlnSalonProfileEntityService _profileEs;
    private readonly ISlnClientMembershipEntityService _membershipEs;
    private readonly ISlnClientLoyaltyEntityService _loyaltyEs;
    private readonly ISlnGiftCardEntityService _giftCardEs;
    private readonly IUnitOfWork _uow;

    public PlatformFactory(
        IPlatformUserSalonEntityService userSalonEs,
        IPlatformUserEntityService platformUserEs,
        ICustomerEntityService customerEs,
        ISlnClientEntityService clientEs,
        ISlnAppointmentEntityService appointmentEs,
        ISlnAppointmentServiceEntityService appointmentServiceEs,
        ISlnServiceEntityService serviceEs,
        ISlnSalonProfileEntityService profileEs,
        ISlnClientMembershipEntityService membershipEs,
        ISlnClientLoyaltyEntityService loyaltyEs,
        ISlnGiftCardEntityService giftCardEs,
        IUnitOfWork uow)
    {
        _userSalonEs = userSalonEs;
        _platformUserEs = platformUserEs;
        _customerEs = customerEs;
        _clientEs = clientEs;
        _appointmentEs = appointmentEs;
        _appointmentServiceEs = appointmentServiceEs;
        _serviceEs = serviceEs;
        _profileEs = profileEs;
        _membershipEs = membershipEs;
        _loyaltyEs = loyaltyEs;
        _giftCardEs = giftCardEs;
        _uow = uow;
    }

    // ═══ SALON ÜYELİK ═══

    public async Task<List<PlatformSalonDto>> GetMySalonsAsync(int platformUserId)
    {
        return await _userSalonEs.GetAllQueryable()
            .Where(s => s.PlatformUserId == platformUserId && s.IsActive)
            .Include(s => s.Customer)
            .Select(s => new PlatformSalonDto
            {
                Id = s.Id,
                CustomerId = s.CustomerId,
                SalonName = s.Customer.Name,
                LogoUrl = _profileEs.GetAllQueryable().Where(p => p.CustomerId == s.CustomerId).Select(p => p.LogoUrl).FirstOrDefault(),
                City = _profileEs.GetAllQueryable().Where(p => p.CustomerId == s.CustomerId).Select(p => p.City).FirstOrDefault(),
                District = _profileEs.GetAllQueryable().Where(p => p.CustomerId == s.CustomerId).Select(p => p.District).FirstOrDefault(),
                IsFavorite = s.IsFavorite,
                JoinedAt = s.JoinedAt
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> JoinSalonAsync(int platformUserId, int customerId)
    {
        // Zaten uye mi?
        var exists = await _userSalonEs.GetAllQueryable()
            .AnyAsync(s => s.PlatformUserId == platformUserId && s.CustomerId == customerId);
        if (exists)
            return (false, "Bu salona zaten üyesiniz.");

        // Salon var mi?
        var salon = await _customerEs.GetByIdAsync(customerId);
        if (salon == null || !salon.IsActive)
            return (false, "Salon bulunamadı.");

        // Platform user bilgilerini al
        var platformUser = await _platformUserEs.GetByIdAsync(platformUserId);
        if (platformUser == null) return (false, null);

        // SlnClient olustur (salonun kendi musteri karti)
        var slnClient = new SlnClient
        {
            CustomerId = customerId,
            FullName = platformUser.FullName,
            Phone = platformUser.Phone,
            Email = platformUser.Email
        };
        _clientEs.Add(slnClient);
        await _uow.SaveChangesAsync();

        // Baglanti olustur
        var link = new PlatformUserSalon
        {
            PlatformUserId = platformUserId,
            CustomerId = customerId,
            SlnClientId = slnClient.Id,
            IsActive = true
        };
        _userSalonEs.Add(link);
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> LeaveSalonAsync(int platformUserId, int customerId)
    {
        var link = await _userSalonEs.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.PlatformUserId == platformUserId && s.CustomerId == customerId);

        if (link == null) return (false, "Üyelik bulunamadı.");
        link.IsActive = false;

        // Salon tarafindaki musteri kaydini da pasif yap — salon personeli artik erisemez
        if (link.SlnClientId.HasValue)
        {
            var client = await _clientEs.GetByIdAsync(link.SlnClientId.Value);
            if (client != null)
                client.IsActive = false;
        }

        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, bool IsFavorite)> ToggleFavoriteAsync(int platformUserId, int customerId)
    {
        var link = await _userSalonEs.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.PlatformUserId == platformUserId && s.CustomerId == customerId);

        if (link == null) return (false, false);
        link.IsFavorite = !link.IsFavorite;
        await _uow.SaveChangesAsync();

        return (true, link.IsFavorite);
    }

    // ═══ RANDEVU ═══

    public async Task<List<PlatformAppointmentDto>> GetMyAppointmentsAsync(int platformUserId, bool past)
    {
        var clientIds = await GetMyClientIds(platformUserId);

        var now = DateTime.UtcNow;
        var baseQuery = _appointmentEs.GetAllQueryable()
            .Include(a => a.Services).ThenInclude(s => s.SlnService)
            .Include(a => a.Personnel)
            .Where(a => clientIds.Contains(a.SlnClientId));

        var query = past
            ? baseQuery.Where(a => a.StartTime < now).OrderByDescending(a => a.StartTime)
            : baseQuery.Where(a => a.StartTime >= now).OrderBy(a => a.StartTime);

        var appointments = await query.Take(50).ToListAsync();

        var customerIds = appointments.Select(a => a.CustomerId).Distinct().ToList();
        var salonNames = await _customerEs.GetAllQueryable()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);
        var salonLogos = await _profileEs.GetAllQueryable()
            .Where(p => customerIds.Contains(p.CustomerId))
            .ToDictionaryAsync(p => p.CustomerId, p => p.LogoUrl);

        return appointments.Select(a => new PlatformAppointmentDto
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
        }).ToList();
    }

    public async Task<(int? AppointmentId, string? Error)> CreateAppointmentAsync(int platformUserId, PlatformCreateAppointmentDto dto)
    {
        // Bu salona uye mi?
        var link = await _userSalonEs.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.PlatformUserId == platformUserId && s.CustomerId == dto.CustomerId && s.IsActive);
        if (link == null || link.SlnClientId == null)
            return (null, "Önce bu salona üye olmanız gerekiyor.");

        // Hizmetleri al, toplam sure ve fiyat hesapla
        var services = await _serviceEs.GetAllQueryable()
            .Where(s => dto.ServiceIds.Contains(s.Id) && s.CustomerId == dto.CustomerId)
            .ToListAsync();

        if (services.Count == 0)
            return (null, "En az bir hizmet seçmelisiniz.");

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

        _appointmentEs.Add(appointment);
        await _uow.SaveChangesAsync();

        // Hizmetleri ekle
        var sortOrder = 0;
        foreach (var svc in services)
        {
            _appointmentServiceEs.Add(new SlnAppointmentService
            {
                SlnAppointmentId = appointment.Id,
                SlnServiceId = svc.Id,
                SortOrder = sortOrder++
            });
        }
        await _uow.SaveChangesAsync();

        return (appointment.Id, null);
    }

    public async Task<(bool Success, string? Error)> CancelAppointmentAsync(int platformUserId, int appointmentId)
    {
        var clientIds = await GetMyClientIds(platformUserId);

        var appointment = await _appointmentEs.GetAllQueryable().FirstOrDefaultAsync(a => a.Id == appointmentId && clientIds.Contains(a.SlnClientId));
        if (appointment == null) return (false, "Randevu bulunamadı.");

        if (appointment.StatusId >= 3) // Completed veya Cancelled
            return (false, "Bu randevu iptal edilemez.");

        appointment.StatusId = 5; // Cancelled
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    // ═══ SADAKAT ═══

    public async Task<List<PlatformLoyaltyDto>> GetMyLoyaltyAsync(int platformUserId)
    {
        var links = await _userSalonEs.GetAllQueryable()
            .Where(s => s.PlatformUserId == platformUserId && s.IsActive && s.SlnClientId != null)
            .Include(s => s.Customer)
            .ToListAsync();

        var result = new List<PlatformLoyaltyDto>();

        foreach (var link in links)
        {
            var loyalty = await _loyaltyEs.GetAllQueryable()
                .FirstOrDefaultAsync(l => l.SlnClientId == link.SlnClientId && l.CustomerId == link.CustomerId);

            var membership = await _membershipEs.GetAllQueryable()
                .Include(m => m.Plan)
                .FirstOrDefaultAsync(m => m.SlnClientId == link.SlnClientId && m.CustomerId == link.CustomerId && m.StatusId == 1);

            var phone = await _platformUserEs.GetAllQueryable()
                .Where(u => u.Id == platformUserId).Select(u => u.Phone).FirstOrDefaultAsync();

            var giftCards = await _giftCardEs.GetAllQueryable()
                .Where(g => g.CustomerId == link.CustomerId && g.IsActive && g.RemainingBalance > 0
                    && g.RecipientPhone == phone)
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

        return result;
    }

    // ═══ SALON KEŞFET ═══

    public async Task<object> DiscoverSalonsAsync(string? city, string? search, int page)
    {
        var query = _profileEs.GetAllQueryable()
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

        return new { total, page, salons };
    }

    // ═══ HELPERS ═══

    private async Task<List<int>> GetMyClientIds(int platformUserId)
    {
        // 1. Salon üyeliği bağlantısı üzerinden (normal yol)
        var linked = await _userSalonEs.GetAllQueryable()
            .Where(s => s.PlatformUserId == platformUserId && s.IsActive && s.SlnClientId != null)
            .Select(s => s.SlnClientId!.Value)
            .ToListAsync();

        // 2. Telefon numarası üzerinden eşleştir (salon üyeliği olmadan Book.cshtml ile alınan randevular)
        var phone = await _platformUserEs.GetAllQueryable()
            .Where(u => u.Id == platformUserId)
            .Select(u => u.Phone)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(phone))
        {
            var byPhone = await _clientEs.GetAllQueryable()
                .Where(c => c.Phone == phone)
                .Select(c => c.Id)
                .ToListAsync();
            linked.AddRange(byPhone);
        }

        return linked.Distinct().ToList();
    }
}
