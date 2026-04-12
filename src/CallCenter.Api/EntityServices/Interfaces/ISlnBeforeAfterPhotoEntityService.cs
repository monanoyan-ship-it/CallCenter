using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnBeforeAfterPhotoEntityService
{
    IQueryable<SlnBeforeAfterPhoto> GetAllQueryable();
    Task<SlnBeforeAfterPhoto?> GetByIdAsync(int id);
    void Add(SlnBeforeAfterPhoto entity);
    void Update(SlnBeforeAfterPhoto entity);
    void Remove(SlnBeforeAfterPhoto entity);
}
