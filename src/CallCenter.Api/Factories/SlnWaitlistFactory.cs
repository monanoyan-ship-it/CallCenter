using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnWaitlistFactory : ISlnWaitlistFactory
{
    private readonly ISlnWaitlistEntryEntityService _waitlistEs;
    private readonly ISlnBranchEntityService _branches;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly IUnitOfWork _uow;

    public SlnWaitlistFactory(
        ISlnWaitlistEntryEntityService waitlistEs,
        ISlnBranchEntityService branches,
        ICustomerPersonnelEntityService personnel,
        IUnitOfWork uow)
    {
        _waitlistEs = waitlistEs;
        _branches = branches;
        _personnel = personnel;
        _uow = uow;
    }

    public async Task<List<SlnWaitlistEntryDto>> GetEntriesAsync(int customerId, DateTime? date = null, int? branchId = null)
    {
        var query = _waitlistEs.GetAllQueryable()
            .Where(w => w.CustomerId == customerId)
            .Include(w => w.SlnClient)
            .Include(w => w.Service)
            .Include(w => w.Branch)
            .Include(w => w.PreferredPersonnel).ThenInclude(p => p!.User)
            .AsQueryable();

        if (date.HasValue)
            query = query.Where(w => w.PreferredDate.Date == date.Value.Date);

        if (branchId.HasValue)
            query = query.Where(w => w.BranchId == branchId.Value);

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => MapToDto(w))
            .ToListAsync();
    }

    public async Task<SlnWaitlistEntryDto?> GetEntryAsync(int id, int customerId)
    {
        var entry = await _waitlistEs.GetAllQueryable()
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
            BranchId = null, // Salon panelinden manuel ekleme — sonradan UpdateEntry ile atanabilir; public tarafta JoinWaitlistAsync slug'tan dolduruyor
            SlnClientId = dto.SlnClientId,
            ServiceId = dto.ServiceId,
            PreferredPersonnelId = dto.PreferredPersonnelId,
            PreferredDate = dto.PreferredDate,
            PreferredTimeSlot = dto.PreferredTimeSlot,
            Notes = dto.Notes,
            StatusId = 1
        };
        _waitlistEs.Add(entry);
        await _uow.SaveChangesAsync();
        return (await GetEntryAsync(entry.Id, customerId))!;
    }

    public async Task<(bool Success, string? Error)> UpdateEntryAsync(int id, SlnWaitlistEntryUpdateDto dto, int customerId)
    {
        var entry = await _waitlistEs.GetAllQueryable().FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);
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
        var entry = await _waitlistEs.GetAllQueryable().FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);
        if (entry == null) return (false, "Kayit bulunamadi");

        entry.StatusId = statusId;
        if (statusId == 2) entry.NotifiedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteEntryAsync(int id, int customerId)
    {
        var entry = await _waitlistEs.GetAllQueryable().FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);
        if (entry == null) return (false, "Kayit bulunamadi");

        _waitlistEs.Remove(entry);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static SlnWaitlistEntryDto MapToDto(SlnWaitlistEntry w) => new()
    {
        Id = w.Id,
        SlnClientId = w.SlnClientId,
        ClientName = w.SlnClient?.FullName ?? "",
        ClientPhone = w.SlnClient?.Phone,
        BranchId = w.BranchId,
        BranchName = w.Branch?.Name,
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

    public async Task<object> NormalizeBranchesAsync(int customerId)
    {
        var hqBranch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter);
        if (hqBranch == null)
            return new { updated = 0, error = "Merkez sube bulunamadi." };

        var orphans = await _waitlistEs.GetAllQueryable()
            .Where(w => w.CustomerId == customerId && w.BranchId == null)
            .ToListAsync();

        int viaPersonnel = 0, viaHq = 0;
        foreach (var w in orphans)
        {
            int? targetBranchId = null;
            if (w.PreferredPersonnelId.HasValue)
            {
                targetBranchId = await _personnel.GetAllQueryable()
                    .Where(p => p.Id == w.PreferredPersonnelId.Value)
                    .Select(p => p.BranchId).FirstOrDefaultAsync();
            }
            if (targetBranchId.HasValue) { w.BranchId = targetBranchId; viaPersonnel++; }
            else { w.BranchId = hqBranch.Id; viaHq++; }
        }

        await _uow.SaveChangesAsync();
        return new { updated = orphans.Count, viaPersonnel, viaHq, hqBranchName = hqBranch.Name };
    }
}
