using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnPublicFactory : ISlnPublicFactory
{
    private readonly ISlnSalonProfileEntityService _profiles;
    private readonly ISlnBranchEntityService _branches;
    private readonly ISlnServiceCategoryEntityService _serviceCategories;
    private readonly ISlnServiceEntityService _services;
    private readonly ISlnServiceComboEntityService _combos;
    private readonly ISlnReviewEntityService _reviews;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly ISlnMembershipPlanEntityService _membershipPlans;
    private readonly ISlnClientMembershipEntityService _clientMemberships;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnAppointmentServiceEntityService _appointmentServices;
    private readonly ISlnServiceResourceRequirementEntityService _requirements;
    private readonly ISlnPersonnelSkillEntityService _skills;
    private readonly ISlnNoShowPolicyEntityService _noShowPolicies;
    private readonly ISlnWaitlistEntryEntityService _waitlist;
    private readonly IPlatformUserEntityService _platformUsers;
    private readonly IPlatformUserSalonEntityService _platformUserSalons;
    private readonly PaymentService _paymentService;
    private readonly IUnitOfWork _uow;

    public SlnPublicFactory(
        ISlnSalonProfileEntityService profiles,
        ISlnBranchEntityService branches,
        ISlnServiceCategoryEntityService serviceCategories,
        ISlnServiceEntityService services,
        ISlnServiceComboEntityService combos,
        ISlnReviewEntityService reviews,
        ICustomerPersonnelEntityService personnel,
        ISlnMembershipPlanEntityService membershipPlans,
        ISlnClientMembershipEntityService clientMemberships,
        ISlnClientEntityService clients,
        ISlnAppointmentEntityService appointments,
        ISlnAppointmentServiceEntityService appointmentServices,
        ISlnServiceResourceRequirementEntityService requirements,
        ISlnPersonnelSkillEntityService skills,
        ISlnNoShowPolicyEntityService noShowPolicies,
        ISlnWaitlistEntryEntityService waitlist,
        IPlatformUserEntityService platformUsers,
        IPlatformUserSalonEntityService platformUserSalons,
        PaymentService paymentService,
        IUnitOfWork uow)
    {
        _profiles = profiles;
        _branches = branches;
        _serviceCategories = serviceCategories;
        _services = services;
        _combos = combos;
        _reviews = reviews;
        _personnel = personnel;
        _membershipPlans = membershipPlans;
        _clientMemberships = clientMemberships;
        _clients = clients;
        _appointments = appointments;
        _appointmentServices = appointmentServices;
        _requirements = requirements;
        _skills = skills;
        _noShowPolicies = noShowPolicies;
        _waitlist = waitlist;
        _platformUsers = platformUsers;
        _platformUserSalons = platformUserSalons;
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
                .Include(p => p.Customer)
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
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);
        }

        var categories = await _serviceCategories.GetAllQueryable()
            .Where(c => c.CustomerId == customerId && c.IsActive)
            .Include(c => c.Services.Where(s => s.IsActive))
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var combos = await _combos.GetAllQueryable()
            .Where(c => c.CustomerId == customerId && c.IsActive)
            .Include(c => c.Items).ThenInclude(i => i.Service)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync();

        return new SlnSalonProfileDto
        {
            Id = profile?.Id ?? 0,
            CustomerId = customerId,
            Slug = branch?.Slug ?? profile?.Slug ?? "",
            SalonName = branch?.Customer?.Name ?? profile?.Customer?.Name ?? "",
            BranchName = branch?.Name,
            IsHeadquarter = branch?.IsHeadquarter ?? true,
            Description = profile?.Description,
            Address = branch?.Address ?? profile?.Address,
            City = branch?.City ?? profile?.City,
            District = branch?.District ?? profile?.District,
            Phone = branch?.Phone ?? profile?.Phone,
            Email = branch?.Email ?? profile?.Email,
            Website = profile?.Website,
            GoogleMapsUrl = branch?.GoogleMapsUrl ?? profile?.GoogleMapsUrl,
            LogoUrl = PreferBranchMedia(branch?.PhotoUrl, profile?.LogoUrl),
            CoverImageUrl = PreferBranchMedia(branch?.CoverImageUrl, profile?.CoverImageUrl),
            FaviconUrl = profile?.FaviconUrl,
            GalleryImagesJson = PreferBranchMedia(branch?.GalleryImagesJson, profile?.GalleryImagesJson),
            WorkingHoursJson = branch?.WorkingHoursJson ?? profile?.WorkingHoursJson,
            IsPublished = profile?.IsPublished ?? true,
            ShowServices = profile?.ShowServices ?? true,
            ShowMemberships = profile?.ShowMemberships ?? true,
            ShowBooking = profile?.ShowBooking ?? true,
            ShowHours = profile?.ShowHours ?? true,
            ShowContact = profile?.ShowContact ?? true,
            SectionOrderJson = profile?.SectionOrderJson,
            ShowBanners = profile?.ShowBanners ?? true,
            ShowTeam = profile?.ShowTeam ?? true,
            ShowReviews = profile?.ShowReviews ?? true,
            ShowMap = profile?.ShowMap ?? true,
            BannersJson = profile?.BannersJson,
            Latitude = branch?.Latitude ?? profile?.Latitude,
            Longitude = branch?.Longitude ?? profile?.Longitude,
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
                    TaxRate = s.TaxRate,
                    SortOrder = s.SortOrder,
                    IsActive = s.IsActive
                }).ToList()
            }).ToList(),
            ServiceCombos = combos
                .Where(c => c.Items.Any(i => i.Service?.IsActive == true))
                .Select(MapPublicComboToDto)
                .ToList()
        };
    }

    /// <summary>Tum yayinlanmis salonlari listele (merkez sube bilgileriyle)</summary>
    public async Task<object> GetAllPublishedAsync()
    {
        var profiles = await _profiles.GetAllQueryable()
            .Include(p => p.Customer)
            .ToListAsync();

        var profileMap = profiles
            .GroupBy(p => p.CustomerId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.IsPublished).First());

        var allBranches = await _branches.GetAllQueryable()
            .Where(b => b.IsActive)
            .Include(b => b.Customer)
            .ToListAsync();

        var discoverBranches = allBranches
            .Where(b => !profileMap.TryGetValue(b.CustomerId, out var p) || p.IsPublished)
            .ToList();
        if (discoverBranches.Count == 0) return new List<object>();

        var customerIds = discoverBranches.Select(b => b.CustomerId).Distinct().ToList();

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

        return discoverBranches
            .OrderBy(b => b.IsHeadquarter ? 0 : 1)
            .ThenBy(b => profileMap.TryGetValue(b.CustomerId, out var pr) ? pr.Customer?.Name : b.Customer?.Name)
            .Select(b =>
            {
                profileMap.TryGetValue(b.CustomerId, out var profile);
                reviewStats.TryGetValue(b.CustomerId, out var rs);
                priceStats.TryGetValue(b.CustomerId, out var ps);
                string? priceRange = null;
                if (ps != null)
                    priceRange = ps.Min == ps.Max
                        ? $"{ps.Min:N0} ₺"
                        : $"{ps.Min:N0} - {ps.Max:N0} ₺";

                return new
                {
                    Slug = b.Slug ?? profile?.Slug ?? "",
                    SalonName = profile?.Customer?.Name ?? b.Customer?.Name ?? "",
                    BranchName = b.Name,
                    b.IsHeadquarter,
                    b.City,
                    b.District,
                    LogoUrl = PreferBranchMedia(b.PhotoUrl, profile?.LogoUrl),
                    Description = profile?.Description,
                    b.Latitude,
                    b.Longitude,
                    AverageRating = rs?.Avg ?? 0,
                    ReviewCount = rs?.Count ?? 0,
                    PriceRange = priceRange
                };
            }).ToList();
    }

    /// <summary>Discover harita için tüm yayınlanan salonların aktif şubelerini koordinatlarıyla döner</summary>
    public async Task<object> GetAllBranchesForMapAsync()
    {
        var profiles = await _profiles.GetAllQueryable()
            .Select(p => new { p.CustomerId, p.IsPublished })
            .ToListAsync();
        var profileMap = profiles
            .GroupBy(p => p.CustomerId)
            .ToDictionary(g => g.Key, g => g.Any(x => x.IsPublished));

        var branches = await _branches.GetAllQueryable()
            .Where(b => b.IsActive
                     && b.Latitude != null && b.Longitude != null)
            .Include(b => b.Customer)
            .ToListAsync();

        branches = branches
            .Where(b => !profileMap.TryGetValue(b.CustomerId, out var isPublished) || isPublished)
            .ToList();

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

    private sealed record ResolvedSalonScope(int CustomerId, int? BranchId);

    /// <summary>Branch slug veya eski profile slug'dan salon ve etkin sube kapsamini bul.</summary>
    private async Task<ResolvedSalonScope?> ResolveSalonScopeAsync(string slug)
    {
        var branch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);
        if (branch != null)
            return new ResolvedSalonScope(branch.CustomerId, branch.Id);

        var profile = await _profiles.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (profile == null)
            return null;

        var headquarterBranchId = await _branches.GetAllQueryable()
            .Where(b => b.CustomerId == profile.CustomerId && b.IsHeadquarter && b.IsActive)
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();

        return new ResolvedSalonScope(profile.CustomerId, headquarterBranchId);
    }

    private async Task<int?> ResolveCustomerIdAsync(string slug)
        => (await ResolveSalonScopeAsync(slug))?.CustomerId;

    private static IQueryable<CustomerPersonnel> ApplyPublicBranchScope(
        IQueryable<CustomerPersonnel> query,
        int? branchId)
    {
        return branchId.HasValue
            ? query.Where(p => p.BranchId == branchId.Value || p.BranchId == null)
            : query;
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

    /// <summary>Salonun ekibini getir — sadece HIZMET VEREN aktif personel (Kuafor/Uzman vs.)</summary>
    public async Task<object?> GetTeamAsync(string slug)
    {
        var scope = await ResolveSalonScopeAsync(slug);
        if (scope == null) return null;

        // Sadece teknik personel (skill atayanlar): Hairdresser, Beautician, Default (Diğer)
        // Hariç: SalonOwner(101), Manager(102), Cashier(105), Receptionist(106), BranchManager(107)
        var serviceRoleIds = new[] {
            Shared.Enums.SalonRoles.Ids.Hairdresser,
            Shared.Enums.SalonRoles.Ids.Beautician
        };

        var teamQuery = _personnel.GetAllQueryable()
            .Where(p => p.CustomerId == scope.CustomerId && p.IsActive && p.PublicVisible
                     && serviceRoleIds.Contains(p.CustomerRoleId));

        var team = await ApplyPublicBranchScope(teamQuery, scope.BranchId)
            .Include(p => p.User)
            .OrderBy(p => p.CustomerRoleId)
            .Select(p => new
            {
                Id = p.Id,
                name = p.PublicShowFullName ? p.User.FullName : p.User.FullName.Split(' ')[0],
                title = p.PublicShowTitle ? p.Title : (string?)null,
                specialty = p.PublicShowSpecialty ? p.Specialty : (string?)null,
                photoUrl = p.PublicShowPhoto ? p.PhotoUrl : (string?)null,
                roleId = p.CustomerRoleId
            })
            .ToListAsync();

        // Yetenekler — SlnPersonnelSkill üzerinden hizmet adlarını ekle
        var personnelIds = team.Select(t => t.Id).ToList();
        var skills = await _skills.GetAllQueryable()
            .Where(s => personnelIds.Contains(s.PersonnelId))
            .Include(s => s.Service)
            .Select(s => new { s.PersonnelId, ServiceName = s.Service != null ? s.Service.Name : "" })
            .ToListAsync();
        var skillsByPersonnel = skills
            .GroupBy(s => s.PersonnelId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ServiceName).Where(n => !string.IsNullOrEmpty(n)).ToList());

        return team.Select(t => new
        {
            t.name,
            t.title,
            t.specialty,
            t.photoUrl,
            t.roleId,
            services = skillsByPersonnel.TryGetValue(t.Id, out var svc) ? svc : new List<string>()
        }).ToList();
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
                b.Id, b.Name, Slug = b.Slug ?? "", b.Address, b.City, b.District,
                b.Phone, b.IsHeadquarter, b.Latitude, b.Longitude,
                b.PhotoUrl, b.CoverImageUrl, b.GalleryImagesJson, b.WorkingHoursJson
            })
            .ToListAsync();

        return branches;
    }

    private static string? PreferBranchMedia(string? branchValue, string? profileValue)
        => string.IsNullOrWhiteSpace(branchValue) ? profileValue : branchValue;

    private async Task<SlnClient> FindOrCreatePublicClientAsync(int customerId, string fullName, string? phone, string? email, int? branchId = null)
    {
        var phoneVariants = PhoneHelper.GetLookupVariants(phone);
        var normalizedPhone = PhoneHelper.Normalize(phone) ?? PhoneHelper.Sanitize(phone);
        var normalizedEmail = NormalizeEmail(email);

        var client = phoneVariants.Count == 0
            ? null
            : await _clients.GetAllQueryable()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Phone != null && phoneVariants.Contains(c.Phone));

        if (client == null)
        {
            client = new SlnClient
            {
                CustomerId = customerId,
                BranchId = branchId,
                FullName = fullName.Trim(),
                Phone = string.IsNullOrWhiteSpace(normalizedPhone) ? phone?.Trim() : normalizedPhone,
                Email = normalizedEmail
            };
            _clients.Add(client);
            await _uow.SaveChangesAsync();
            return client;
        }

        var changed = false;
        if (client.BranchId == null && branchId.HasValue)
        {
            client.BranchId = branchId;
            changed = true;
        }

        if (!client.IsActive && !client.IsBlacklisted)
        {
            client.IsActive = true;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(client.FullName))
        {
            client.FullName = fullName.Trim();
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(normalizedPhone) && client.Phone != normalizedPhone)
        {
            client.Phone = normalizedPhone;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(normalizedEmail) && string.IsNullOrWhiteSpace(client.Email))
        {
            client.Email = normalizedEmail;
            changed = true;
        }

        if (changed)
        {
            client.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
        }

        return client;
    }

    private async Task<SlnClient?> FindPublicClientAsync(int customerId, string? phone, string? email)
    {
        var phoneVariants = PhoneHelper.GetLookupVariants(phone);
        if (phoneVariants.Count > 0)
        {
            var byPhone = await _clients.GetAllQueryable()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Phone != null && phoneVariants.Contains(c.Phone));
            if (byPhone != null) return byPhone;
        }

        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail)) return null;

        return await _clients.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Email != null && c.Email.ToLower() == normalizedEmail);
    }

    private static IQueryable<SlnAppointment> ApplyAppointmentBranchScope(IQueryable<SlnAppointment> query, int? branchId)
        => branchId.HasValue
            ? query.Where(a => a.BranchId == branchId.Value)
            : query.Where(a => a.BranchId == null);

    private async Task ExpireStalePendingAppointmentsAsync(int customerId, int? branchId)
    {
        var cutoff = DateTime.UtcNow - PaymentService.PendingPaymentHoldTimeout;
        var staleAppointments = await ApplyAppointmentBranchScope(
                _appointments.GetAllQueryable()
                    .Where(a => a.CustomerId == customerId
                             && a.StatusId == 6
                             && !a.IsPrepaid
                             && a.CreatedAt <= cutoff),
                branchId)
            .ToListAsync();

        if (staleAppointments.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var stale in staleAppointments)
        {
            stale.StatusId = 4;
            stale.UpdatedAt = now;
        }

        await _uow.SaveChangesAsync();
    }

    private static bool PendingAppointmentMatchesServices(SlnAppointment appointment, int? comboId, IReadOnlyCollection<int> serviceIds)
    {
        if (comboId.HasValue && appointment.ComboId == comboId.Value)
            return true;

        var expected = serviceIds.OrderBy(id => id).ToList();
        var actual = (appointment.Services?.Select(s => s.SlnServiceId).ToList() ?? new List<int>())
            .OrderBy(id => id)
            .ToList();

        if (actual.Count == 0 && appointment.ServiceId.HasValue)
            actual = new List<int> { appointment.ServiceId.Value };

        return actual.Count == expected.Count && actual.SequenceEqual(expected);
    }

    private async Task<SlnAppointment?> FindMatchingPendingPaymentAppointmentAsync(
        int customerId,
        int clientId,
        int? branchId,
        DateTime startTime,
        DateTime endTime,
        int? comboId,
        IReadOnlyCollection<int> serviceIds)
    {
        var candidates = await ApplyAppointmentBranchScope(
                _appointments.GetAllQueryable()
                    .Where(a => a.CustomerId == customerId
                             && a.SlnClientId == clientId
                             && a.StatusId == 6
                             && !a.IsPrepaid
                             && a.StartTime == startTime
                             && a.EndTime == endTime),
                branchId)
            .Include(a => a.Services)
            .ToListAsync();

        return candidates.FirstOrDefault(a => PendingAppointmentMatchesServices(a, comboId, serviceIds));
    }

    private async Task<bool> HasActivePendingPaymentAppointmentAsync(int customerId, int clientId, int? branchId)
    {
        var now = DateTime.UtcNow;
        return await ApplyAppointmentBranchScope(
                _appointments.GetAllQueryable()
                    .Where(a => a.CustomerId == customerId
                             && a.SlnClientId == clientId
                             && a.StatusId == 6
                             && !a.IsPrepaid
                             && a.EndTime >= now),
                branchId)
            .AnyAsync();
    }

    private async Task LinkPlatformUserToSalonAsync(int customerId, SlnClient client, string? phone, string? email)
    {
        var platformUser = await FindPlatformUserAsync(phone ?? client.Phone, email ?? client.Email);
        if (platformUser == null || !platformUser.IsActive) return;

        var link = await _platformUserSalons.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.PlatformUserId == platformUser.Id && s.CustomerId == customerId);

        if (link == null)
        {
            _platformUserSalons.Add(new PlatformUserSalon
            {
                PlatformUserId = platformUser.Id,
                CustomerId = customerId,
                SlnClientId = client.Id,
                IsActive = true
            });
            await _uow.SaveChangesAsync();
            return;
        }

        var changed = false;
        if (!link.IsActive)
        {
            link.IsActive = true;
            link.JoinedAt = DateTime.UtcNow;
            changed = true;
        }

        if (link.SlnClientId == null)
        {
            link.SlnClientId = client.Id;
            changed = true;
        }

        if (changed)
            await _uow.SaveChangesAsync();
    }

    private async Task<PlatformUser?> FindPlatformUserAsync(string? phone, string? email)
    {
        var phoneVariants = PhoneHelper.GetLookupVariants(phone);
        if (phoneVariants.Count > 0)
        {
            var byPhone = await _platformUsers.GetAllQueryable()
                .FirstOrDefaultAsync(u => phoneVariants.Contains(u.Phone));
            if (byPhone != null) return byPhone;
        }

        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail)) return null;

        return await _platformUsers.GetAllQueryable()
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail);
    }

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    /// <summary>Salonun aktif uyelik planlarini getir</summary>
    public async Task<object?> GetMembershipPlansAsync(string slug)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return null;

        var plans = await _membershipPlans.GetAllQueryable()
            .Where(p => p.CustomerId == customerId.Value && p.IsActive)
            .Include(p => p.Services).ThenInclude(s => s.Service)
            .OrderBy(p => p.SortOrder)
            .Select(p => new
            {
                p.Id, p.Name, p.Description, p.IconClass, p.Color,
                p.DurationType,
                p.DurationDays,
                p.Price,
                MonthlyPrice = p.Price,
                Currency = "TRY",
                TaxIncluded = true,
                IsSalonCustomerMembership = true,
                p.DiscountPercent,
                p.PriorityBooking,
                ServiceDetails = p.Services.Select(s => new
                {
                    s.ServiceId,
                    ServiceName = s.Service != null ? s.Service.Name : "",
                    s.FreeCount,
                    DiscountPercent = s.DiscountPercent ?? 0
                }).ToList()
            })
            .ToListAsync();

        return plans;
    }

    /// <summary>Online uyelik basvurusu (auth gerekmez)</summary>
    public async Task<(bool Success, string? Error, object? Result)> MembershipSignupAsync(string slug, SlnMembershipSignupDto dto)
    {
        var scope = await ResolveSalonScopeAsync(slug);
        if (scope == null) return (false, "Salon bulunamadi", null);
        var cid = scope.CustomerId;

        var plan = await _membershipPlans.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.CustomerId == cid && p.IsActive);
        if (plan == null) return (false, "Uyelik plani bulunamadi.", null);

        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
            return (false, "Ad ve telefon zorunludur.", null);

        // Musteri bul veya olustur
        var client = await FindOrCreatePublicClientAsync(cid, dto.FullName, dto.Phone, dto.Email, scope.BranchId);

        // Mevcut musterinin zaten aktif uyeligi var mi?
        var existing = await _clientMemberships.GetAllQueryable()
            .AnyAsync(m => m.SlnClientId == client.Id && m.CustomerId == cid && m.StatusId == 1);
        if (existing)
            return (false, "Bu telefon numarasina ait zaten aktif bir uyelik bulunmaktadir.", null);

        if (plan.Price > 0m)
        {
            return (true, null, new
            {
                success = true,
                requiresPayment = true,
                slnClientId = client.Id,
                planId = plan.Id,
                amount = plan.Price,
                message = "Uyelik basvurunuz alindi. Uyelik odeme tamamlandiktan sonra aktif edilecektir."
            });
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
        await LinkPlatformUserToSalonAsync(cid, client, dto.Phone, dto.Email);

        return (true, null, new
        {
            success = true,
            requiresPayment = false,
            slnClientId = client.Id,
            planId = plan.Id,
            message = "Uyelik basvurunuz alinmistir."
        });
    }

    /// <summary>Belirli salon + tarih + hizmet icin musait saatleri getir</summary>
    public async Task<object?> GetAvailableSlotsAsync(string slug, IReadOnlyCollection<int> requestedServiceIds, DateTime date, int? personnelId = null, int? comboId = null)
    {
        var scope = await ResolveSalonScopeAsync(slug);
        if (scope == null) return null;
        var cid = scope.CustomerId;

        var resolvedServices = await ResolvePublicBookingServicesAsync(cid, requestedServiceIds, comboId);
        if (resolvedServices.Error != null) return null;
        var serviceIds = resolvedServices.Services.Select(s => s.Id).ToList();

        // Calisma saatleri (branch'ten al) + Customer.TimeZone
        var branch = await _branches.GetAllQueryable().Include(b => b.Customer).FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive)
            ?? await _branches.GetAllQueryable().Include(b => b.Customer).FirstOrDefaultAsync(b => b.CustomerId == cid && b.IsHeadquarter);

        var dayKey = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "mon", DayOfWeek.Tuesday => "tue", DayOfWeek.Wednesday => "wed",
            DayOfWeek.Thursday => "thu", DayOfWeek.Friday => "fri", DayOfWeek.Saturday => "sat",
            _ => "sun"
        };

        // Calisma saatlerini coz
        var openHour = 9; var closeHour = 19;
        bool isClosed = false;
        var workingHoursJson = branch?.WorkingHoursJson;
        if (!string.IsNullOrEmpty(workingHoursJson))
        {
            try
            {
                var hours = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(workingHoursJson);
                if (hours != null && hours.TryGetValue(dayKey, out var val))
                {
                    if (val == "closed")
                        isClosed = true;
                    else
                    {
                        var parts = val.Split('-');
                        if (parts.Length == 2)
                        {
                            openHour = int.Parse(parts[0].Split(':')[0]);
                            closeHour = int.Parse(parts[1].Split(':')[0]);
                        }
                    }
                }
            }
            catch { isClosed = true; } // parse hatasi = kapalı say
        }

        if (isClosed)
            return new { isClosed = true, slots = new List<object>() };

        // Hizmet icin yetenekli personelleri bul
        var skillScope = await GetSkillScopeAsync(serviceIds);

        var staffQuery = _personnel.GetAllQueryable()
            .Where(p => p.CustomerId == cid && p.IsActive
                     && p.CustomerRoleId != Shared.Enums.SalonRoles.Ids.SalonOwner);
        staffQuery = ApplyPublicBranchScope(staffQuery, scope.BranchId);

        if (skillScope.HasSkillDefinitions)
            staffQuery = staffQuery.Where(p => skillScope.PersonnelIds.Contains(p.Id));

        // Belirli personel istendiyse sadece onu al
        if (personnelId.HasValue)
            staffQuery = staffQuery.Where(p => p.Id == personnelId.Value);

        var allStaff = await staffQuery
            .Include(p => p.User)
            .OrderBy(p => p.User.FullName)
            .Select(p => new
            {
                p.Id,
                Name = p.PublicShowFullName ? p.User.FullName : p.User.FullName.Split(' ')[0],
                p.PhotoUrl
            })
            .ToListAsync();

        if (allStaff.Count == 0)
            return new { isClosed = false, slots = new List<object>() };

        await ExpireStalePendingAppointmentsAsync(cid, branch?.Id ?? scope.BranchId);

        // Gunun randevularini personel bazinda yukle
        var dayStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);
        var staffIds = allStaff.Select(s => s.Id).ToList();
        var existingAppointments = await _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == cid
                     && staffIds.Contains(a.PersonnelId)
                     && a.StartTime >= dayStart && a.StartTime < dayEnd
                     && a.StatusId != 4)
            .Select(a => new { a.PersonnelId, a.StartTime, a.EndTime })
            .ToListAsync();

        // Zaman dilimi
        var salonTzId = branch?.Customer?.TimeZone ?? "Europe/Istanbul";
        TimeZoneInfo salonTz;
        try { salonTz = TimeZoneInfo.FindSystemTimeZoneById(salonTzId); }
        catch { salonTz = TimeZoneInfo.Utc; }
        var nowForCompare = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, salonTz), DateTimeKind.Utc);

        var slots = new List<object>();
        var slotDuration = CalculateBookableMinutes(resolvedServices.Services);

        for (var hour = openHour; hour < closeHour; hour++)
        {
            for (var min = 0; min < 60; min += 30)
            {
                var slotStart = dayStart.AddHours(hour).AddMinutes(min);
                var slotEnd = slotStart.AddMinutes(slotDuration);

                if (slotEnd > dayStart.AddHours(closeHour)) break;
                if (slotStart < nowForCompare) continue;

                var resourceConflict = await FindResourceConflictAsync(cid, branch?.Id ?? scope.BranchId, serviceIds, slotStart, slotEnd);
                if (resourceConflict != null) continue;

                // Bu slotta hangi personeller musait?
                var freeStaff = allStaff.Where(s =>
                    !existingAppointments.Any(a =>
                        a.PersonnelId == s.Id &&
                        slotStart < a.EndTime && slotEnd > a.StartTime))
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        initials = BuildInitials(s.Name),
                        photoUrl = s.PhotoUrl
                    })
                    .ToList();

                if (freeStaff.Count == 0) continue; // hic musait personel yok

                slots.Add(new
                {
                    startTime = slotStart.ToString("yyyy-MM-ddTHH:mm:ss"),
                    endTime = slotEnd.ToString("yyyy-MM-ddTHH:mm:ss"),
                    timeText = slotStart.ToString("HH:mm"),
                    availableStaff = freeStaff
                });
            }
        }

        return new { isClosed = false, slots };
    }

    private static string BuildInitials(string name)
    {
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpperInvariant();
    }

    private static int CalculateBookableMinutes(SlnService service)
        => Math.Max(5, Math.Max(service.DurationMinutes, service.ProcessingMinutes) + service.BufferBeforeMinutes + service.BufferAfterMinutes);

    private static int CalculateBookableMinutes(IEnumerable<SlnService> services)
        => services.Sum(CalculateBookableMinutes);

    private static SlnServiceComboDto MapPublicComboToDto(SlnServiceCombo combo) => new()
    {
        Id = combo.Id,
        Name = combo.Name,
        Description = combo.Description,
        Price = combo.Price,
        DurationMinutes = CalculateBookableMinutes(combo.Items
            .OrderBy(i => i.SortOrder)
            .Select(i => i.Service)
            .Where(s => s != null && s.IsActive)
            .Select(s => s!)),
        IsActive = combo.IsActive,
        SortOrder = combo.SortOrder,
        Items = combo.Items.OrderBy(i => i.SortOrder).Where(i => i.Service?.IsActive == true).Select(i => new SlnServiceComboItemDto
        {
            Id = i.Id,
            ServiceId = i.ServiceId,
            ServiceName = i.Service?.Name ?? "",
            DurationMinutes = i.Service == null ? 0 : CalculateBookableMinutes(i.Service),
            SortOrder = i.SortOrder
        }).ToList()
    };

    private async Task<PublicBookingServices> ResolvePublicBookingServicesAsync(int customerId, IReadOnlyCollection<int> requestedServiceIds, int? comboId)
    {
        if (comboId.HasValue && comboId.Value > 0)
        {
            var combo = await _combos.GetAllQueryable()
                .Include(c => c.Items).ThenInclude(i => i.Service)
                .FirstOrDefaultAsync(c => c.Id == comboId.Value && c.CustomerId == customerId && c.IsActive);

            if (combo == null)
                return new([], null, "Combo bulunamadi");

            var comboServices = combo.Items
                .OrderBy(i => i.SortOrder)
                .Select(i => i.Service)
                .Where(s => s != null && s.CustomerId == customerId && s.IsActive)
                .Select(s => s!)
                .ToList();

            return comboServices.Count == 0
                ? new([], combo, "Combo icinde aktif hizmet yok")
                : new(comboServices, combo, null);
        }

        var serviceIds = requestedServiceIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (serviceIds.Count == 0)
            return new([], null, "En az bir hizmet secilmeli");

        var services = await _services.GetAllQueryable()
            .Where(s => serviceIds.Contains(s.Id) && s.CustomerId == customerId && s.IsActive)
            .ToListAsync();

        if (services.Count != serviceIds.Count)
            return new([], null, "Hizmet bulunamadi");

        var orderedServices = serviceIds
            .Select(id => services.First(s => s.Id == id))
            .ToList();

        return new(orderedServices, null, null);
    }

    private sealed record PublicBookingServices(List<SlnService> Services, SlnServiceCombo? Combo, string? Error);

    private static List<int> ResolveBookingDtoServiceIds(SlnOnlineBookingDto dto)
    {
        var serviceIds = dto.ServiceIds
            .Where(id => id > 0)
            .ToList();
        if (dto.ServiceId > 0 && !serviceIds.Contains(dto.ServiceId))
            serviceIds.Insert(0, dto.ServiceId);
        return serviceIds.Distinct().ToList();
    }

    private async Task<(List<int> PersonnelIds, bool HasSkillDefinitions)> GetSkillScopeAsync(List<int> serviceIds)
    {
        var skillRows = await _skills.GetAllQueryable()
            .Where(s => serviceIds.Contains(s.ServiceId))
            .Select(s => new { s.ServiceId, s.PersonnelId })
            .ToListAsync();

        var serviceIdsWithSkills = skillRows.Select(s => s.ServiceId).Distinct().ToList();
        var personnelIds = skillRows
            .GroupBy(s => s.PersonnelId)
            .Where(g => serviceIdsWithSkills.All(serviceId => g.Any(s => s.ServiceId == serviceId)))
            .Select(g => g.Key)
            .ToList();

        return (personnelIds, serviceIdsWithSkills.Count > 0);
    }

    private async Task<string?> FindResourceConflictAsync(int customerId, int? branchId, List<int> serviceIds, DateTime startTime, DateTime endTime)
    {
        if (serviceIds.Count == 0) return null;

        var requirements = await _requirements.GetAllQueryable()
            .Where(r => serviceIds.Contains(r.ServiceId))
            .Include(r => r.Resource)
            .ToListAsync();

        var needed = requirements
            .Where(r => r.Resource != null)
            .GroupBy(r => r.ResourceId)
            .Select(g => new
            {
                Resource = g.First().Resource!,
                QuantityRequired = g.Max(x => Math.Max(1, x.QuantityRequired))
            })
            .ToList();

        if (needed.Count == 0) return null;

        var overlapping = await _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId
                && a.StatusId != 4
                && a.StatusId != 5
                && a.StartTime < endTime
                && a.EndTime > startTime)
            .Include(a => a.Service)!.ThenInclude(s => s!.ResourceRequirements)
            .Include(a => a.Services).ThenInclude(s => s.SlnService)!.ThenInclude(s => s!.ResourceRequirements)
            .ToListAsync();

        foreach (var item in needed)
        {
            var resource = item.Resource;
            if (resource.CustomerId != customerId || !resource.IsActive)
                return $"{resource.Name} kaynagi aktif degil";
            if (resource.BranchId.HasValue && branchId.HasValue && resource.BranchId.Value != branchId.Value)
                return $"{resource.Name} bu sube icin uygun degil";
            if (resource.BranchId.HasValue && !branchId.HasValue)
                return $"{resource.Name} icin sube secimi gerekli";

            var used = 0;
            foreach (var appointment in overlapping)
            {
                var appointmentRequirements = appointment.Services.Count > 0
                    ? appointment.Services
                        .Where(s => s.SlnService != null)
                        .SelectMany(s => s.SlnService!.ResourceRequirements)
                    : appointment.Service?.ResourceRequirements ?? [];

                used += appointmentRequirements
                    .Where(r => r.ResourceId == resource.Id)
                    .Select(r => Math.Max(1, r.QuantityRequired))
                    .DefaultIfEmpty(0)
                    .Max();
            }

            if (used + item.QuantityRequired > resource.Quantity)
                return $"{resource.Name} kapasitesi dolu";
        }

        return null;
    }

    /// <summary>Hizmet icin musait personelleri getir (skill eslemesi)</summary>
    public async Task<object?> GetAvailableStaffForServiceAsync(string slug, IReadOnlyCollection<int> requestedServiceIds, int? comboId = null)
    {
        var scope = await ResolveSalonScopeAsync(slug);
        if (scope == null) return null;
        var cid = scope.CustomerId;

        var resolvedServices = await ResolvePublicBookingServicesAsync(cid, requestedServiceIds, comboId);
        if (resolvedServices.Error != null) return null;
        var serviceIds = resolvedServices.Services.Select(s => s.Id).ToList();

        // Skill tanimi olan hizmetlerde personel tum gerekli skill'lere sahip olmali.
        var skillScope = await GetSkillScopeAsync(serviceIds);

        var query = _personnel.GetAllQueryable()
            .Where(p => p.CustomerId == cid && p.IsActive && p.PublicVisible
                     && p.CustomerRoleId != Shared.Enums.SalonRoles.Ids.SalonOwner);
        query = ApplyPublicBranchScope(query, scope.BranchId);

        // Skill tanimlanmissa filtrele, yoksa tum aktif personelleri don
        if (skillScope.HasSkillDefinitions)
            query = query.Where(p => skillScope.PersonnelIds.Contains(p.Id));

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
        var scope = await ResolveSalonScopeAsync(slug);
        if (scope == null) return (false, "Salon bulunamadi", null);
        var cid = scope.CustomerId;

        var resolvedServices = await ResolvePublicBookingServicesAsync(cid, ResolveBookingDtoServiceIds(dto), dto.ComboId);
        if (resolvedServices.Error != null) return (false, resolvedServices.Error, null);
        var serviceIds = resolvedServices.Services.Select(s => s.Id).ToList();
        var primaryService = resolvedServices.Services.First();

        // ── ADIM 1: Zaman + müsaitlik doğrulaması ──────────────────────────
        // Kart çekilmeden ÖNCE tüm kontroller yapılır; slot müsait değilse
        // para tahsil edilmez.
        var start = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc);
        var end = start.AddMinutes(CalculateBookableMinutes(resolvedServices.Services));

        // Kapalı gün kontrolü
        var bookBranch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive)
            ?? await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.CustomerId == cid && b.IsHeadquarter);

        if (!string.IsNullOrEmpty(bookBranch?.WorkingHoursJson))
        {
            try
            {
                var bookDayKey = start.DayOfWeek switch
                {
                    DayOfWeek.Monday => "mon", DayOfWeek.Tuesday => "tue", DayOfWeek.Wednesday => "wed",
                    DayOfWeek.Thursday => "thu", DayOfWeek.Friday => "fri", DayOfWeek.Saturday => "sat",
                    _ => "sun"
                };
                var wh = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(bookBranch.WorkingHoursJson);
                if (wh != null && wh.TryGetValue(bookDayKey, out var whVal) && whVal == "closed")
                    return (false, "Salon bu gun kapalidir. Lutfen baska bir tarih secin.", null);
            }
            catch { }
        }

        // Personel belirle + overlap kontrolü
        var resolvedPersonnelId = dto.PersonnelId ?? 0;
        if (resolvedPersonnelId == 0)
        {
            var skillScope = await GetSkillScopeAsync(serviceIds);

            var bookStaffQuery = _personnel.GetAllQueryable()
                .Where(p => p.CustomerId == cid && p.IsActive
                         && p.CustomerRoleId != Shared.Enums.SalonRoles.Ids.SalonOwner);
            bookStaffQuery = ApplyPublicBranchScope(bookStaffQuery, scope.BranchId);

            if (skillScope.HasSkillDefinitions)
                bookStaffQuery = bookStaffQuery.Where(p => skillScope.PersonnelIds.Contains(p.Id));

            var busyPersonnelIds = await _appointments.GetAllQueryable()
                .Where(a => a.CustomerId == cid
                         && a.StatusId != 4
                         && a.StartTime < end && a.EndTime > start)
                .Select(a => a.PersonnelId)
                .ToListAsync();

            var freePersonnel = await bookStaffQuery
                .Where(p => !busyPersonnelIds.Contains(p.Id))
                .FirstOrDefaultAsync();

            if (freePersonnel == null)
                return (false, "Secilen saatte musait personel bulunamamadi. Lutfen baska bir saat secin.", null);

            resolvedPersonnelId = freePersonnel.Id;
        }
        else
        {
            var isBookableStaff = await ApplyPublicBranchScope(_personnel.GetAllQueryable()
                    .Where(p => p.Id == resolvedPersonnelId
                             && p.CustomerId == cid
                             && p.IsActive
                             && p.CustomerRoleId != Shared.Enums.SalonRoles.Ids.SalonOwner),
                    scope.BranchId)
                .AnyAsync();
            if (!isBookableStaff)
                return (false, "Secilen personel bu subede musait degil.", null);

            var skillScope = await GetSkillScopeAsync(serviceIds);
            if (skillScope.HasSkillDefinitions && !skillScope.PersonnelIds.Contains(resolvedPersonnelId))
                return (false, "Secilen personel bu hizmetler icin uygun degil.", null);

            var hasOverlap = await _appointments.GetAllQueryable()
                .AnyAsync(a => a.PersonnelId == resolvedPersonnelId
                            && a.CustomerId == cid
                            && a.StatusId != 4
                            && a.StartTime < end
                            && a.EndTime > start);

            if (hasOverlap)
                return (false, "Secilen personel bu saatte baska bir randevuya sahip. Lutfen baska bir saat secin.", null);
        }

        var bookResourceConflict = await FindResourceConflictAsync(cid, bookBranch?.Id ?? scope.BranchId, serviceIds, start, end);
        if (bookResourceConflict != null)
            return (false, bookResourceConflict + ". Lutfen baska bir saat secin.", null);

        // ── ADIM 2: Politika + kart validasyonu ────────────────────────────
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

        // ── ADIM 3: Müşteri bul / oluştur ──────────────────────────────────
        var client = await FindOrCreatePublicClientAsync(cid, dto.FullName, dto.Phone, dto.Email, scope.BranchId);
        if (client.IsBlacklisted)
        {
            return (false, "Gecmis randevu ihlalleri nedeniyle online randevu olusturulamiyor. Lutfen salonu arayiniz.", null);
        }

        // ── ADIM 4: Depozito tahsil ─────────────────────────────────────────
        // Slot müsait olduğu doğrulandıktan SONRA kart çekilir.
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
                Installment = 1,
                BuyerEmail = dto.Email,
                BuyerPhone = dto.Phone,
                BuyerFullName = dto.FullName
            };
            var payRes = await _paymentService.ProcessAppointmentDepositAsync(cid, depositAmount, card, buyerIp);
            if (!payRes.Success)
                return (false, "Depozito tahsil edilemedi: " + (payRes.Error ?? "Bilinmeyen hata"), null);
            paymentTxUid = payRes.TransactionUid;
        }

        // ── ADIM 5: Randevu kaydet ──────────────────────────────────────────
        // Online odeme alindiysa otomatik onaylanir (StatusId=2 Onaylandi).
        // Odeme yoksa salon admin onayi bekler (StatusId=1 Planlanmis).
        var appointment = new SlnAppointment
        {
            CustomerId = cid,
            BranchId = scope.BranchId,
            SlnClientId = client.Id,
            PersonnelId = resolvedPersonnelId,
            ServiceId = primaryService.Id,
            ComboId = resolvedServices.Combo?.Id,
            StartTime = start,
            EndTime = end,
            StatusId = requireDeposit ? 2 : 1,
            Notes = dto.Notes,
            DepositAmount = depositAmount,
            IsPrepaid = requireDeposit,
            PrepaidAmount = requireDeposit ? depositAmount : 0
        };

        _appointments.Add(appointment);
        await _uow.SaveChangesAsync();
        var sortOrder = 0;
        foreach (var itemServiceId in serviceIds)
        {
            _appointmentServices.Add(new SlnAppointmentService
            {
                SlnAppointmentId = appointment.Id,
                SlnServiceId = itemServiceId,
                SortOrder = sortOrder++
            });
        }
        await _uow.SaveChangesAsync();
        await LinkPlatformUserToSalonAsync(cid, client, dto.Phone, dto.Email);

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

    public async Task<(bool Success, string? Error, object? Result)> BookCheckoutAsync(string slug, SlnOnlineBookingDto dto, string callbackUrl, string? buyerIp = null)
    {
        var scope = await ResolveSalonScopeAsync(slug);
        if (scope == null) return (false, "Salon bulunamadi", null);
        var cid = scope.CustomerId;

        var resolvedServices = await ResolvePublicBookingServicesAsync(cid, ResolveBookingDtoServiceIds(dto), dto.ComboId);
        if (resolvedServices.Error != null) return (false, resolvedServices.Error, null);
        var serviceIds = resolvedServices.Services.Select(s => s.Id).ToList();
        var primaryService = resolvedServices.Services.First();

        var start = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc);
        var end = start.AddMinutes(CalculateBookableMinutes(resolvedServices.Services));

        // Kapalı gün kontrolü
        var bookBranch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive)
            ?? await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.CustomerId == cid && b.IsHeadquarter);

        if (!string.IsNullOrEmpty(bookBranch?.WorkingHoursJson))
        {
            try
            {
                var bookDayKey = start.DayOfWeek switch
                {
                    DayOfWeek.Monday => "mon", DayOfWeek.Tuesday => "tue", DayOfWeek.Wednesday => "wed",
                    DayOfWeek.Thursday => "thu", DayOfWeek.Friday => "fri", DayOfWeek.Saturday => "sat",
                    _ => "sun"
                };
                var wh = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(bookBranch.WorkingHoursJson);
                if (wh != null && wh.TryGetValue(bookDayKey, out var whVal) && whVal == "closed")
                    return (false, "Salon bu gun kapalidir. Lutfen baska bir tarih secin.", null);
            }
            catch { }
        }

        // Personel belirle + overlap kontrolü
        var existingPolicy = await _noShowPolicies.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.CustomerId == cid);
        var existingDepositAmount = existingPolicy?.IsActive == true
                                   && existingPolicy.RequireDeposit
                                   && existingPolicy.DepositAmount > 0m
            ? existingPolicy.DepositAmount
            : 0m;

        await ExpireStalePendingAppointmentsAsync(cid, bookBranch?.Id ?? scope.BranchId);

        var existingClient = await FindPublicClientAsync(cid, dto.Phone, dto.Email);
        if (existingClient?.IsBlacklisted == true)
        {
            return (false, "Daha onceki randevu ihlalleri nedeniyle online randevu olusturulamiyor. Lutfen bu subeden randevu almak icin dogrudan salonla iletisime gecin.", null);
        }

        if (existingClient != null)
        {
            var reusableAppointment = await FindMatchingPendingPaymentAppointmentAsync(
                cid,
                existingClient.Id,
                bookBranch?.Id ?? scope.BranchId,
                start,
                end,
                resolvedServices.Combo?.Id,
                serviceIds);

            if (reusableAppointment != null)
            {
                var reusableDepositAmount = reusableAppointment.DepositAmount > 0m
                    ? reusableAppointment.DepositAmount
                    : existingDepositAmount;

                if (reusableDepositAmount <= 0m)
                    return (false, "Bekleyen odeme tutari bulunamadi. Lutfen randevuyu yeniden olusturun.", null);

                var checkoutRetry = await _paymentService.InitBookingDepositCheckoutAsync(
                    cid,
                    reusableAppointment.Id,
                    slug,
                    reusableDepositAmount,
                    dto.FullName,
                    dto.Email ?? "noreply@corplynk.com",
                    callbackUrl,
                    buyerIp);

                if (!checkoutRetry.Success)
                    return (false, "Odeme formu olusturulamadi: " + checkoutRetry.Error, null);

                return (true, null, new
                {
                    success = true,
                    requireDeposit = true,
                    appointmentId = reusableAppointment.Id,
                    depositAmount = reusableDepositAmount,
                    htmlContent = checkoutRetry.HtmlContent,
                    token = checkoutRetry.Token,
                    message = $"Bekleyen odemeniz bulundu. {reusableDepositAmount:N2} TL depozito icin lutfen odemenizi tamamlayin."
                });
            }

            if (await HasActivePendingPaymentAppointmentAsync(cid, existingClient.Id, bookBranch?.Id ?? scope.BranchId))
            {
                return (false, "Devam eden bir odeme sureciniz var. Panelim > Randevularim ekranindan odemenizi tamamlayin veya bekleme suresinin dolmasini bekleyin.", null);
            }
        }

        var resolvedPersonnelId = dto.PersonnelId ?? 0;
        if (resolvedPersonnelId == 0)
        {
            var skillScope = await GetSkillScopeAsync(serviceIds);

            var bookStaffQuery = _personnel.GetAllQueryable()
                .Where(p => p.CustomerId == cid && p.IsActive
                         && p.CustomerRoleId != Shared.Enums.SalonRoles.Ids.SalonOwner);
            bookStaffQuery = ApplyPublicBranchScope(bookStaffQuery, scope.BranchId);

            if (skillScope.HasSkillDefinitions)
                bookStaffQuery = bookStaffQuery.Where(p => skillScope.PersonnelIds.Contains(p.Id));

            var busyPersonnelIds = await _appointments.GetAllQueryable()
                .Where(a => a.CustomerId == cid
                         && a.StatusId != 4
                         && a.StartTime < end && a.EndTime > start)
                .Select(a => a.PersonnelId)
                .ToListAsync();

            var freePersonnel = await bookStaffQuery
                .Where(p => !busyPersonnelIds.Contains(p.Id))
                .FirstOrDefaultAsync();

            if (freePersonnel == null)
                return (false, "Secilen saatte musait personel bulunamamadi. Lutfen baska bir saat secin.", null);

            resolvedPersonnelId = freePersonnel.Id;
        }
        else
        {
            var isBookableStaff = await ApplyPublicBranchScope(_personnel.GetAllQueryable()
                    .Where(p => p.Id == resolvedPersonnelId
                             && p.CustomerId == cid
                             && p.IsActive
                             && p.CustomerRoleId != Shared.Enums.SalonRoles.Ids.SalonOwner),
                    scope.BranchId)
                .AnyAsync();
            if (!isBookableStaff)
                return (false, "Secilen personel bu subede musait degil.", null);

            var skillScope = await GetSkillScopeAsync(serviceIds);
            if (skillScope.HasSkillDefinitions && !skillScope.PersonnelIds.Contains(resolvedPersonnelId))
                return (false, "Secilen personel bu hizmetler icin uygun degil.", null);

            var hasOverlap = await _appointments.GetAllQueryable()
                .AnyAsync(a => a.PersonnelId == resolvedPersonnelId
                            && a.CustomerId == cid
                            && a.StatusId != 4
                            && a.StartTime < end
                            && a.EndTime > start);

            if (hasOverlap)
                return (false, "Secilen personel bu saatte baska bir randevuya sahip. Lutfen baska bir saat secin.", null);
        }

        // Depozito kontrolü — online randevu her zaman ödeme gerektirir
        var checkoutResourceConflict = await FindResourceConflictAsync(cid, bookBranch?.Id ?? scope.BranchId, serviceIds, start, end);
        if (checkoutResourceConflict != null)
            return (false, checkoutResourceConflict + ". Lutfen baska bir saat secin.", null);

        var policy = await _noShowPolicies.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.CustomerId == cid);
        var requireDeposit = policy?.IsActive == true && policy.RequireDeposit && policy.DepositAmount > 0m;
        var depositAmount = requireDeposit ? policy!.DepositAmount : 0m;

        // Müşteri bul / oluştur
        var client = existingClient ?? await FindOrCreatePublicClientAsync(cid, dto.FullName, dto.Phone, dto.Email, scope.BranchId);
        if (client.BranchId == null && scope.BranchId.HasValue)
        {
            client.BranchId = scope.BranchId;
            await _uow.SaveChangesAsync();
        }
        if (client.IsBlacklisted)
        {
            return (false, "Daha onceki randevu ihlalleri nedeniyle online randevu olusturulamiyor. Lutfen bu subeden randevu almak icin dogrudan salonla iletisime gecin.", null);
        }

        // Randevu oluştur (her zaman StatusId=6: AwaitingPayment — ödeme sonrası 2 olur)
        var appointment = new SlnAppointment
        {
            CustomerId = cid,
            BranchId = scope.BranchId,
            SlnClientId = client.Id,
            PersonnelId = resolvedPersonnelId,
            ServiceId = primaryService.Id,
            ComboId = resolvedServices.Combo?.Id,
            StartTime = start,
            EndTime = end,
            StatusId = requireDeposit ? 6 : 1,
            Notes = dto.Notes,
            DepositAmount = depositAmount
        };

        _appointments.Add(appointment);
        await _uow.SaveChangesAsync();
        var sortOrder = 0;
        foreach (var itemServiceId in serviceIds)
        {
            _appointmentServices.Add(new SlnAppointmentService
            {
                SlnAppointmentId = appointment.Id,
                SlnServiceId = itemServiceId,
                SortOrder = sortOrder++
            });
        }
        await _uow.SaveChangesAsync();

        if (!requireDeposit)
        {
            await LinkPlatformUserToSalonAsync(cid, client, dto.Phone, dto.Email);
            return (true, null, new
            {
                success = true,
                requireDeposit = false,
                appointmentId = appointment.Id,
                depositAmount = 0m,
                message = "Randevunuz olusturuldu. Salon tarafindan onaylanacaktir."
            });
        }

        // Depozito gerekli → Iyzico checkout form başlat
        var checkoutResult = await _paymentService.InitBookingDepositCheckoutAsync(
            cid, appointment.Id, slug, depositAmount,
            dto.FullName, dto.Email ?? "noreply@corplynk.com", callbackUrl, buyerIp);

        if (!checkoutResult.Success)
        {
            appointment.StatusId = 4; // Cancelled
            await _uow.SaveChangesAsync();
            return (false, "Odeme formu olusturulamadi: " + checkoutResult.Error, null);
        }

        return (true, null, new
        {
            success = true,
            requireDeposit = true,
            appointmentId = appointment.Id,
            depositAmount,
            htmlContent = checkoutResult.HtmlContent,
            token = checkoutResult.Token,
            message = $"Randevunuz olusturuldu. {depositAmount:N2} TL depozito icin lutfen odeme formunu doldurun."
        });
    }

    public async Task<(bool Success, string? Error, object? Result)> JoinWaitlistAsync(string slug, SlnPublicWaitlistDto dto)
    {
        var customerId = await ResolveCustomerIdAsync(slug);
        if (customerId == null) return (false, "Salon bulunamadi", null);
        var cid = customerId.Value;

        // Slug branch slug ise o subenin id'si, parent profile slug ise merkez sube
        var branch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.CustomerId == cid && b.Slug == slug && b.IsActive);
        var branchId = branch?.Id ?? (await _branches.GetAllQueryable()
            .Where(b => b.CustomerId == cid && b.IsHeadquarter)
            .Select(b => (int?)b.Id).FirstOrDefaultAsync());

        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
            return (false, "Ad ve telefon zorunlu", null);

        var service = await _services.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId && s.CustomerId == cid && s.IsActive);
        if (service == null) return (false, "Hizmet bulunamadi", null);

        // Telefonla mevcut musteri var mi? Yoksa hizli olustur (lead).
        var client = await FindOrCreatePublicClientAsync(cid, dto.FullName, dto.Phone, dto.Email, branchId);

        // Ayni telefon + tarih + hizmet icin aktif/aksiyon bekleyen waitlist varsa cogaltma.
        var preferredDate = DateTime.SpecifyKind(dto.PreferredDate.Date, DateTimeKind.Utc);
        var activeWaitlistStatusIds = new[]
        {
            SlnWaitlistStatuses.Ids.Waiting,
            SlnWaitlistStatuses.Ids.Notified,
            SlnWaitlistStatuses.Ids.AppointmentBooked
        };
        var existing = await _waitlist.GetAllQueryable().AnyAsync(w =>
            w.CustomerId == cid &&
            w.SlnClientId == client.Id &&
            w.ServiceId == dto.ServiceId &&
            w.PreferredDate == preferredDate &&
            activeWaitlistStatusIds.Contains(w.StatusId));
        if (existing)
            return (true, null, new { duplicate = true, message = "Bu tarih icin zaten bekleme listesindesiniz." });

        var entry = new SlnWaitlistEntry
        {
            CustomerId = cid,
            BranchId = branchId,
            SlnClientId = client.Id,
            ServiceId = dto.ServiceId,
            PreferredPersonnelId = dto.PersonnelId,
            PreferredDate = preferredDate,
            PreferredTimeSlot = string.IsNullOrWhiteSpace(dto.PreferredTimeSlot) ? "Farketmez" : dto.PreferredTimeSlot,
            Notes = dto.Notes,
            StatusId = SlnWaitlistStatuses.Ids.Waiting
        };
        _waitlist.Add(entry);
        await _uow.SaveChangesAsync();
        await LinkPlatformUserToSalonAsync(cid, client, dto.Phone, dto.Email);

        return (true, null, new
        {
            id = entry.Id,
            message = "Bekleme listesine eklendiniz. Slot bosaldiginda salon sizinle iletisime gececek."
        });
    }
}
