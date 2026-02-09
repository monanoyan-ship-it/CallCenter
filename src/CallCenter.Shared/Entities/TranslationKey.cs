namespace CallCenter.Shared.Entities;

public class TranslationKey
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;       // "login.title", "agent.status.available"
    public string? Description { get; set; }               // Moderatörler için açıklama
    public string Module { get; set; } = "common";         // "common", "auth", "agent", "admin", "queue"

    public ICollection<Translation> Translations { get; set; } = new List<Translation>();
}
