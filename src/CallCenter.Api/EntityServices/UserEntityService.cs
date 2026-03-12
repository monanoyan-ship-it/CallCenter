using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class UserEntityService : IUserEntityService
{
    private readonly AppDbContext _db;

    public UserEntityService(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(int id)
        => _db.Users.FindAsync(id).AsTask();

    public Task<User?> GetByUsernameWithPersonnelAsync(string username)
        => _db.Users
            .Include(u => u.CustomerPersonnel)
            .FirstOrDefaultAsync(u => u.UserName == username);

    public IQueryable<User> GetAllQueryable()
        => _db.Users.AsQueryable();

    public Task<bool> ExistsByUsernameAsync(string username)
        => _db.Users.AnyAsync(u => u.UserName == username);

    public Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
        => excludeId.HasValue
            ? _db.Users.AnyAsync(u => u.Email == email && u.Id != excludeId.Value)
            : _db.Users.AnyAsync(u => u.Email == email);

    public Task<int> GetActiveAdminCountAsync()
        => _db.Users.CountAsync(u => u.RoleId == UserRoles.Ids.Admin && u.IsActive);

    public void Add(User entity) => _db.Users.Add(entity);
    public void Update(User entity) => _db.Users.Update(entity);
    public void Remove(User entity) => _db.Users.Remove(entity);
}
