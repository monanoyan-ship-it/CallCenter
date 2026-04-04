using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnWaitlistFactory : ISlnWaitlistFactory
{
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;

    public SlnWaitlistFactory(AppDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<List<SlnWaitlistEntryDto>> GetEntriesAsync(int customerId, DateTime? date = null)
    {
        var query = _db.SlnWaitlistEntries
            .Where(w => w.CustomerId == customerId)
            .Include(w => w.SlnClient)
            .Include(w => w.Service)
            .Include(w => w.PreferredPersonnel).ThenInclude(p => p!.User)
            .AsQueryable();

        if (date.HasValue)
            query = query.Where(w => w.PreferredDate.Date == date.Value.Date);

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => MapToDto(w))
            .ToListAsync();
    }

    public async Task<SlnWaitlistEntryDto?> GetEntryAsync(int id, int customerId)
    {
        var entry = await _db.SlnWaitlistEntries
            .Include(w => w.SlnClient)
            .Include(w => w.Service)
            .Include(w => w.PreferredPersonnel).ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);
        return entry != null ? MapToDto(entry) : null;
    }

    public async Task<SlnWaitlistEntryDto> CreateEntryAsync(SlnWaitlistEntryCreateDto dto, int customerId)
    {
        var entry = new SlnWaitlistEntry
        {
            CustomerId = customerId,
            SlnClientId = dto.SlnClientId,
            ServiceId = dto.ServiceId,
            PreferredPersonnelId = dto.PreferredPersonnelId,
            PreferredDate = dto.PreferredDate,
            PreferredTimeSlot = dto.PreferredTimeSlot,
            Notes = dto.Notes,
            StatusId = 1
        };
        _db.SlnWaitlistEntries.Add(entry);
        await _uow.SaveChangesAsync();
        return (await GetEntryAsync(entry.Id, customerId))!;
    }

    public async Task<(bool Success, string? Error)> UpdateEntryAsync(int id, SlnWaitlistEntryUpdateDto dto, int customerId)
    {
        var entry = await _db.SlnWaitlistEntries.FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);
        if (entry == null) return (false, "Kayit bulunamadi");

        entry.SlnClientId = dto.SlnClientId;
        entry.ServiceId = dto.ServiceId;
        entry.PreferredPersonnelId = dto.PreferredPersonnelId;
        entry.PreferredDate = dto.PreferredDate;
        entry.PreferredTimeSlot = dto.PreferredTimeSlot;
        entry.Notes = dto.Notes;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateStatusAsync(int id, int statusId, int customerId)
    {
        var entry = await _db.SlnWaitlistEntries.FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);
        if (entry == null) return (false, "Kayit bulunamadi");

        entry.StatusId = statusId;
        if (statusId == 2) entry.NotifiedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteEntryAsync(int id, int customerId)
    {
        var entry = await _db.SlnWaitlistEntries.FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);
        if (entry == null) return (false, "Kayit bulunamadi");

        _db.SlnWaitlistEntries.Remove(entry);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static SlnWaitlistEntryDto MapToDto(SlnWaitlistEntry w) => new()
    {
        Id = w.Id,
        SlnClientId = w.SlnClientId,
        ClientName = w.SlnClient?.FullName ?? "",
        ClientPhone = w.SlnClient?.Phone,
        ServiceId = w.ServiceId,
        ServiceName = w.Service?.Name ?? "",
        PreferredPersonnelId = w.PreferredPersonnelId,
        PreferredPersonnelName = w.PreferredPersonnel?.User?.FullName,
        PreferredDate = w.PreferredDate,
        PreferredTimeSlot = w.PreferredTimeSlot,
        Notes = w.Notes,
        StatusId = w.StatusId,
        NotifiedAt = w.NotifiedAt,
        CreatedAt = w.CreatedAt
    };
}
