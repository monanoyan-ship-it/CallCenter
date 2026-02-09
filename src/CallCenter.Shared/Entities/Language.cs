namespace CallCenter.Shared.Entities;

public class Language
{
    public string Code { get; set; } = string.Empty;  // "tr", "en"
    public string Name { get; set; } = string.Empty;  // "Türkçe", "English"
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Translation> Translations { get; set; } = new List<Translation>();
}
