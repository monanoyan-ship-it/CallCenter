using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class PrivacyNoticeEntityService : IPrivacyNoticeEntityService
{
    private readonly AppDbContext _db;

    public PrivacyNoticeEntityService(AppDbContext db) => _db = db;

    public IQueryable<PrivacyNotice> GetAllQueryable() => _db.PrivacyNotices.AsQueryable();
    public Task<PrivacyNotice?> GetByIdAsync(int id) => _db.PrivacyNotices.FindAsync(id).AsTask();
    public Task<PrivacyNotice?> GetByUidAsync(Guid uid) => _db.PrivacyNotices.FirstOrDefaultAsync(p => p.Uid == uid);
    public void Add(PrivacyNotice entity) => _db.PrivacyNotices.Add(entity);
    public void Update(PrivacyNotice entity) => _db.PrivacyNotices.Update(entity);
    public void Remove(PrivacyNotice entity) => _db.PrivacyNotices.Remove(entity);
}
