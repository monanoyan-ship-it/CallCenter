using CallCenter.Shared.Enums;

namespace CallCenter.Shared.DTOs;

public class AgentStatusUpdate
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
}
