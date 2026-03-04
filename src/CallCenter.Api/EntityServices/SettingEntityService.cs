using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class SettingEntityService : ISettingEntityService
{
    private readonly AppDbContext _db;

    public SettingEntityService(AppDbContext db) => _db = db;

    public IQueryable<SystemSetting> GetAllQueryable()
        => _db.SystemSettings.AsQueryable();

    public Task<SystemSetting?> GetByIdAsync(int id)
        => _db.SystemSettings.FindAsync(id).AsTask();

    public Task<bool> ExistsByKeyAsync(string key)
        => _db.SystemSettings.AnyAsync(s => s.Key == key);

    public void Add(SystemSetting entity) => _db.SystemSettings.Add(entity);
    public void Update(SystemSetting entity) => _db.SystemSettings.Update(entity);
    public void Remove(SystemSetting entity) => _db.SystemSettings.Remove(entity);
}
