using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IPasswordHistoryEntityService
{
    Task<List<string>> GetRecentHashesAsync(int userId, int count);
    void Add(PasswordHistory entity);
}
