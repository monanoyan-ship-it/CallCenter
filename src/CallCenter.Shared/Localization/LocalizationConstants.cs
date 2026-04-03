namespace CallCenter.Shared.Localization;

public static class LocalizationConstants
{
    public const string DefaultLanguage = "tr";

    public static readonly HashSet<string> SupportedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "tr", "en", "de", "ar", "ru"
    };

    public static readonly Dictionary<string, LanguageInfo> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tr"] = new("tr", "Türkçe", "TR"),
        ["en"] = new("en", "English", "EN"),
        ["de"] = new("de", "Deutsch", "DE"),
        ["ar"] = new("ar", "العربية", "AR"),
        ["ru"] = new("ru", "Русский", "RU")
    };

    public record LanguageInfo(string Code, string Name, string Label);
}
