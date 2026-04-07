namespace CallCenter.Shared.Enums;

/// <summary>
/// Salon personel rolleri. CallCenter CustomerRoles'tan bagimsiz.
/// Salon musterilerinin personeline atanir.
/// </summary>
public static class SalonRoles
{
    public static readonly TypeItem SalonOwner = new(101, "SalonOwner", "SalonRole.SalonOwner", "Salon Sahibi", "bi-shop", "bg-danger", 1);
    public static readonly TypeItem Manager = new(102, "Manager", "SalonRole.Manager", "Müdür", "bi-person-badge", "bg-warning text-dark", 2);
    public static readonly TypeItem Hairdresser = new(103, "Hairdresser", "SalonRole.Hairdresser", "Kuaför", "bi-scissors", "bg-primary", 3, isDefault: true);
    public static readonly TypeItem Beautician = new(104, "Beautician", "SalonRole.Beautician", "Güzellik Uzmanı", "bi-stars", "bg-pink", 4);
    public static readonly TypeItem Cashier = new(105, "Cashier", "SalonRole.Cashier", "Kasiyer", "bi-cash-stack", "bg-success", 5);
    public static readonly TypeItem Receptionist = new(106, "Receptionist", "SalonRole.Receptionist", "Resepsiyonist", "bi-person-lines-fill", "bg-info", 6);

    public static IEnumerable<TypeItem> All => new[] { SalonOwner, Manager, Hairdresser, Beautician, Cashier, Receptionist };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int SalonOwner = 101;
        public const int Manager = 102;
        public const int Hairdresser = 103;
        public const int Beautician = 104;
        public const int Cashier = 105;
        public const int Receptionist = 106;
    }
}
