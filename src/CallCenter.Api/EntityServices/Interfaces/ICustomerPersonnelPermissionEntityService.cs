using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICustomerPersonnelPermissionEntityService
{
    IQueryable<CustomerPersonnelPermission> GetAllQueryable();
    Task<CustomerPersonnelPermission?> GetByIdAsync(int id, int personnelId, int customerId);
    Task<List<CustomerPersonnelPermission>> GetByPersonnelIdAsync(int personnelId);
    void Add(CustomerPersonnelPermission entity);
    void Remove(CustomerPersonnelPermission entity);
    void RemoveRange(IEnumerable<CustomerPersonnelPermission> entities);
}
