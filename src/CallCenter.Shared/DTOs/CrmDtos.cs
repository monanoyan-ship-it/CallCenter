namespace CallCenter.Shared.DTOs;

// ─── CrmContact (360 derece gorunum) ───

public class CrmContactDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PhoneNumber2 { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }

    // 360 derece: iliskili kayitlarin sayilari
    public int TicketCount { get; set; }
    public int DealCount { get; set; }
    public int ActivityCount { get; set; }
    public int TaskCount { get; set; }
}

public class CrmContactCreateDto
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PhoneNumber2 { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
}

public class CrmContactUpdateDto : CrmContactCreateDto
{
    public bool? IsFavorite { get; set; }
}

// ─── Ticket ───

public class CrmTicketDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? StatusCss { get; set; }
    public int PriorityId { get; set; }
    public string? PriorityName { get; set; }
    public string? PriorityCss { get; set; }
    public int? CrmContactId { get; set; }
    public string? CrmContactName { get; set; }
    public int? AssignedToPersonnelId { get; set; }
    public string? AssignedToName { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int ActivityCount { get; set; }
}

public class CrmTicketCreateDto
{
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PriorityId { get; set; } = 2;
    public int? CrmContactId { get; set; }
    public int? AssignedToPersonnelId { get; set; }
}

public class CrmTicketUpdateDto : CrmTicketCreateDto
{
    public int StatusId { get; set; }
}

// ─── Deal ───

public class CrmDealDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int StageId { get; set; }
    public string? StageName { get; set; }
    public string? StageCss { get; set; }
    public int Probability { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime? ActualCloseDate { get; set; }
    public string? Notes { get; set; }
    public int? CrmContactId { get; set; }
    public string? CrmContactName { get; set; }
    public int? OwnerPersonnelId { get; set; }
    public string? OwnerName { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ActivityCount { get; set; }
}

public class CrmDealCreateDto
{
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int StageId { get; set; } = 1;
    public int Probability { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public string? Notes { get; set; }
    public int? CrmContactId { get; set; }
    public int? OwnerPersonnelId { get; set; }
}

public class CrmDealUpdateDto : CrmDealCreateDto;

// ─── Activity ───

public class CrmActivityDto
{
    public int Id { get; set; }
    public int TypeId { get; set; }
    public string? TypeName { get; set; }
    public string? TypeIcon { get; set; }
    public string? TypeCss { get; set; }
    public string? Summary { get; set; }
    public string? Detail { get; set; }
    public int? CrmContactId { get; set; }
    public string? CrmContactName { get; set; }
    public int? TicketId { get; set; }
    public int? DealId { get; set; }
    public int? CallRecordId { get; set; }
    public string? PersonnelName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrmActivityCreateDto
{
    public int TypeId { get; set; } = 1;
    public string? Summary { get; set; }
    public string? Detail { get; set; }
    public int? CrmContactId { get; set; }
    public int? TicketId { get; set; }
    public int? DealId { get; set; }
}

// ─── Task ───

public class CrmTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? StatusCss { get; set; }
    public DateTime? DueDate { get; set; }
    public int? CrmContactId { get; set; }
    public string? CrmContactName { get; set; }
    public int? TicketId { get; set; }
    public int? DealId { get; set; }
    public string? AssignedToName { get; set; }
    public int AssignedToPersonnelId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CrmTaskCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public int? CrmContactId { get; set; }
    public int? TicketId { get; set; }
    public int? DealId { get; set; }
    public int AssignedToPersonnelId { get; set; }
}

public class CrmTaskUpdateDto : CrmTaskCreateDto
{
    public int StatusId { get; set; }
}

// ─── Caller ID Lookup ───

public class CrmCallerIdDto
{
    public int CrmContactId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public int OpenTickets { get; set; }
    public int ActiveDeals { get; set; }
}

// ─── Dashboard ───

public class CrmDashboardDto
{
    public int TotalContacts { get; set; }
    public int OpenTickets { get; set; }
    public int ActiveDeals { get; set; }
    public int TodayActivities { get; set; }
    public decimal PipelineValue { get; set; }
    public List<CrmActivityDto> RecentActivities { get; set; } = new();
    public List<CrmTaskDto> UpcomingTasks { get; set; } = new();
}

public class CrmModuleDto
{
    public int Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string NameResourceKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? CssClass { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int? SourceSalonModuleId { get; set; }
    public string? SourceSalonModuleName { get; set; }
}

public class CrmScopeDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DashboardUrl { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class CrmEntitlementsDto
{
    public bool HasCrmProduct { get; set; }
    public bool HasStandaloneCrm { get; set; }
    public bool HasSalonCrm { get; set; }
    public bool HasCallCenterCrm { get; set; }
    public int? BranchId { get; set; }
    public string DefaultScope { get; set; } = string.Empty;
    public List<int> ActiveProductTypeIds { get; set; } = new();
    public List<int> ActiveCrmModuleIds { get; set; } = new();
    public List<int> ActiveSalonModuleIds { get; set; } = new();
    public List<CrmScopeDto> Scopes { get; set; } = new();
}
