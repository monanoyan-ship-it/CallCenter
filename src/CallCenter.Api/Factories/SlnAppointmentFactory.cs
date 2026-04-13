using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnAppointmentFactory : ISlnAppointmentFactory
{
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnServiceEntityService _services;
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnNoShowPolicyEntityService _noShowPolicies;
    private readonly ISlnPersonnelSkillEntityService _skills;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly ISlnBranchEntityService _branches;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnAppointmentFactory> _logger;

    public SlnAppointmentFactory(
        ISlnAppointmentEntityService appointments,
        ISlnServiceEntityService services,
        ISlnClientEntityService clients,
        ISlnNoShowPolicyEntityService noShowPolicies,
        ISlnPersonnelSkillEntityService skills,
        ICustomerPersonnelEntityService personnel,
        ISlnBranchEntityService branches,
        IUnitOfWork uow,
        ILogger<SlnAppointmentFactory> logger)
    {
        _appointments = appointments;
        _services = services;
        _clients = clients;
        _noShowPolicies = noShowPolicies;
        _skills = skills;
        _personnel = personnel;
        _branches = branches;
        _uow = uow;
        _logger = logger;
    }

    private IQueryable<SlnAppointment> IncludeAll(IQueryable<SlnAppointment> q) => q
        .Include(a => a.SlnClient)
        .Include(a => a.Personnel).ThenInclude(p => p!.User)
        .Include(a => a.Service)
        .Include(a => a.Services).ThenInclude(s => s.SlnService);

    public async Task<List<SlnAppointmentDto>> GetAppointmentsAsync(int customerId, DateTime? from, DateTime? to, int? personnelId = null, int? statusId = null, int? branchId = null)
    {
        var query = _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId);

        if (branchId.HasValue)
            query = query.Where(a => a.BranchId == branchId.Value);

        if (from.HasValue)
            query = query.Where(a => a.StartTime >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.StartTime <= to.Value);

        if (personnelId.HasValue)
            query = query.Where(a => a.PersonnelId == personnelId.Value);

        if (statusId.HasValue)
            query = query.Where(a => a.StatusId == statusId.Value);

        var appointments = await IncludeAll(query)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<SlnAppointmentDto?> GetAppointmentAsync(int appointmentId, int customerId)
    {
        var appointment = await IncludeAll(_appointments.GetAllQueryable())
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CustomerId == customerId);

        return appointment != null ? MapToDto(appointment) : null;
    }

    public async Task<(SlnAppointmentDto? Appointment, string? Error)> CreateAppointmentAsync(SlnAppointmentCreateDto dto, int userId, int customerId, int? branchId = null)
    {
        if (dto.ServiceIds.Count == 0)
            return (null, "En az bir hizmet secilmeli");

        var services = await _services.GetAllQueryable()
            .Where(s => dto.ServiceIds.Contains(s.Id))
            .ToListAsync();

        if (services.Count != dto.ServiceIds.Count)
            return (null, "Bir veya daha fazla hizmet bulunamadi");

        var totalMinutes = services.Sum(s => s.DurationMinutes);
        var endTime = dto.StartTime.AddMinutes(totalMinutes);

        // Engelli musteri kontrolu
        if (dto.SlnClientId > 0)
        {
            var client = await _clients.GetByIdAsync(dto.SlnClientId);
            if (client?.IsBlacklisted == true)
                return (null, $"Bu musteri engellenmis ({client.NoShowCount} kez gelmedi). Engeli kaldirmak icin musteri kartini kullanin.");
        }

        var hasConflict = await CheckConflictAsync(dto.PersonnelId, dto.StartTime, endTime, customerId);
        if (hasConflict)
            return (null, "Secilen saatte personelin baska bir randevusu var");

        var appointment = new SlnAppointment
        {
            CustomerId = customerId,
            BranchId = branchId,
            SlnClientId = dto.SlnClientId,
            PersonnelId = dto.PersonnelId,
            StartTime = dto.StartTime,
            EndTime = endTime,
            Notes = dto.Notes,
            CreatedByPersonnelId = userId,
            Services = dto.ServiceIds.Select((id, i) => new SlnAppointmentService
            {
                SlnServiceId = id,
                SortOrder = i
            }).ToList()
        };

        _appointments.Add(appointment);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni randevu olusturuldu: {AppointmentId} - {StartTime} ({ServiceCount} hizmet)",
            appointment.Id, appointment.StartTime, dto.ServiceIds.Count);

        var created = await IncludeAll(_appointments.GetAllQueryable())
            .FirstAsync(a => a.Id == appointment.Id);

        return (MapToDto(created), null);
    }

    public async Task<(bool Success, string? Error)> UpdateAppointmentAsync(int appointmentId, SlnAppointmentCreateDto dto, int customerId)
    {
        var appointment = await _appointments.GetAllQueryable()
            .Include(a => a.Services)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CustomerId == customerId);

        if (appointment == null) return (false, "Randevu bulunamadi");
        if (appointment.StatusId == 4) return (false, "Iptal edilmis randevu guncellenemez");

        if (dto.ServiceIds.Count == 0)
            return (false, "En az bir hizmet secilmeli");

        var services = await _services.GetAllQueryable()
            .Where(s => dto.ServiceIds.Contains(s.Id))
            .ToListAsync();

        if (services.Count != dto.ServiceIds.Count)
            return (false, "Bir veya daha fazla hizmet bulunamadi");

        var totalMinutes = services.Sum(s => s.DurationMinutes);
        var endTime = dto.StartTime.AddMinutes(totalMinutes);

        var hasConflict = await CheckConflictAsync(dto.PersonnelId, dto.StartTime, endTime, customerId, appointmentId);
        if (hasConflict) return (false, "Secilen saatte personelin baska bir randevusu var");

        appointment.SlnClientId = dto.SlnClientId;
        appointment.PersonnelId = dto.PersonnelId;
        appointment.ServiceId = null;
        appointment.StartTime = dto.StartTime;
        appointment.EndTime = endTime;
        appointment.Notes = dto.Notes;
        appointment.UpdatedAt = DateTime.UtcNow;

        // Eski hizmetleri temizle, yenilerini ekle
        appointment.Services.Clear();
        foreach (var (id, i) in dto.ServiceIds.Select((id, i) => (id, i)))
            appointment.Services.Add(new SlnAppointmentService { SlnServiceId = id, SortOrder = i });

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error, decimal Penalty)> UpdateStatusAsync(int appointmentId, int statusId, int customerId)
    {
        var appointment = await _appointments.GetAllQueryable()
            .Include(a => a.SlnClient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CustomerId == customerId);

        if (appointment == null) return (false, "Randevu bulunamadi", 0);

        var policy = await _noShowPolicies.GetAllQueryable()
            .FirstOrDefaultAsync(p => p.CustomerId == customerId && p.IsActive);

        decimal penalty = 0;

        // ═══ GELMEDİ (StatusId=5) ═══
        if (statusId == 5 && appointment.SlnClient != null)
        {
            appointment.SlnClient.NoShowCount++;

            if (policy != null)
            {
                // Ceza hesapla
                penalty = policy.NoShowFee > 0 ? policy.NoShowFee : appointment.DepositAmount;
                appointment.PenaltyAmount = penalty;

                // Engelleme esigi kontrolu
                if (appointment.SlnClient.NoShowCount >= policy.BlacklistThreshold)
                {
                    appointment.SlnClient.IsBlacklisted = true;
                    _logger.LogWarning("Musteri engellendi: ClientId={ClientId}, NoShowCount={Count}",
                        appointment.SlnClientId, appointment.SlnClient.NoShowCount);
                }
            }

            _logger.LogInformation("Randevu gelmedi: AppointmentId={Id}, ClientId={ClientId}, NoShowCount={Count}",
                appointmentId, appointment.SlnClientId, appointment.SlnClient.NoShowCount);
        }

        // ═══ İPTAL (StatusId=4) ═══
        if (statusId == 4 && policy != null)
        {
            var hoursUntilAppointment = (appointment.StartTime - DateTime.UtcNow).TotalHours;

            if (hoursUntilAppointment < policy.FreeCancellationHours)
            {
                // Gec iptal — ceza uygula
                penalty = policy.LateCancellationFee > 0 ? policy.LateCancellationFee : appointment.DepositAmount;
                appointment.PenaltyAmount = penalty;
                appointment.DepositRefunded = false;
            }
            else
            {
                // Ucretsiz iptal — depozito iade
                appointment.PenaltyAmount = 0;
                if (appointment.DepositAmount > 0)
                    appointment.DepositRefunded = true;
            }
        }

        appointment.StatusId = statusId;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null, penalty);
    }

    public async Task<(bool Success, string? Error)> DeleteAppointmentAsync(int appointmentId, int customerId)
    {
        var appointment = await _appointments.GetAllQueryable()
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CustomerId == customerId);

        if (appointment == null) return (false, "Randevu bulunamadi");

        _appointments.Remove(appointment);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> CheckConflictAsync(int personnelId, DateTime startTime, DateTime endTime, int customerId, int? excludeAppointmentId = null)
    {
        var query = _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId
                && a.PersonnelId == personnelId
                && a.StatusId != 4 // Iptal edilmis randevular haric
                && a.StatusId != 5 // NoShow haric
                && a.StartTime < endTime
                && a.EndTime > startTime);

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        return await query.AnyAsync();
    }

    private static SlnAppointmentDto MapToDto(SlnAppointment a)
    {
        // Yeni kayitlar Services koleksiyonunu kullanir, eski kayitlar ServiceId FK'yi
        var serviceIds = a.Services.Count > 0
            ? a.Services.OrderBy(s => s.SortOrder).Select(s => s.SlnServiceId).ToList()
            : a.ServiceId.HasValue ? new List<int> { a.ServiceId.Value } : new List<int>();

        var serviceNames = a.Services.Count > 0
            ? a.Services.OrderBy(s => s.SortOrder).Select(s => s.SlnService?.Name ?? "").ToList()
            : a.Service != null ? new List<string> { a.Service.Name } : new List<string>();

        var duration = (int)(a.EndTime - a.StartTime).TotalMinutes;

        return new SlnAppointmentDto
        {
            Id = a.Id,
            SlnClientId = a.SlnClientId,
            ClientName = a.SlnClient?.FullName ?? "",
            ClientPhone = a.SlnClient?.Phone,
            PersonnelId = a.PersonnelId,
            PersonnelName = a.Personnel?.User?.FullName ?? "",
            ServiceIds = serviceIds,
            ServiceNames = serviceNames,
            DurationMinutes = duration,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            StatusId = a.StatusId,
            Notes = a.Notes
        };
    }

    public async Task<List<object>> GetAvailableStaffAsync(int customerId, List<int> serviceIds, int? branchId = null)
    {
        // Skill eslemesi olan personelleri bul
        var skillQuery = _skills.GetAllQueryable()
            .Where(s => serviceIds.Contains(s.ServiceId));

        var skilledPersonnelIds = await skillQuery
            .Select(s => s.PersonnelId)
            .Distinct()
            .ToListAsync();

        var personnelQuery = _personnel.GetAllQueryable()
            .Where(p => p.CustomerId == customerId && p.IsActive);

        if (branchId.HasValue)
            personnelQuery = personnelQuery.Where(p => p.BranchId == branchId.Value || p.BranchId == null);

        // Skill tanimlanmissa filtrele, tanimlanmamissa tum aktif personelleri don
        if (skilledPersonnelIds.Count > 0)
            personnelQuery = personnelQuery.Where(p => skilledPersonnelIds.Contains(p.Id));

        return await personnelQuery
            .Include(p => p.User)
            .OrderBy(p => p.User.FullName)
            .Select(p => (object)new
            {
                p.Id,
                Name = p.User.FullName,
                p.Title,
                p.PhotoUrl,
                p.Specialty,
                p.BranchId
            })
            .ToListAsync();
    }

    public async Task<List<object>> GetAvailableSlotsAsync(int customerId, int personnelId, DateTime date, int durationMinutes, int? branchId = null)
    {
        // Subenin calisma saatlerini al
        var branch = branchId.HasValue
            ? await _branches.GetAllQueryable().FirstOrDefaultAsync(b => b.Id == branchId.Value)
            : await _branches.GetAllQueryable().FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);

        var dayKey = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "mon", DayOfWeek.Tuesday => "tue", DayOfWeek.Wednesday => "wed",
            DayOfWeek.Thursday => "thu", DayOfWeek.Friday => "fri", DayOfWeek.Saturday => "sat",
            _ => "sun"
        };

        var openHour = 9; var openMin = 0;
        var closeHour = 19; var closeMin = 0;
        if (branch?.WorkingHoursJson != null)
        {
            try
            {
                var hours = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(branch.WorkingHoursJson);
                if (hours != null && hours.TryGetValue(dayKey, out var val))
                {
                    if (val == "closed") return new List<object>();
                    var parts = val.Split('-');
                    if (parts.Length == 2)
                    {
                        var openParts = parts[0].Split(':');
                        var closeParts = parts[1].Split(':');
                        openHour = int.Parse(openParts[0]);
                        openMin = openParts.Length > 1 ? int.Parse(openParts[1]) : 0;
                        closeHour = int.Parse(closeParts[0]);
                        closeMin = closeParts.Length > 1 ? int.Parse(closeParts[1]) : 0;
                    }
                }
            }
            catch { }
        }

        // Personelin o gundeki mevcut randevulari (UTC)
        var dayStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);
        var existingAppointments = await _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId
                && a.PersonnelId == personnelId
                && a.StatusId != 4 && a.StatusId != 5
                && a.StartTime >= dayStart && a.StartTime < dayEnd)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        // Musait slotlari hesapla (30 dk aralikla)
        var slots = new List<object>();
        var slotStart = dayStart.AddHours(openHour).AddMinutes(openMin);
        var dayClose = dayStart.AddHours(closeHour).AddMinutes(closeMin);

        while (slotStart.AddMinutes(durationMinutes) <= dayClose)
        {
            var slotEnd = slotStart.AddMinutes(durationMinutes);
            var hasConflict = existingAppointments.Any(a => slotStart < a.EndTime && slotEnd > a.StartTime);

            slots.Add(new
            {
                startTime = slotStart,
                endTime = slotEnd,
                available = !hasConflict,
                timeText = slotStart.ToString("HH:mm")
            });

            slotStart = slotStart.AddMinutes(30);
        }

        return slots;
    }
}
