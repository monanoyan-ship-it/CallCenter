namespace CallCenter.Shared.Enums;

public static class OrganizationUnitTypes
{
    public static readonly TypeItem Region = new(1, "Region", "OrgUnit.Region", "Bölge", "bi-geo-alt-fill", "bg-danger", 1);
    public static readonly TypeItem Branch = new(2, "Branch", "OrgUnit.Branch", "Şube", "bi-building", "bg-primary", 2);
    public static readonly TypeItem Department = new(3, "Department", "OrgUnit.Department", "Departman", "bi-diagram-3-fill", "bg-success", 3);
    public static readonly TypeItem Unit = new(4, "Unit", "OrgUnit.Unit", "Birim", "bi-collection", "bg-info", 4);
    public static readonly TypeItem Team = new(5, "Team", "OrgUnit.Team", "Takım", "bi-people-fill", "bg-warning text-dark", 5);

    public static IEnumerable<TypeItem> All => new[] { Region, Branch, Department, Unit, Team };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Region = 1;
        public const int Branch = 2;
        public const int Department = 3;
        public const int Unit = 4;
        public const int Team = 5;
    }
}

public static class PermissionScopes
{
    public static readonly TypeItem All = new(1, "All", "PermissionScope.All", "Tüm Kaynaklara Erişim", "bi-globe", "bg-success", 1);
    public static readonly TypeItem Own = new(2, "Own", "PermissionScope.Own", "Sadece Kendi Oluşturduğu Kaynaklar", "bi-person", "bg-primary", 2);
    public static readonly TypeItem Customer = new(3, "Customer", "PermissionScope.Customer", "Kendi Müşterisine Ait Kaynaklar", "bi-person-badge", "bg-secondary", 3, isDefault: true);

    public static IEnumerable<TypeItem> AllItems => new[] { All, Own, Customer };
    public static TypeItem Default => AllItems.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => AllItems.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => AllItems.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int All = 1;
        public const int Own = 2;
        public const int Customer = 3;
    }
}
