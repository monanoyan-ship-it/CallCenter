using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnProductBranchStockEntityService : ISlnProductBranchStockEntityService
{
    private readonly AppDbContext _db;

    public SlnProductBranchStockEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnProductBranchStock> GetAllQueryable()
        => _db.SlnProductBranchStocks.AsQueryable();

    public Task<SlnProductBranchStock?> GetByIdAsync(int id)
        => _db.SlnProductBranchStocks.FindAsync(id).AsTask();

    public void Add(SlnProductBranchStock entity) => _db.SlnProductBranchStocks.Add(entity);
    public void Update(SlnProductBranchStock entity) => _db.SlnProductBranchStocks.Update(entity);
    public void Remove(SlnProductBranchStock entity) => _db.SlnProductBranchStocks.Remove(entity);
}
