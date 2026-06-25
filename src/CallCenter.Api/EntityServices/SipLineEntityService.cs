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
    public async Task<SipLine?> AcquireUnassignedAsync(int customerId, int? excludeLineId = null)
    {
        // Oncelikle default gateway'in bos hattini al
        var line = await _db.SipLines
            .Include(l => l.SipAccount)
            .OrderBy(l => l.ChannelNumber)
            .FirstOrDefaultAsync(l =>
                l.SipAccount.CustomerId == customerId &&
                l.SipAccount.IsActive &&
                l.SipAccount.IsDefault &&
                l.IsActive &&
                (!excludeLineId.HasValue || l.Id != excludeLineId.Value) &&
                l.AssignedPersonnelId == null);

        // Default gateway'de bos hat yoksa, herhangi bir gateway'in bos hatti
        line ??= await _db.SipLines
            .Include(l => l.SipAccount)
            .OrderByDescending(l => l.SipAccount.IsDefault)
            .ThenBy(l => l.SipAccount.Name)
            .ThenBy(l => l.ChannelNumber)
            .FirstOrDefaultAsync(l =>
                l.SipAccount.CustomerId == customerId &&
                l.SipAccount.IsActive &&
                l.IsActive &&
                (!excludeLineId.HasValue || l.Id != excludeLineId.Value) &&
                l.AssignedPersonnelId == null);

        return line;
    }

    /// <summary>Belirli bir gateway'den bos hat tahsisi</summary>
    public async Task<SipLine?> AcquireUnassignedAsync(int customerId, int gatewayId, int? excludeLineId = null)
    {
        return await _db.SipLines
            .Include(l => l.SipAccount)
            .OrderBy(l => l.ChannelNumber)
            .FirstOrDefaultAsync(l =>
                l.SipAccount.CustomerId == customerId &&
                l.SipAccountId == gatewayId &&
                l.SipAccount.IsActive &&
                l.IsActive &&
                (!excludeLineId.HasValue || l.Id != excludeLineId.Value) &&
                l.AssignedPersonnelId == null);
    }

    public async Task<SipLine?> AcquireNextRotatingAsync(int customerId, int? excludeLineId = null)
    {
        // Musterinin TUM aktif hatlarini stabil sirayla al — rotasyon pozisyonu sabit kalsin.
        var allLines = await _db.SipLines
            .Include(l => l.SipAccount)
            .Where(l => l.SipAccount.CustomerId == customerId && l.SipAccount.IsActive && l.IsActive)
            .OrderByDescending(l => l.SipAccount.IsDefault)
            .ThenBy(l => l.SipAccount.Name)
            .ThenBy(l => l.ChannelNumber)
            .ThenBy(l => l.Id)
            .ToListAsync();

        if (allLines.Count == 0) return null;

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId);

        // Son tahsis edilen hattin pozisyonundan SONRAKINDEN basla (yoksa bastan).
        var startIdx = 0;
        if (customer?.LastAssignedSipLineId is int lastId)
        {
            var idx = allLines.FindIndex(l => l.Id == lastId);
            startIdx = idx >= 0 ? idx + 1 : 0;
        }

        // startIdx'ten itibaren (sona gelince basa donerek) ilk BOS ve haric-olmayan hatti bul.
        for (var i = 0; i < allLines.Count; i++)
        {
            var line = allLines[(startIdx + i) % allLines.Count];
            if (line.AssignedPersonnelId == null &&
                (!excludeLineId.HasValue || line.Id != excludeLineId.Value))
            {
                // Pointer'i ilerlet — cagiranin SaveChangesAsync'i kalici yapar (ayni DbContext).
                if (customer != null)
                    customer.LastAssignedSipLineId = line.Id;
                return line;
            }
        }

        return null; // tum hatlar dolu
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
