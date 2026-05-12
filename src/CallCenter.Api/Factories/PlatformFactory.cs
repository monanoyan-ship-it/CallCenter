using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Helpers;
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
    private readonly ISlnBranchEntityService _branchEs;
    private readonly ISlnClientMembershipEntityService _membershipEs;
    private readonly ISlnClientLoyaltyEntityService _loyaltyEs;
    private readonly ISlnGiftCardEntityService _giftCardEs;
    private readonly ISlnNoShowPolicyEntityService _noShowPolicyEs;
    private readonly PaymentService _paymentService;
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
        ISlnBranchEntityService branchEs,
        ISlnClientMembershipEntityService membershipEs,
        ISlnClientLoyaltyEntityService loyaltyEs,
        ISlnGiftCardEntityService giftCardEs,
        ISlnNoShowPolicyEntityService noShowPolicyEs,
        PaymentService paymentService,
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
        _branchEs = branchEs;
        _membershipEs = membershipEs;
        _loyaltyEs = loyaltyEs;
        _giftCardEs = giftCardEs;
        _noShowPolicyEs = noShowPolicyEs;
        _paymentService = paymentService;
        _uow = uow;
    }

    // ═══ SALON ÜYELİK ═══

    public async Task<List<PlatformSalonDto>> GetMySalonsAsync(int platformUserId)
    {
        var links = await _userSalonEs.GetAllQueryable()
            .Where(s => s.PlatformUserId == platformUserId && s.IsActive)
            .Include(s => s.Customer)
            .ToListAsync();

        var linkCustomerIds = links.Select(s => s.CustomerId).Distinct().ToHashSet();
        var derivedCustomerIds = new HashSet<int>();
        var derivedJoinedAt = new Dictionary<int, DateTime>();

        var phone = await _platformUserEs.GetAllQueryable()
            .Where(u => u.Id == platformUserId)
            .Select(u => u.Phone)
            .FirstOrDefaultAsync();

        var phoneVariants = PhoneHelper.GetLookupVariants(phone);
        if (phoneVariants.Count > 0)
        {
            var matchingClients = await _clientEs.GetAllQueryable()
                .Where(c => c.Phone != null && phoneVariants.Contains(c.Phone) && c.IsActive)
                .Select(c => new { c.Id, c.CustomerId, c.CreatedAt })
                .ToListAsync();

            var clientIds = matchingClients.Select(c => c.Id).ToList();
            var appointmentCustomerIds = clientIds.Count == 0
                ? new HashSet<int>()
                : (await _appointmentEs.GetAllQueryable()
                    .Where(a => clientIds.Contains(a.SlnClientId))
                    .Select(a => a.CustomerId)
                    .Distinct()
                    .ToListAsync()).ToHashSet();

            foreach (var client in matchingClients.Where(c => appointmentCustomerIds.Contains(c.CustomerId)))
            {
                if (linkCustomerIds.Contains(client.CustomerId)) continue;
                derivedCustomerIds.Add(client.CustomerId);
                if (!derivedJoinedAt.TryGetValue(client.CustomerId, out var current) || client.CreatedAt < current)
                    derivedJoinedAt[client.CustomerId] = client.CreatedAt;
            }
        }

        var allCustomerIds = linkCustomerIds.Concat(derivedCustomerIds).Distinct().ToList();
        if (allCustomerIds.Count == 0)
            return new List<PlatformSalonDto>();

        var customers = await _customerEs.GetAllQueryable()
            .Where(c => allCustomerIds.Contains(c.Id) && c.IsActive)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        var customerMap = customers.ToDictionary(c => c.Id);

        var profiles = await _profileEs.GetAllQueryable()
            .Where(p => allCustomerIds.Contains(p.CustomerId))
            .Select(p => new { p.CustomerId, p.Slug, p.LogoUrl, p.City, p.District })
            .ToListAsync();
        var profileMap = profiles
            .GroupBy(p => p.CustomerId)
            .ToDictionary(g => g.Key, g => g.First());

        var branches = await _branchEs.GetAllQueryable()
            .Where(b => allCustomerIds.Contains(b.CustomerId) && b.IsHeadquarter && b.IsActive)
            .Select(b => new { b.CustomerId, b.Slug, b.City, b.District })
            .ToListAsync();
        var branchMap = branches
            .GroupBy(b => b.CustomerId)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<PlatformSalonDto>();
        foreach (var link in links)
        {
            if (!customerMap.TryGetValue(link.CustomerId, out var customer)) continue;
            profileMap.TryGetValue(link.CustomerId, out var profile);
            branchMap.TryGetValue(link.CustomerId, out var branch);
            result.Add(new PlatformSalonDto
            {
                Id = link.Id,
                CustomerId = link.CustomerId,
                Slug = branch?.Slug ?? profile?.Slug,
                SalonName = customer.Name,
                LogoUrl = profile?.LogoUrl,
                City = branch?.City ?? profile?.City,
                District = branch?.District ?? profile?.District,
                IsFavorite = link.IsFavorite,
                JoinedAt = link.JoinedAt
            });
        }

        foreach (var customerId in derivedCustomerIds)
        {
            if (!customerMap.TryGetValue(customerId, out var customer)) continue;
            profileMap.TryGetValue(customerId, out var profile);
            branchMap.TryGetValue(customerId, out var branch);
            result.Add(new PlatformSalonDto
            {
                Id = 0,
                CustomerId = customerId,
                Slug = branch?.Slug ?? profile?.Slug,
                SalonName = customer.Name,
                LogoUrl = profile?.LogoUrl,
                City = branch?.City ?? profile?.City,
                District = branch?.District ?? profile?.District,
                IsFavorite = false,
                JoinedAt = derivedJoinedAt.GetValueOrDefault(customerId, DateTime.UtcNow)
            });
        }

        return result
            .OrderByDescending(s => s.IsFavorite)
            .ThenBy(s => s.SalonName)
            .ToList();
    }

    public async Task<(bool Success, string? Error)> JoinSalonAsync(int platformUserId, int customerId)
    {
        // Salon var mi?
        var salon = await _customerEs.GetByIdAsync(customerId);
        if (salon == null || !salon.IsActive)
            return (false, "Salon bulunamadı.");

        // Platform user bilgilerini al
        var platformUser = await _platformUserEs.GetByIdAsync(platformUserId);
        if (platformUser == null) return (false, null);

        var phoneVariants = PhoneHelper.GetLookupVariants(platformUser.Phone);
        var normalizedPhone = PhoneHelper.Normalize(platformUser.Phone) ?? platformUser.Phone;

        var slnClient = phoneVariants.Count == 0
            ? null
            : await _clientEs.GetAllQueryable()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Phone != null && phoneVariants.Contains(c.Phone));
        if (slnClient == null)
        {
            slnClient = new SlnClient
            {
                CustomerId = customerId,
                FullName = platformUser.FullName,
                Phone = normalizedPhone,
                Email = platformUser.Email
            };
            _clientEs.Add(slnClient);
            await _uow.SaveChangesAsync();
        }
        else
        {
            slnClient.IsActive = true;
            if (string.IsNullOrWhiteSpace(slnClient.FullName)) slnClient.FullName = platformUser.FullName;
            if (string.IsNullOrWhiteSpace(slnClient.Email)) slnClient.Email = platformUser.Email;
            if (!string.IsNullOrWhiteSpace(normalizedPhone) && slnClient.Phone != normalizedPhone) slnClient.Phone = normalizedPhone;
            slnClient.UpdatedAt = DateTime.UtcNow;
        }

        // Zaten uye mi?
        var existing = await _userSalonEs.GetAllQueryable()
            .FirstOrDefaultAsync(s => s.PlatformUserId == platformUserId && s.CustomerId == customerId);
        if (existing != null)
        {
            if (existing.IsActive)
            {
                existing.SlnClientId ??= slnClient.Id;
                await _uow.SaveChangesAsync();
                return (true, null);
            }

            existing.IsActive = true;
            existing.JoinedAt = DateTime.UtcNow;
            existing.SlnClientId ??= slnClient.Id;
            await _uow.SaveChangesAsync();
            return (true, null);
        }

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
        await ExpireStalePendingAppointmentsAsync(clientIds);

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

        var legacyServiceIds = appointments
            .Where(a => a.ServiceId.HasValue && (a.Services == null || a.Services.Count == 0))
            .Select(a => a.ServiceId!.Value)
            .Distinct()
            .ToList();
        var legacyServices = legacyServiceIds.Count == 0
            ? new Dictionary<int, SlnService>()
            : await _serviceEs.GetAllQueryable()
                .Where(s => legacyServiceIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

        // Phase 9 — CanPay + RemainingAmount: salonun sub-merchant durumu + bu randevu icin yapilmis post-pay toplami.
        var subMerchantCustomerIds = await _paymentService.GetCustomersWithActiveSubMerchantAsync(customerIds);
        var paidByApt = await _paymentService.GetAppointmentPaidAmountsAsync(appointments.Select(a => a.Id));

        return appointments.Select(a =>
        {
            var serviceNames = a.Services?
                .Select(s => s.SlnService?.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .ToList() ?? new List<string>();
            var totalPrice = a.Services?.Sum(s => s.SlnService?.Price ?? 0) ?? 0;

            if (serviceNames.Count == 0
                && a.ServiceId.HasValue
                && legacyServices.TryGetValue(a.ServiceId.Value, out var legacyService))
            {
                serviceNames.Add(legacyService.Name);
                totalPrice = legacyService.Price;
            }

            var awaitingDepositPayment = a.StatusId == 6 && a.DepositAmount > 0m && !a.IsPrepaid;
            var alreadyPaid = paidByApt.GetValueOrDefault(a.Id, 0m);
            var remaining = awaitingDepositPayment ? a.DepositAmount : totalPrice - alreadyPaid;
            if (remaining < 0m) remaining = 0m;

            // StatusId=6 randevu depozitosu yarim kalmis checkout'tur; retry platform odeme
            // akisini kullanir ve salon sub-merchant kaydina bagli degildir.
            var canPay = awaitingDepositPayment
                         || (remaining > 0m
                             && totalPrice > 0m
                             && a.StatusId != 4 && a.StatusId != 5 && a.StatusId != 6
                             && subMerchantCustomerIds.Contains(a.CustomerId));

            return new PlatformAppointmentDto
            {
                Id = a.Id,
                SalonName = salonNames.GetValueOrDefault(a.CustomerId, "-"),
                SalonLogoUrl = salonLogos.GetValueOrDefault(a.CustomerId),
                AppointmentDate = a.StartTime.Date,
                StartTime = a.StartTime.TimeOfDay,
                EndTime = a.EndTime.TimeOfDay,
                PersonnelName = a.Personnel?.Title,
                ServiceNames = serviceNames,
                TotalPrice = totalPrice,
                StatusId = a.StatusId,
                StatusName = GetAppointmentStatusName(a.StatusId),
                IsPrepaid = a.IsPrepaid,
                PrepaidAmount = a.PrepaidAmount,
                CanPay = canPay,
                RemainingAmount = remaining
            };
        }).ToList();
    }

    public async Task<PlatformPayAppointmentResponse> PayAppointmentCheckoutAsync(
        int platformUserId, int appointmentId, string? callbackUrl, string? buyerIp)
    {
        var clientIds = await GetMyClientIds(platformUserId);

        var apt = await _appointmentEs.GetAllQueryable()
            .Include(a => a.Services).ThenInclude(s => s.SlnService)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && clientIds.Contains(a.SlnClientId));
        if (apt == null)
            return new PlatformPayAppointmentResponse { Success = false, Error = "Randevu bulunamadi." };

        if (apt.StatusId == 4 || apt.StatusId == 5)
            return new PlatformPayAppointmentResponse { Success = false, Error = "Iptal veya gelinmedi durumundaki randevu icin tahsilat yapilamaz." };

        if (apt.StatusId == 6
            && !apt.IsPrepaid
            && apt.CreatedAt <= DateTime.UtcNow - PaymentService.PendingPaymentHoldTimeout)
        {
            apt.StatusId = 4;
            apt.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return new PlatformPayAppointmentResponse { Success = false, Error = "Odeme suresi doldu. Lutfen randevunuzu yeniden olusturun." };
        }

        var platformUser = await _platformUserEs.GetAllQueryable()
            .FirstOrDefaultAsync(u => u.Id == platformUserId);
        if (platformUser == null)
            return new PlatformPayAppointmentResponse { Success = false, Error = "Platform kullanicisi bulunamadi." };

        var fallbackCallback = string.IsNullOrWhiteSpace(callbackUrl)
            ? "corplynk-salon://payment/callback"
            : callbackUrl!;

        if (apt.StatusId == 6 && apt.DepositAmount > 0m && !apt.IsPrepaid)
        {
            var slug = await GetAppointmentPublicSlugAsync(apt);
            var depositCheckout = await _paymentService.InitBookingDepositCheckoutAsync(
                customerId: apt.CustomerId,
                appointmentId: apt.Id,
                slug: slug,
                amount: apt.DepositAmount,
                buyerFullName: platformUser.FullName ?? "Musteri",
                buyerEmail: platformUser.Email ?? "noreply@corplynk.com",
                callbackUrl: fallbackCallback,
                buyerIp: buyerIp);

            return new PlatformPayAppointmentResponse
            {
                Success = depositCheckout.Success,
                HtmlContent = depositCheckout.HtmlContent,
                Token = depositCheckout.Token,
                Error = depositCheckout.Error
            };
        }

        var totalPrice = apt.Services?.Sum(s => s.SlnService?.Price ?? 0) ?? 0;
        if (totalPrice <= 0m && apt.ServiceId.HasValue)
        {
            var legacy = await _serviceEs.GetAllQueryable()
                .FirstOrDefaultAsync(s => s.Id == apt.ServiceId.Value);
            if (legacy != null) totalPrice = legacy.Price;
        }
        if (totalPrice <= 0m)
            return new PlatformPayAppointmentResponse { Success = false, Error = "Bu randevuda tutar tanimli degil." };

        var alreadyPaid = await _paymentService.GetAppointmentPaidAmountAsync(apt.Id);
        var remaining = totalPrice - alreadyPaid;
        if (remaining <= 0m)
            return new PlatformPayAppointmentResponse { Success = false, Error = "Bu randevu icin odenecek tutar kalmadi." };

        var result = await _paymentService.InitPayAppointmentCheckoutAsync(
            customerId: apt.CustomerId,
            appointmentId: apt.Id,
            amount: remaining,
            platformUserId: platformUserId,
            buyerFullName: platformUser.FullName ?? "Musteri",
            buyerEmail: platformUser.Email ?? "noreply@corplynk.com",
            callbackUrl: fallbackCallback,
            buyerIp: buyerIp);

        return new PlatformPayAppointmentResponse
        {
            Success = result.Success,
            HtmlContent = result.HtmlContent,
            Token = result.Token,
            Error = result.Error
        };
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

    public async Task<(bool Success, string? Error, string? Message)> CancelAppointmentAsync(int platformUserId, int appointmentId)
    {
        var clientIds = await GetMyClientIds(platformUserId);

        var appointment = await _appointmentEs.GetAllQueryable()
            .FirstOrDefaultAsync(a => a.Id == appointmentId && clientIds.Contains(a.SlnClientId));
        if (appointment == null) return (false, "Randevu bulunamadı.", null);

        if (appointment.StatusId == 3 || appointment.StatusId == 4)
            return (false, "Bu randevu zaten tamamlanmış veya iptal edilmiş.", null);

        // NoShow politikasını yükle
        var policy = await _noShowPolicyEs.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.CustomerId == appointment.CustomerId);

        var now = DateTime.UtcNow;
        var hoursUntilAppt = (appointment.StartTime - now).TotalHours;
        var freeCancelHours = policy?.FreeCancellationHours ?? 24;
        var withinFreeWindow = hoursUntilAppt >= freeCancelHours;

        appointment.StatusId = 4; // Cancelled
        string resultMessage;

        if (appointment.IsPrepaid && appointment.PrepaidAmount > 0)
        {
            if (withinFreeWindow)
            {
                // Ücretsiz iptal — depozito iade edilir
                var (refundOk, refundErr) = await _paymentService.RefundAppointmentDepositAsync(appointmentId);
                if (refundOk)
                {
                    appointment.DepositRefunded = true;
                    resultMessage = $"Randevunuz iptal edildi. {appointment.PrepaidAmount:N2} TL depozitonuz iade edilecektir.";
                }
                else
                {
                    resultMessage = $"Randevunuz iptal edildi ancak depozito iadesi yapılamadı: {refundErr}. Lütfen salonla iletişime geçin.";
                }
            }
            else
            {
                // Geç iptal — depozito iade edilmez (politikaya göre kesinti)
                var lateFee = policy?.LateCancellationFee > 0 ? policy.LateCancellationFee : appointment.PrepaidAmount;
                appointment.PenaltyAmount = lateFee;
                resultMessage = $"Randevunuz iptal edildi. Ücretsiz iptal süresi ({freeCancelHours} saat) geçtiğinden {lateFee:N2} TL kesinti uygulandı. Depozitonuz iade edilmeyecektir.";
            }
        }
        else
        {
            resultMessage = "Randevunuz iptal edildi.";
        }

        await _uow.SaveChangesAsync();
        return (true, null, resultMessage);
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
            var phoneVariants = PhoneHelper.GetLookupVariants(phone);

            var giftCards = await _giftCardEs.GetAllQueryable()
                .Where(g => g.CustomerId == link.CustomerId && g.IsActive && g.RemainingBalance > 0
                    && g.RecipientPhone != null && phoneVariants.Contains(g.RecipientPhone))
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

    private static string GetAppointmentStatusName(int statusId) => statusId switch
    {
        1 => "Planlandı",
        2 => "Onaylandı",
        3 => "Tamamlandı",
        4 => "İptal",
        5 => "Gelmedi",
        6 => "Ödeme bekliyor",
        _ => "Bilinmiyor"
    };

    private async Task<string> GetAppointmentPublicSlugAsync(SlnAppointment appointment)
    {
        if (appointment.BranchId.HasValue)
        {
            var branchSlug = await _branchEs.GetAllQueryable()
                .Where(b => b.Id == appointment.BranchId.Value && b.Slug != null && b.Slug != "")
                .Select(b => b.Slug)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(branchSlug))
                return branchSlug!;
        }

        var headquarterSlug = await _branchEs.GetAllQueryable()
            .Where(b => b.CustomerId == appointment.CustomerId
                        && b.IsHeadquarter
                        && b.Slug != null
                        && b.Slug != "")
            .Select(b => b.Slug)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(headquarterSlug))
            return headquarterSlug!;

        return await _profileEs.GetAllQueryable()
            .Where(p => p.CustomerId == appointment.CustomerId)
            .Select(p => p.Slug)
            .FirstOrDefaultAsync()
            ?? string.Empty;
    }

    private async Task ExpireStalePendingAppointmentsAsync(IReadOnlyCollection<int> clientIds)
    {
        if (clientIds.Count == 0) return;

        var cutoff = DateTime.UtcNow - PaymentService.PendingPaymentHoldTimeout;
        var staleAppointments = await _appointmentEs.GetAllQueryable()
            .Where(a => clientIds.Contains(a.SlnClientId)
                     && a.StatusId == 6
                     && !a.IsPrepaid
                     && a.CreatedAt <= cutoff)
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

        var phoneVariants = PhoneHelper.GetLookupVariants(phone);
        if (phoneVariants.Count > 0)
        {
            var byPhone = await _clientEs.GetAllQueryable()
                .Where(c => c.Phone != null && phoneVariants.Contains(c.Phone))
                .Select(c => c.Id)
                .ToListAsync();
            linked.AddRange(byPhone);
        }

        return linked.Distinct().ToList();
    }
}
