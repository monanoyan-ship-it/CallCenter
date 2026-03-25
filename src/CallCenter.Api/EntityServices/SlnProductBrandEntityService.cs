using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnProductBrandEntityService : ISlnProductBrandEntityService
{
    private readonly AppDbContext _db;

    public SlnProductBrandEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnProductBrand> GetAllQueryable()
        => _db.SlnProductBrands.AsQueryable();

    public Task<SlnProductBrand?> GetByIdAsync(int id)
        => _db.SlnProductBrands.FindAsync(id).AsTask();

    public void Add(SlnProductBrand entity) => _db.SlnProductBrands.Add(entity);
    public void Update(SlnProductBrand entity) => _db.SlnProductBrands.Update(entity);
    public void Remove(SlnProductBrand entity) => _db.SlnProductBrands.Remove(entity);
}
