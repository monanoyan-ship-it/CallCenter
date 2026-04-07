namespace CallCenter.Shared.Enums;

/// <summary>
/// Salon modül grupları. Modüller bu gruplara aittir.
/// Grup aktif edildiğinde altındaki tüm modüller toplu aktif olur.
/// Tekli modül açma/kapama da mümkündür.
/// </summary>
public static class SalonModuleGroups
{
    public static readonly TypeItem StockAndSupply = new(1, "StockAndSupply", "ModuleGroup.StockAndSupply", "Stok ve Tedarik", "bi-box-seam", "bg-secondary", 1);
    public static readonly TypeItem Finance = new(2, "Finance", "ModuleGroup.Finance", "Finans", "bi-cash-stack", "bg-success", 2);
    public static readonly TypeItem CustomerLoyalty = new(3, "CustomerLoyalty", "ModuleGroup.CustomerLoyalty", "Müşteri Sadakati", "bi-heart-fill", "bg-danger", 3);
    public static readonly TypeItem Marketing = new(4, "Marketing", "ModuleGroup.Marketing", "Pazarlama", "bi-megaphone-fill", "bg-pink", 4);
    public static readonly TypeItem Professional = new(5, "Professional", "ModuleGroup.Professional", "Profesyonel", "bi-star-fill", "bg-warning text-dark", 5);
    public static readonly TypeItem Enterprise = new(6, "Enterprise", "ModuleGroup.Enterprise", "Kurumsal", "bi-building", "bg-primary", 6);

    public static IEnumerable<TypeItem> All => new[] { StockAndSupply, Finance, CustomerLoyalty, Marketing, Professional, Enterprise };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int StockAndSupply = 1;
        public const int Finance = 2;
        public const int CustomerLoyalty = 3;
        public const int Marketing = 4;
        public const int Professional = 5;
        public const int Enterprise = 6;
    }

    /// <summary>Modül ID → Grup ID mapping. Default modüller grupsuz (null).</summary>
    private static readonly Dictionary<int, int> ModuleGroupMap = new()
    {
        // Stok ve Tedarik
        [SalonPortalModules.Ids.SlnProducts] = Ids.StockAndSupply,
        [SalonPortalModules.Ids.SlnSuppliers] = Ids.StockAndSupply,

        // Finans
        [SalonPortalModules.Ids.SlnCash] = Ids.Finance,
        [SalonPortalModules.Ids.SlnExpenses] = Ids.Finance,

        // Müşteri Sadakati
        [SalonPortalModules.Ids.SlnGiftCards] = Ids.CustomerLoyalty,
        [SalonPortalModules.Ids.SlnPackages] = Ids.CustomerLoyalty,
        [SalonPortalModules.Ids.SlnMemberships] = Ids.CustomerLoyalty,
        [SalonPortalModules.Ids.SlnLoyalty] = Ids.CustomerLoyalty,

        // Pazarlama
        [SalonPortalModules.Ids.SlnCampaigns] = Ids.Marketing,
        [SalonPortalModules.Ids.SlnEmailCampaigns] = Ids.Marketing,
        [SalonPortalModules.Ids.SlnWinback] = Ids.Marketing,
        [SalonPortalModules.Ids.SlnReviews] = Ids.Marketing,

        // Profesyonel
        [SalonPortalModules.Ids.SlnNoShowPolicy] = Ids.Professional,
        [SalonPortalModules.Ids.SlnConsentForms] = Ids.Professional,
        [SalonPortalModules.Ids.SlnBeforeAfter] = Ids.Professional,
        [SalonPortalModules.Ids.SlnWaitlist] = Ids.Professional,
        [SalonPortalModules.Ids.SlnPersonnelPrices] = Ids.Professional,

        // Kurumsal
        [SalonPortalModules.Ids.SlnReports] = Ids.Enterprise,
        [SalonPortalModules.Ids.SlnBranches] = Ids.Enterprise,
    };

    /// <summary>Modül ID'sine karşılık gelen grup ID'si. null = grupsuz (default modül).</summary>
    public static int? GetGroupId(int moduleId) =>
        ModuleGroupMap.TryGetValue(moduleId, out var groupId) ? groupId : null;

    /// <summary>Gruba ait modül ID'lerini döndürür.</summary>
    public static List<int> GetModuleIds(int groupId) =>
        ModuleGroupMap.Where(kv => kv.Value == groupId).Select(kv => kv.Key).ToList();

    /// <summary>Gruba ait modül TypeItem'larını döndürür.</summary>
    public static List<TypeItem> GetModules(int groupId) =>
        GetModuleIds(groupId)
            .Select(id => SalonPortalModules.GetById(id))
            .Where(m => m != null)
            .Cast<TypeItem>()
            .OrderBy(m => m.DisplayOrder)
            .ToList();

    /// <summary>Tüm grupları modülleriyle birlikte döndürür.</summary>
    public static List<ModuleGroupInfo> GetAllWithModules() =>
        All.Select(g => new ModuleGroupInfo
        {
            Group = g,
            Modules = GetModules(g.Id)
        }).ToList();
}

/// <summary>Grup + altındaki modüller</summary>
public class ModuleGroupInfo
{
    public TypeItem Group { get; set; } = null!;
    public List<TypeItem> Modules { get; set; } = new();
}
