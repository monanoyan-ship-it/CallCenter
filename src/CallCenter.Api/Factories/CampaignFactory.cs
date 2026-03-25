using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class CampaignFactory : ICampaignFactory
{
    private readonly ICampaignEntityService _campaignEs;
    private readonly ICampaignContactEntityService _ccEs;
    private readonly ICrmContactEntityService _contactEs;
    private readonly ICustomerPersonnelEntityService _personnelEs;
    private readonly IUnitOfWork _uow;

    public CampaignFactory(
        ICampaignEntityService campaignEs,
        ICampaignContactEntityService ccEs,
        ICrmContactEntityService contactEs,
        ICustomerPersonnelEntityService personnelEs,
        IUnitOfWork uow)
    {
        _campaignEs = campaignEs;
        _ccEs = ccEs;
        _contactEs = contactEs;
        _personnelEs = personnelEs;
        _uow = uow;
    }

    public async Task<List<CampaignDto>> GetCampaignsAsync(int? customerId)
    {
        var query = _campaignEs.GetAllQueryable()
            .Include(c => c.CreatedByPersonnel!)
                .ThenInclude(p => p.User)
            .Include(c => c.CampaignContacts)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);

        var campaigns = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return campaigns.Select(MapToDto).ToList();
    }

    public async Task<CampaignDetailDto?> GetCampaignByUidAsync(Guid uid, int? customerId)
    {
        var query = _campaignEs.GetAllQueryable()
            .Include(c => c.CreatedByPersonnel!)
                .ThenInclude(p => p.User)
            .Include(c => c.CampaignContacts)
                .ThenInclude(cc => cc.CrmContact)
            .Include(c => c.CampaignContacts)
                .ThenInclude(cc => cc.AssignedPersonnel!)
                    .ThenInclude(p => p.User)
            .Where(c => c.Uid == uid);

        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);

        var campaign = await query.FirstOrDefaultAsync();
        if (campaign == null) return null;

        var dto = new CampaignDetailDto
        {
            Id = campaign.Id,
            Uid = campaign.Uid,
            Name = campaign.Name,
            Description = campaign.Description,
            StatusId = campaign.StatusId,
            StatusName = CampaignStatuses.GetById(campaign.StatusId)?.Description ?? "",
            ScheduledDate = campaign.ScheduledDate,
            CreatedByName = campaign.CreatedByPersonnel?.User?.FullName ?? "",
            CreatedAt = campaign.CreatedAt,
            TotalContacts = campaign.CampaignContacts.Count,
            PendingCount = campaign.CampaignContacts.Count(cc => cc.StatusId == CampaignContactStatuses.Ids.Pending),
            ReachedCount = campaign.CampaignContacts.Count(cc => cc.StatusId == CampaignContactStatuses.Ids.Reached),
            NotReachedCount = campaign.CampaignContacts.Count(cc => cc.StatusId == CampaignContactStatuses.Ids.NotReached),
            CancelledCount = campaign.CampaignContacts.Count(cc => cc.StatusId == CampaignContactStatuses.Ids.Cancelled),
            CrmContacts = campaign.CampaignContacts.Select(cc => new CampaignContactDto
            {
                Id = cc.Id,
                CrmContactId = cc.CrmContactId,
                CrmContactName = cc.CrmContact?.FullName ?? "",
                PhoneNumber = cc.CrmContact?.PhoneNumber ?? "",
                Company = cc.CrmContact?.Company,
                AssignedPersonnelId = cc.AssignedPersonnelId,
                AssignedPersonnelName = cc.AssignedPersonnel?.User?.FullName,
                StatusId = cc.StatusId,
                StatusName = CampaignContactStatuses.GetById(cc.StatusId)?.Description ?? "",
                AttemptCount = cc.AttemptCount,
                LastAttemptAt = cc.LastAttemptAt,
                CompletedAt = cc.CompletedAt,
                ResultNotes = cc.ResultNotes
            }).ToList()
        };

        return dto;
    }

    public async Task<CampaignDto> CreateCampaignAsync(CreateCampaignRequest req, int personnelId, int? customerId)
    {
        var campaign = new CallCampaign
        {
            Name = req.Name,
            Description = req.Description,
            ScheduledDate = req.ScheduledDate.HasValue
                ? DateTime.SpecifyKind(req.ScheduledDate.Value, DateTimeKind.Utc)
                : null,
            CreatedByPersonnelId = personnelId,
            CustomerId = customerId ?? 0
        };

        _campaignEs.Add(campaign);
        await _uow.SaveChangesAsync();

        // Reload with navigation
        var created = await _campaignEs.GetAllQueryable()
            .Include(c => c.CreatedByPersonnel!)
                .ThenInclude(p => p.User)
            .FirstAsync(c => c.Id == campaign.Id);

        return MapToDto(created);
    }

    public async Task<(bool Success, string? Error)> UpdateCampaignAsync(Guid uid, UpdateCampaignRequest req, int? customerId)
    {
        var campaign = await GetCampaignByUidAndCustomer(uid, customerId);
        if (campaign == null) return (false, "Kampanya bulunamadi");

        if (req.Name != null) campaign.Name = req.Name;
        if (req.Description != null) campaign.Description = req.Description;
        if (req.StatusId.HasValue) campaign.StatusId = req.StatusId.Value;
        if (req.ScheduledDate.HasValue) campaign.ScheduledDate = DateTime.SpecifyKind(req.ScheduledDate.Value, DateTimeKind.Utc);
        campaign.UpdatedAt = DateTime.UtcNow;

        _campaignEs.Update(campaign);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCampaignAsync(Guid uid, int? customerId)
    {
        var campaign = await _campaignEs.GetAllQueryable()
            .Include(c => c.CampaignContacts)
            .Where(c => c.Uid == uid)
            .FirstOrDefaultAsync();

        if (campaign == null) return (false, "Kampanya bulunamadi");
        if (customerId.HasValue && campaign.CustomerId != customerId.Value) return (false, "Yetki hatasi");

        if (campaign.CampaignContacts.Count > 0)
            _ccEs.DeleteRange(campaign.CampaignContacts);

        _campaignEs.Delete(campaign);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AddContactsAsync(Guid campaignUid, AddCampaignContactsRequest req, int? customerId)
    {
        var campaign = await _campaignEs.GetAllQueryable()
            .Include(c => c.CampaignContacts)
            .Where(c => c.Uid == campaignUid)
            .FirstOrDefaultAsync();

        if (campaign == null) return (false, "Kampanya bulunamadi");
        if (customerId.HasValue && campaign.CustomerId != customerId.Value) return (false, "Yetki hatasi");

        var existingContactIds = campaign.CampaignContacts.Select(cc => cc.CrmContactId).ToHashSet();
        var newItems = req.CrmContactIds
            .Where(cid => !existingContactIds.Contains(cid))
            .Select(cid => new CampaignContact
            {
                CampaignId = campaign.Id,
                CrmContactId = cid
            })
            .ToList();

        if (newItems.Count > 0)
        {
            _ccEs.AddRange(newItems);
            await _uow.SaveChangesAsync();
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveContactAsync(Guid campaignUid, int campaignContactId, int? customerId)
    {
        var campaign = await GetCampaignByUidAndCustomer(campaignUid, customerId);
        if (campaign == null) return (false, "Kampanya bulunamadi");

        var cc = await _ccEs.GetByIdAsync(campaignContactId);
        if (cc == null || cc.CampaignId != campaign.Id) return (false, "Kayit bulunamadi");

        _ccEs.Delete(cc);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AssignContactsAsync(Guid campaignUid, AssignCampaignContactsRequest req, int? customerId)
    {
        var campaign = await GetCampaignByUidAndCustomer(campaignUid, customerId);
        if (campaign == null) return (false, "Kampanya bulunamadi");

        var contacts = await _ccEs.GetAllQueryable()
            .Where(cc => cc.CampaignId == campaign.Id && req.CampaignContactIds.Contains(cc.Id))
            .ToListAsync();

        foreach (var cc in contacts)
        {
            cc.AssignedPersonnelId = req.PersonnelId;
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateContactStatusAsync(Guid campaignUid, int campaignContactId, UpdateCampaignContactStatusRequest req, int? customerId)
    {
        var campaign = await GetCampaignByUidAndCustomer(campaignUid, customerId);
        if (campaign == null) return (false, "Kampanya bulunamadi");

        var cc = await _ccEs.GetByIdAsync(campaignContactId);
        if (cc == null || cc.CampaignId != campaign.Id) return (false, "Kayit bulunamadi");

        cc.StatusId = req.StatusId;
        if (req.ResultNotes != null) cc.ResultNotes = req.ResultNotes;
        cc.AttemptCount++;
        cc.LastAttemptAt = DateTime.UtcNow;

        if (CampaignContactStatuses.FinishedIds.Contains(req.StatusId))
            cc.CompletedAt = DateTime.UtcNow;

        _ccEs.Update(cc);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<MyCallListItemDto>> GetMyCallListAsync(int personnelId)
    {
        var items = await _ccEs.GetAllQueryable()
            .Include(cc => cc.Campaign)
            .Include(cc => cc.CrmContact)
            .Where(cc => cc.AssignedPersonnelId == personnelId
                && cc.Campaign!.StatusId == CampaignStatuses.Ids.Active
                && (cc.StatusId == CampaignContactStatuses.Ids.Pending || cc.StatusId == CampaignContactStatuses.Ids.NotReached))
            .OrderBy(cc => cc.Campaign!.ScheduledDate)
            .ThenBy(cc => cc.Id)
            .ToListAsync();

        return items.Select(cc => new MyCallListItemDto
        {
            CampaignContactId = cc.Id,
            CampaignId = cc.CampaignId,
            CampaignName = cc.Campaign?.Name ?? "",
            CrmContactId = cc.CrmContactId,
            CrmContactName = cc.CrmContact?.FullName ?? "",
            PhoneNumber = cc.CrmContact?.PhoneNumber ?? "",
            Company = cc.CrmContact?.Company,
            StatusId = cc.StatusId,
            StatusName = CampaignContactStatuses.GetById(cc.StatusId)?.Description ?? "",
            AttemptCount = cc.AttemptCount,
            ResultNotes = cc.ResultNotes
        }).ToList();
    }

    public async Task<(bool Success, string? Error)> UpdateMyListItemAsync(int campaignContactId, UpdateCampaignContactStatusRequest req, int personnelId)
    {
        var cc = await _ccEs.GetByIdAsync(campaignContactId);
        if (cc == null) return (false, "Kayit bulunamadi");
        if (cc.AssignedPersonnelId != personnelId) return (false, "Bu kayit size atanmamis");

        cc.StatusId = req.StatusId;
        if (req.ResultNotes != null) cc.ResultNotes = req.ResultNotes;
        cc.AttemptCount++;
        cc.LastAttemptAt = DateTime.UtcNow;

        if (CampaignContactStatuses.FinishedIds.Contains(req.StatusId))
            cc.CompletedAt = DateTime.UtcNow;

        _ccEs.Update(cc);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<PersonnelSimpleDto>> GetOperatorsAsync(int? customerId)
    {
        var query = _personnelEs.GetAllQueryable()
            .Include(p => p.User)
            .Where(p => p.CustomerRoleId == CustomerRoles.Ids.Operator && p.User!.IsActive);

        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);

        var operators = await query.ToListAsync();
        return operators.Select(p => new PersonnelSimpleDto
        {
            Id = p.Id,
            FullName = p.User?.FullName ?? ""
        }).ToList();
    }

    // ─── Helpers ───

    private async Task<CallCampaign?> GetCampaignByUidAndCustomer(Guid uid, int? customerId)
    {
        var query = _campaignEs.GetAllQueryable().Where(c => c.Uid == uid);
        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);
        return await query.FirstOrDefaultAsync();
    }

    private static CampaignDto MapToDto(CallCampaign c) => new()
    {
        Id = c.Id,
        Uid = c.Uid,
        Name = c.Name,
        Description = c.Description,
        StatusId = c.StatusId,
        StatusName = CampaignStatuses.GetById(c.StatusId)?.Description ?? "",
        ScheduledDate = c.ScheduledDate,
        CreatedByName = c.CreatedByPersonnel?.User?.FullName ?? "",
        CreatedAt = c.CreatedAt,
        TotalContacts = c.CampaignContacts?.Count ?? 0,
        PendingCount = c.CampaignContacts?.Count(cc => cc.StatusId == CampaignContactStatuses.Ids.Pending) ?? 0,
        ReachedCount = c.CampaignContacts?.Count(cc => cc.StatusId == CampaignContactStatuses.Ids.Reached) ?? 0,
        NotReachedCount = c.CampaignContacts?.Count(cc => cc.StatusId == CampaignContactStatuses.Ids.NotReached) ?? 0,
        CancelledCount = c.CampaignContacts?.Count(cc => cc.StatusId == CampaignContactStatuses.Ids.Cancelled) ?? 0
    };
}
