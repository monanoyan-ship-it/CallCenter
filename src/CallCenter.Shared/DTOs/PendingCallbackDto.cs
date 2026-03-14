namespace CallCenter.Shared.DTOs;

public class PendingCallbackDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string CallerNumber { get; set; } = "";
    public string? CalleeNumber { get; set; }
    public DateTime StartedAt { get; set; }
    public int CallbackStatusId { get; set; }
    public string? CallbackNote { get; set; }
}
