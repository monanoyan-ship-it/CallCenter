using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnClientMembershipEntityService
{
    IQueryable<SlnClientMembership> GetAllQueryable();
    Task<SlnClientMembership?> GetByIdAsync(int id);
    void Add(SlnClientMembership entity);
    void Update(SlnClientMembership entity);
    void Remove(SlnClientMembership entity);
}
