namespace CallCenter.Shared.Enums;

// ═══════════════════════════════════════════════════════════════
// KULLANICI ROLLERİ
// ═══════════════════════════════════════════════════════════════

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

// ═══════════════════════════════════════════════════════════════
// AGENT DURUMLARI
// ═══════════════════════════════════════════════════════════════

public static class AgentStatuses
{
    public static readonly TypeItem Offline = new(1, "Offline", "AgentStatus.Offline", "Cevrimdisi", "bi-circle-fill", "offline", 1);
    public static readonly TypeItem Available = new(2, "Available", "AgentStatus.Available", "Musait", "bi-circle-fill", "online", 2, isDefault: true);
    public static readonly TypeItem Busy = new(3, "Busy", "AgentStatus.Busy", "Mesgul", "bi-circle-fill", "busy", 3);
    public static readonly TypeItem OnBreak = new(4, "OnBreak", "AgentStatus.OnBreak", "Mola", "bi-circle-fill", "break", 4);
    public static readonly TypeItem InCall = new(5, "InCall", "AgentStatus.InCall", "Aramada", "bi-telephone-fill", "busy", 5);
    public static readonly TypeItem AfterCallWork = new(6, "AfterCallWork", "AgentStatus.AfterCallWork", "Arama sonrasi is", "bi-pencil-fill", "busy", 6);

    public static IEnumerable<TypeItem> All => new[] { Offline, Available, Busy, OnBreak, InCall, AfterCallWork };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>Agent'in manuel secebilecegi durumlar (InCall ve AfterCallWork otomatik)</summary>
    public static IEnumerable<TypeItem> Selectable => new[] { Available, Busy, OnBreak, Offline };

    public static class Ids
    {
        public const int Offline = 1;
        public const int Available = 2;
        public const int Busy = 3;
        public const int OnBreak = 4;
        public const int InCall = 5;
        public const int AfterCallWork = 6;
    }
}

// ═══════════════════════════════════════════════════════════════
// ÇAĞRI DURUMLARI
// ═══════════════════════════════════════════════════════════════

public static class CallStatuses
{
    public static readonly TypeItem Ringing = new(1, "Ringing", "CallStatus.Ringing", "Caliyor", "bi-bell-fill", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(2, "InProgress", "CallStatus.InProgress", "Devam ediyor", "bi-telephone-fill", "bg-success", 2);
    public static readonly TypeItem OnHold = new(3, "OnHold", "CallStatus.OnHold", "Beklemede", "bi-pause-circle-fill", "bg-info", 3);
    public static readonly TypeItem Transferred = new(4, "Transferred", "CallStatus.Transferred", "Transfer edildi", "bi-arrow-left-right", "bg-secondary", 4);
    public static readonly TypeItem Completed = new(5, "Completed", "CallStatus.Completed", "Tamamlandi", "bi-check-circle-fill", "bg-success", 5);
    public static readonly TypeItem Missed = new(6, "Missed", "CallStatus.Missed", "Cevapsiz", "bi-telephone-x-fill", "bg-warning text-dark", 6);
    public static readonly TypeItem Failed = new(7, "Failed", "CallStatus.Failed", "Basarisiz", "bi-x-circle-fill", "bg-danger", 7);
    public static readonly TypeItem Queued = new(8, "Queued", "CallStatus.Queued", "Kuyrukta bekliyor", "bi-hourglass-split", "bg-purple", 8);

    public static IEnumerable<TypeItem> All => new[] { Ringing, InProgress, OnHold, Transferred, Completed, Missed, Failed, Queued };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>Aktif (devam eden) cagri durumlari</summary>
    public static IEnumerable<TypeItem> ActiveStatuses => new[] { Ringing, InProgress, OnHold };

    /// <summary>Kuyrukta bekleyen aramalar dahil aktif durumlar</summary>
    public static IEnumerable<TypeItem> ActiveAndQueuedStatuses => new[] { Ringing, InProgress, OnHold, Queued };

    /// <summary>Sonlanmis cagri durumlari</summary>
    public static IEnumerable<TypeItem> FinishedStatuses => new[] { Transferred, Completed, Missed, Failed };

    public static class Ids
    {
        public const int Ringing = 1;
        public const int InProgress = 2;
        public const int OnHold = 3;
        public const int Transferred = 4;
        public const int Completed = 5;
        public const int Missed = 6;
        public const int Failed = 7;
        public const int Queued = 8;
    }
}

// ═══════════════════════════════════════════════════════════════
// ÇAĞRI YÖNÜ
// ═══════════════════════════════════════════════════════════════

public static class CallDirections
{
    public static readonly TypeItem Inbound = new(1, "Inbound", "CallDirection.Inbound", "Gelen arama", "bi-telephone-inbound-fill", "text-success", 1, isDefault: true);
    public static readonly TypeItem Outbound = new(2, "Outbound", "CallDirection.Outbound", "Giden arama", "bi-telephone-outbound-fill", "text-primary", 2);

