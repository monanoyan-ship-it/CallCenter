using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class CampaignEntityService : ICampaignEntityService
{
    private readonly AppDbContext _db;

    public CampaignEntityService(AppDbContext db) => _db = db;

    public Task<CallCampaign?> GetByIdAsync(int id)
        => _db.CallCampaigns.FindAsync(id).AsTask();

    public Task<CallCampaign?> GetByUidAsync(Guid uid)
        => _db.CallCampaigns.FirstOrDefaultAsync(c => c.Uid == uid);

    public IQueryable<CallCampaign> GetAllQueryable()
        => _db.CallCampaigns.AsQueryable();

    public void Add(CallCampaign entity) => _db.CallCampaigns.Add(entity);
    public void Update(CallCampaign entity) => _db.CallCampaigns.Update(entity);
    public void Delete(CallCampaign entity) => _db.CallCampaigns.Remove(entity);
}
