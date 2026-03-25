using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnCampaignEntityService : ISlnCampaignEntityService
{
    private readonly AppDbContext _db;

    public SlnCampaignEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnCampaign> GetAllQueryable()
        => _db.SlnCampaigns.AsQueryable();

    public Task<SlnCampaign?> GetByIdAsync(int id)
        => _db.SlnCampaigns.FindAsync(id).AsTask();

    public void Add(SlnCampaign entity) => _db.SlnCampaigns.Add(entity);
    public void Update(SlnCampaign entity) => _db.SlnCampaigns.Update(entity);
    public void Remove(SlnCampaign entity) => _db.SlnCampaigns.Remove(entity);
}