    public static IEnumerable<TypeItem> All => new[] { Inbound, Outbound };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Inbound = 1;
        public const int Outbound = 2;
    }
}

// ═══════════════════════════════════════════════════════════════
// PORTAL MODÜLLERİ (Müşteri portalında açılabilir modüller)
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Musteri portalindaki modul tanimlari.
/// Her modul bir yetki kategorisine karsilik gelir.
/// CustomerPortalModule DB tablosu ile hangi musteriye hangi modul acik tutulur.
/// </summary>
public static class PortalModules
{
    public static readonly TypeItem Dashboard = new(1, "Dashboard", "PortalModule.Dashboard", "Gosterge paneli", "bi-speedometer2", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Calls = new(2, "Calls", "PortalModule.Calls", "Arama yonetimi", "bi-telephone-fill", "bg-success", 2, isDefault: true);
    public static readonly TypeItem Reports = new(3, "Reports", "PortalModule.Reports", "Raporlama", "bi-file-earmark-bar-graph", "bg-info", 3);
    public static readonly TypeItem Agents = new(4, "Agents", "PortalModule.Agents", "Temsilci yonetimi", "bi-headset", "bg-warning text-dark", 4);
    public static readonly TypeItem Queues = new(5, "Queues", "PortalModule.Queues", "Kuyruk yonetimi", "bi-people-fill", "bg-secondary", 5);
    public static readonly TypeItem Settings = new(6, "Settings", "PortalModule.Settings", "Ayarlar", "bi-gear-fill", "bg-danger", 6);
    public static readonly TypeItem Personnel = new(7, "Personnel", "PortalModule.Personnel", "Personel yonetimi", "bi-person-badge", "bg-dark", 7, isDefault: true);
    public static readonly TypeItem Organizations = new(8, "Organizations", "PortalModule.Organizations", "Organizasyon yonetimi", "bi-diagram-3-fill", "bg-indigo", 8);

    // Yeni moduller (Faz 4 — sektorel arastirma sonucu)
    public static readonly TypeItem SipSettings = new(9, "SipSettings", "PortalModule.SipSettings", "SIP/VoIP yapilandirmasi", "bi-router-fill", "bg-teal", 9);
    public static readonly TypeItem CallRecords = new(10, "CallRecords", "PortalModule.CallRecords", "Arama kaydi dinleme/yonetimi", "bi-record-circle", "bg-orange", 10);
    public static readonly TypeItem QualityManagement = new(11, "QualityManagement", "PortalModule.QualityManagement", "Kalite degerlendirme formlari", "bi-clipboard-check", "bg-pink", 11);
    public static readonly TypeItem KnowledgeBase = new(12, "KnowledgeBase", "PortalModule.KnowledgeBase", "Bilgi bankasi, agent senaryolari", "bi-book", "bg-cyan", 12);
    public static readonly TypeItem Integrations = new(13, "Integrations", "PortalModule.Integrations", "API/webhook/CRM entegrasyonlari", "bi-plug-fill", "bg-purple", 13);
    public static readonly TypeItem Campaigns = new(14, "Campaigns", "PortalModule.Campaigns", "Giden arama kampanyalari", "bi-megaphone-fill", "bg-yellow text-dark", 14);

    public static IEnumerable<TypeItem> All => new[] { Dashboard, Calls, Reports, Agents, Queues, Settings, Personnel, Organizations, SipSettings, CallRecords, QualityManagement, KnowledgeBase, Integrations, Campaigns };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>Yeni musteri olusturuldiginda varsayilan olarak acilacak moduller</summary>
    public static IEnumerable<TypeItem> Defaults => All.Where(x => x.IsDefault);

    public static class Ids
    {
        public const int Dashboard = 1;
        public const int Calls = 2;
        public const int Reports = 3;
        public const int Agents = 4;
        public const int Queues = 5;
        public const int Settings = 6;
        public const int Personnel = 7;
        public const int Organizations = 8;
        public const int SipSettings = 9;
        public const int CallRecords = 10;
        public const int QualityManagement = 11;
        public const int KnowledgeBase = 12;
        public const int Integrations = 13;
        public const int Campaigns = 14;
    }
}

// ═══════════════════════════════════════════════════════════════
// MÜŞTERİ YETKİ TİPLERİ
// ═══════════════════════════════════════════════════════════════

public static class CustomerPermissionTypes
{
    // Dashboard
    public static readonly TypeItem DashboardView = new(1, "DashboardView", "CustomerPermission.DashboardView", "Dashboard goruntuleyebilir", "bi-speedometer2", "bg-primary", 1);
    public static readonly TypeItem DashboardExport = new(2, "DashboardExport", "CustomerPermission.DashboardExport", "Dashboard verisi disari aktarabilir", "bi-download", "bg-primary", 2);

