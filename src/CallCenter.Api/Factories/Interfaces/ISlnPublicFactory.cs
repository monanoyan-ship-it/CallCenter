using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnPublicFactory
{
    Task<SlnSalonProfileDto?> GetBySlugAsync(string slug);
    Task<object> GetAllPublishedAsync();
    Task<object> GetAllBranchesForMapAsync();
    Task<object?> GetReviewsAsync(string slug);
    Task<object?> GetTeamAsync(string slug);
    Task<object?> GetBranchesAsync(string slug);
    Task<object?> GetMembershipPlansAsync(string slug);
    Task<(bool Success, string? Error, object? Result)> MembershipSignupAsync(string slug, SlnMembershipSignupDto dto);
    Task<object?> GetAvailableSlotsAsync(string slug, int serviceId, DateTime date, int? personnelId = null);
    Task<object?> GetAvailableStaffForServiceAsync(string slug, int serviceId);
    Task<object?> GetBookingPolicyAsync(string slug);
    Task<(bool Success, string? Error, object? Result)> BookAppointmentAsync(string slug, SlnOnlineBookingDto dto, string? buyerIp = null);

    /// <summary>Public bekleme listesi basvurusu (musait saat yoksa). Telefon ile ayni musteri varsa onu kullanir, yoksa olusturur.</summary>
    Task<(bool Success, string? Error, object? Result)> JoinWaitlistAsync(string slug, SlnPublicWaitlistDto dto);
}
