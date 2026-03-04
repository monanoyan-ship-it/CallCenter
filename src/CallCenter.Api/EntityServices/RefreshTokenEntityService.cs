using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class RefreshTokenEntityService : IRefreshTokenEntityService
{
    private readonly AppDbContext _db;

    public RefreshTokenEntityService(AppDbContext db) => _db = db;

    public Task<RefreshToken?> GetByTokenWithUserAsync(string token)
        => _db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.CustomerPersonnel)
                    .ThenInclude(cp => cp!.Permissions)
            .FirstOrDefaultAsync(rt => rt.Token == token);

    public Task<RefreshToken?> GetByTokenAsync(string token)
        => _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);

    public Task<List<RefreshToken>> GetActiveByUserIdAsync(int userId)
        => _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

    public void Add(RefreshToken entity) => _db.RefreshTokens.Add(entity);
}