    // Call (Arama)
    public static readonly TypeItem CallListen = new(10, "CallListen", "CustomerPermission.CallListen", "Aramalari dinleyebilir", "bi-ear-fill", "bg-success", 10);
    public static readonly TypeItem CallMake = new(11, "CallMake", "CustomerPermission.CallMake", "Arama yapabilir", "bi-telephone-outbound-fill", "bg-success", 11);
    public static readonly TypeItem CallRecord = new(12, "CallRecord", "CustomerPermission.CallRecord", "Arama kayitlarini dinleyebilir", "bi-record-circle", "bg-success", 12);

    // Report (Rapor)
    public static readonly TypeItem ReportView = new(20, "ReportView", "CustomerPermission.ReportView", "Raporlari goruntuleyebilir", "bi-file-earmark-bar-graph", "bg-info", 20);
    public static readonly TypeItem ReportExport = new(21, "ReportExport", "CustomerPermission.ReportExport", "Raporlari disari aktarabilir", "bi-download", "bg-info", 21);

    // Agent (Temsilci)
    public static readonly TypeItem AgentView = new(30, "AgentView", "CustomerPermission.AgentView", "Temsilcileri goruntuleyebilir", "bi-headset", "bg-warning text-dark", 30);
    public static readonly TypeItem AgentManage = new(31, "AgentManage", "CustomerPermission.AgentManage", "Temsilcileri yonetebilir", "bi-person-gear", "bg-warning text-dark", 31);

    // Queue (Kuyruk)
    public static readonly TypeItem QueueView = new(40, "QueueView", "CustomerPermission.QueueView", "Kuyruklari goruntuleyebilir", "bi-people-fill", "bg-secondary", 40);
    public static readonly TypeItem QueueManage = new(41, "QueueManage", "CustomerPermission.QueueManage", "Kuyruklari yonetebilir", "bi-diagram-3-fill", "bg-secondary", 41);

    // Settings (Ayarlar)
    public static readonly TypeItem SettingsView = new(50, "SettingsView", "CustomerPermission.SettingsView", "Ayarlari goruntuleyebilir", "bi-gear", "bg-danger", 50);
    public static readonly TypeItem SettingsManage = new(51, "SettingsManage", "CustomerPermission.SettingsManage", "Ayarlari yonetebilir", "bi-gear-fill", "bg-danger", 51);

    // Personnel (Personel)
    public static readonly TypeItem PersonnelView = new(60, "PersonnelView", "CustomerPermission.PersonnelView", "Personeli goruntuleyebilir", "bi-people", "bg-dark", 60);
    public static readonly TypeItem PersonnelManage = new(61, "PersonnelManage", "CustomerPermission.PersonnelManage", "Personeli yonetebilir", "bi-person-plus-fill", "bg-dark", 61);

    // Organizations (Organizasyon)
    public static readonly TypeItem OrgView = new(70, "OrgView", "CustomerPermission.OrgView", "Organizasyonlari goruntuleyebilir", "bi-diagram-3", "bg-indigo", 70);
    public static readonly TypeItem OrgManage = new(71, "OrgManage", "CustomerPermission.OrgManage", "Organizasyonlari yonetebilir", "bi-diagram-3-fill", "bg-indigo", 71);

    // SipSettings (SIP Ayarlari)
    public static readonly TypeItem SipView = new(80, "SipView", "CustomerPermission.SipView", "SIP hesaplarini goruntuleyebilir", "bi-router", "bg-teal", 80);
    public static readonly TypeItem SipManage = new(81, "SipManage", "CustomerPermission.SipManage", "SIP hesaplarini yonetebilir", "bi-router-fill", "bg-teal", 81);

    // CallRecords (Arama Kayitlari)
    public static readonly TypeItem RecordListen = new(90, "RecordListen", "CustomerPermission.RecordListen", "Arama kayitlarini dinleyebilir", "bi-play-circle", "bg-orange", 90);
    public static readonly TypeItem RecordDownload = new(91, "RecordDownload", "CustomerPermission.RecordDownload", "Arama kayitlarini indirebilir", "bi-download", "bg-orange", 91);
    public static readonly TypeItem RecordDelete = new(92, "RecordDelete", "CustomerPermission.RecordDelete", "Arama kayitlarini silebilir", "bi-trash", "bg-orange", 92);

