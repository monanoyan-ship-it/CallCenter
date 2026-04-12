using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnEmailCampaignEntityService : ISlnEmailCampaignEntityService
{
    private readonly AppDbContext _db;

    public SlnEmailCampaignEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnEmailCampaign> GetAllQueryable()
        => _db.SlnEmailCampaigns.AsQueryable();

    public Task<SlnEmailCampaign?> GetByIdAsync(int id)
        => _db.SlnEmailCampaigns.FindAsync(id).AsTask();

    public void Add(SlnEmailCampaign entity) => _db.SlnEmailCampaigns.Add(entity);
    public void Update(SlnEmailCampaign entity) => _db.SlnEmailCampaigns.Update(entity);
    public void Remove(SlnEmailCampaign entity) => _db.SlnEmailCampaigns.Remove(entity);
}
