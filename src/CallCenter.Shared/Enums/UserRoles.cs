namespace CallCenter.Shared.Enums;

public static class UserRoles
{
    public static readonly TypeItem Agent = new(1, "Agent", "Role.Agent", "Cagri merkezi temsilcisi", "bi-headset", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Supervisor = new(2, "Supervisor", "Role.Supervisor", "Takim lideri / denetleyici", "bi-eye-fill", "bg-info", 2);
    public static readonly TypeItem Admin = new(3, "Admin", "Role.Admin", "Sistem yoneticisi", "bi-shield-fill-check", "bg-danger", 3);
    public static readonly TypeItem CustomerUser = new(4, "CustomerUser", "Role.CustomerUser", "Musteri kullanicisi", "bi-building", "bg-warning", 4);

    public static IEnumerable<TypeItem> All => new[] { Agent, Supervisor, Admin, CustomerUser };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Agent = 1;
        public const int Supervisor = 2;
        public const int Admin = 3;
        public const int CustomerUser = 4;
    }
}
