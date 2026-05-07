using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IUserFactory
{
    Task<PagedResult<UserListDto>> GetAllAsync(int page, int pageSize, string? search, int? roleId);
    Task<UserListDto?> GetByIdAsync(int id);
    Task<(bool Success, int? Id, string? Error)> CreateAsync(UserCreateDto dto);
    Task<(bool Success, string? Error)> UpdateAsync(int id, UserUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(int id, int currentUserId);
    Task<(bool Success, string? Error)> VerifyEmailManuallyAsync(int userId);
}