    // QualityManagement (Kalite Yonetimi)
    public static readonly TypeItem QualityView = new(100, "QualityView", "CustomerPermission.QualityView", "Kalite degerlendirmelerini goruntuleyebilir", "bi-clipboard-data", "bg-pink", 100);
    public static readonly TypeItem QualityManage = new(101, "QualityManage", "CustomerPermission.QualityManage", "Kalite formlarini yonetebilir", "bi-clipboard-check", "bg-pink", 101);
    public static readonly TypeItem QualityScore = new(102, "QualityScore", "CustomerPermission.QualityScore", "Kalite puanlamasi yapabilir", "bi-star-fill", "bg-pink", 102);

    // KnowledgeBase (Bilgi Bankasi)
    public static readonly TypeItem KBView = new(110, "KBView", "CustomerPermission.KBView", "Bilgi bankasini goruntuleyebilir", "bi-book", "bg-cyan", 110);
    public static readonly TypeItem KBManage = new(111, "KBManage", "CustomerPermission.KBManage", "Bilgi bankasini yonetebilir", "bi-book-fill", "bg-cyan", 111);

    // Integrations (Entegrasyonlar)
    public static readonly TypeItem IntegrationView = new(120, "IntegrationView", "CustomerPermission.IntegrationView", "Entegrasyonlari goruntuleyebilir", "bi-plug", "bg-purple", 120);
    public static readonly TypeItem IntegrationManage = new(121, "IntegrationManage", "CustomerPermission.IntegrationManage", "Entegrasyonlari yonetebilir", "bi-plug-fill", "bg-purple", 121);

    // Campaigns (Kampanyalar)
    public static readonly TypeItem CampaignView = new(130, "CampaignView", "CustomerPermission.CampaignView", "Kampanyalari goruntuleyebilir", "bi-megaphone", "bg-yellow text-dark", 130);
    public static readonly TypeItem CampaignManage = new(131, "CampaignManage", "CustomerPermission.CampaignManage", "Kampanyalari yonetebilir", "bi-megaphone-fill", "bg-yellow text-dark", 131);
    public static readonly TypeItem CampaignExecute = new(132, "CampaignExecute", "CustomerPermission.CampaignExecute", "Kampanya calistirabilir", "bi-play-fill", "bg-yellow text-dark", 132);

    public static IEnumerable<TypeItem> All => new[]
    {
        DashboardView, DashboardExport,
        CallListen, CallMake, CallRecord,
        ReportView, ReportExport,
        AgentView, AgentManage,
        QueueView, QueueManage,
        SettingsView, SettingsManage,
        PersonnelView, PersonnelManage,
        OrgView, OrgManage,
        SipView, SipManage,
        RecordListen, RecordDownload, RecordDelete,
        QualityView, QualityManage, QualityScore,
        KBView, KBManage,
        IntegrationView, IntegrationManage,
        CampaignView, CampaignManage, CampaignExecute
    };

    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>Belirli bir moduldeki yetki tiplerini getirir</summary>
    public static IEnumerable<TypeItem> GetByModule(int moduleId)
    {
        return moduleId switch
        {
            PortalModules.Ids.Dashboard => new[] { DashboardView, DashboardExport },
            PortalModules.Ids.Calls => new[] { CallListen, CallMake, CallRecord },
            PortalModules.Ids.Reports => new[] { ReportView, ReportExport },
            PortalModules.Ids.Agents => new[] { AgentView, AgentManage },
            PortalModules.Ids.Queues => new[] { QueueView, QueueManage },
            PortalModules.Ids.Settings => new[] { SettingsView, SettingsManage },
            PortalModules.Ids.Personnel => new[] { PersonnelView, PersonnelManage },
            PortalModules.Ids.Organizations => new[] { OrgView, OrgManage },
            PortalModules.Ids.SipSettings => new[] { SipView, SipManage },
            PortalModules.Ids.CallRecords => new[] { RecordListen, RecordDownload, RecordDelete },
            PortalModules.Ids.QualityManagement => new[] { QualityView, QualityManage, QualityScore },
            PortalModules.Ids.KnowledgeBase => new[] { KBView, KBManage },
            PortalModules.Ids.Integrations => new[] { IntegrationView, IntegrationManage },
            PortalModules.Ids.Campaigns => new[] { CampaignView, CampaignManage, CampaignExecute },
            _ => Enumerable.Empty<TypeItem>()
        };
    }

