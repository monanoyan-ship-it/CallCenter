namespace CallCenter.Shared.Entities;

public class QueueAgent
{
    public int QueueId { get; set; }
    public Queue Queue { get; set; } = null!;

    public int AgentId { get; set; }
    public User Agent { get; set; } = null!;

    public int Priority { get; set; } = 1;
}
