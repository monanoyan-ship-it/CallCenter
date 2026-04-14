using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnPublicFactory : ISlnPublicFactory
{
    private readonly ISlnSalonProfileEntityService _profiles;
    private readonly ISlnBranchEntityService _branches;
    private readonly ISlnServiceCategoryEntityService _serviceCategories;
    private readonly ISlnServiceEntityService _services;
    private readonly ISlnReviewEntityService _reviews;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly ISlnMembershipPlanEntityService _membershipPlans;
    private readonly ISlnClientMembershipEntityService _clientMemberships;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnPersonnelSkillEntityService _skills;
    private readonly ISlnNoShowPolicyEntityService _noShowPolicies;
    private readonly PaymentService _paymentService;
    private readonly IUnitOfWork _uow;

    public SlnPublicFactory(
        ISlnSalonProfileEntityService profiles,
        ISlnBranchEntityService branches,
        ISlnServiceCategoryEntityService serviceCategories,
        ISlnServiceEntityService services,
        ISlnReviewEntityService reviews,
        ICustomerPersonnelEntityService personnel,
        ISlnMembershipPlanEntityService membershipPlans,
        ISlnClientMembershipEntityService clientMemberships,
        ISlnClientEntityService clients,
        ISlnAppointmentEntityService appointments,
        ISlnPersonnelSkillEntityService skills,
        ISlnNoShowPolicyEntityService noShowPolicies,
        PaymentService paymentService,
        IUnitOfWork uow)
    {
        _profiles = profiles;
        _branches = branches;
        _serviceCategories = serviceCategories;
        _services = services;
        _reviews = reviews;
        _personnel = personnel;
        _membershipPlans = membershipPlans;
        _clientMemberships = clientMemberships;
        _clients = clients;
        _appointments = appointments;
        _skills = skills;
        _noShowPolicies = noShowPolicies;
        _paymentService = paymentService;
        _uow = uow;
    }

    /// <summary>Slug ile salon profili getir (branch slug veya eski profile slug)</summary>
    public async Task<SlnSalonProfileDto?> GetBySlugAsync(string slug)
    {
        // Oncelik: branch slug -> sonra eski profile slug (geriye uyumluluk)
        var branch = await _branches.GetAllQueryable()
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);

        SlnSalonProfile? profile;
        int customerId;

        if (branch != null)
        {
            customerId = branch.CustomerId;
            profile = await _profiles.GetAllQueryable()
                .FirstOrDefaultAsync(p => p.CustomerId == customerId && p.IsPublished);
        }
        else
        {
            profile = await _profiles.GetAllQueryable()
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

            if (profile == null) return null;
            customerId = profile.CustomerId;

            // Eski slug ile geldiyse merkez subeyi bul
            branch = await _branches.GetAllQueryable()
                .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);
        }

        if (profile == null) return null;

        var categories = await _serviceCategories.GetAllQueryable()
            .Where(c => c.CustomerId == customerId && c.IsActive)
            .Include(c => c.Services.Where(s => s.IsActive))
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return new SlnSalonProfileDto
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
        };
    }

    /// <summary>Tum yayinlanmis salonlari listele (merkez sube bilgileriyle)</summary>
    public async Task<object> GetAllPublishedAsync()
    {
        var profiles = await _profiles.GetAllQueryable()
            .Where(p => p.IsPublished)
            .Include(p => p.Customer)
            .ToListAsync();

        var customerIds = profiles.Select(p => p.CustomerId).ToList();
        var hqBranches = await _branches.GetAllQueryable()
            .Where(b => customerIds.Contains(b.CustomerId) && b.IsHeadquarter && b.IsActive)
            .ToDictionaryAsync(b => b.CustomerId);

        // Yorumlar — onaylı (StatusId=2) ortalama + sayı
        var reviewStats = await _reviews.GetAllQueryable()
            .Where(r => customerIds.Contains(r.CustomerId) && r.StatusId == 2)
            .GroupBy(r => r.CustomerId)
            .Select(g => new { CustomerId = g.Key, Avg = g.Average(x => x.Rating), Count = g.Count() })
            .ToDictionaryAsync(x => x.CustomerId);

        // Fiyat araligi — aktif hizmet min/max (firma bazli)
        var priceStats = await _services.GetAllQueryable()
            .Where(s => customerIds.Contains(s.CustomerId) && s.IsActive && s.Price > 0)
            .GroupBy(s => s.CustomerId)
            .Select(g => new { CustomerId = g.Key, Min = g.Min(x => x.Price), Max = g.Max(x => x.Price) })
            .ToDictionaryAsync(x => x.CustomerId);

        return profiles.OrderBy(p => p.Customer?.Name).Select(p =>
        {
            hqBranches.TryGetValue(p.CustomerId, out var hq);
            reviewStats.TryGetValue(p.CustomerId, out var rs);
            priceStats.TryGetValue(p.CustomerId, out var ps);
            string? priceRange = null;
            if (ps != null)
                priceRange = ps.Min == ps.Max
                    ? $"{ps.Min:N0} ₺"
                    : $"{ps.Min:N0} - {ps.Max:N0} ₺";

            return new
            {
                Slug = hq?.Slug ?? p.Slug ?? "",
                SalonName = p.Customer?.Name ?? "",
                City = hq?.City ?? p.City,
                District = hq?.District ?? p.District,
                p.LogoUrl,
                p.Description,
                Latitude = hq?.Latitude ?? p.Latitude,
                Longitude = hq?.Longitude ?? p.Longitude,
                AverageRating = rs?.Avg ?? 0,
                ReviewCount = rs?.Count ?? 0,
                PriceRange = priceRange
            };
        }).ToList();
    }

    /// <summary>Discover harita için tüm yayınlanan salonların aktif şubelerini koordinatlarıyla döner</summary>
    public async Task<object> GetAllBranchesForMapAsync()
    {
        var publishedCustomerIds = await _profiles.GetAllQueryable()
            .Where(p => p.IsPublished)
            .Select(p => p.CustomerId)
            .ToListAsync();

        if (publishedCustomerIds.Count == 0)
            return new List<object>();

        var branches = await _branches.GetAllQueryable()
            .Where(b => publishedCustomerIds.Contains(b.CustomerId)
                     && b.IsActive
                     && b.Latitude != null && b.Longitude != null)
            .Include(b => b.Customer)
            .ToListAsync();

        return branches.Select(b => new
        {
            Slug = b.Slug ?? "",
            SalonName = b.Customer?.Name ?? "",
            BranchName = b.Name,
            b.IsHeadquarter,
            b.City,
            b.District,
            b.Latitude,
            b.Longitude
        }).ToList();
    }

    /// <summary>Branch slug veya eski profile slug'dan customerId bul</summary>
    private async Task<int?> ResolveCustomerIdAsync(string slug)
    {
        var branch = await _branches.GetAllQueryable().FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);
        if (branch != null) return branch.CustomerId;

        var profile = await _profiles.GetAllQueryable().FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        return profile?.CustomerId;
    }

    /// <summary>Salonun onaylanmis yorumlarini getir</summary>
    public async Task<object?> GetReviewsAsync(string slug)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return null;

        var reviews = await _reviews.GetAllQueryable()
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

        return new { reviews, stats };
    }

    /// <summary>Salonun ekibini getir (aktif personel)</summary>
    public async Task<object?> GetTeamAsync(string slug)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return null;

        var team = await _personnel.GetAllQueryable()
            .Where(p => p.CustomerId == customerId.Value && p.IsActive && p.PublicVisible
                     && p.CustomerRoleId != Shared.Enums.SalonRoles.Ids.SalonOwner)
            .Include(p => p.User)
            .OrderBy(p => p.CustomerRoleId)
            .Select(p => new
            {
                name = p.PublicShowFullName ? p.User.FullName : p.User.FullName.Split(' ')[0],
                title = p.PublicShowTitle ? p.Title : (string?)null,
                specialty = p.PublicShowSpecialty ? p.Specialty : (string?)null,
                photoUrl = p.PublicShowPhoto ? p.PhotoUrl : (string?)null,
                roleId = p.CustomerRoleId
            })
            .ToListAsync();

        return team;
    }

    /// <summary>Salonun subelerini getir (online randevu icin sube secimi)</summary>
    public async Task<object?> GetBranchesAsync(string slug)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return null;

        var branches = await _branches.GetAllQueryable()
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

        return branches;
    }

    /// <summary>Salonun aktif uyelik planlarini getir</summary>
    public async Task<object?> GetMembershipPlansAsync(string slug)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return null;

        var plans = await _membershipPlans.GetAllQueryable()
            .Where(p => p.CustomerId == customerId.Value && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new
            {
                p.Id, p.Name, p.Description, p.IconClass, p.Color,
                p.DurationType, p.DurationDays, p.Price, p.DiscountPercent, p.PriorityBooking
            })
            .ToListAsync();

        return plans;
    }

    /// <summary>Online uyelik basvurusu (auth gerekmez)</summary>
    public async Task<(bool Success, string? Error, object? Result)> MembershipSignupAsync(string slug, SlnMembershipSignupDto dto)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return (false, "Salon bulunamadi", null);
        var cid = customerId.Value;

        var plan = await _membershipPlans.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.CustomerId == cid && p.IsActive);
        if (plan == null) return (false, "Uyelik plani bulunamadi.", null);

        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
            return (false, "Ad ve telefon zorunludur.", null);

        // Musteri bul veya olustur
        var client = await _clients.GetAllQueryable()
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
            _clients.Add(client);
            await _uow.SaveChangesAsync();
        }
        else
        {
            // Mevcut musterinin zaten aktif uyeligi var mi?
            var existing = await _clientMemberships.GetAllQueryable()
                .AnyAsync(m => m.SlnClientId == client.Id && m.CustomerId == cid && m.StatusId == 1);
            if (existing)
                return (false, "Bu telefon numarasina ait zaten aktif bir uyelik bulunmaktadir.", null);
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

        _clientMemberships.Add(membership);
        await _uow.SaveChangesAsync();

        return (true, null, new { success = true, message = "Uyelik basvurunuz alinmistir. Salonumuz sizinle iletisime gececektir." });
    }

    /// <summary>Belirli salon + tarih + hizmet icin musait saatleri getir</summary>
    public async Task<object?> GetAvailableSlotsAsync(string slug, int serviceId, DateTime date)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return null;
        var cid = customerId.Value;

        var service = await _services.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.CustomerId == cid && s.IsActive);
        if (service == null) return null;

        // Calisma saatleri (branch'ten al)
        var branch = await _branches.GetAllQueryable().FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive)
            ?? await _branches.GetAllQueryable().FirstOrDefaultAsync(b => b.CustomerId == cid && b.IsHeadquarter);

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
                    if (val == "closed") return new List<object>();
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

        // Mevcut randevulari al (UTC kind — Npgsql timestamptz icin)
        var dayStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);
        var existingAppointments = await _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == cid && a.StartTime >= dayStart && a.StartTime < dayEnd && a.StatusId != 4)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        // Musait slotlari hesapla (30 dk aralikla)
        // ISO string olarak Z suffix'siz dondurulur ki JS local time olarak yorumlasin.
        var slots = new List<object>();
        var slotDuration = service.DurationMinutes;
        var nowUtc = DateTime.UtcNow;
        for (var hour = openHour; hour < closeHour; hour++)
        {
            for (var min = 0; min < 60; min += 30)
            {
                var slotStart = dayStart.AddHours(hour).AddMinutes(min);
                var slotEnd = slotStart.AddMinutes(slotDuration);

                if (slotEnd > dayStart.AddHours(closeHour)) break;
                if (slotStart < nowUtc) continue;

                var hasConflict = existingAppointments.Any(a => slotStart < a.EndTime && slotEnd > a.StartTime);
                if (!hasConflict)
                {
                    slots.Add(new
                    {
                        startTime = slotStart.ToString("yyyy-MM-ddTHH:mm:ss"),
                        endTime = slotEnd.ToString("yyyy-MM-ddTHH:mm:ss"),
                        timeText = slotStart.ToString("HH:mm")
                    });
                }
            }
        }

        return slots;
    }

    /// <summary>Hizmet icin musait personelleri getir (skill eslemesi)</summary>
    public async Task<object?> GetAvailableStaffForServiceAsync(string slug, int serviceId)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return null;
        var cid = customerId.Value;

        // Skill eslesmesi olan personelleri bul
        var skilledPersonnelIds = await _skills.GetAllQueryable()
            .Where(s => s.ServiceId == serviceId)
            .Select(s => s.PersonnelId)
            .Distinct()
            .ToListAsync();

        var query = _personnel.GetAllQueryable()
            .Where(p => p.CustomerId == cid && p.IsActive && p.PublicVisible
                     && p.CustomerRoleId != Shared.Enums.SalonRoles.Ids.SalonOwner);

        // Skill tanimlanmissa filtrele, yoksa tum aktif personelleri don
        if (skilledPersonnelIds.Count > 0)
            query = query.Where(p => skilledPersonnelIds.Contains(p.Id));

        return await query
            .Include(p => p.User)
            .OrderBy(p => p.User.FullName)
            .Select(p => new
            {
                id = p.Id,
                name = p.PublicShowFullName ? p.User.FullName : p.User.FullName.Split(' ')[0],
                title = p.PublicShowTitle ? p.Title : (string?)null,
                photoUrl = p.PublicShowPhoto ? p.PhotoUrl : (string?)null,
                specialty = p.PublicShowSpecialty ? p.Specialty : (string?)null
            })
            .ToListAsync();
    }

    /// <summary>Salonun randevu politikasini getir (depozito, iptal kurallari)</summary>
    public async Task<object?> GetBookingPolicyAsync(string slug)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return null;
        var cid = customerId.Value;

        var policy = await _noShowPolicies.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.CustomerId == cid);

        if (policy == null)
            return new { hasPolicy = false };

        return new
        {
            hasPolicy = true,
            requireDeposit = policy.RequireDeposit,
            depositAmount = policy.DepositAmount,
            freeCancellationHours = policy.FreeCancellationHours,
            lateCancellationFee = policy.LateCancellationFee,
            noShowFee = policy.NoShowFee
        };
    }

    /// <summary>Online randevu olustur (auth gerekmez). Salon politikasi depozito gerektiriyorsa karttan tahsil eder.</summary>
    public async Task<(bool Success, string? Error, object? Result)> BookAppointmentAsync(string slug, SlnOnlineBookingDto dto, string? buyerIp = null)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return (false, "Salon bulunamadi", null);
        var cid = customerId.Value;

        var service = await _services.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId && s.CustomerId == cid && s.IsActive);
        if (service == null) return (false, "Hizmet bulunamadi", null);

        // Politika kontrolu — depozito zorunlu mu?
        var policy = await _noShowPolicies.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.CustomerId == cid);
        var requireDeposit = policy?.IsActive == true && policy.RequireDeposit && policy.DepositAmount > 0;
        var depositAmount = requireDeposit ? policy!.DepositAmount : 0m;

        if (requireDeposit)
        {
            if (dto.Card == null
                || string.IsNullOrWhiteSpace(dto.Card.CardNumber)
                || string.IsNullOrWhiteSpace(dto.Card.ExpireMonth)
                || string.IsNullOrWhiteSpace(dto.Card.ExpireYear)
                || string.IsNullOrWhiteSpace(dto.Card.Cvc))
            {
                return (false, $"Bu salonda online randevu icin {depositAmount:N2} TL depozito gereklidir. Kart bilgileri eksik.", null);
            }
        }

        // Musteri bul veya olustur
        var client = await _clients.GetAllQueryable()
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
            _clients.Add(client);
            await _uow.SaveChangesAsync();
        }
        else if (client.IsBlacklisted)
        {
            return (false, "Gecmis randevu ihlalleri nedeniyle online randevu olusturulamiyor. Lutfen salonu arayiniz.", null);
        }

        // Depozito karttan cek (basarisizsa randevu olusturulmaz)
        Guid? paymentTxUid = null;
        if (requireDeposit)
        {
            var card = new PaymentCardInfo
            {
                CardHolderName = dto.Card!.CardHolderName ?? dto.FullName,
                CardNumber = dto.Card.CardNumber,
                ExpireMonth = dto.Card.ExpireMonth,
                ExpireYear = dto.Card.ExpireYear?.Length == 2 ? "20" + dto.Card.ExpireYear : dto.Card.ExpireYear,
                Cvc = dto.Card.Cvc,
                Installment = 1
            };
            var payRes = await _paymentService.ProcessAppointmentDepositAsync(cid, depositAmount, card, buyerIp);
            if (!payRes.Success)
                return (false, "Depozito tahsil edilemedi: " + (payRes.Error ?? "Bilinmeyen hata"), null);
            paymentTxUid = payRes.TransactionUid;
        }

        // Randevu olustur (StatusId=1: Planlanmis, onay bekliyor)
        // Npgsql timestamptz Utc kind bekler — local/unspecified -> utc kind olarak isaretle (saat degismez)
        var start = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc);
        var appointment = new SlnAppointment
        {
            CustomerId = cid,
            SlnClientId = client.Id,
            PersonnelId = dto.PersonnelId ?? 0,
            ServiceId = dto.ServiceId,
            StartTime = start,
            EndTime = start.AddMinutes(service.DurationMinutes),
            StatusId = 1,
            Notes = dto.Notes,
            DepositAmount = depositAmount,
            IsPrepaid = requireDeposit,
            PrepaidAmount = requireDeposit ? depositAmount : 0
        };

        // PersonnelId 0 ise ilk musait personeli ata
        if (appointment.PersonnelId == 0)
        {
            var firstPersonnel = await _personnel.GetAllQueryable()
                .FirstOrDefaultAsync(p => p.CustomerId == cid && p.IsActive);
            appointment.PersonnelId = firstPersonnel?.Id ?? 0;
        }

        _appointments.Add(appointment);
        await _uow.SaveChangesAsync();

        return (true, null, new
        {
            success = true,
            appointmentId = appointment.Id,
            isPrepaid = requireDeposit,
            prepaidAmount = depositAmount,
            paymentTxUid,
            message = requireDeposit
                ? $"Randevunuz alindi. {depositAmount:N2} TL depozito tahsil edildi."
                : "Randevunuz alindi. Salon tarafindan onaylanacaktir."
        });
    }
}
