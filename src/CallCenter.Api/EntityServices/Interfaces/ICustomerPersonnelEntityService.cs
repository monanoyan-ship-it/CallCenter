using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ICustomerPersonnelEntityService
{
    Task<CustomerPersonnel?> GetByUserIdAsync(int userId);
    Task<CustomerPersonnel?> GetByIdAndCustomerAsync(int personnelId, int customerId);
    Task<CustomerPersonnel?> GetByIdWithUserAsync(int personnelId, int customerId);
    Task<CustomerPersonnel?> GetByIdWithPermissionsAsync(int personnelId, int customerId);
    Task<CustomerPersonnel?> GetCustomerAdminAsync(int customerId);
    Task<int> GetActiveCountAsync(int customerId, bool excludeAdmin = false);
    Task<int> GetActiveAdminCountAsync(int customerId);
    Task<List<int>> GetTeamMemberIdsAsync(int personnelId, int customerId);
    IQueryable<CustomerPersonnel> GetAllQueryable();
    void Add(CustomerPersonnel entity);
    void Update(CustomerPersonnel entity);
    void Remove(CustomerPersonnel entity);
}
