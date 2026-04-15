namespace CallCenter.Shared.Enums;

/// <summary>
/// Salon modül paketleri. Her modül bir pakete ait; paket aktif edilince alt modüller topluca aktiftir.
/// Temel Paket her salon için zorunlu ve sabit fiyat (1.700 TL) — modülleri IsDefault=true olarak işaretli.
/// Fiyatlar KDV dahil aylıktır. Ek şubeler için %10 indirim abonelik hesabında uygulanır.
/// </summary>
public static class SalonModuleGroups
{
    // Not: Grupların id'leri mevcut kayıtlarla uyumlu tutulmalı — mevcut mapping ile geri uyumluluk.
    // Eski 6 grup yerine 5 grup: Stok+Finans birleştirildi (Id=1), Sadakat+Pazarlama birleştirildi (Id=3).
    public static readonly ModuleGroup StockFinance = new(1, "StockFinance", "Stok Tedarik / Finans", "bi-box-seam", "bg-secondary", 400m, 1);
    public static readonly ModuleGroup LoyaltyMarketing = new(3, "LoyaltyMarketing", "Müşteri Sadakati / Pazarlama", "bi-heart-fill", "bg-danger", 1500m, 2);
    public static readonly ModuleGroup Professional = new(5, "Professional", "Profesyonel", "bi-star-fill", "bg-warning text-dark", 1500m, 3);
    public static readonly ModuleGroup Enterprise = new(6, "Enterprise", "Kurumsal", "bi-building", "bg-primary", 200m, 4);

    public static IEnumerable<ModuleGroup> All => new[] { StockFinance, LoyaltyMarketing, Professional, Enterprise };
    public static ModuleGroup? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int StockFinance = 1;
        public const int LoyaltyMarketing = 3;
        public const int Professional = 5;
        public const int Enterprise = 6;
    }

    /// <summary>Modül ID → Grup ID mapping. Default modüller grupsuz (null, Temel Pakete ait).</summary>
    private static readonly Dictionary<int, int> ModuleGroupMap = new()
    {
        // Stok Tedarik / Finans (400 TL) — Stok + Finans birleşti
        [SalonPortalModules.Ids.SlnProducts] = Ids.StockFinance,
        [SalonPortalModules.Ids.SlnSuppliers] = Ids.StockFinance,
        [SalonPortalModules.Ids.SlnExpenses] = Ids.StockFinance,
        // Not: SlnCash (Kasa) default modül — Temel pakettedir, buradan hariç.

        // Müşteri Sadakati / Pazarlama (1.500 TL) — Sadakat + Pazarlama birleşti
        [SalonPortalModules.Ids.SlnGiftCards] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnPackages] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnMemberships] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnLoyalty] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnCampaigns] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnEmailCampaigns] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnWinback] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnReviews] = Ids.LoyaltyMarketing,

        // Profesyonel (1.500 TL)
        [SalonPortalModules.Ids.SlnNoShowPolicy] = Ids.Professional,
        [SalonPortalModules.Ids.SlnConsentForms] = Ids.Professional,
        [SalonPortalModules.Ids.SlnBeforeAfter] = Ids.Professional,
        [SalonPortalModules.Ids.SlnWaitlist] = Ids.Professional,
        [SalonPortalModules.Ids.SlnPersonnelPrices] = Ids.Professional,

        // Kurumsal (200 TL)
        [SalonPortalModules.Ids.SlnReports] = Ids.Enterprise,
        [SalonPortalModules.Ids.SlnBranches] = Ids.Enterprise,
    };

    /// <summary>Modül ID'sine karşılık gelen grup ID'si. null = grupsuz (Temel Pakete ait default modül).</summary>
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

/// <summary>Salon modül grubu (paket). Aylık sabit paket fiyatı içerir.</summary>
public record ModuleGroup(int Id, string SystemName, string Description, string Icon, string CssClass, decimal MonthlyPrice, int DisplayOrder);

/// <summary>Grup + altındaki modüller</summary>
public class ModuleGroupInfo
{
    public ModuleGroup Group { get; set; } = null!;
    public List<TypeItem> Modules { get; set; } = new();
}
