namespace CallCenter.Shared.Entities;

public class Translation
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }

    public int TranslationKeyId { get; set; }
    public TranslationKey TranslationKey { get; set; } = null!;

    public string LanguageCode { get; set; } = string.Empty;
}
