using CallCenter.Shared.Entities;

namespace CallCenter.Api.EntityServices.Interfaces;

public interface IIvrEntityService
{
    IQueryable<GreetingMessage> GetGreetingsQueryable();
    IQueryable<IvrMenu> GetMenusQueryable();
    IQueryable<IvrMenuOption> GetMenuOptionsQueryable();
    IQueryable<HoldMusic> GetHoldMusicsQueryable();
    IQueryable<BusinessHours> GetBusinessHoursQueryable();
    IQueryable<Holiday> GetHolidaysQueryable();
    Task<GreetingMessage?> GetGreetingByIdAsync(int id, int customerId);
    Task<IvrMenu?> GetMenuByIdAsync(int id, int customerId);
    Task<IvrMenu?> GetMenuByIdWithOptionsAsync(int id, int customerId);
    Task<HoldMusic?> GetHoldMusicByIdAsync(int id, int customerId);
    Task<Holiday?> GetHolidayByIdAsync(int id, int customerId);
    void AddGreeting(GreetingMessage entity);
    void RemoveGreeting(GreetingMessage entity);
    void AddMenu(IvrMenu entity);
    void RemoveMenu(IvrMenu entity);
    void AddMenuOption(IvrMenuOption entity);
    void RemoveMenuOptions(IEnumerable<IvrMenuOption> options);
    void AddHoldMusic(HoldMusic entity);
    void RemoveHoldMusic(HoldMusic entity);
    void AddBusinessHours(BusinessHours entity);
    void RemoveBusinessHoursRange(IEnumerable<BusinessHours> entities);
    void AddHoliday(Holiday entity);
    void RemoveHoliday(Holiday entity);
}
