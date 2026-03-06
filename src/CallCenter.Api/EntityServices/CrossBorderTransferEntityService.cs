using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CrossBorderTransferEntityService : ICrossBorderTransferEntityService
{
    private readonly AppDbContext _db;

    public CrossBorderTransferEntityService(AppDbContext db) => _db = db;

    public IQueryable<CrossBorderTransfer> GetAllQueryable() => _db.CrossBorderTransfers.AsQueryable();
    public Task<CrossBorderTransfer?> GetByIdAsync(int id) => _db.CrossBorderTransfers.FindAsync(id).AsTask();
    public Task<CrossBorderTransfer?> GetByUidAsync(Guid uid) => _db.CrossBorderTransfers.FirstOrDefaultAsync(t => t.Uid == uid);
    public void Add(CrossBorderTransfer entity) => _db.CrossBorderTransfers.Add(entity);
    public void Update(CrossBorderTransfer entity) => _db.CrossBorderTransfers.Update(entity);
    public void Remove(CrossBorderTransfer entity) => _db.CrossBorderTransfers.Remove(entity);
}