    /// <summary>Bir yetki tipinin hangi modüle ait oldugunu bul</summary>
    public static int GetModuleId(int permissionTypeId)
    {
        return permissionTypeId switch
        {
            >= 1 and <= 9 => PortalModules.Ids.Dashboard,
            >= 10 and <= 19 => PortalModules.Ids.Calls,
            >= 20 and <= 29 => PortalModules.Ids.Reports,
            >= 30 and <= 39 => PortalModules.Ids.Agents,
            >= 40 and <= 49 => PortalModules.Ids.Queues,
            >= 50 and <= 59 => PortalModules.Ids.Settings,
            >= 60 and <= 69 => PortalModules.Ids.Personnel,
            >= 70 and <= 79 => PortalModules.Ids.Organizations,
            >= 80 and <= 89 => PortalModules.Ids.SipSettings,
            >= 90 and <= 99 => PortalModules.Ids.CallRecords,
            >= 100 and <= 109 => PortalModules.Ids.QualityManagement,
            >= 110 and <= 119 => PortalModules.Ids.KnowledgeBase,
            >= 120 and <= 129 => PortalModules.Ids.Integrations,
            >= 130 and <= 139 => PortalModules.Ids.Campaigns,
            _ => 0
        };
    }

    public static class Ids
    {
        // Dashboard
        public const int DashboardView = 1;
        public const int DashboardExport = 2;
        // Call
        public const int CallListen = 10;
        public const int CallMake = 11;
        public const int CallRecord = 12;
        // Report
        public const int ReportView = 20;
        public const int ReportExport = 21;
        // Agent
        public const int AgentView = 30;
        public const int AgentManage = 31;
        // Queue
        public const int QueueView = 40;
        public const int QueueManage = 41;
        // Settings
        public const int SettingsView = 50;
        public const int SettingsManage = 51;
        // Personnel
        public const int PersonnelView = 60;
        public const int PersonnelManage = 61;
        // Organizations
        public const int OrgView = 70;
        public const int OrgManage = 71;
        // SipSettings
        public const int SipView = 80;
        public const int SipManage = 81;
        // CallRecords
        public const int RecordListen = 90;
        public const int RecordDownload = 91;
        public const int RecordDelete = 92;
        // QualityManagement
        public const int QualityView = 100;
        public const int QualityManage = 101;
        public const int QualityScore = 102;
        // KnowledgeBase
        public const int KBView = 110;
        public const int KBManage = 111;
        // Integrations
        public const int IntegrationView = 120;
        public const int IntegrationManage = 121;
        // Campaigns
        public const int CampaignView = 130;
        public const int CampaignManage = 131;
        public const int CampaignExecute = 132;
    }
}

// ═══════════════════════════════════════════════════════════════
// ORGANİZASYON BİRİM TİPLERİ
// ═══════════════════════════════════════════════════════════════

public static class OrganizationUnitTypes
{
    public static readonly TypeItem Region = new(1, "Region", "OrgUnit.Region", "Bolge", "bi-geo-alt-fill", "bg-danger", 1);
    public static readonly TypeItem Branch = new(2, "Branch", "OrgUnit.Branch", "Sube", "bi-building", "bg-primary", 2);
    public static readonly TypeItem Department = new(3, "Department", "OrgUnit.Department", "Departman", "bi-diagram-3-fill", "bg-success", 3);
    public static readonly TypeItem Unit = new(4, "Unit", "OrgUnit.Unit", "Birim", "bi-collection", "bg-info", 4);
    public static readonly TypeItem Team = new(5, "Team", "OrgUnit.Team", "Takim", "bi-people-fill", "bg-warning text-dark", 5);

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

// ═══════════════════════════════════════════════════════════════
// YETKİ KAPSAMI (SCOPE)
// ═══════════════════════════════════════════════════════════════

public static class PermissionScopes
{
    public static readonly TypeItem All = new(1, "All", "PermissionScope.All", "Tum kaynaklara erisim", "bi-globe", "bg-success", 1);
    public static readonly TypeItem Own = new(2, "Own", "PermissionScope.Own", "Sadece kendi olusturdugu kaynaklar", "bi-person", "bg-primary", 2);
    public static readonly TypeItem Customer = new(3, "Customer", "PermissionScope.Customer", "Kendi musterisine ait kaynaklar", "bi-person-badge", "bg-secondary", 3, isDefault: true);

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

// ═══════════════════════════════════════════════════════════════
// KONFERANS DURUMLARI
// ═══════════════════════════════════════════════════════════════

public static class ConferenceStatuses
{
    public static readonly TypeItem Active = new(1, "Active", "Conference.Active", "Aktif konferans", "bi-people-fill", "bg-success", 1, isDefault: true);
    public static readonly TypeItem Ended = new(2, "Ended", "Conference.Ended", "Sonlandi", "bi-check-circle-fill", "bg-secondary", 2);
    public static readonly TypeItem Cancelled = new(3, "Cancelled", "Conference.Cancelled", "Iptal edildi", "bi-x-circle-fill", "bg-danger", 3);

