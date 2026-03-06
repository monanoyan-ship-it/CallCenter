namespace CallCenter.Shared.DTOs;

public class CampaignDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? ScheduledDate { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Ozet istatistikler
    public int TotalContacts { get; set; }
    public int PendingCount { get; set; }
    public int ReachedCount { get; set; }
    public int NotReachedCount { get; set; }
    public int CancelledCount { get; set; }
}

public class CampaignDetailDto : CampaignDto
{
    public List<CampaignContactDto> Contacts { get; set; } = new();
}

public class CampaignContactDto
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Company { get; set; }
    public int? AssignedPersonnelId { get; set; }
    public string? AssignedPersonnelName { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResultNotes { get; set; }
}

public class CreateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

public class UpdateCampaignRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? StatusId { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

public class AddCampaignContactsRequest
{
    public List<int> ContactIds { get; set; } = new();
}

public class AssignCampaignContactsRequest
{
    public List<int> CampaignContactIds { get; set; } = new();
    public int PersonnelId { get; set; }
}

public class UpdateCampaignContactStatusRequest
{
    public int StatusId { get; set; }
    public string? ResultNotes { get; set; }
}

/// <summary>Operatorun kendi atanan listesi</summary>
public class MyCallListItemDto
{
    public int CampaignContactId { get; set; }
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public int ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Company { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public string? ResultNotes { get; set; }
}
