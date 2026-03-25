using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnProductEntityService : ISlnProductEntityService
{
    private readonly AppDbContext _db;

    public SlnProductEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnProduct> GetAllQueryable()
        => _db.SlnProducts.AsQueryable();

    public Task<SlnProduct?> GetByIdAsync(int id)
        => _db.SlnProducts.FindAsync(id).AsTask();

    public void Add(SlnProduct entity) => _db.SlnProducts.Add(entity);
    public void Update(SlnProduct entity) => _db.SlnProducts.Update(entity);
    public void Remove(SlnProduct entity) => _db.SlnProducts.Remove(entity);
}
