namespace CallCenter.Shared.Enums;

/// <summary>
/// Modul talep tipleri.
/// </summary>
public static class ModuleRequestTypes
{
    public static readonly TypeItem Activation = new(1, "Activation", "ModuleRequestType.Activation", "Aktivasyon", "bi-plus-circle", "bg-success", 1);
    public static readonly TypeItem Deactivation = new(2, "Deactivation", "ModuleRequestType.Deactivation", "İptal", "bi-dash-circle", "bg-danger", 2);

    public static IEnumerable<TypeItem> All => new[] { Activation, Deactivation };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Activation = 1;
        public const int Deactivation = 2;
    }
}