    public static IEnumerable<TypeItem> All => new[] { Active, Ended, Cancelled };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Active = 1;
        public const int Ended = 2;
        public const int Cancelled = 3;
    }
}

// ═══════════════════════════════════════════════════════════════
// KONFERANS KATILIMCI ROLLERİ
// ═══════════════════════════════════════════════════════════════

public static class ConferenceParticipantRoles
{
    public static readonly TypeItem Host = new(1, "Host", "ConfRole.Host", "Konferans sahibi", "bi-star-fill", "bg-warning text-dark", 1);
    public static readonly TypeItem Participant = new(2, "Participant", "ConfRole.Participant", "Katilimci", "bi-person-fill", "bg-primary", 2, isDefault: true);
    public static readonly TypeItem Listener = new(3, "Listener", "ConfRole.Listener", "Dinleyici (sessiz izleme)", "bi-ear-fill", "bg-info", 3);

    public static IEnumerable<TypeItem> All => new[] { Host, Participant, Listener };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Host = 1;
        public const int Participant = 2;
        public const int Listener = 3;
    }
}

// ═══════════════════════════════════════════════════════════════
// KONFERANS KATILIMCI DURUMLARI
// ═══════════════════════════════════════════════════════════════

public static class ConferenceParticipantStatuses
{
    public static readonly TypeItem Invited = new(1, "Invited", "ConfPart.Invited", "Davet edildi", "bi-envelope-fill", "bg-info", 1);
    public static readonly TypeItem Joining = new(2, "Joining", "ConfPart.Joining", "Katiliyor", "bi-hourglass-split", "bg-warning text-dark", 2);
    public static readonly TypeItem Joined = new(3, "Joined", "ConfPart.Joined", "Katildi", "bi-check-circle-fill", "bg-success", 3, isDefault: true);
    public static readonly TypeItem Left = new(4, "Left", "ConfPart.Left", "Ayrildi", "bi-box-arrow-right", "bg-secondary", 4);
    public static readonly TypeItem Muted = new(5, "Muted", "ConfPart.Muted", "Sessiz", "bi-mic-mute-fill", "bg-warning text-dark", 5);
    public static readonly TypeItem Kicked = new(6, "Kicked", "ConfPart.Kicked", "Cikarildi", "bi-x-circle-fill", "bg-danger", 6);

    public static IEnumerable<TypeItem> All => new[] { Invited, Joining, Joined, Left, Muted, Kicked };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Invited = 1;
        public const int Joining = 2;
        public const int Joined = 3;
        public const int Left = 4;
        public const int Muted = 5;
        public const int Kicked = 6;
    }
}

// ═══════════════════════════════════════════════════════════════
// İZLEME (MONITORING) MODLARI
// ═══════════════════════════════════════════════════════════════

public static class MonitoringModes
{
    public static readonly TypeItem Silent = new(1, "Silent", "Monitor.Silent", "Sessiz dinleme", "bi-ear-fill", "bg-info", 1, isDefault: true);
    public static readonly TypeItem Whisper = new(2, "Whisper", "Monitor.Whisper", "Fisildama (sadece agent duyar)", "bi-chat-dots-fill", "bg-warning text-dark", 2);
    public static readonly TypeItem Barge = new(3, "Barge", "Monitor.Barge", "Aramaya katilma (herkes duyar)", "bi-megaphone-fill", "bg-danger", 3);

    public static IEnumerable<TypeItem> All => new[] { Silent, Whisper, Barge };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Silent = 1;
        public const int Whisper = 2;
        public const int Barge = 3;
    }
}

// ═══════════════════════════════════════════════════════════════
// SES CODEC'LERİ (Audio Codecs)
// ═══════════════════════════════════════════════════════════════

public static class AudioCodecs
{
    public static readonly TypeItem PCMU = new(1, "PCMU", "AudioCodec.PCMU", "G.711 µ-law (8kHz, 64kbps)", "bi-soundwave", "bg-secondary", 1);
    public static readonly TypeItem PCMA = new(2, "PCMA", "AudioCodec.PCMA", "G.711 A-law (8kHz, 64kbps)", "bi-soundwave", "bg-secondary", 2);
    public static readonly TypeItem G722 = new(3, "G722", "AudioCodec.G722", "G.722 Wideband (16kHz, 64kbps)", "bi-soundwave", "bg-info", 3);
    public static readonly TypeItem Opus = new(4, "Opus", "AudioCodec.Opus", "Opus (8-48kHz, 6-510kbps, adaptif)", "bi-soundwave", "bg-success", 4, isDefault: true);
    public static readonly TypeItem G726 = new(5, "G726", "AudioCodec.G726", "G.726 ADPCM (8kHz, 32kbps)", "bi-soundwave", "bg-secondary", 5);
    public static readonly TypeItem Speex = new(6, "Speex", "AudioCodec.Speex", "Speex (8-32kHz, degisken)", "bi-soundwave", "bg-warning text-dark", 6);
    public static readonly TypeItem ILBC = new(7, "iLBC", "AudioCodec.iLBC", "iLBC (8kHz, 13.3/15.2kbps)", "bi-soundwave", "bg-dark", 7);

