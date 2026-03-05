using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IRetentionPolicyEntityService
{
    IQueryable<RetentionPolicy> GetAllQueryable();
    Task<RetentionPolicy?> GetByIdAsync(int id);
    void Add(RetentionPolicy entity);
    void Update(RetentionPolicy entity);
    void Remove(RetentionPolicy entity);
}
