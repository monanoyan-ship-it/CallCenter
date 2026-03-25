using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnCampaignEntityService
{
    IQueryable<SlnCampaign> GetAllQueryable();
    Task<SlnCampaign?> GetByIdAsync(int id);
    void Add(SlnCampaign entity);
    void Update(SlnCampaign entity);
    void Remove(SlnCampaign entity);
}
