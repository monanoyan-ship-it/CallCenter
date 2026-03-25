using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnProductBrandEntityService
{
    IQueryable<SlnProductBrand> GetAllQueryable();
    Task<SlnProductBrand?> GetByIdAsync(int id);
    void Add(SlnProductBrand entity);
    void Update(SlnProductBrand entity);
    void Remove(SlnProductBrand entity);
}
