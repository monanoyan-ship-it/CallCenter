using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
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

    public async Task<List<SlnWaitlistEntryDto>> GetEntriesAsync(int customerId, DateTime? date = null, int? branchId = null, string? scope = null)
    {
        var normalizedScope = SlnWaitlistStatuses.NormalizeScope(scope) ?? SlnWaitlistStatuses.ScopeAll;
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

        if (normalizedScope == SlnWaitlistStatuses.ScopeActive)
        {
            var activeStatusIds = new[]
            {
                SlnWaitlistStatuses.Ids.Waiting,
                SlnWaitlistStatuses.Ids.Notified,
                SlnWaitlistStatuses.Ids.AppointmentBooked
            };
            query = query.Where(w => activeStatusIds.Contains(w.StatusId));
        }
        else if (normalizedScope == SlnWaitlistStatuses.ScopeArchive)
        {
            var archivedStatusIds = new[]
            {
                SlnWaitlistStatuses.Ids.Cancelled,
                SlnWaitlistStatuses.Ids.Completed
            };
            query = query.Where(w => archivedStatusIds.Contains(w.StatusId));
        }

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

    public async Task<SlnWaitlistEntryDto> CreateEntryAsync(SlnWaitlistEntryCreateDto dto, int customerId, int? branchScopeId = null)
    {
        var branchId = await ResolveBranchIdAsync(customerId, dto.PreferredPersonnelId, branchScopeId, dto.BranchId);
        var entry = new SlnWaitlistEntry
        {
            CustomerId = customerId,
            BranchId = branchId,
            SlnClientId = dto.SlnClientId,
            ServiceId = dto.ServiceId,
            PreferredPersonnelId = dto.PreferredPersonnelId,
            PreferredDate = dto.PreferredDate,
            PreferredTimeSlot = dto.PreferredTimeSlot,
            Notes = dto.Notes,
            StatusId = SlnWaitlistStatuses.Ids.Waiting
        };
        _waitlistEs.Add(entry);
        await _uow.SaveChangesAsync();
        return (await GetEntryAsync(entry.Id, customerId))!;
    }

    public async Task<(bool Success, string? Error)> UpdateEntryAsync(int id, SlnWaitlistEntryUpdateDto dto, int customerId, int? branchScopeId = null)
    {
        var entry = await _waitlistEs.GetAllQueryable().FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);
        if (entry == null) return (false, "Kayit bulunamadi");
        if (branchScopeId.HasValue && entry.BranchId.HasValue && entry.BranchId.Value != branchScopeId.Value)
            return (false, "Bu kayit icin yetkiniz yok");

        entry.BranchId = await ResolveBranchIdAsync(customerId, dto.PreferredPersonnelId, branchScopeId, dto.BranchId);
        entry.SlnClientId = dto.SlnClientId;
        entry.ServiceId = dto.ServiceId;
        entry.PreferredPersonnelId = dto.PreferredPersonnelId;
        entry.PreferredDate = dto.PreferredDate;
        entry.PreferredTimeSlot = dto.PreferredTimeSlot;
        entry.Notes = dto.Notes;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private async Task<int?> ResolveBranchIdAsync(int customerId, int? preferredPersonnelId, int? branchScopeId, int? requestedBranchId)
    {
        if (branchScopeId.HasValue)
            return branchScopeId.Value;

        if (preferredPersonnelId.HasValue)
        {
            var personnelBranchId = await _personnel.GetAllQueryable()
                .Where(p => p.Id == preferredPersonnelId.Value && p.CustomerId == customerId && p.IsActive)
                .Select(p => p.BranchId)
                .FirstOrDefaultAsync();
            if (personnelBranchId.HasValue)
                return personnelBranchId.Value;
        }

        if (requestedBranchId.HasValue)
        {
            var branchExists = await _branches.GetAllQueryable()
                .AnyAsync(b => b.Id == requestedBranchId.Value && b.CustomerId == customerId && b.IsActive);
            if (branchExists)
                return requestedBranchId.Value;
        }

        var hqBranch = await _branches.GetAllQueryable()
            .FirstOrDefaultAsync(b => b.CustomerId == customerId && b.IsHeadquarter && b.IsActive);
        return hqBranch?.Id;
    }

    public async Task<(bool Success, string? Error)> UpdateStatusAsync(int id, int statusId, int customerId)
    {
        var entry = await _waitlistEs.GetAllQueryable().FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);
        if (entry == null) return (false, "Kayit bulunamadi");
        if (!SlnWaitlistStatuses.IsDefined(statusId))
            return (false, "Gecersiz bekleme listesi durumu");
        if (!SlnWaitlistStatuses.CanTransition(entry.StatusId, statusId))
            return (false, "Bu bekleme listesi durum gecisi yapilamaz");
        if (entry.StatusId == statusId)
            return (true, null);

        entry.StatusId = statusId;
        if (statusId == SlnWaitlistStatuses.Ids.Notified && entry.NotifiedAt == null)
            entry.NotifiedAt = DateTime.UtcNow;
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

    private static SlnWaitlistEntryDto MapToDto(SlnWaitlistEntry w)
    {
        var status = SlnWaitlistStatuses.GetById(w.StatusId);
        return new SlnWaitlistEntryDto
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
            StatusName = status?.Description ?? w.StatusId.ToString(),
            StatusSystemName = status?.SystemName ?? "Unknown",
            StatusTranslationKey = status?.NameResourceKey ?? "",
            StatusCssClass = status?.CssClass ?? "bg-secondary",
            IsActive = SlnWaitlistStatuses.IsActive(w.StatusId),
            IsArchived = SlnWaitlistStatuses.IsArchived(w.StatusId),
            IsTerminal = SlnWaitlistStatuses.IsTerminal(w.StatusId),
            NotifiedAt = w.NotifiedAt,
            CreatedAt = w.CreatedAt
        };
    }

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
