namespace CallCenter.Shared.Entities;

public class Queue
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxWaitTimeSeconds { get; set; } = 300;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CallRecord> CallRecords { get; set; } = new List<CallRecord>();
    public ICollection<QueueAgent> QueueAgents { get; set; } = new List<QueueAgent>();
}
