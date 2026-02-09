namespace CallCenter.Shared.Entities;

public class QueueAgent
{
    public Guid QueueId { get; set; }
    public Queue Queue { get; set; } = null!;

    public Guid AgentId { get; set; }
    public User Agent { get; set; } = null!;

    public int Priority { get; set; } = 1;
}
