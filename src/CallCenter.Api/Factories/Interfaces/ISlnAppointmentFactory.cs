using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnAppointmentFactory
{
    Task<List<SlnAppointmentDto>> GetAppointmentsAsync(int customerId, DateTime? from, DateTime? to, int? personnelId = null, int? statusId = null, int? branchId = null, int? slnClientId = null);
    Task<SlnAppointmentDto?> GetAppointmentAsync(int appointmentId, int customerId, int? branchId = null);
    Task<(SlnAppointmentDto? Appointment, string? Error)> CreateAppointmentAsync(SlnAppointmentCreateDto dto, int userId, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> UpdateAppointmentAsync(int appointmentId, SlnAppointmentCreateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error, decimal Penalty)> UpdateStatusAsync(int appointmentId, int statusId, int customerId, int? branchId = null, bool skipRecipeStockConsumption = false);
    Task<(bool Success, string? Error)> DeleteAppointmentAsync(int appointmentId, int customerId, int? branchId = null);
    Task<bool> CheckConflictAsync(int personnelId, DateTime startTime, DateTime endTime, int customerId, int? excludeAppointmentId = null);

    /// <summary>Belirtilen hizmetleri yapabilecek personeller (skill eslemesi yoksa tum aktifler)</summary>
    Task<List<object>> GetAvailableStaffAsync(int customerId, List<int> serviceIds, int? branchId = null);

    /// <summary>Personelin belirtilen gundeki musait saat slotlari</summary>
    Task<List<object>> GetAvailableSlotsAsync(int customerId, int personnelId, DateTime date, int durationMinutes, int? branchId = null, List<int>? serviceIds = null);

    /// <summary>BranchId NULL olan randevulari merkez subeye bagla (multi-sube gecis oncesi veri temizligi).</summary>
    Task<object> NormalizeBranchesAsync(int customerId);
}
