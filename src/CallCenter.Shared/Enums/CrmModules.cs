namespace CallCenter.Shared.Enums;

/// <summary>
/// CRM uygulamasinin kendi modül kataloğu.
/// Salon kaynaklı müşteri ilişkileri araçları burada CRM namespace karşılığıyla tutulur;
/// kaynak Salon module id'leri veri uyumluluğu için korunur.
/// </summary>
public static class CrmModules
{
    public const int ProductTypeId = 3; // CRM

    public static readonly TypeItem Dashboard = new(301, "CrmDashboard", "CrmModule.Dashboard", "Dashboard", "bi-speedometer2", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Contacts = new(302, "CrmContacts", "CrmModule.Contacts", "Kişiler", "bi-people-fill", "bg-success", 2, isDefault: true);
    public static readonly TypeItem Tickets = new(303, "CrmTickets", "CrmModule.Tickets", "Talepler", "bi-ticket-detailed-fill", "bg-warning text-dark", 3, isDefault: true);
    public static readonly TypeItem Deals = new(304, "CrmDeals", "CrmModule.Deals", "Fırsatlar", "bi-kanban-fill", "bg-success", 4, isDefault: true);
    public static readonly TypeItem Activities = new(305, "CrmActivities", "CrmModule.Activities", "Etkileşimler", "bi-clock-history", "bg-info", 5, isDefault: true);
    public static readonly TypeItem Tasks = new(306, "CrmTasks", "CrmModule.Tasks", "Görevler", "bi-check2-square", "bg-secondary", 6, isDefault: true);
    public static readonly TypeItem Surveys = new(307, "CrmSurveys", "CrmModule.Surveys", "Anketler", "bi-clipboard2-data", "bg-purple", 7, isDefault: true);
    public static readonly TypeItem Campaigns = new(308, "CrmCampaigns", "CrmModule.Campaigns", "Kampanyalar", "bi-megaphone-fill", "bg-pink", 8, isDefault: true);
    public static readonly TypeItem Reports = new(309, "CrmReports", "CrmModule.Reports", "Raporlar", "bi-bar-chart-fill", "bg-orange", 9, isDefault: true);
    public static readonly TypeItem Integrations = new(310, "CrmIntegrations", "CrmModule.Integrations", "Entegrasyonlar", "bi-plug-fill", "bg-dark", 10, isDefault: true);

    public static readonly TypeItem SalonGiftCards = new(401, "CrmSalonGiftCards", "CrmModule.SalonGiftCards", "Hediye Kartları", "bi-gift", "bg-warning text-dark", 101);
    public static readonly TypeItem SalonMemberships = new(402, "CrmSalonMemberships", "CrmModule.SalonMemberships", "Üyelik Planları", "bi-award", "bg-primary", 102);
    public static readonly TypeItem SalonLoyalty = new(403, "CrmSalonLoyalty", "CrmModule.SalonLoyalty", "Sadakat Programı", "bi-star", "bg-warning text-dark", 103);
    public static readonly TypeItem SalonEmailCampaigns = new(404, "CrmSalonEmailCampaigns", "CrmModule.SalonEmailCampaigns", "E-posta Kampanyaları", "bi-envelope", "bg-info", 104);
    public static readonly TypeItem SalonReviews = new(405, "CrmSalonReviews", "CrmModule.SalonReviews", "Yorum Yönetimi", "bi-chat-square-text", "bg-success", 105);
    public static readonly TypeItem SalonWinback = new(406, "CrmSalonWinback", "CrmModule.SalonWinback", "Kayıp Müşteri Geri Kazanım", "bi-arrow-repeat", "bg-orange", 106);
    public static readonly TypeItem SalonBeforeAfter = new(407, "CrmSalonBeforeAfter", "CrmModule.SalonBeforeAfter", "Önce/Sonra Takibi", "bi-images", "bg-pink", 107);
    public static readonly TypeItem SalonExpenses = new(408, "CrmSalonExpenses", "CrmModule.SalonExpenses", "Salon Masraf Takibi", "bi-credit-card", "bg-dark", 108);
    public static readonly TypeItem SalonSuppliers = new(409, "CrmSalonSuppliers", "CrmModule.SalonSuppliers", "Salon Tedarikçi ve Cari", "bi-truck", "bg-teal", 109);
    public static readonly TypeItem SalonReports = new(410, "CrmSalonReports", "CrmModule.SalonReports", "Salon Raporlama", "bi-bar-chart-line", "bg-orange", 110);

    public static IEnumerable<TypeItem> Core => new[]
    {
        Dashboard, Contacts, Tickets, Deals, Activities, Tasks, Surveys, Campaigns, Reports, Integrations
    };

    public static IEnumerable<TypeItem> SalonVertical => new[]
    {
        SalonGiftCards, SalonMemberships, SalonLoyalty, SalonEmailCampaigns, SalonReviews, SalonWinback,
        SalonBeforeAfter, SalonExpenses, SalonSuppliers, SalonReports
    };

    public static IEnumerable<TypeItem> All => Core.Concat(SalonVertical);
    public static IEnumerable<TypeItem> Defaults => All.Where(x => x.IsDefault);

    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static bool IsSalonVertical(int crmModuleId) => SalonModuleMap.ContainsValue(crmModuleId);

    public static TypeItem? GetBySalonModuleId(int salonModuleId)
    {
        if (!SalonModuleMap.TryGetValue(salonModuleId, out var crmModuleId))
            return null;

        return GetById(crmModuleId);
    }

    public static int? GetSalonModuleId(int crmModuleId)
    {
        foreach (var kv in SalonModuleMap)
        {
            if (kv.Value == crmModuleId)
                return kv.Key;
        }

        return null;
    }

    public static bool HasSalonModule(int salonModuleId) => SalonModuleMap.ContainsKey(salonModuleId);

    private static readonly Dictionary<int, int> SalonModuleMap = new()
    {
        [SalonPortalModules.Ids.SlnGiftCards] = Ids.SalonGiftCards,
        [SalonPortalModules.Ids.SlnMemberships] = Ids.SalonMemberships,
        [SalonPortalModules.Ids.SlnLoyalty] = Ids.SalonLoyalty,
        [SalonPortalModules.Ids.SlnCampaigns] = Ids.Campaigns,
        [SalonPortalModules.Ids.SlnEmailCampaigns] = Ids.SalonEmailCampaigns,
        [SalonPortalModules.Ids.SlnReviews] = Ids.SalonReviews,
        [SalonPortalModules.Ids.SlnWinback] = Ids.SalonWinback,
        [SalonPortalModules.Ids.SlnBeforeAfter] = Ids.SalonBeforeAfter,
        [SalonPortalModules.Ids.SlnExpenses] = Ids.SalonExpenses,
        [SalonPortalModules.Ids.SlnSuppliers] = Ids.SalonSuppliers,
        [SalonPortalModules.Ids.SlnReports] = Ids.SalonReports
    };

    public static class Ids
    {
        public const int Dashboard = 301;
        public const int Contacts = 302;
        public const int Tickets = 303;
        public const int Deals = 304;
        public const int Activities = 305;
        public const int Tasks = 306;
        public const int Surveys = 307;
        public const int Campaigns = 308;
        public const int Reports = 309;
        public const int Integrations = 310;
        public const int SalonGiftCards = 401;
        public const int SalonMemberships = 402;
        public const int SalonLoyalty = 403;
        public const int SalonEmailCampaigns = 404;
        public const int SalonReviews = 405;
        public const int SalonWinback = 406;
        public const int SalonBeforeAfter = 407;
        public const int SalonExpenses = 408;
        public const int SalonSuppliers = 409;
        public const int SalonReports = 410;
    }
}
