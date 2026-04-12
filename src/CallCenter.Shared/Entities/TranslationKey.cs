namespace CallCenter.Shared.Entities;

public class TranslationKey
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;       // "login.title", "agent.status.available"
    public string? Description { get; set; }               // Moderatörler için açıklama
    public string Module { get; set; } = "common";         // "common", "auth", "agent", "admin", "queue"

    /// <summary>Platform: 1=Landing, 2=Salon, 3=Management, 4=CRM, 5=CallCenter</summary>
    public int PlatformId { get; set; } = 5;

    public ICollection<Translation> Translations { get; set; } = new List<Translation>();
}
