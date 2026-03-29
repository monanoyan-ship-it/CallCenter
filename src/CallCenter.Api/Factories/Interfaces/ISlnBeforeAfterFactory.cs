using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnBeforeAfterFactory
{
    Task<List<SlnBeforeAfterPhotoDto>> GetPhotosAsync(int customerId);
    Task<SlnBeforeAfterPhotoDto?> GetPhotoAsync(int id, int customerId);
    Task<SlnBeforeAfterPhotoDto> CreatePhotoAsync(SlnBeforeAfterPhotoCreateDto dto, int customerId);
    Task<(bool Success, string? Error)> UpdatePhotoAsync(int id, SlnBeforeAfterPhotoUpdateDto dto, int customerId);
    Task<(bool Success, string? Error)> DeletePhotoAsync(int id, int customerId);
}
