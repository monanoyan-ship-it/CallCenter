using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface ISlnReviewEntityService
{
    IQueryable<SlnReview> GetAllQueryable();
    Task<SlnReview?> GetByIdAsync(int id);
    void Add(SlnReview entity);
    void Update(SlnReview entity);
    void Remove(SlnReview entity);
}
