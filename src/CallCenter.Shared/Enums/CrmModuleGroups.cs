namespace CallCenter.Shared.Enums;

/// <summary>
/// CRM satis baglamlari. ProductTypes.Crm ortak kapsayicidir; asil ticari paket bu gruplardan gelir.
/// </summary>
public static class CrmModuleGroups
{
    public static readonly CrmModuleGroup Core = new(0, "CoreCrm", "Genel CRM", "bi-person-lines-fill", "bg-info", 1500m, 0, CustomerBillingKinds.Crm);
    public static readonly CrmModuleGroup Salon = new(1, "SalonCrm", "Salon CRM", "bi-scissors", "bg-warning text-dark", 1500m, 1, CustomerBillingKinds.SalonCrm);
    public static readonly CrmModuleGroup CallCenter = new(2, "CallCenterCrm", "CallCenter CRM", "bi-headset", "bg-primary", 1500m, 2, CustomerBillingKinds.CallCenterCrm);

    public static IEnumerable<CrmModuleGroup> All => new[] { Core, Salon, CallCenter };

    public static CrmModuleGroup? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static CrmModuleGroup? GetByBillingKindId(int billingKindId) =>
        All.FirstOrDefault(x => x.BillingKindId == billingKindId);

    public static int? GetGroupId(int moduleId)
    {
        if (CrmModules.Core.Any(m => m.Id == moduleId)) return Ids.Core;
        if (CrmModules.SalonVertical.Any(m => m.Id == moduleId)) return Ids.Salon;
        if (CrmModules.CallCenterVertical.Any(m => m.Id == moduleId)) return Ids.CallCenter;
        return null;
    }

    public static List<int> GetModuleIds(int groupId) =>
        groupId switch
        {
            Ids.Core => CrmModules.Core.Select(m => m.Id).ToList(),
            Ids.Salon => CrmModules.SalonVertical.Select(m => m.Id).ToList(),
            Ids.CallCenter => CrmModules.CallCenterVertical.Select(m => m.Id).ToList(),
            _ => new List<int>()
        };

    public static class Ids
    {
        public const int Core = 0;
        public const int Salon = 1;
        public const int CallCenter = 2;
    }
}

public record CrmModuleGroup(
    int Id,
    string SystemName,
    string Description,
    string Icon,
    string CssClass,
    decimal MonthlyPrice,
    int DisplayOrder,
    int BillingKindId);
