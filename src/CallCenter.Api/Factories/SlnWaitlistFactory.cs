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
    private readonly ISlnClientEntityService _clients;
    private readonly ISlnAppointmentFactory _appointments;
    private readonly ISlnServiceEntityService _services;
    private readonly ISlnBranchEntityService _branches;
    private readonly ICustomerPersonnelEntityService _personnel;
    private readonly IUnitOfWork _uow;

    public SlnWaitlistFactory(
        ISlnWaitlistEntryEntityService waitlistEs,
        ISlnClientEntityService clients,
        ISlnAppointmentFactory appointments,
        ISlnServiceEntityService services,
        ISlnBranchEntityService branches,
        ICustomerPersonnelEntityService personnel,
        IUnitOfWork uow)
    {
        _waitlistEs = waitlistEs;
        _clients = clients;
        _appointments = appointments;
        _services = services;
        _branches = branches;
        _personnel = personnel;
        _uow = uow;
    }

    public async Task<List<SlnWaitlistEntryDto>> GetEntriesAsync(int customerId, DateTime? date = null, int? branchId = null, string? scope = null, string? search = null)
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

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(w =>
                (w.SlnClient != null && (
                    w.SlnClient.FullName.ToLower().Contains(q) ||
                    (w.SlnClient.Phone != null && w.SlnClient.Phone.ToLower().Contains(q)) ||
                    (w.SlnClient.Email != null && w.SlnClient.Email.ToLower().Contains(q)))) ||
                (w.Service != null && w.Service.Name.ToLower().Contains(q)) ||
                (w.PreferredPersonnel != null && w.PreferredPersonnel.User != null && w.PreferredPersonnel.User.FullName.ToLower().Contains(q)) ||
                (w.Notes != null && w.Notes.ToLower().Contains(q)));
        }

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
            .Include(w => w.Branch)
            .FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);
        return entry != null ? MapToDto(entry) : null;
    }

    public async Task<(bool Success, string? Error, SlnWaitlistEntryDto? Entry)> CreateEntryAsync(SlnWaitlistEntryCreateDto dto, int customerId, int? branchScopeId = null)
    {
        var validation = await ValidateLookupOwnershipAsync(dto, customerId, branchScopeId);
        if (!validation.Success) return (false, validation.Error, null);

        var branchId = await ResolveBranchIdAsync(customerId, dto.PreferredPersonnelId, branchScopeId, dto.BranchId);
        var preferredDate = ToDateOnlyUtc(dto.PreferredDate);
        var duplicateExists = await ActiveDuplicateExistsAsync(customerId, dto.SlnClientId, dto.ServiceId, preferredDate);
        if (duplicateExists)
            return (false, "Bu tarih icin zaten aktif bekleme kaydi var", null);

        var entry = new SlnWaitlistEntry
        {
            CustomerId = customerId,
            BranchId = branchId,
            SlnClientId = dto.SlnClientId,
            ServiceId = dto.ServiceId,
            PreferredPersonnelId = dto.PreferredPersonnelId,
            PreferredDate = preferredDate,
            PreferredTimeSlot = dto.PreferredTimeSlot,
            Notes = dto.Notes,
            StatusId = SlnWaitlistStatuses.Ids.Waiting
        };
        _waitlistEs.Add(entry);
        await _uow.SaveChangesAsync();
        return (true, null, (await GetEntryAsync(entry.Id, customerId))!);
    }

    public async Task<(bool Success, string? Error)> UpdateEntryAsync(int id, SlnWaitlistEntryUpdateDto dto, int customerId, int? branchScopeId = null)
    {
        var entry = await _waitlistEs.GetAllQueryable().FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);
        if (entry == null) return (false, "Kayit bulunamadi");
        if (branchScopeId.HasValue && entry.BranchId.HasValue && entry.BranchId.Value != branchScopeId.Value)
            return (false, "Bu kayit icin yetkiniz yok");
        var validation = await ValidateLookupOwnershipAsync(dto, customerId, branchScopeId);
        if (!validation.Success) return (false, validation.Error);

        entry.BranchId = await ResolveBranchIdAsync(customerId, dto.PreferredPersonnelId, branchScopeId, dto.BranchId);
        entry.SlnClientId = dto.SlnClientId;
        entry.ServiceId = dto.ServiceId;
        entry.PreferredPersonnelId = dto.PreferredPersonnelId;
        entry.PreferredDate = ToDateOnlyUtc(dto.PreferredDate);
        entry.PreferredTimeSlot = dto.PreferredTimeSlot;
        entry.Notes = dto.Notes;
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private static DateTime ToDateOnlyUtc(DateTime value)
        => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private Task<bool> ActiveDuplicateExistsAsync(int customerId, int slnClientId, int serviceId, DateTime preferredDate)
    {
        var activeStatusIds = new[]
        {
            SlnWaitlistStatuses.Ids.Waiting,
            SlnWaitlistStatuses.Ids.Notified,
            SlnWaitlistStatuses.Ids.AppointmentBooked
        };
        return _waitlistEs.GetAllQueryable().AnyAsync(w =>
            w.CustomerId == customerId &&
            w.SlnClientId == slnClientId &&
            w.ServiceId == serviceId &&
            w.PreferredDate == preferredDate &&
            activeStatusIds.Contains(w.StatusId));
    }

    private async Task<(bool Success, string? Error)> ValidateLookupOwnershipAsync(SlnWaitlistEntryCreateDto dto, int customerId, int? branchScopeId)
    {
        if (dto.SlnClientId <= 0)
            return (false, "Musteri zorunludur");
        if (dto.ServiceId <= 0)
            return (false, "Hizmet zorunludur");
        if (branchScopeId.HasValue && dto.BranchId.HasValue && dto.BranchId.Value != branchScopeId.Value)
            return (false, "Bu sube icin yetkiniz yok");

        var clientExists = await _clients.GetAllQueryable()
            .AnyAsync(c => c.Id == dto.SlnClientId && c.CustomerId == customerId && c.IsActive);
        if (!clientExists)
            return (false, "Musteri bulunamadi");

        var serviceExists = await _services.GetAllQueryable()
            .AnyAsync(s => s.Id == dto.ServiceId && s.CustomerId == customerId && s.IsActive);
        if (!serviceExists)
            return (false, "Hizmet bulunamadi");

        if (dto.PreferredPersonnelId.HasValue)
        {
            var personnelExists = await _personnel.GetAllQueryable()
                .AnyAsync(p => p.Id == dto.PreferredPersonnelId.Value && p.CustomerId == customerId && p.IsActive);
            if (!personnelExists)
                return (false, "Personel bulunamadi");
        }

        var requestedBranchId = branchScopeId ?? dto.BranchId;
        if (requestedBranchId.HasValue)
        {
            var branchExists = await _branches.GetAllQueryable()
                .AnyAsync(b => b.Id == requestedBranchId.Value && b.CustomerId == customerId && b.IsActive);
            if (!branchExists)
                return (false, "Sube bulunamadi");
        }

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

    public async Task<(bool Success, string? Error, SlnWaitlistConversionDto? Result)> ConvertToAppointmentAsync(int id, SlnWaitlistConvertToAppointmentDto dto, int userId, int customerId, int? branchScopeId = null)
    {
        if (dto.PersonnelId <= 0)
            return (false, "Personel zorunludur", null);
        if (dto.StartTime == default)
            return (false, "Randevu zamani zorunludur", null);
        if (branchScopeId.HasValue && dto.BranchId.HasValue && dto.BranchId.Value != branchScopeId.Value)
            return (false, "Bu sube icin yetkiniz yok", null);

        await using var tx = await _uow.BeginTransactionAsync();

        var entry = await _waitlistEs.GetAllQueryable()
            .Include(w => w.SlnClient)
            .Include(w => w.Service)
            .Include(w => w.Branch)
            .Include(w => w.PreferredPersonnel).ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);

        if (entry == null)
            return (false, "Kayit bulunamadi", null);
        if (branchScopeId.HasValue && entry.BranchId.HasValue && entry.BranchId.Value != branchScopeId.Value)
            return (false, "Bu kayit icin yetkiniz yok", null);
        if (entry.SlnAppointmentId.HasValue)
            return (false, "Bu bekleme kaydi zaten bir randevuya bagli", null);
        if (entry.StatusId is not (SlnWaitlistStatuses.Ids.Waiting or SlnWaitlistStatuses.Ids.Notified))
            return (false, "Yalnizca bekleyen veya bildirilen kayitlar randevuya donusturulebilir", null);

        var appointmentDto = new SlnAppointmentCreateDto
        {
            SlnClientId = entry.SlnClientId,
            PersonnelId = dto.PersonnelId,
            BranchId = dto.BranchId ?? entry.BranchId,
            ServiceIds = [entry.ServiceId],
            StartTime = dto.StartTime,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? entry.Notes : dto.Notes
        };

        var (appointment, error) = await _appointments.CreateAppointmentAsync(
            appointmentDto,
            userId,
            customerId,
            branchScopeId);
        if (appointment == null)
            return (false, error ?? "Randevu olusturulamadi", null);

        entry.StatusId = SlnWaitlistStatuses.Ids.AppointmentBooked;
        entry.SlnAppointmentId = appointment.Id;
        await _uow.SaveChangesAsync();
        await tx.CommitAsync();

        var waitlistEntry = await GetEntryAsync(entry.Id, customerId) ?? MapToDto(entry);
        return (true, null, new SlnWaitlistConversionDto
        {
            WaitlistEntry = waitlistEntry,
            Appointment = appointment
        });
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
            SlnAppointmentId = w.SlnAppointmentId,
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
