using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICampaignContactEntityService
{
    Task<CampaignContact?> GetByIdAsync(int id);
    IQueryable<CampaignContact> GetAllQueryable();
    void Add(CampaignContact entity);
    void AddRange(IEnumerable<CampaignContact> entities);
    void Update(CampaignContact entity);
    void Delete(CampaignContact entity);
    void DeleteRange(IEnumerable<CampaignContact> entities);
}
