namespace CallCenter.Shared.Enums;

/// <summary>
/// CRM uygulamasinin kendi modul katalogu.
/// ProductTypes.Crm ortak konteynerdir; Core, Salon ve CallCenter baglamlari CrmModuleGroups ile ayrilir.
/// </summary>
public static class CrmModules
{
    public const int ProductTypeId = 3; // CRM

    public static readonly TypeItem Dashboard = new(301, "CrmDashboard", "CrmModule.Dashboard", "Dashboard", "bi-speedometer2", "bg-primary", 1);
    public static readonly TypeItem Contacts = new(302, "CrmContacts", "CrmModule.Contacts", "Kişiler", "bi-people-fill", "bg-success", 2);
    public static readonly TypeItem Tickets = new(303, "CrmTickets", "CrmModule.Tickets", "Talepler", "bi-ticket-detailed-fill", "bg-warning text-dark", 3);
    public static readonly TypeItem Deals = new(304, "CrmDeals", "CrmModule.Deals", "Fırsatlar", "bi-kanban-fill", "bg-success", 4);
    public static readonly TypeItem Activities = new(305, "CrmActivities", "CrmModule.Activities", "Etkileşimler", "bi-clock-history", "bg-info", 5);
    public static readonly TypeItem Tasks = new(306, "CrmTasks", "CrmModule.Tasks", "Görevler", "bi-check2-square", "bg-secondary", 6);
    public static readonly TypeItem Surveys = new(307, "CrmSurveys", "CrmModule.Surveys", "Anketler", "bi-clipboard2-data", "bg-purple", 7);
    public static readonly TypeItem Campaigns = new(308, "CrmCampaigns", "CrmModule.Campaigns", "Kampanyalar", "bi-megaphone-fill", "bg-pink", 8);
    public static readonly TypeItem Reports = new(309, "CrmReports", "CrmModule.Reports", "Raporlar", "bi-bar-chart-fill", "bg-orange", 9);
    public static readonly TypeItem Integrations = new(310, "CrmIntegrations", "CrmModule.Integrations", "Entegrasyonlar", "bi-plug-fill", "bg-dark", 10);

    public static readonly TypeItem SalonGiftCards = new(401, "CrmSalonGiftCards", "CrmModule.SalonGiftCards", "Hediye Kartları", "bi-gift", "bg-warning text-dark", 101);
    public static readonly TypeItem SalonMemberships = new(402, "CrmSalonMemberships", "CrmModule.SalonMemberships", "Üyelik Planları", "bi-award", "bg-primary", 102);
    public static readonly TypeItem SalonLoyalty = new(403, "CrmSalonLoyalty", "CrmModule.SalonLoyalty", "Sadakat Programı", "bi-star", "bg-warning text-dark", 103);
    public static readonly TypeItem SalonCampaigns = new(404, "CrmSalonCampaigns", "CrmModule.SalonCampaigns", "Pazarlama ve SMS", "bi-megaphone-fill", "bg-pink", 104);
    public static readonly TypeItem SalonEmailCampaigns = new(405, "CrmSalonEmailCampaigns", "CrmModule.SalonEmailCampaigns", "E-posta Kampanyaları", "bi-envelope", "bg-info", 105);
    public static readonly TypeItem SalonReviews = new(406, "CrmSalonReviews", "CrmModule.SalonReviews", "Yorum Yönetimi", "bi-chat-square-text", "bg-success", 106);
    public static readonly TypeItem SalonWinback = new(407, "CrmSalonWinback", "CrmModule.SalonWinback", "Kayıp Müşteri Geri Kazanım", "bi-arrow-repeat", "bg-orange", 107);

    public static readonly TypeItem CallCenterDashboard = new(501, "CrmCallCenterDashboard", "CrmModule.CallCenterDashboard", "CallCenter Dashboard", "bi-speedometer2", "bg-primary", 201);
    public static readonly TypeItem CallCenterContacts = new(502, "CrmCallCenterContacts", "CrmModule.CallCenterContacts", "Çağrı Kişileri", "bi-telephone-inbound", "bg-success", 202);
    public static readonly TypeItem CallCenterTickets = new(503, "CrmCallCenterTickets", "CrmModule.CallCenterTickets", "Destek Talepleri", "bi-ticket-detailed", "bg-warning text-dark", 203);
    public static readonly TypeItem CallCenterActivities = new(504, "CrmCallCenterActivities", "CrmModule.CallCenterActivities", "Çağrı Etkileşimleri", "bi-clock-history", "bg-info", 204);
    public static readonly TypeItem CallCenterCampaigns = new(505, "CrmCallCenterCampaigns", "CrmModule.CallCenterCampaigns", "Arama Kampanyaları", "bi-megaphone", "bg-pink", 205);
    public static readonly TypeItem CallCenterReports = new(506, "CrmCallCenterReports", "CrmModule.CallCenterReports", "CallCenter CRM Raporları", "bi-bar-chart-fill", "bg-orange", 206);

    public static IEnumerable<TypeItem> Core => new[]
    {
        Dashboard, Contacts, Tickets, Deals, Activities, Tasks, Surveys, Campaigns, Reports, Integrations
    };

    public static IEnumerable<TypeItem> SalonVertical => new[]
    {
        SalonGiftCards, SalonMemberships, SalonLoyalty, SalonCampaigns, SalonEmailCampaigns, SalonReviews, SalonWinback
    };

    public static IEnumerable<TypeItem> CallCenterVertical => new[]
    {
        CallCenterDashboard, CallCenterContacts, CallCenterTickets, CallCenterActivities, CallCenterCampaigns, CallCenterReports
    };

    public static IEnumerable<TypeItem> All => Core.Concat(SalonVertical).Concat(CallCenterVertical);
    public static IEnumerable<TypeItem> Defaults => All.Where(x => x.IsDefault);

    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static bool IsSalonVertical(int crmModuleId) => SalonVertical.Any(m => m.Id == crmModuleId);
    public static bool IsCallCenterVertical(int crmModuleId) => CallCenterVertical.Any(m => m.Id == crmModuleId);

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
        [SalonPortalModules.Ids.SlnCampaigns] = Ids.SalonCampaigns,
        [SalonPortalModules.Ids.SlnEmailCampaigns] = Ids.SalonEmailCampaigns,
        [SalonPortalModules.Ids.SlnReviews] = Ids.SalonReviews,
        [SalonPortalModules.Ids.SlnWinback] = Ids.SalonWinback
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
        public const int SalonCampaigns = 404;
        public const int SalonEmailCampaigns = 405;
        public const int SalonReviews = 406;
        public const int SalonWinback = 407;
        public const int CallCenterDashboard = 501;
        public const int CallCenterContacts = 502;
        public const int CallCenterTickets = 503;
        public const int CallCenterActivities = 504;
        public const int CallCenterCampaigns = 505;
        public const int CallCenterReports = 506;
    }
}
