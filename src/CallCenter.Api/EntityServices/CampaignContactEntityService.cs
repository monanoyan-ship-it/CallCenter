using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CampaignContactEntityService : ICampaignContactEntityService
{
    private readonly AppDbContext _db;

    public CampaignContactEntityService(AppDbContext db) => _db = db;

    public Task<CampaignContact?> GetByIdAsync(int id)
        => _db.CampaignContacts.FindAsync(id).AsTask();

    public IQueryable<CampaignContact> GetAllQueryable()
        => _db.CampaignContacts.AsQueryable();

    public void Add(CampaignContact entity) => _db.CampaignContacts.Add(entity);
    public void AddRange(IEnumerable<CampaignContact> entities) => _db.CampaignContacts.AddRange(entities);
    public void Update(CampaignContact entity) => _db.CampaignContacts.Update(entity);
    public void Delete(CampaignContact entity) => _db.CampaignContacts.Remove(entity);
    public void DeleteRange(IEnumerable<CampaignContact> entities) => _db.CampaignContacts.RemoveRange(entities);
}
