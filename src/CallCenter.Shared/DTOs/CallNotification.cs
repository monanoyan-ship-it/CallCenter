namespace CallCenter.Shared.DTOs;

public class CallNotification
{
    public int CallId { get; set; }
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public int DirectionId { get; set; }
    public int StatusId { get; set; }
    public string? QueueName { get; set; }

    // CRM Caller ID bilgileri
    public int? CrmContactId { get; set; }
    public string? CrmContactName { get; set; }
    public string? CrmContactCompany { get; set; }
}
