using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnAppointmentFactory
{
    Task<List<SlnAppointmentDto>> GetAppointmentsAsync(int customerId, DateTime? from, DateTime? to, int? personnelId = null, int? statusId = null, int? branchId = null);
    Task<SlnAppointmentDto?> GetAppointmentAsync(int appointmentId, int customerId);
    Task<(SlnAppointmentDto? Appointment, string? Error)> CreateAppointmentAsync(SlnAppointmentCreateDto dto, int userId, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> UpdateAppointmentAsync(int appointmentId, SlnAppointmentCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdateStatusAsync(int appointmentId, int statusId, int customerId);
    Task<(bool Success, string? Error)> DeleteAppointmentAsync(int appointmentId, int customerId);
    Task<bool> CheckConflictAsync(int personnelId, DateTime startTime, DateTime endTime, int customerId, int? excludeAppointmentId = null);
}
