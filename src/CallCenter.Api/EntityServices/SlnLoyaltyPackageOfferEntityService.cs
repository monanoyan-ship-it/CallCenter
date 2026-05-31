using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class SlnLoyaltyPackageOfferEntityService : ISlnLoyaltyPackageOfferEntityService
{
    private readonly AppDbContext _db;

    public SlnLoyaltyPackageOfferEntityService(AppDbContext db) => _db = db;

    public IQueryable<SlnLoyaltyPackageOffer> GetAllQueryable()
        => _db.SlnLoyaltyPackageOffers.AsQueryable();

    public Task<SlnLoyaltyPackageOffer?> GetByIdAsync(int id)
        => _db.SlnLoyaltyPackageOffers.FindAsync(id).AsTask();

    public void Add(SlnLoyaltyPackageOffer entity) => _db.SlnLoyaltyPackageOffers.Add(entity);
    public void Update(SlnLoyaltyPackageOffer entity) => _db.SlnLoyaltyPackageOffers.Update(entity);
    public void Remove(SlnLoyaltyPackageOffer entity) => _db.SlnLoyaltyPackageOffers.Remove(entity);
}
