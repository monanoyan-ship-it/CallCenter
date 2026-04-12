namespace CallCenter.Shared.Enums;

/// <summary>
/// Desteklenen çeviri dilleri.
/// </summary>
public static class TranslationLanguages
{
    public static readonly TypeItem Turkish = new(1, "tr", "Language.Turkish", "Türkçe", "🇹🇷", "", 1, isDefault: true);
    public static readonly TypeItem English = new(2, "en", "Language.English", "English", "🇬🇧", "", 2);
    public static readonly TypeItem German = new(3, "de", "Language.German", "Deutsch", "🇩🇪", "", 3);
    public static readonly TypeItem Arabic = new(4, "ar", "Language.Arabic", "العربية", "🇸🇦", "", 4);
    public static readonly TypeItem Russian = new(5, "ru", "Language.Russian", "Русский", "🇷🇺", "", 5);

    public static IEnumerable<TypeItem> All => new[] { Turkish, English, German, Arabic, Russian };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetByCode(string code) => All.FirstOrDefault(x => x.SystemName.Equals(code, StringComparison.OrdinalIgnoreCase));

    public static class Ids
    {
        public const int Turkish = 1;
        public const int English = 2;
        public const int German = 3;
        public const int Arabic = 4;
        public const int Russian = 5;
    }

    public static class Codes
    {
        public const string Turkish = "tr";
        public const string English = "en";
        public const string German = "de";
        public const string Arabic = "ar";
        public const string Russian = "ru";
    }
}
