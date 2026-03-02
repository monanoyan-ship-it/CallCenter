using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class IvrService : IIvrService
{
    private readonly AppDbContext _db;

    public IvrService(AppDbContext db)
    {
        _db = db;
    }

    private static readonly string[] DayNames = { "Pazar", "Pazartesi", "Sali", "Carsamba", "Persembe", "Cuma", "Cumartesi" };

    // ═══════════════════════════════════════════
    // GREETING MESSAGES
    // ═══════════════════════════════════════════

    public async Task<List<GreetingMessageDto>> GetGreetingsAsync(int customerId)
    {
        return await _db.GreetingMessages
            .Where(g => g.CustomerId == customerId)
            .OrderBy(g => g.Type).ThenBy(g => g.Name)
            .Select(g => new GreetingMessageDto
            {
                Id = g.Id,
                Uid = g.Uid,
                Name = g.Name,
                Type = g.Type,
                AudioFileName = g.AudioFileName,
                DurationSeconds = g.DurationSeconds,
                IsActive = g.IsActive,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<GreetingMessageDto?> GetGreetingAsync(int id, int customerId)
    {
        return await _db.GreetingMessages
            .Where(g => g.Id == id && g.CustomerId == customerId)
            .Select(g => new GreetingMessageDto
            {
                Id = g.Id, Uid = g.Uid, Name = g.Name, Type = g.Type,
                AudioFileName = g.AudioFileName, DurationSeconds = g.DurationSeconds,
                IsActive = g.IsActive, CreatedAt = g.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<GreetingMessageDto> CreateGreetingAsync(int customerId, CreateGreetingMessageRequest request, string audioFilePath, string? audioFileName)
    {
        var entity = new GreetingMessage
        {
            CustomerId = customerId,
            Name = request.Name,
            Type = request.Type,
            AudioFilePath = audioFilePath,
            AudioFileName = audioFileName
        };
        _db.GreetingMessages.Add(entity);
        await _db.SaveChangesAsync();

        return new GreetingMessageDto
        {
            Id = entity.Id, Uid = entity.Uid, Name = entity.Name, Type = entity.Type,
            AudioFileName = entity.AudioFileName, IsActive = true, CreatedAt = entity.CreatedAt
        };
    }

    public async Task<(bool, string?)> UpdateGreetingAsync(int id, int customerId, UpdateGreetingMessageRequest request)
    {
        var entity = await _db.GreetingMessages.FirstOrDefaultAsync(g => g.Id == id && g.CustomerId == customerId);
        if (entity == null) return (false, "Karsilama mesaji bulunamadi");

        if (request.Name != null) entity.Name = request.Name;
        if (request.Type != null) entity.Type = request.Type;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool, string?)> DeleteGreetingAsync(int id, int customerId)
    {
        var entity = await _db.GreetingMessages.FirstOrDefaultAsync(g => g.Id == id && g.CustomerId == customerId);
        if (entity == null) return (false, "Karsilama mesaji bulunamadi");

        _db.GreetingMessages.Remove(entity);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════════
    // IVR MENUS
    // ═══════════════════════════════════════════

    public async Task<List<IvrMenuDto>> GetIvrMenusAsync(int customerId)
    {
        return await _db.IvrMenus
            .Where(m => m.CustomerId == customerId)
            .Include(m => m.GreetingMessage)
            .Include(m => m.Options).ThenInclude(o => o.TargetQueue)
            .OrderBy(m => m.Name)
            .Select(m => MapIvrMenu(m))
            .ToListAsync();
    }

    public async Task<IvrMenuDto?> GetIvrMenuAsync(int id, int customerId)
    {
        var menu = await _db.IvrMenus
            .Where(m => m.Id == id && m.CustomerId == customerId)
            .Include(m => m.GreetingMessage)
            .Include(m => m.Options).ThenInclude(o => o.TargetQueue)
            .FirstOrDefaultAsync();
        return menu == null ? null : MapIvrMenu(menu);
    }

    public async Task<IvrMenuDto> CreateIvrMenuAsync(int customerId, CreateIvrMenuRequest request)
    {
        var menu = new IvrMenu
        {
            CustomerId = customerId,
            Name = request.Name,
            GreetingMessageId = request.GreetingMessageId,
            MaxRetries = request.MaxRetries,
            TimeoutSeconds = request.TimeoutSeconds
        };

        foreach (var opt in request.Options)
        {
            menu.Options.Add(new IvrMenuOption
            {
                Digit = opt.Digit,
                ActionType = opt.ActionType,
                TargetQueueId = opt.TargetQueueId,
                TargetExtension = opt.TargetExtension,
                TargetIvrMenuId = opt.TargetIvrMenuId,
                TargetGreetingMessageId = opt.TargetGreetingMessageId,
                Label = opt.Label,
                SortOrder = opt.SortOrder
            });
        }

        _db.IvrMenus.Add(menu);
        await _db.SaveChangesAsync();
        return MapIvrMenu(menu);
    }

    public async Task<(bool, string?)> UpdateIvrMenuAsync(int id, int customerId, UpdateIvrMenuRequest request)
    {
        var menu = await _db.IvrMenus
            .Include(m => m.Options)
            .FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId);
        if (menu == null) return (false, "IVR menusu bulunamadi");

        if (request.Name != null) menu.Name = request.Name;
        if (request.GreetingMessageId.HasValue) menu.GreetingMessageId = request.GreetingMessageId;
        if (request.MaxRetries.HasValue) menu.MaxRetries = request.MaxRetries.Value;
        if (request.TimeoutSeconds.HasValue) menu.TimeoutSeconds = request.TimeoutSeconds.Value;
        if (request.IsActive.HasValue) menu.IsActive = request.IsActive.Value;

        if (request.Options != null)
        {
            // Mevcut secenekleri sil, yenilerini ekle
            _db.IvrMenuOptions.RemoveRange(menu.Options);
            foreach (var opt in request.Options)
            {
                menu.Options.Add(new IvrMenuOption
                {
                    Digit = opt.Digit,
                    ActionType = opt.ActionType,
                    TargetQueueId = opt.TargetQueueId,
                    TargetExtension = opt.TargetExtension,
                    TargetIvrMenuId = opt.TargetIvrMenuId,
                    TargetGreetingMessageId = opt.TargetGreetingMessageId,
                    Label = opt.Label,
                    SortOrder = opt.SortOrder
                });
            }
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool, string?)> DeleteIvrMenuAsync(int id, int customerId)
    {
        var menu = await _db.IvrMenus.FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId);
        if (menu == null) return (false, "IVR menusu bulunamadi");

        _db.IvrMenus.Remove(menu);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private static IvrMenuDto MapIvrMenu(IvrMenu m) => new()
    {
        Id = m.Id,
        Uid = m.Uid,
        Name = m.Name,
        GreetingMessageId = m.GreetingMessageId,
        GreetingMessageName = m.GreetingMessage?.Name,
        MaxRetries = m.MaxRetries,
        TimeoutSeconds = m.TimeoutSeconds,
        IsActive = m.IsActive,
        Options = m.Options.OrderBy(o => o.SortOrder).Select(o => new IvrMenuOptionDto
        {
            Id = o.Id,
            Digit = o.Digit,
            ActionType = o.ActionType,
            TargetQueueId = o.TargetQueueId,
            TargetQueueName = o.TargetQueue?.Name,
            TargetExtension = o.TargetExtension,
            TargetIvrMenuId = o.TargetIvrMenuId,
            TargetGreetingMessageId = o.TargetGreetingMessageId,
            Label = o.Label,
            SortOrder = o.SortOrder
        }).ToList()
    };

    // ═══════════════════════════════════════════
    // HOLD MUSIC
    // ═══════════════════════════════════════════

    public async Task<List<HoldMusicDto>> GetHoldMusicsAsync(int customerId)
    {
        return await _db.HoldMusics
            .Where(h => h.CustomerId == customerId)
            .Include(h => h.Queue)
            .OrderBy(h => h.Name)
            .Select(h => new HoldMusicDto
            {
                Id = h.Id, Uid = h.Uid, Name = h.Name,
                QueueId = h.QueueId, QueueName = h.Queue != null ? h.Queue.Name : null,
                AudioFileName = h.AudioFileName, DurationSeconds = h.DurationSeconds,
                IsDefault = h.IsDefault, IsActive = h.IsActive
            })
            .ToListAsync();
    }

    public async Task<HoldMusicDto> CreateHoldMusicAsync(int customerId, CreateHoldMusicRequest request, string audioFilePath, string? audioFileName)
    {
        var entity = new HoldMusic
        {
            CustomerId = customerId,
            Name = request.Name,
            QueueId = request.QueueId,
            AudioFilePath = audioFilePath,
            AudioFileName = audioFileName,
            IsDefault = request.IsDefault
        };

        // IsDefault ise diger default'lari kaldir
        if (request.IsDefault)
        {
            var others = await _db.HoldMusics
                .Where(h => h.CustomerId == customerId && h.IsDefault)
                .ToListAsync();
            foreach (var o in others) o.IsDefault = false;
        }

        _db.HoldMusics.Add(entity);
        await _db.SaveChangesAsync();

        return new HoldMusicDto
        {
            Id = entity.Id, Uid = entity.Uid, Name = entity.Name,
            AudioFileName = audioFileName, IsDefault = entity.IsDefault, IsActive = true
        };
    }

    public async Task<(bool, string?)> DeleteHoldMusicAsync(int id, int customerId)
    {
        var entity = await _db.HoldMusics.FirstOrDefaultAsync(h => h.Id == id && h.CustomerId == customerId);
        if (entity == null) return (false, "Bekleme muzigi bulunamadi");

        _db.HoldMusics.Remove(entity);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════════
    // BUSINESS HOURS
    // ═══════════════════════════════════════════

    public async Task<List<BusinessHoursDto>> GetBusinessHoursAsync(int customerId)
    {
        var hours = await _db.BusinessHours
            .Where(b => b.CustomerId == customerId)
            .OrderBy(b => b.DayOfWeek)
            .ToListAsync();

        // 7 gun yoksa varsayilan olarak olustur
        if (hours.Count == 0)
        {
            return Enumerable.Range(0, 7).Select(d => new BusinessHoursDto
            {
                DayOfWeek = d,
                DayName = DayNames[d],
                StartTime = d is >= 1 and <= 5 ? new TimeSpan(9, 0, 0) : TimeSpan.Zero,
                EndTime = d is >= 1 and <= 5 ? new TimeSpan(18, 0, 0) : TimeSpan.Zero,
                IsWorkday = d is >= 1 and <= 5
            }).ToList();
        }

        return hours.Select(b => new BusinessHoursDto
        {
            Id = b.Id,
            DayOfWeek = b.DayOfWeek,
            DayName = DayNames[b.DayOfWeek],
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            IsWorkday = b.IsWorkday
        }).ToList();
    }

    public async Task SetBusinessHoursAsync(int customerId, SetBusinessHoursRequest request)
    {
        // Mevcut kayitlari sil
        var existing = await _db.BusinessHours.Where(b => b.CustomerId == customerId).ToListAsync();
        _db.BusinessHours.RemoveRange(existing);

        // Yenilerini ekle
        foreach (var day in request.Days)
        {
            _db.BusinessHours.Add(new BusinessHours
            {
                CustomerId = customerId,
                DayOfWeek = day.DayOfWeek,
                StartTime = day.StartTime,
                EndTime = day.EndTime,
                IsWorkday = day.IsWorkday
            });
        }

        await _db.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════
    // HOLIDAYS
    // ═══════════════════════════════════════════

    public async Task<List<HolidayDto>> GetHolidaysAsync(int customerId)
    {
        return await _db.Holidays
            .Where(h => h.CustomerId == customerId)
            .Include(h => h.GreetingMessage)
            .OrderBy(h => h.Date)
            .Select(h => new HolidayDto
            {
                Id = h.Id,
                Date = h.Date,
                Name = h.Name,
                GreetingMessageId = h.GreetingMessageId,
                GreetingMessageName = h.GreetingMessage != null ? h.GreetingMessage.Name : null,
                IsRecurring = h.IsRecurring
            })
            .ToListAsync();
    }

    public async Task<HolidayDto> CreateHolidayAsync(int customerId, CreateHolidayRequest request)
    {
        var entity = new Holiday
        {
            CustomerId = customerId,
            Date = request.Date,
            Name = request.Name,
            GreetingMessageId = request.GreetingMessageId,
            IsRecurring = request.IsRecurring
        };
        _db.Holidays.Add(entity);
        await _db.SaveChangesAsync();

        return new HolidayDto
        {
            Id = entity.Id, Date = entity.Date, Name = entity.Name,
            GreetingMessageId = entity.GreetingMessageId, IsRecurring = entity.IsRecurring
        };
    }

    public async Task<(bool, string?)> DeleteHolidayAsync(int id, int customerId)
    {
        var entity = await _db.Holidays.FirstOrDefaultAsync(h => h.Id == id && h.CustomerId == customerId);
        if (entity == null) return (false, "Tatil bulunamadi");

        _db.Holidays.Remove(entity);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════════
    // INCOMING CALL PIPELINE CONFIG
    // ═══════════════════════════════════════════

    public async Task<IncomingCallConfigDto> GetIncomingCallConfigAsync(int customerId, int? queueId = null)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var currentTime = now.TimeOfDay;
        var dayOfWeek = (int)now.DayOfWeek;

        var config = new IncomingCallConfigDto();

        // 1. Tatil kontrolu
        var holiday = await _db.Holidays
            .Include(h => h.GreetingMessage)
            .Where(h => h.CustomerId == customerId &&
                        (h.Date == today || (h.IsRecurring && h.Date.Month == today.Month && h.Date.Day == today.Day)))
            .FirstOrDefaultAsync();

        if (holiday != null)
        {
            config.IsHoliday = true;
            config.HolidayName = holiday.Name;
            config.IsWithinBusinessHours = false;

            // Tatile ozel karsilama veya genel tatil karsilamasi
            var greetingPath = holiday.GreetingMessage?.AudioFilePath;
            if (string.IsNullOrEmpty(greetingPath))
            {
                var holidayGreeting = await _db.GreetingMessages
                    .FirstOrDefaultAsync(g => g.CustomerId == customerId && g.Type == "holiday" && g.IsActive);
                greetingPath = holidayGreeting?.AudioFilePath;
            }
            config.GreetingAudioPath = greetingPath;

            return config;
        }

        // 2. Mesai saati kontrolu
        var businessHour = await _db.BusinessHours
            .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.DayOfWeek == dayOfWeek);

        if (businessHour != null && businessHour.IsWorkday)
        {
            config.IsWithinBusinessHours = currentTime >= businessHour.StartTime && currentTime <= businessHour.EndTime;
        }
        else if (businessHour == null)
        {
            // Varsayilan: Pazartesi-Cuma 09-18
            config.IsWithinBusinessHours = dayOfWeek >= 1 && dayOfWeek <= 5 &&
                                           currentTime >= new TimeSpan(9, 0, 0) && currentTime <= new TimeSpan(18, 0, 0);
        }

        // 3. Karsilama mesaji
        var greetingType = config.IsWithinBusinessHours ? "business_hours" : "after_hours";
        var greeting = await _db.GreetingMessages
            .FirstOrDefaultAsync(g => g.CustomerId == customerId && g.Type == greetingType && g.IsActive);
        config.GreetingAudioPath = greeting?.AudioFilePath;

        // 4. IVR menu (sadece mesai saatlerinde)
        if (config.IsWithinBusinessHours)
        {
            var ivrMenu = await _db.IvrMenus
                .Include(m => m.Options).ThenInclude(o => o.TargetQueue)
                .FirstOrDefaultAsync(m => m.CustomerId == customerId && m.IsActive);

            if (ivrMenu != null)
            {
                config.HasIvrMenu = true;
                config.IvrMenuId = ivrMenu.Id;
                config.IvrOptions = ivrMenu.Options.OrderBy(o => o.SortOrder).Select(o => new IvrMenuOptionDto
                {
                    Id = o.Id, Digit = o.Digit, ActionType = o.ActionType,
                    TargetQueueId = o.TargetQueueId, TargetQueueName = o.TargetQueue?.Name,
                    TargetExtension = o.TargetExtension, TargetIvrMenuId = o.TargetIvrMenuId,
                    Label = o.Label, SortOrder = o.SortOrder
                }).ToList();
            }
        }

        // 5. Bekleme muzigi
        var holdMusic = queueId.HasValue
            ? await _db.HoldMusics.FirstOrDefaultAsync(h => h.CustomerId == customerId && h.QueueId == queueId && h.IsActive)
            : null;
        holdMusic ??= await _db.HoldMusics.FirstOrDefaultAsync(h => h.CustomerId == customerId && h.IsDefault && h.IsActive);
        config.HoldMusicAudioPath = holdMusic?.AudioFilePath;

        return config;
    }
}
