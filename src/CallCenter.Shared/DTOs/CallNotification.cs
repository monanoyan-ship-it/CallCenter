using CallCenter.Shared.Enums;

namespace CallCenter.Shared.DTOs;

public class CallNotification
{
    public int CallId { get; set; }
    public string CallerNumber { get; set; } = string.Empty;
    public string CalleeNumber { get; set; } = string.Empty;
    public CallDirection Direction { get; set; }
    public CallStatus Status { get; set; }
    public string? QueueName { get; set; }
}
