using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IUserEntityService
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameWithPersonnelAsync(string username);
    IQueryable<User> GetAllQueryable();
    Task<bool> ExistsByUsernameAsync(string username);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
    Task<int> GetActiveAdminCountAsync();
    void Add(User entity);
    void Update(User entity);
}
