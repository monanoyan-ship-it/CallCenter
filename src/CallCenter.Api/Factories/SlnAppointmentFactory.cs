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
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlnAppointmentFactory> _logger;

    public SlnAppointmentFactory(
        ISlnAppointmentEntityService appointments,
        ISlnServiceEntityService services,
        IUnitOfWork uow,
        ILogger<SlnAppointmentFactory> logger)
    {
        _appointments = appointments;
        _services = services;
        _uow = uow;
        _logger = logger;
    }

    public async Task<List<SlnAppointmentDto>> GetAppointmentsAsync(int customerId, DateTime? from, DateTime? to, int? personnelId = null, int? statusId = null)
    {
        var query = _appointments.GetAllQueryable()
            .Where(a => a.CustomerId == customerId);

        if (from.HasValue)
            query = query.Where(a => a.StartTime >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.StartTime <= to.Value);

        if (personnelId.HasValue)
            query = query.Where(a => a.PersonnelId == personnelId.Value);

        if (statusId.HasValue)
            query = query.Where(a => a.StatusId == statusId.Value);

        var appointments = await query
            .Include(a => a.SlnClient)
            .Include(a => a.Personnel).ThenInclude(p => p!.User)
            .Include(a => a.Service)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<SlnAppointmentDto?> GetAppointmentAsync(int appointmentId, int customerId)
    {
        var appointment = await _appointments.GetAllQueryable()
            .Include(a => a.SlnClient)
            .Include(a => a.Personnel).ThenInclude(p => p!.User)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CustomerId == customerId);

        return appointment != null ? MapToDto(appointment) : null;
    }

    public async Task<(SlnAppointmentDto? Appointment, string? Error)> CreateAppointmentAsync(SlnAppointmentCreateDto dto, int userId, int customerId)
    {
        // Hizmet suresini al
        var service = await _services.GetByIdAsync(dto.ServiceId);
        if (service == null)
            return (null, "Hizmet bulunamadi");

        var endTime = dto.StartTime.AddMinutes(service.DurationMinutes);

        // Cakisma kontrolu
        var hasConflict = await CheckConflictAsync(dto.PersonnelId, dto.StartTime, endTime, customerId);
        if (hasConflict)
            return (null, "Secilen saatte personelin baska bir randevusu var");

        var appointment = new SlnAppointment
        {
            CustomerId = customerId,
            SlnClientId = dto.SlnClientId,
            PersonnelId = dto.PersonnelId,
            ServiceId = dto.ServiceId,
            StartTime = dto.StartTime,
            EndTime = endTime,
            Notes = dto.Notes,
            CreatedByPersonnelId = userId
        };

        _appointments.Add(appointment);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Yeni randevu olusturuldu: {AppointmentId} - {StartTime}", appointment.Id, appointment.StartTime);

        // Include'li tekrar cek
        var created = await _appointments.GetAllQueryable()
            .Include(a => a.SlnClient)
            .Include(a => a.Personnel).ThenInclude(p => p!.User)
            .Include(a => a.Service)
            .FirstAsync(a => a.Id == appointment.Id);

        return (MapToDto(created), null);
    }

    public async Task<(bool Success, string? Error)> UpdateAppointmentAsync(int appointmentId, SlnAppointmentCreateDto dto, int customerId)
    {
        var appointment = await _appointments.GetAllQueryable()
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CustomerId == customerId);

        if (appointment == null) return (false, "Randevu bulunamadi");
        if (appointment.StatusId == 4) return (false, "Iptal edilmis randevu guncellenemez");

        var service = await _services.GetByIdAsync(dto.ServiceId);
        if (service == null) return (false, "Hizmet bulunamadi");

        var endTime = dto.StartTime.AddMinutes(service.DurationMinutes);

        var hasConflict = await CheckConflictAsync(dto.PersonnelId, dto.StartTime, endTime, customerId, appointmentId);
        if (hasConflict) return (false, "Secilen saatte personelin baska bir randevusu var");

        appointment.SlnClientId = dto.SlnClientId;
        appointment.PersonnelId = dto.PersonnelId;
        appointment.ServiceId = dto.ServiceId;
        appointment.StartTime = dto.StartTime;
        appointment.EndTime = endTime;
        appointment.Notes = dto.Notes;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateStatusAsync(int appointmentId, int statusId, int customerId)
    {
        var appointment = await _appointments.GetAllQueryable()
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.CustomerId == customerId);

        if (appointment == null) return (false, "Randevu bulunamadi");

        appointment.StatusId = statusId;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, null);
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

    private static SlnAppointmentDto MapToDto(SlnAppointment a) => new()
    {
        Id = a.Id,
        SlnClientId = a.SlnClientId,
        ClientName = a.SlnClient?.FullName ?? "",
        ClientPhone = a.SlnClient?.Phone,
        PersonnelId = a.PersonnelId,
        PersonnelName = a.Personnel?.User?.FullName ?? "",
        ServiceId = a.ServiceId,
        ServiceName = a.Service?.Name ?? "",
        StartTime = a.StartTime,
        EndTime = a.EndTime,
        StatusId = a.StatusId,
        Notes = a.Notes
    };
}
