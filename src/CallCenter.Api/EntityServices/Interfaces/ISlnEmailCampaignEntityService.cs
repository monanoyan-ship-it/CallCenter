using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnEmailCampaignEntityService
{
    IQueryable<SlnEmailCampaign> GetAllQueryable();
    Task<SlnEmailCampaign?> GetByIdAsync(int id);
    void Add(SlnEmailCampaign entity);
    void Update(SlnEmailCampaign entity);
    void Remove(SlnEmailCampaign entity);
}
