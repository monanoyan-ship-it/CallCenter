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
    public int RoleId { get; set; } = UserRoles.Ids.Agent;
    public int StatusId { get; set; } = AgentStatuses.Ids.Offline;
    public string? Extension { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Sifre politikasi alanlari
    public DateTime? PasswordChangedAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public bool MustChangePassword { get; set; }

    // Email dogrulama
    public bool IsEmailVerified { get; set; }
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationSentAt { get; set; }

    // Sifre sifirlama
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetSentAt { get; set; }

    // Dil tercihi (null = sistem varsayilani "tr")
    public string? PreferredLanguage { get; set; }

    public ICollection<CallRecord> CallRecords { get; set; } = new List<CallRecord>();
    public ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
    public ICollection<CallForwardingRule> CallForwardingRules { get; set; } = new List<CallForwardingRule>();

    // Musteri kullanicilari icin (RoleId == CustomerUser ise dolu)
    public CustomerPersonnel? CustomerPersonnel { get; set; }
}
