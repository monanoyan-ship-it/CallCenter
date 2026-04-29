namespace CallCenter.Shared.Enums;

/// <summary>
/// Salon modül paketleri. Her modül tam olarak bir gruba aittir — hiçbir modül grupsuz kalmaz.
/// Id=0 Temel Paket her salon için zorunlu ve sabit fiyat (1.700 TL).
/// Fiyatlar KDV dahil aylıktır. Ek şubeler için %10 indirim abonelik hesabında uygulanır.
/// </summary>
public static class SalonModuleGroups
{
    // Id=0 Temel → pricing key 0 ile eşleşir. Diğer gruplar mevcut kayıtlarla uyumlu tutulur.
    public static readonly ModuleGroup Core = new(0, "Core", "Temel Paket", "bi-house-fill", "bg-success", 1700m, 0);
    public static readonly ModuleGroup StockFinance = new(1, "StockFinance", "Stok Tedarik / Finans", "bi-box-seam", "bg-secondary", 400m, 1);
    public static readonly ModuleGroup LoyaltyMarketing = new(3, "LoyaltyMarketing", "Müşteri Sadakati / Pazarlama", "bi-heart-fill", "bg-danger", 1500m, 2);
    public static readonly ModuleGroup Enterprise = new(6, "Enterprise", "Kurumsal", "bi-building", "bg-primary", 200m, 4);

    public static IEnumerable<ModuleGroup> All => new[] { Core, StockFinance, LoyaltyMarketing, Enterprise };
    public static ModuleGroup? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Core = 0;
        public const int StockFinance = 1;
        public const int LoyaltyMarketing = 3;
        /// <summary>Eski paket (Profesyonel). Artık <see cref="All"/> içinde yok; DB kalıntıları için.</summary>
        public const int LegacyProfessional = 5;
        public const int Enterprise = 6;
    }

    /// <summary>
    /// Modül ID → Grup ID mapping. Her modül tam olarak bir gruba aittir; Temel Paket Id=0.
    /// IsDefault=true olan modüller burada Core (0) olarak listelenir.
    /// </summary>
    private static readonly Dictionary<int, int> ModuleGroupMap = new()
    {
        // ── Temel Paket (0) — IsDefault=true, her salonda zorunlu ────────────
        [SalonPortalModules.Ids.SlnDashboard] = Ids.Core,
        [SalonPortalModules.Ids.SlnClients] = Ids.Core,
        [SalonPortalModules.Ids.SlnAppointments] = Ids.Core,
        [SalonPortalModules.Ids.SlnServices] = Ids.Core,
        [SalonPortalModules.Ids.SlnInvoices] = Ids.Core,
        [SalonPortalModules.Ids.SlnCash] = Ids.Core,
        [SalonPortalModules.Ids.SlnStaff] = Ids.Core,
        [SalonPortalModules.Ids.SlnBranches] = Ids.Core,
        [SalonPortalModules.Ids.SlnSales] = Ids.Core,
        [SalonPortalModules.Ids.SlnRecipes] = Ids.Core,
        [SalonPortalModules.Ids.SlnProfile] = Ids.Core,
        [SalonPortalModules.Ids.SlnPersonnelPrices] = Ids.Core,
        [SalonPortalModules.Ids.SlnWaitlist] = Ids.Core,
        [SalonPortalModules.Ids.SlnNoShowPolicy] = Ids.Core,
        [SalonPortalModules.Ids.SlnConsentForms] = Ids.Core,

        // ── Stok Tedarik / Finans (1) — 400 TL ─────────────────────────────
        [SalonPortalModules.Ids.SlnProducts] = Ids.StockFinance,
        [SalonPortalModules.Ids.SlnSuppliers] = Ids.StockFinance,
        [SalonPortalModules.Ids.SlnExpenses] = Ids.StockFinance,

        // ── Müşteri Sadakati / Pazarlama (3) — 1.500 TL ────────────────────
        [SalonPortalModules.Ids.SlnGiftCards] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnPackages] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnMemberships] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnLoyalty] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnCampaigns] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnEmailCampaigns] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnWinback] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnReviews] = Ids.LoyaltyMarketing,
        [SalonPortalModules.Ids.SlnBeforeAfter] = Ids.LoyaltyMarketing,

        // ── Kurumsal (6) — 200 TL ─────────────────────────────────────────
        [SalonPortalModules.Ids.SlnReports] = Ids.Enterprise,
    };

    /// <summary>
    /// Modül ID'sine karşılık gelen grup ID'si.
    /// Bilinmeyen ID için null döner (katalogda tanımsız modül).
    /// </summary>
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
