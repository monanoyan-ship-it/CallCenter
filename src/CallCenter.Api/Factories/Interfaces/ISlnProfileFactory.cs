using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnProfileFactory
{
    Task<object?> GetProfileAsync(int customerId);
    Task<(bool Success, string? Error)> SaveProfileAsync(SlnSalonProfileUpdateDto dto, int customerId);
    Task<(bool Success, string? Error)> SavePageSettingsAsync(SlnPageSettingsDto dto, int customerId);
}
