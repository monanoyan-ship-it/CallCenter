namespace CallCenter.Shared.Localization;

public static class LocalizationConstants
{
    public const string DefaultLanguage = "tr";

    public static readonly HashSet<string> SupportedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "tr", "en", "de", "ar", "ru"
    };
}
