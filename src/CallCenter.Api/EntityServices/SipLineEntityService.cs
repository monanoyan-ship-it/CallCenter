using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class SipLineEntityService : ISipLineEntityService
{
    private readonly AppDbContext _db;

    public SipLineEntityService(AppDbContext db) => _db = db;

    public IQueryable<SipLine> GetAllQueryable()
        => _db.SipLines.AsQueryable();

    public Task<SipLine?> GetByIdAsync(int id)
        => _db.SipLines.FindAsync(id).AsTask();

    public Task<SipLine?> GetByIdWithGatewayAsync(int id)
        => _db.SipLines.Include(l => l.SipAccount).FirstOrDefaultAsync(l => l.Id == id);

    public Task<SipLine?> GetByPersonnelAsync(int personnelId)
        => _db.SipLines
            .Include(l => l.SipAccount)
            .FirstOrDefaultAsync(l => l.AssignedPersonnelId == personnelId && l.IsActive);

    /// <summary>Bos hat tahsisi: gateway'in default hattini tercih et, yoksa herhangi bir aktif bos hat</summary>
    public async Task<SipLine?> AcquireUnassignedAsync(int customerId)
    {
        // Oncelikle default gateway'in bos hattini al
        var line = await _db.SipLines
            .Include(l => l.SipAccount)
            .FirstOrDefaultAsync(l =>
                l.SipAccount.CustomerId == customerId &&
                l.SipAccount.IsActive &&
                l.SipAccount.IsDefault &&
                l.IsActive &&
                l.AssignedPersonnelId == null);

        // Default gateway'de bos hat yoksa, herhangi bir gateway'in bos hatti
        line ??= await _db.SipLines
            .Include(l => l.SipAccount)
            .FirstOrDefaultAsync(l =>
                l.SipAccount.CustomerId == customerId &&
                l.SipAccount.IsActive &&
                l.IsActive &&
                l.AssignedPersonnelId == null);

        return line;
    }

    public async Task ReleaseByPersonnelAsync(int personnelId)
    {
        var line = await _db.SipLines
            .FirstOrDefaultAsync(l => l.AssignedPersonnelId == personnelId);

        if (line != null)
        {
            line.AssignedPersonnelId = null;
            line.AssignedAt = null;
        }
    }

    public async Task ReleaseStaleAllocationsAsync(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var stale = await _db.SipLines
            .Where(l => l.AssignedPersonnelId != null && l.AssignedAt != null && l.AssignedAt < cutoff)
            .ToListAsync();

        foreach (var line in stale)
        {
            line.AssignedPersonnelId = null;
            line.AssignedAt = null;
        }
    }

    public void Add(SipLine entity) => _db.SipLines.Add(entity);
    public void Update(SipLine entity) => _db.SipLines.Update(entity);
}
