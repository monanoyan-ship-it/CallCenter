using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IInstantMessageEntityService
{
    IQueryable<InstantMessage> GetAllQueryable();
    Task<InstantMessage?> GetByIdAsync(int id);
    void Add(InstantMessage entity);
}
