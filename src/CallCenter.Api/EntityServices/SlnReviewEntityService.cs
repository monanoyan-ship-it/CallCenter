using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnReviewEntityService : ISlnReviewEntityService
{
    private readonly AppDbContext _db;

    public SlnReviewEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnReview> GetAllQueryable()
        => _db.SlnReviews.AsQueryable();

    public Task<SlnReview?> GetByIdAsync(int id)
        => _db.SlnReviews.FindAsync(id).AsTask();

    public void Add(SlnReview entity) => _db.SlnReviews.Add(entity);
    public void Update(SlnReview entity) => _db.SlnReviews.Update(entity);
    public void Remove(SlnReview entity) => _db.SlnReviews.Remove(entity);
}
