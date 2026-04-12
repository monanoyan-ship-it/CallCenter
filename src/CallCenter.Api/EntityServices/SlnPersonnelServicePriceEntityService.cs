using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnPersonnelServicePriceEntityService : ISlnPersonnelServicePriceEntityService
{
    private readonly AppDbContext _db;

    public SlnPersonnelServicePriceEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnPersonnelServicePrice> GetAllQueryable()
        => _db.SlnPersonnelServicePrices.AsQueryable();

    public Task<SlnPersonnelServicePrice?> GetByIdAsync(int id)
        => _db.SlnPersonnelServicePrices.FindAsync(id).AsTask();

    public void Add(SlnPersonnelServicePrice entity) => _db.SlnPersonnelServicePrices.Add(entity);
    public void Update(SlnPersonnelServicePrice entity) => _db.SlnPersonnelServicePrices.Update(entity);
    public void Remove(SlnPersonnelServicePrice entity) => _db.SlnPersonnelServicePrices.Remove(entity);
}
