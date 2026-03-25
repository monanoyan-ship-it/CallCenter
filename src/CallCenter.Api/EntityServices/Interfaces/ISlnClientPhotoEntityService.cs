using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnClientPhotoEntityService
{
    IQueryable<SlnClientPhoto> GetAllQueryable();
    Task<SlnClientPhoto?> GetByIdAsync(int id);
    void Add(SlnClientPhoto entity);
    void Remove(SlnClientPhoto entity);
}
