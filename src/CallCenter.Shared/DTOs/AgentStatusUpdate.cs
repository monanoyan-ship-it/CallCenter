namespace CallCenter.Shared.DTOs;

public class AgentStatusUpdate
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}
