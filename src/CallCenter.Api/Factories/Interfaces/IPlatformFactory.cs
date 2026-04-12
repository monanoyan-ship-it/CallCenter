using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IPlatformFactory
{
    // Salon uyelik
    Task<List<PlatformSalonDto>> GetMySalonsAsync(int platformUserId);
    Task<(bool Success, string? Error)> JoinSalonAsync(int platformUserId, int customerId);
    Task<(bool Success, string? Error)> LeaveSalonAsync(int platformUserId, int customerId);
    Task<(bool Success, bool IsFavorite)> ToggleFavoriteAsync(int platformUserId, int customerId);

    // Randevu
    Task<List<PlatformAppointmentDto>> GetMyAppointmentsAsync(int platformUserId, bool past);
    Task<(int? AppointmentId, string? Error)> CreateAppointmentAsync(int platformUserId, PlatformCreateAppointmentDto dto);
    Task<(bool Success, string? Error)> CancelAppointmentAsync(int platformUserId, int appointmentId);

    // Sadakat
    Task<List<PlatformLoyaltyDto>> GetMyLoyaltyAsync(int platformUserId);

    // Kesfet
    Task<object> DiscoverSalonsAsync(string? city, string? search, int page);
}
