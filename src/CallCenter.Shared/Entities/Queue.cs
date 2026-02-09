namespace CallCenter.Shared.Entities;

public class Queue
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxWaitTimeSeconds { get; set; } = 300;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Hangi musteriye ait (her kuyruk bir firmaya baglidir)
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public ICollection<CallRecord> CallRecords { get; set; } = new List<CallRecord>();
    public ICollection<QueueAgent> QueueAgents { get; set; } = new List<QueueAgent>();
}
