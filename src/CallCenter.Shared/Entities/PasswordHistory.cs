namespace CallCenter.Shared.Entities;

/// <summary>
/// Sifre gecmisi. Son N sifrenin tekrar kullanilmasini engellemek icin tutulur.
/// BCrypt hash saklanir, plain text asla.
/// </summary>
public class PasswordHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
