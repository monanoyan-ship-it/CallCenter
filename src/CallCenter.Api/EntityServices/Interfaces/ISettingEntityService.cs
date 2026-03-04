using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISettingEntityService
{
    IQueryable<SystemSetting> GetAllQueryable();
    Task<SystemSetting?> GetByIdAsync(int id);
    Task<bool> ExistsByKeyAsync(string key);
    void Add(SystemSetting entity);
    void Update(SystemSetting entity);
    void Remove(SystemSetting entity);
}
