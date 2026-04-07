using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class ModuleRequestEntityService : IModuleRequestEntityService
{
    private readonly AppDbContext _db;

    public ModuleRequestEntityService(AppDbContext db) => _db = db;

    public IQueryable<ModuleRequest> GetAllQueryable()
        => _db.ModuleRequests.AsQueryable();

    public Task<ModuleRequest?> GetByIdAsync(int id)
        => _db.ModuleRequests
            .Include(r => r.Customer)
            .Include(r => r.RequestedByPersonnel)
            .Include(r => r.ReviewedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

    public Task<List<ModuleRequest>> GetByCustomerAsync(int customerId)
        => _db.ModuleRequests
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public Task<List<ModuleRequest>> GetPendingAsync()
        => _db.ModuleRequests
            .Where(r => r.StatusId == ModuleRequestStatuses.Ids.Pending)
            .Include(r => r.Customer)
            .Include(r => r.RequestedByPersonnel)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

    public Task<bool> HasPendingRequestAsync(int customerId, int moduleId)
        => _db.ModuleRequests
            .AnyAsync(r => r.CustomerId == customerId
                        && r.ModuleId == moduleId
                        && r.StatusId == ModuleRequestStatuses.Ids.Pending);

    public void Add(ModuleRequest entity) => _db.ModuleRequests.Add(entity);
    public void Update(ModuleRequest entity) => _db.ModuleRequests.Update(entity);
}
