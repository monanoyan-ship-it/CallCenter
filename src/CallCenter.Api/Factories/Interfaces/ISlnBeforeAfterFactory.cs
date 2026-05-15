using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnBeforeAfterFactory
{
    Task<List<SlnBeforeAfterPhotoDto>> GetPhotosAsync(int customerId, int? branchId = null);
    Task<SlnBeforeAfterPhotoDto?> GetPhotoAsync(int id, int customerId, int? branchId = null);
    Task<SlnBeforeAfterPhotoDto> CreatePhotoAsync(SlnBeforeAfterPhotoCreateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> UpdatePhotoAsync(int id, SlnBeforeAfterPhotoUpdateDto dto, int customerId, int? branchId = null);
    Task<(bool Success, string? Error)> DeletePhotoAsync(int id, int customerId, int? branchId = null);
}
