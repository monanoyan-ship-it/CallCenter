namespace CallCenter.Shared.DTOs;

public class GatewayHealthUpdate
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public bool IsRegistered { get; set; }
    public string? GatewayName { get; set; }
    public string? SipServer { get; set; }
    public string? Transport { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}
