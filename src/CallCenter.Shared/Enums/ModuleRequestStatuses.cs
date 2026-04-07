namespace CallCenter.Shared.Enums;

/// <summary>
/// Modul talep durumlari.
/// </summary>
public static class ModuleRequestStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", "ModuleRequestStatus.Pending", "Beklemede", "bi-hourglass-split", "bg-warning text-dark", 1);
    public static readonly TypeItem Approved = new(2, "Approved", "ModuleRequestStatus.Approved", "Onaylandı", "bi-check-circle", "bg-success", 2);
    public static readonly TypeItem Rejected = new(3, "Rejected", "ModuleRequestStatus.Rejected", "Reddedildi", "bi-x-circle", "bg-danger", 3);
    public static readonly TypeItem Cancelled = new(4, "Cancelled", "ModuleRequestStatus.Cancelled", "İptal Edildi", "bi-slash-circle", "bg-secondary", 4);

    public static IEnumerable<TypeItem> All => new[] { Pending, Approved, Rejected, Cancelled };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Pending = 1;
        public const int Approved = 2;
        public const int Rejected = 3;
        public const int Cancelled = 4;
    }
}
