using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnAppointmentFactory : ISlnAppointmentFactory
{
    private readonly ISlnAppointmentEntityService _appointments;
    private readonly ISlnServiceEntityService _services;
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnAppointmentFactory> _logger;

    public SlnAppointmentFactory(
        ISlnAppointmentEntityService appointments,
        ISlnServiceEntityService services,
        AppDbContext db,
        IUnitOfWork uow,
        ILogger<SlnAppointmentFactory> logger)
    {
        _appointments = appointments;
        _services = services;
        _db = db;
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
            var client = await _db.SlnClients.FindAsync(dto.SlnClientId);
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

        var policy = await _db.SlnNoShowPolicies
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
}
