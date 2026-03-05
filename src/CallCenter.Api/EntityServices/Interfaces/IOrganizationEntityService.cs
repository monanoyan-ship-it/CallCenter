using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IOrganizationEntityService
{
    IQueryable<CustomerOrganizationUnit> GetAllQueryable();
    Task<CustomerOrganizationUnit?> GetByIdAsync(int customerId, int id);
    Task<CustomerOrganizationUnit?> GetByIdWithChildrenAsync(int customerId, int id);
    Task<List<CustomerOrganizationUnit>> GetTreeDataAsync(int customerId);
    Task<bool> ExistsByNameAsync(int customerId, string name, int? parentId, int? excludeId = null);
    void Add(CustomerOrganizationUnit entity);
    void Update(CustomerOrganizationUnit entity);
    void Remove(CustomerOrganizationUnit entity);

    // Personel atama (junction)
    Task<bool> IsPersonnelAssignedAsync(int personnelId, int orgId);
    Task<CustomerPersonnelOrganizationUnit?> GetPersonnelAssignmentAsync(int personnelId, int orgId);
    void AddPersonnelAssignment(CustomerPersonnelOrganizationUnit assignment);
    void RemovePersonnelAssignment(CustomerPersonnelOrganizationUnit assignment);
    Task<List<OrgUnitPersonnelDto>> GetAvailablePersonnelAsync(int customerId, int orgId);
}
