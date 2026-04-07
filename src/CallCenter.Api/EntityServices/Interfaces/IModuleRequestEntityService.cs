using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IModuleRequestEntityService
{
    IQueryable<ModuleRequest> GetAllQueryable();
    Task<ModuleRequest?> GetByIdAsync(int id);
    Task<List<ModuleRequest>> GetByCustomerAsync(int customerId);
    Task<List<ModuleRequest>> GetPendingAsync();
    Task<bool> HasPendingRequestAsync(int customerId, int moduleId);
    void Add(ModuleRequest entity);
    void Update(ModuleRequest entity);
}
