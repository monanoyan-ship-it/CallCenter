using CallCenter.Shared.Enums;

namespace CallCenter.Shared.DTOs;

public class AgentStatusUpdate
{
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
}
