using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IModulePricingEntityService
{
    Task<List<ModulePricing>> GetAllAsync();
    Task<ModulePricing?> GetByModuleIdAsync(int moduleId);
    void Add(ModulePricing entity);
    void Update(ModulePricing entity);
}
