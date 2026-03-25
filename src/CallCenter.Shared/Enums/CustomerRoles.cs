namespace CallCenter.Shared.Enums;

public static class CustomerRoles
{
    public static readonly TypeItem FirmaAdmin = new(1, "FirmaAdmin", "CustomerRole.FirmaAdmin", "Firma Yoneticisi", "bi-shield-fill-check", "bg-danger", 1);
    public static readonly TypeItem EkipLideri = new(2, "EkipLideri", "CustomerRole.EkipLideri", "Ekip Lideri", "bi-people-fill", "bg-info", 2);
    public static readonly TypeItem Operator = new(3, "Operator", "CustomerRole.Operator", "Operator", "bi-headset", "bg-primary", 3, isDefault: true);

    public static IEnumerable<TypeItem> All => new[] { FirmaAdmin, EkipLideri, Operator };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    /// <summary>Aranabilir roller (MaxUsers limitine dahil). EkipLideri arama yapmaz, limitte sayilmaz.</summary>
    public static IEnumerable<TypeItem> CallableRoles => new[] { Operator };

    /// <summary>Rol bazli statik yetki eslestirmesi. FirmaAdmin tum izinlere sahiptir.</summary>
    public static IEnumerable<int> GetPermissionsForRole(int roleId)
    {
        return roleId switch
        {
            Ids.FirmaAdmin => CustomerPermissionTypes.All.Select(p => p.Id),
            Ids.EkipLideri => new[]
            {
                CustomerPermissionTypes.Ids.DashboardView,
                CustomerPermissionTypes.Ids.CallListen, CustomerPermissionTypes.Ids.CallMake,
                CustomerPermissionTypes.Ids.AgentView,
                CustomerPermissionTypes.Ids.QueueView,
                CustomerPermissionTypes.Ids.PersonnelView,
                CustomerPermissionTypes.Ids.OrgView,
                CustomerPermissionTypes.Ids.RecordListen,
                CustomerPermissionTypes.Ids.CrmQualityView, CustomerPermissionTypes.Ids.CrmQualityScore,
                CustomerPermissionTypes.Ids.KBView,
                CustomerPermissionTypes.Ids.ReportView,
                CustomerPermissionTypes.Ids.KvkkView
            },
            Ids.Operator => new[]
            {
                CustomerPermissionTypes.Ids.DashboardView,
                CustomerPermissionTypes.Ids.CallListen, CustomerPermissionTypes.Ids.CallMake,
                CustomerPermissionTypes.Ids.KBView
            },
            _ => Enumerable.Empty<int>()
        };
    }

    public static class Ids
    {
        public const int FirmaAdmin = 1;
        public const int EkipLideri = 2;
        public const int Operator = 3;
    }
}
