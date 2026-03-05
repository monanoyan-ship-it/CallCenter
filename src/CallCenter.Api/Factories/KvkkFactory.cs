using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class KvkkFactory : IKvkkFactory
{
    private readonly IConsentRecordEntityService _consentEs;
    private readonly IDataSubjectRequestEntityService _requestEs;
    private readonly IDataBreachEntityService _breachEs;
    private readonly IRetentionPolicyEntityService _retentionEs;
    private readonly IDataDestructionLogEntityService _destructionEs;
    private readonly IDataInventoryEntityService _inventoryEs;
    private readonly IUnitOfWork _uow;

    public KvkkFactory(
        IConsentRecordEntityService consentEs,
        IDataSubjectRequestEntityService requestEs,
        IDataBreachEntityService breachEs,
        IRetentionPolicyEntityService retentionEs,
        IDataDestructionLogEntityService destructionEs,
        IDataInventoryEntityService inventoryEs,
        IUnitOfWork uow)
    {
        _consentEs = consentEs;
        _requestEs = requestEs;
        _breachEs = breachEs;
        _retentionEs = retentionEs;
        _destructionEs = destructionEs;
        _inventoryEs = inventoryEs;
        _uow = uow;
    }

    // ═══════════════════════════════════════════════════════════════
    // CONSENT
    // ═══════════════════════════════════════════════════════════════

    public async Task<(List<ConsentRecordDto> Items, int TotalCount)> GetConsentsAsync(int? customerId, int page, int pageSize)
    {
        var query = _consentEs.GetAllQueryable().Include(c => c.Customer).AsQueryable();
        if (customerId.HasValue) query = query.Where(c => c.CustomerId == customerId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => MapConsent(c)).ToListAsync();

        return (items, total);
    }

    public async Task<(bool Success, object Result)> CreateConsentAsync(ConsentCreateDto dto)
    {
        var entity = new ConsentRecord
        {
            CustomerId = dto.CustomerId,
            ConsentTypeId = dto.ConsentTypeId,
            StatusId = dto.StatusId > 0 ? dto.StatusId : ConsentStatuses.Ids.Granted,
            SubjectIdentifier = dto.SubjectIdentifier,
            SubjectName = dto.SubjectName,
            ConsentMethod = dto.ConsentMethod,
            LegalBasis = dto.LegalBasis,
            PrivacyNoticeVersion = dto.PrivacyNoticeVersion,
            ConsentDate = dto.ConsentDate ?? DateTime.UtcNow,
            Notes = dto.Notes
        };

        _consentEs.Add(entity);
        await _uow.SaveChangesAsync();
        return (true, new { entity.Id, entity.Uid });
    }

    public async Task<(bool Success, string? Error)> RevokeConsentAsync(Guid uid, string revokedBy)
    {
        var entity = await _consentEs.GetByUidAsync(uid);
        if (entity == null) return (false, "Riza kaydi bulunamadi.");
        if (entity.StatusId == ConsentStatuses.Ids.Revoked) return (false, "Riza zaten iptal edilmis.");

        entity.StatusId = ConsentStatuses.Ids.Revoked;
        entity.RevokedAt = DateTime.UtcNow;
        entity.RevokedBy = revokedBy;
        _consentEs.Update(entity);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<ConsentRecordDto>> GetConsentBySubjectAsync(int customerId, string identifier)
    {
        return await _consentEs.GetAllQueryable()
            .Include(c => c.Customer)
            .Where(c => c.CustomerId == customerId && c.SubjectIdentifier == identifier)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapConsent(c)).ToListAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // DATA SUBJECT REQUEST
    // ═══════════════════════════════════════════════════════════════

    public async Task<(List<DataSubjectRequestDto> Items, int TotalCount)> GetRequestsAsync(int? customerId, int? statusId, int page, int pageSize)
    {
        var query = _requestEs.GetAllQueryable().Include(r => r.Customer).AsQueryable();
        if (customerId.HasValue) query = query.Where(r => r.CustomerId == customerId.Value);
        if (statusId.HasValue) query = query.Where(r => r.StatusId == statusId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => MapRequest(r)).ToListAsync();

        return (items, total);
    }

    public async Task<(bool Success, object Result)> CreateRequestAsync(DataSubjectRequestCreateDto dto)
    {
        var entity = new DataSubjectRequest
        {
            CustomerId = dto.CustomerId,
            RequestTypeId = dto.RequestTypeId,
            StatusId = DataSubjectRequestStatuses.Ids.Received,
            RequesterName = dto.RequesterName,
            RequesterIdentifier = dto.RequesterIdentifier,
            RequesterContact = dto.RequesterContact,
            RequestDescription = dto.RequestDescription,
            RequestDate = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(30) // KVKK 30 gun
        };

        _requestEs.Add(entity);
        await _uow.SaveChangesAsync();
        return (true, new { entity.Id, entity.Uid, entity.Deadline });
    }

    public async Task<(bool Success, string? Error)> UpdateRequestAsync(Guid uid, DataSubjectRequestUpdateDto dto)
    {
        var entity = await _requestEs.GetByUidAsync(uid);
        if (entity == null) return (false, "Basvuru bulunamadi.");

        if (dto.StatusId.HasValue) entity.StatusId = dto.StatusId.Value;
        if (dto.ResponseDescription != null) entity.ResponseDescription = dto.ResponseDescription;
        if (dto.AssignedToUserName != null) entity.AssignedToUserName = dto.AssignedToUserName;
        if (dto.AssignedToUserId.HasValue) entity.AssignedToUserId = dto.AssignedToUserId;
        if (dto.RejectionReason != null) entity.RejectionReason = dto.RejectionReason;

        if (dto.StatusId == DataSubjectRequestStatuses.Ids.Completed || dto.StatusId == DataSubjectRequestStatuses.Ids.Rejected)
            entity.CompletedAt = DateTime.UtcNow;

        _requestEs.Update(entity);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<DataSubjectRequestDto>> GetOverdueRequestsAsync()
    {
        return await _requestEs.GetAllQueryable()
            .Include(r => r.Customer)
            .Where(r => r.StatusId != DataSubjectRequestStatuses.Ids.Completed
                     && r.StatusId != DataSubjectRequestStatuses.Ids.Rejected
                     && r.Deadline < DateTime.UtcNow)
            .OrderBy(r => r.Deadline)
            .Select(r => MapRequest(r)).ToListAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // DATA BREACH
    // ═══════════════════════════════════════════════════════════════

    public async Task<(List<DataBreachDto> Items, int TotalCount)> GetBreachesAsync(int? customerId, int page, int pageSize)
    {
        var query = _breachEs.GetAllQueryable().Include(b => b.Customer).AsQueryable();
        if (customerId.HasValue) query = query.Where(b => b.CustomerId == customerId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(b => b.DetectedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => MapBreach(b)).ToListAsync();

        return (items, total);
    }

    public async Task<(bool Success, object Result)> CreateBreachAsync(DataBreachCreateDto dto, string? reportedByUserName, int? reportedByUserId)
    {
        var entity = new DataBreach
        {
            CustomerId = dto.CustomerId,
            SeverityId = dto.SeverityId,
            StatusId = BreachStatuses.Ids.Detected,
            Title = dto.Title,
            Description = dto.Description,
            DetectedAt = DateTime.UtcNow,
            NotificationDeadline = DateTime.UtcNow.AddHours(72), // KVKK 72 saat
            AffectedSubjectCount = dto.AffectedSubjectCount,
            AffectedDataCategories = dto.AffectedDataCategories,
            CauseSummary = dto.CauseSummary,
            ReportedByUserName = reportedByUserName,
            ReportedByUserId = reportedByUserId
        };

        _breachEs.Add(entity);
        await _uow.SaveChangesAsync();
        return (true, new { entity.Id, entity.Uid, entity.NotificationDeadline });
    }

    public async Task<(bool Success, string? Error)> UpdateBreachAsync(Guid uid, DataBreachUpdateDto dto)
    {
        var entity = await _breachEs.GetByUidAsync(uid);
        if (entity == null) return (false, "Ihlal kaydi bulunamadi.");

        if (dto.StatusId.HasValue) entity.StatusId = dto.StatusId.Value;
        if (dto.AuthorityNotifiedAt.HasValue) entity.AuthorityNotifiedAt = dto.AuthorityNotifiedAt;
        if (dto.SubjectsNotifiedAt.HasValue) entity.SubjectsNotifiedAt = dto.SubjectsNotifiedAt;
        if (dto.AffectedSubjectCount.HasValue) entity.AffectedSubjectCount = dto.AffectedSubjectCount;
        if (dto.AffectedDataCategories != null) entity.AffectedDataCategories = dto.AffectedDataCategories;
        if (dto.CauseSummary != null) entity.CauseSummary = dto.CauseSummary;
        if (dto.MeasuresTaken != null) entity.MeasuresTaken = dto.MeasuresTaken;

        _breachEs.Update(entity);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════════
    // RETENTION POLICY
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<RetentionPolicyDto>> GetPoliciesAsync(int customerId)
    {
        return await _retentionEs.GetAllQueryable()
            .Where(r => r.CustomerId == customerId)
            .OrderBy(r => r.CategoryId)
            .Select(r => new RetentionPolicyDto
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                CategoryId = r.CategoryId,
                CategoryName = r.CategoryName,
                RetentionDays = r.RetentionDays,
                LegalBasis = r.LegalBasis,
                IsActive = r.IsActive,
                Description = r.Description,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToListAsync();
    }

    public async Task<(bool Success, object Result)> CreateOrUpdatePolicyAsync(RetentionPolicyCreateDto dto)
    {
        var existing = await _retentionEs.GetAllQueryable()
            .FirstOrDefaultAsync(r => r.CustomerId == dto.CustomerId && r.CategoryId == dto.CategoryId);

        var categoryName = RetentionCategories.GetById(dto.CategoryId)?.Description ?? "Bilinmiyor";

        if (existing != null)
        {
            existing.RetentionDays = dto.RetentionDays;
            existing.LegalBasis = dto.LegalBasis;
            existing.Description = dto.Description;
            existing.CategoryName = categoryName;
            existing.UpdatedAt = DateTime.UtcNow;
            _retentionEs.Update(existing);
            await _uow.SaveChangesAsync();
            return (true, new { existing.Id, Updated = true });
        }

        var entity = new RetentionPolicy
        {
            CustomerId = dto.CustomerId,
            CategoryId = dto.CategoryId,
            CategoryName = categoryName,
            RetentionDays = dto.RetentionDays,
            LegalBasis = dto.LegalBasis,
            Description = dto.Description
        };

        _retentionEs.Add(entity);
        await _uow.SaveChangesAsync();
        return (true, new { entity.Id, Updated = false });
    }

    public async Task SeedDefaultPoliciesAsync(int customerId)
    {
        var hasAny = await _retentionEs.GetAllQueryable().AnyAsync(r => r.CustomerId == customerId);
        if (hasAny) return;

        var defaults = new (int catId, int days, string basis)[]
        {
            (RetentionCategories.Ids.CallRecording, 3650, "TTK md.82 — 10 yil"),
            (RetentionCategories.Ids.AuditLog, 1095, "KVKK — 3 yil"),
            (RetentionCategories.Ids.AccessLog, 730, "BTK — 2 yil"),
            (RetentionCategories.Ids.PersonalData, 1095, "KVKK — 3 yil"),
            (RetentionCategories.Ids.FinancialData, 3650, "TTK md.82 — 10 yil")
        };

        foreach (var (catId, days, basis) in defaults)
        {
            _retentionEs.Add(new RetentionPolicy
            {
                CustomerId = customerId,
                CategoryId = catId,
                CategoryName = RetentionCategories.GetById(catId)?.Description ?? "",
                RetentionDays = days,
                LegalBasis = basis
            });
        }

        await _uow.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // DATA DESTRUCTION LOG
    // ═══════════════════════════════════════════════════════════════

    public async Task<(List<DataDestructionLogDto> Items, int TotalCount)> GetDestructionLogsAsync(int? customerId, int page, int pageSize)
    {
        var query = _destructionEs.GetAllQueryable().Include(d => d.Customer).AsQueryable();
        if (customerId.HasValue) query = query.Where(d => d.CustomerId == customerId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(d => d.DestroyedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new DataDestructionLogDto
            {
                Id = d.Id,
                CustomerId = d.CustomerId,
                CustomerName = d.Customer != null ? d.Customer.Name : null,
                MethodId = d.MethodId,
                MethodName = DestructionMethods.GetById(d.MethodId) != null ? DestructionMethods.GetById(d.MethodId)!.Description : "",
                DataCategory = d.DataCategory,
                Description = d.Description,
                DestroyedRecordCount = d.DestroyedRecordCount,
                LegalBasis = d.LegalBasis,
                ApprovedByUserName = d.ApprovedByUserName,
                DestroyedAt = d.DestroyedAt,
                CreatedAt = d.CreatedAt
            }).ToListAsync();

        return (items, total);
    }

    public async Task<(bool Success, object Result)> LogDestructionAsync(DataDestructionCreateDto dto, string approvedByUserName, int? approvedByUserId)
    {
        var entity = new DataDestructionLog
        {
            CustomerId = dto.CustomerId,
            MethodId = dto.MethodId,
            DataCategory = dto.DataCategory,
            Description = dto.Description,
            DestroyedRecordCount = dto.DestroyedRecordCount,
            LegalBasis = dto.LegalBasis,
            ApprovedByUserName = approvedByUserName,
            ApprovedByUserId = approvedByUserId,
            DestroyedAt = DateTime.UtcNow
        };

        _destructionEs.Add(entity);
        await _uow.SaveChangesAsync();
        return (true, new { entity.Id });
    }

    // ═══════════════════════════════════════════════════════════════
    // DATA INVENTORY
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<DataInventoryItemDto>> GetInventoryAsync(int? customerId)
    {
        var query = _inventoryEs.GetAllQueryable().Include(i => i.Customer).AsQueryable();
        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);

        return await query.OrderBy(i => i.DataCategory)
            .Select(i => new DataInventoryItemDto
            {
                Id = i.Id,
                CustomerId = i.CustomerId,
                CustomerName = i.Customer != null ? i.Customer.Name : null,
                DataCategory = i.DataCategory,
                Purpose = i.Purpose,
                LegalBasis = i.LegalBasis,
                DataSubjectGroup = i.DataSubjectGroup,
                RecipientGroup = i.RecipientGroup,
                TransferCountry = i.TransferCountry,
                RetentionDays = i.RetentionDays,
                SecurityMeasures = i.SecurityMeasures,
                IsActive = i.IsActive,
                VerbisRegistrationNo = i.VerbisRegistrationNo,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt
            }).ToListAsync();
    }

    public async Task<(bool Success, object Result)> CreateOrUpdateItemAsync(DataInventoryCreateDto dto)
    {
        var existing = await _inventoryEs.GetAllQueryable()
            .FirstOrDefaultAsync(i => i.CustomerId == dto.CustomerId && i.DataCategory == dto.DataCategory);

        if (existing != null)
        {
            existing.Purpose = dto.Purpose;
            existing.LegalBasis = dto.LegalBasis;
            existing.DataSubjectGroup = dto.DataSubjectGroup;
            existing.RecipientGroup = dto.RecipientGroup;
            existing.TransferCountry = dto.TransferCountry;
            existing.RetentionDays = dto.RetentionDays;
            existing.SecurityMeasures = dto.SecurityMeasures;
            existing.VerbisRegistrationNo = dto.VerbisRegistrationNo;
            existing.UpdatedAt = DateTime.UtcNow;
            _inventoryEs.Update(existing);
            await _uow.SaveChangesAsync();
            return (true, new { existing.Id, Updated = true });
        }

        var entity = new DataInventoryItem
        {
            CustomerId = dto.CustomerId,
            DataCategory = dto.DataCategory,
            Purpose = dto.Purpose,
            LegalBasis = dto.LegalBasis,
            DataSubjectGroup = dto.DataSubjectGroup,
            RecipientGroup = dto.RecipientGroup,
            TransferCountry = dto.TransferCountry,
            RetentionDays = dto.RetentionDays,
            SecurityMeasures = dto.SecurityMeasures,
            VerbisRegistrationNo = dto.VerbisRegistrationNo
        };

        _inventoryEs.Add(entity);
        await _uow.SaveChangesAsync();
        return (true, new { entity.Id, Updated = false });
    }

    // ═══════════════════════════════════════════════════════════════
    // DASHBOARD
    // ═══════════════════════════════════════════════════════════════

    public async Task<KvkkDashboardDto> GetDashboardAsync(int? customerId)
    {
        var requestQuery = _requestEs.GetAllQueryable();
        var breachQuery = _breachEs.GetAllQueryable();
        var consentQuery = _consentEs.GetAllQueryable();
        var retentionQuery = _retentionEs.GetAllQueryable();
        var destructionQuery = _destructionEs.GetAllQueryable();
        var inventoryQuery = _inventoryEs.GetAllQueryable();

        if (customerId.HasValue)
        {
            requestQuery = requestQuery.Where(r => r.CustomerId == customerId.Value);
            breachQuery = breachQuery.Where(b => b.CustomerId == customerId.Value);
            consentQuery = consentQuery.Where(c => c.CustomerId == customerId.Value);
            retentionQuery = retentionQuery.Where(r => r.CustomerId == customerId.Value);
            destructionQuery = destructionQuery.Where(d => d.CustomerId == customerId.Value);
            inventoryQuery = inventoryQuery.Where(i => i.CustomerId == customerId.Value);
        }

        var dto = new KvkkDashboardDto
        {
            PendingRequests = await requestQuery.CountAsync(r =>
                r.StatusId == DataSubjectRequestStatuses.Ids.Received || r.StatusId == DataSubjectRequestStatuses.Ids.InProgress),
            OverdueRequests = await requestQuery.CountAsync(r =>
                r.StatusId != DataSubjectRequestStatuses.Ids.Completed
                && r.StatusId != DataSubjectRequestStatuses.Ids.Rejected
                && r.Deadline < DateTime.UtcNow),
            ActiveBreaches = await breachQuery.CountAsync(b =>
                b.StatusId != BreachStatuses.Ids.Resolved),
            TotalConsents = await consentQuery.CountAsync(),
            RevokedConsents = await consentQuery.CountAsync(c => c.StatusId == ConsentStatuses.Ids.Revoked),
            RetentionPolicies = await retentionQuery.CountAsync(r => r.IsActive),
            DestructionCount = await destructionQuery.CountAsync(),
            InventoryItems = await inventoryQuery.CountAsync(i => i.IsActive)
        };

        dto.RecentRequests = await requestQuery
            .Include(r => r.Customer)
            .OrderByDescending(r => r.CreatedAt).Take(5)
            .Select(r => MapRequest(r)).ToListAsync();

        dto.RecentBreaches = await breachQuery
            .Include(b => b.Customer)
            .OrderByDescending(b => b.DetectedAt).Take(5)
            .Select(b => MapBreach(b)).ToListAsync();

        return dto;
    }

    // ═══════════════════════════════════════════════════════════════
    // MAPPING
    // ═══════════════════════════════════════════════════════════════

    private static ConsentRecordDto MapConsent(ConsentRecord c) => new()
    {
        Id = c.Id,
        Uid = c.Uid,
        CustomerId = c.CustomerId,
        CustomerName = c.Customer != null ? c.Customer.Name : "",
        ConsentTypeId = c.ConsentTypeId,
        ConsentTypeName = ConsentTypes.GetById(c.ConsentTypeId)?.Description ?? "",
        StatusId = c.StatusId,
        StatusName = ConsentStatuses.GetById(c.StatusId)?.Description ?? "",
        SubjectIdentifier = c.SubjectIdentifier,
        SubjectName = c.SubjectName,
        ConsentMethod = c.ConsentMethod,
        LegalBasis = c.LegalBasis,
        PrivacyNoticeVersion = c.PrivacyNoticeVersion,
        ConsentDate = c.ConsentDate,
        RevokedAt = c.RevokedAt,
        RevokedBy = c.RevokedBy,
        Notes = c.Notes,
        CreatedAt = c.CreatedAt
    };

    private static DataSubjectRequestDto MapRequest(DataSubjectRequest r) => new()
    {
        Id = r.Id,
        Uid = r.Uid,
        CustomerId = r.CustomerId,
        CustomerName = r.Customer != null ? r.Customer.Name : "",
        RequestTypeId = r.RequestTypeId,
        RequestTypeName = DataSubjectRequestTypes.GetById(r.RequestTypeId)?.Description ?? "",
        StatusId = r.StatusId,
        StatusName = DataSubjectRequestStatuses.GetById(r.StatusId)?.Description ?? "",
        RequesterName = r.RequesterName,
        RequesterIdentifier = r.RequesterIdentifier,
        RequesterContact = r.RequesterContact,
        RequestDescription = r.RequestDescription,
        ResponseDescription = r.ResponseDescription,
        AssignedToUserName = r.AssignedToUserName,
        RequestDate = r.RequestDate,
        Deadline = r.Deadline,
        CompletedAt = r.CompletedAt,
        RejectionReason = r.RejectionReason,
        CreatedAt = r.CreatedAt
    };

    private static DataBreachDto MapBreach(DataBreach b) => new()
    {
        Id = b.Id,
        Uid = b.Uid,
        CustomerId = b.CustomerId,
        CustomerName = b.Customer?.Name,
        SeverityId = b.SeverityId,
        SeverityName = BreachSeverities.GetById(b.SeverityId)?.Description ?? "",
        StatusId = b.StatusId,
        StatusName = BreachStatuses.GetById(b.StatusId)?.Description ?? "",
        Title = b.Title,
        Description = b.Description,
        DetectedAt = b.DetectedAt,
        NotificationDeadline = b.NotificationDeadline,
        AuthorityNotifiedAt = b.AuthorityNotifiedAt,
        SubjectsNotifiedAt = b.SubjectsNotifiedAt,
        AffectedSubjectCount = b.AffectedSubjectCount,
        AffectedDataCategories = b.AffectedDataCategories,
        CauseSummary = b.CauseSummary,
        MeasuresTaken = b.MeasuresTaken,
        ReportedByUserName = b.ReportedByUserName,
        CreatedAt = b.CreatedAt
    };
}
