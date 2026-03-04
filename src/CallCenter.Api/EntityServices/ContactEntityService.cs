using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices;

public class ContactEntityService : IContactEntityService
{
    private readonly AppDbContext _db;

    public ContactEntityService(AppDbContext db) => _db = db;

    public IQueryable<Contact> GetAllQueryable()
        => _db.Contacts.AsQueryable();

    public Task<Contact?> GetByIdAsync(int id)
        => _db.Contacts.FindAsync(id).AsTask();

    public void Add(Contact entity) => _db.Contacts.Add(entity);
    public void AddRange(IEnumerable<Contact> entities) => _db.Contacts.AddRange(entities);
    public void Update(Contact entity) => _db.Contacts.Update(entity);
    public void Remove(Contact entity) => _db.Contacts.Remove(entity);
}
