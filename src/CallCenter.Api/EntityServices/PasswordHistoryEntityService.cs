using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class PasswordHistoryEntityService : IPasswordHistoryEntityService
{
    private readonly AppDbContext _db;

    public PasswordHistoryEntityService(AppDbContext db) => _db = db;

    public Task<List<string>> GetRecentHashesAsync(int userId, int count)
        => _db.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(count)
            .Select(ph => ph.PasswordHash)
            .ToListAsync();

    public void Add(PasswordHistory entity) => _db.PasswordHistories.Add(entity);
}
