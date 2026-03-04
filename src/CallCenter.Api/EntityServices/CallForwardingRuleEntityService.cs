using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CallForwardingRuleEntityService : ICallForwardingRuleEntityService
{
    private readonly AppDbContext _db;

    public CallForwardingRuleEntityService(AppDbContext db) => _db = db;

    public IQueryable<CallForwardingRule> GetAllQueryable()
        => _db.CallForwardingRules.AsQueryable();

    public Task<CallForwardingRule?> GetByIdAsync(int id)
        => _db.CallForwardingRules.FindAsync(id).AsTask();

    public Task<CallForwardingRule?> GetByIdWithUserAsync(int id)
        => _db.CallForwardingRules
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == id);

    public void Add(CallForwardingRule entity) => _db.CallForwardingRules.Add(entity);
    public void Remove(CallForwardingRule entity) => _db.CallForwardingRules.Remove(entity);
}
