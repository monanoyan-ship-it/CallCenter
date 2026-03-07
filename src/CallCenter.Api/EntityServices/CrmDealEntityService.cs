using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class CrmDealEntityService : ICrmDealEntityService
{
    private readonly AppDbContext _db;

    public CrmDealEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrmDeal> GetAllQueryable()
        => _db.CrmDeals.AsQueryable();

    public Task<CrmDeal?> GetByIdAsync(int id)
        => _db.CrmDeals.FindAsync(id).AsTask();

    public void Add(CrmDeal entity) => _db.CrmDeals.Add(entity);
    public void Update(CrmDeal entity) => _db.CrmDeals.Update(entity);
    public void Remove(CrmDeal entity) => _db.CrmDeals.Remove(entity);
}
