using CallCenter.Shared.Enums;

namespace CallCenter.Shared.Entities;

public class User
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Agent;
    public AgentStatus Status { get; set; } = AgentStatus.Offline;
    public string? Extension { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<CallRecord> CallRecords { get; set; } = new List<CallRecord>();

    // Müşteri kullanıcıları için (Role == CustomerUser ise dolu)
    public CustomerPersonnel? CustomerPersonnel { get; set; }
}