    public static IEnumerable<TypeItem> All => new[] { PCMU, PCMA, G722, Opus, G726, Speex, ILBC };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>Varsayilan codec oncelik sirasi (en yuksek kalite once)</summary>
    public static IEnumerable<TypeItem> DefaultPriority => new[] { Opus, G722, PCMU, PCMA };

    /// <summary>Web (WebRTC) tarafinda desteklenen codec'ler</summary>
    public static IEnumerable<TypeItem> WebSupported => new[] { Opus, G722, PCMU, PCMA };

    /// <summary>Windows (SIPSorcery) tarafinda desteklenen codec'ler</summary>
    public static IEnumerable<TypeItem> WindowsSupported => All;

    public static class Ids
    {
        public const int PCMU = 1;
        public const int PCMA = 2;
        public const int G722 = 3;
        public const int Opus = 4;
        public const int G726 = 5;
        public const int Speex = 6;
        public const int ILBC = 7;
    }
}

// ═══════════════════════════════════════════════════════════════
// VİDEO CODEC'LERİ (Video Codecs)
// ═══════════════════════════════════════════════════════════════

public static class VideoCodecs
{
    public static readonly TypeItem VP8 = new(1, "VP8", "VideoCodec.VP8", "VP8 (WebRTC varsayilan, 720p)", "bi-camera-video", "bg-success", 1, isDefault: true);
    public static readonly TypeItem H264 = new(2, "H264", "VideoCodec.H264", "H.264/AVC (yuksek uyumluluk)", "bi-camera-video-fill", "bg-primary", 2);
    public static readonly TypeItem VP9 = new(3, "VP9", "VideoCodec.VP9", "VP9 (verimli, yuksek kalite)", "bi-camera-video", "bg-info", 3);

    public static IEnumerable<TypeItem> All => new[] { VP8, H264, VP9 };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>Web (WebRTC) tarafinda desteklenen video codec'ler</summary>
    public static IEnumerable<TypeItem> WebSupported => All;

    /// <summary>Windows (SIPSorcery) tarafinda desteklenen video codec'ler</summary>
    public static IEnumerable<TypeItem> WindowsSupported => new[] { VP8, H264 };

    public static class Ids
    {
        public const int VP8 = 1;
        public const int H264 = 2;
        public const int VP9 = 3;
    }
}

// ═══════════════════════════════════════════════════════════════
// MESAJ TİPLERİ (Instant Messaging)
// ═══════════════════════════════════════════════════════════════

public static class MessageTypes
{
    public static readonly TypeItem Text = new(1, "Text", "MessageType.Text", "Metin mesaji", "bi-chat-dots", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem System = new(2, "System", "MessageType.System", "Sistem mesaji", "bi-info-circle", "bg-secondary", 2);
    public static readonly TypeItem File = new(3, "File", "MessageType.File", "Dosya mesaji", "bi-paperclip", "bg-info", 3);

    public static IEnumerable<TypeItem> All => new[] { Text, System, File };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Text = 1;
        public const int System = 2;
        public const int File = 3;
    }
}

// ═══════════════════════════════════════════════════════════════
// SIP PRESENCE DURUMLARI (RFC 3856 / RFC 4480)
// AgentStatuses ile eslestirilmis SIP presence durumlari
// ═══════════════════════════════════════════════════════════════

public static class SipPresenceStatuses
{
    // RFC 3863 PIDF basic status + RFC 4480 RPID activities
    public static readonly TypeItem Offline = new(1, "closed", "SipPresence.Offline", "Cevrimdisi (closed)", "bi-circle", "offline", 1);
    public static readonly TypeItem Online = new(2, "open", "SipPresence.Online", "Cevrimici (open)", "bi-circle-fill", "online", 2, isDefault: true);
    public static readonly TypeItem Busy = new(3, "busy", "SipPresence.Busy", "Mesgul (busy)", "bi-circle-fill", "busy", 3);
    public static readonly TypeItem Away = new(4, "away", "SipPresence.Away", "Uzakta (away)", "bi-circle-fill", "break", 4);
    public static readonly TypeItem OnThePhone = new(5, "on-the-phone", "SipPresence.OnThePhone", "Aramada (on-the-phone)", "bi-telephone-fill", "busy", 5);
    public static readonly TypeItem DoNotDisturb = new(6, "dnd", "SipPresence.DND", "Rahatsiz etmeyin (DND)", "bi-slash-circle-fill", "busy", 6);

