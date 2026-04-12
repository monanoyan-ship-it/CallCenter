namespace CallCenter.Shared.Enums;

/// <summary>
/// Çeviri platformları. Her platform kendi çeviri key'lerine sahiptir.
/// </summary>
public static class TranslationPlatforms
{
    public static readonly TypeItem Landing = new(1, "Landing", "Platform.Landing", "Landing", "bi-house", "bg-dark", 1);
    public static readonly TypeItem Salon = new(2, "Salon", "Platform.Salon", "Salon", "bi-scissors", "bg-purple", 2);
    public static readonly TypeItem Management = new(3, "Management", "Platform.Management", "Management", "bi-gear", "bg-primary", 3);
    public static readonly TypeItem CRM = new(4, "CRM", "Platform.CRM", "CRM", "bi-people", "bg-success", 4);
    public static readonly TypeItem CallCenter = new(5, "CallCenter", "Platform.CallCenter", "Call Center", "bi-headset", "bg-danger", 5);

    public static IEnumerable<TypeItem> All => new[] { Landing, Salon, Management, CRM, CallCenter };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Landing = 1;
        public const int Salon = 2;
        public const int Management = 3;
        public const int CRM = 4;
        public const int CallCenter = 5;
    }
}
