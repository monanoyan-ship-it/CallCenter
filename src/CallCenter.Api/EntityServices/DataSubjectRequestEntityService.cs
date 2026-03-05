using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class DataSubjectRequestEntityService : IDataSubjectRequestEntityService
{
    private readonly AppDbContext _db;

    public DataSubjectRequestEntityService(AppDbContext db) => _db = db;

    public IQueryable<DataSubjectRequest> GetAllQueryable() => _db.DataSubjectRequests.AsQueryable();
    public Task<DataSubjectRequest?> GetByIdAsync(int id) => _db.DataSubjectRequests.FindAsync(id).AsTask();
    public Task<DataSubjectRequest?> GetByUidAsync(Guid uid) => _db.DataSubjectRequests.FirstOrDefaultAsync(d => d.Uid == uid);
    public void Add(DataSubjectRequest entity) => _db.DataSubjectRequests.Add(entity);
    public void Update(DataSubjectRequest entity) => _db.DataSubjectRequests.Update(entity);
    public void Remove(DataSubjectRequest entity) => _db.DataSubjectRequests.Remove(entity);
}
