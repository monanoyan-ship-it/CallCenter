using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.EntityServices;

public class IvrEntityService : IIvrEntityService
{
    private readonly AppDbContext _db;

    public IvrEntityService(AppDbContext db) => _db = db;

    public IQueryable<GreetingMessage> GetGreetingsQueryable()
        => _db.GreetingMessages.AsQueryable();

    public IQueryable<IvrMenu> GetMenusQueryable()
        => _db.IvrMenus.AsQueryable();

    public IQueryable<IvrMenuOption> GetMenuOptionsQueryable()
        => _db.IvrMenuOptions.AsQueryable();

    public IQueryable<HoldMusic> GetHoldMusicsQueryable()
        => _db.HoldMusics.AsQueryable();

    public IQueryable<BusinessHours> GetBusinessHoursQueryable()
        => _db.BusinessHours.AsQueryable();

    public IQueryable<Holiday> GetHolidaysQueryable()
        => _db.Holidays.AsQueryable();

    public Task<GreetingMessage?> GetGreetingByIdAsync(int id, int customerId)
        => _db.GreetingMessages
            .FirstOrDefaultAsync(g => g.Id == id && g.CustomerId == customerId);

    public Task<IvrMenu?> GetMenuByIdAsync(int id, int customerId)
        => _db.IvrMenus
            .FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId);

    public Task<IvrMenu?> GetMenuByIdWithOptionsAsync(int id, int customerId)
        => _db.IvrMenus
            .Include(m => m.Options)
            .FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId);

    public Task<HoldMusic?> GetHoldMusicByIdAsync(int id, int customerId)
        => _db.HoldMusics
            .FirstOrDefaultAsync(h => h.Id == id && h.CustomerId == customerId);

    public Task<Holiday?> GetHolidayByIdAsync(int id, int customerId)
        => _db.Holidays
            .FirstOrDefaultAsync(h => h.Id == id && h.CustomerId == customerId);

    public void AddGreeting(GreetingMessage entity) => _db.GreetingMessages.Add(entity);
    public void RemoveGreeting(GreetingMessage entity) => _db.GreetingMessages.Remove(entity);
    public void AddMenu(IvrMenu entity) => _db.IvrMenus.Add(entity);
    public void RemoveMenu(IvrMenu entity) => _db.IvrMenus.Remove(entity);
    public void AddMenuOption(IvrMenuOption entity) => _db.IvrMenuOptions.Add(entity);
    public void RemoveMenuOptions(IEnumerable<IvrMenuOption> options) => _db.IvrMenuOptions.RemoveRange(options);
    public void AddHoldMusic(HoldMusic entity) => _db.HoldMusics.Add(entity);
    public void RemoveHoldMusic(HoldMusic entity) => _db.HoldMusics.Remove(entity);
    public void AddBusinessHours(BusinessHours entity) => _db.BusinessHours.Add(entity);
    public void RemoveBusinessHoursRange(IEnumerable<BusinessHours> entities) => _db.BusinessHours.RemoveRange(entities);
    public void AddHoliday(Holiday entity) => _db.Holidays.Add(entity);
    public void RemoveHoliday(Holiday entity) => _db.Holidays.Remove(entity);
}
