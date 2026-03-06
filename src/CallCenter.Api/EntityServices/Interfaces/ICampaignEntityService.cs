using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICampaignEntityService
{
    Task<CallCampaign?> GetByIdAsync(int id);
    Task<CallCampaign?> GetByUidAsync(Guid uid);
    IQueryable<CallCampaign> GetAllQueryable();
    void Add(CallCampaign entity);
    void Update(CallCampaign entity);
    void Delete(CallCampaign entity);
}