    public static IEnumerable<TypeItem> All => new[] { Offline, Online, Busy, Away, OnThePhone, DoNotDisturb };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySipStatus(string sipStatus) => All.FirstOrDefault(x => x.SystemName == sipStatus);

    /// <summary>AgentStatuses ID → SIP Presence durumu eslestirmesi</summary>
    public static TypeItem FromAgentStatus(int agentStatusId) => agentStatusId switch
    {
        1 => Offline,      // AgentStatuses.Offline → closed
        2 => Online,       // AgentStatuses.Available → open
        3 => Busy,         // AgentStatuses.Busy → busy
        4 => Away,         // AgentStatuses.OnBreak → away
        5 => OnThePhone,   // AgentStatuses.InCall → on-the-phone
        6 => DoNotDisturb, // AgentStatuses.AfterCallWork → dnd
        _ => Offline
    };

    /// <summary>SIP Presence durumu → AgentStatuses ID eslestirmesi</summary>
    public static int ToAgentStatusId(string sipStatus) => sipStatus switch
    {
        "closed" => AgentStatuses.Ids.Offline,
        "open" => AgentStatuses.Ids.Available,
        "busy" => AgentStatuses.Ids.Busy,
        "away" => AgentStatuses.Ids.OnBreak,
        "on-the-phone" => AgentStatuses.Ids.InCall,
        "dnd" => AgentStatuses.Ids.AfterCallWork,
        _ => AgentStatuses.Ids.Offline
    };

    public static class Ids
    {
        public const int Offline = 1;
        public const int Online = 2;
        public const int Busy = 3;
        public const int Away = 4;
        public const int OnThePhone = 5;
        public const int DoNotDisturb = 6;
    }
}

// ═══════════════════════════════════════════════════════════════
// MÜŞTERİ ROLLERİ (Sabit firma rolleri)
// ═══════════════════════════════════════════════════════════════

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

    public static class Ids
    {
        public const int FirmaAdmin = 1;
        public const int EkipLideri = 2;
        public const int Operator = 3;
    }
}

// ═══════════════════════════════════════════════════════════════
// DEPOLAMA SAĞLAYICILARI (Cloud Storage Providers)
// ═══════════════════════════════════════════════════════════════

public static class StorageProviders
{
    public static readonly TypeItem GoogleDrive = new(1, "GoogleDrive", "StorageProvider.GoogleDrive", "Google Drive", "bi-google", "bg-danger", 1);
    public static readonly TypeItem OneDrive = new(2, "OneDrive", "StorageProvider.OneDrive", "Microsoft OneDrive", "bi-microsoft", "bg-primary", 2);
    public static readonly TypeItem YandexDisk = new(3, "YandexDisk", "StorageProvider.YandexDisk", "Yandex Disk", "bi-cloud-fill", "bg-warning text-dark", 3);
    public static readonly TypeItem AmazonS3 = new(4, "AmazonS3", "StorageProvider.AmazonS3", "Amazon S3", "bi-cloud-arrow-up-fill", "bg-warning", 4);
    public static readonly TypeItem MinIO = new(5, "MinIO", "StorageProvider.MinIO", "MinIO (S3 uyumlu)", "bi-hdd-rack-fill", "bg-dark", 5);

    public static IEnumerable<TypeItem> All => new[] { GoogleDrive, OneDrive, YandexDisk, AmazonS3, MinIO };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int GoogleDrive = 1;
        public const int OneDrive = 2;
        public const int YandexDisk = 3;
        public const int AmazonS3 = 4;
        public const int MinIO = 5;
    }
}

// ═══════════════════════════════════════════════════════════════
// REHBER KAYNAKLARI (Contact Sources)
// ═══════════════════════════════════════════════════════════════

public static class ContactSources
{
    public static readonly TypeItem Manual = new(1, "Manual", "ContactSource.Manual", "Manuel eklendi", "bi-person-plus", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem LDAP = new(2, "LDAP", "ContactSource.LDAP", "LDAP/Active Directory", "bi-diagram-3-fill", "bg-info", 2);
    public static readonly TypeItem CSV = new(3, "CSV", "ContactSource.CSV", "CSV dosyasindan icerildi", "bi-filetype-csv", "bg-success", 3);

    public static IEnumerable<TypeItem> All => new[] { Manual, LDAP, CSV };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Manual = 1;
        public const int LDAP = 2;
        public const int CSV = 3;
    }
}
