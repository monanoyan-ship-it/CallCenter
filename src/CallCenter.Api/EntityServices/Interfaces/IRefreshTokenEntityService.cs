using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IRefreshTokenEntityService
{
    Task<RefreshToken?> GetByTokenWithUserAsync(string token);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<List<RefreshToken>> GetActiveByUserIdAsync(int userId);
    void Add(RefreshToken entity);
}
