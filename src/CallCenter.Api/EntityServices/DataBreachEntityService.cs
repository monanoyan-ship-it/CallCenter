using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class DataBreachEntityService : IDataBreachEntityService
{
    private readonly AppDbContext _db;

    public DataBreachEntityService(AppDbContext db) => _db = db;

    public IQueryable<DataBreach> GetAllQueryable() => _db.DataBreaches.AsQueryable();
    public Task<DataBreach?> GetByIdAsync(int id) => _db.DataBreaches.FindAsync(id).AsTask();
    public Task<DataBreach?> GetByUidAsync(Guid uid) => _db.DataBreaches.FirstOrDefaultAsync(b => b.Uid == uid);
    public void Add(DataBreach entity) => _db.DataBreaches.Add(entity);
    public void Update(DataBreach entity) => _db.DataBreaches.Update(entity);
    public void Remove(DataBreach entity) => _db.DataBreaches.Remove(entity);
}
