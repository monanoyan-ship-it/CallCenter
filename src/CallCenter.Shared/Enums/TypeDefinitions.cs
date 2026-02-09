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

    public static IEnumerable<TypeItem> All => new[] { Ringing, InProgress, OnHold, Transferred, Completed, Missed, Failed };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>Aktif (devam eden) cagri durumlari</summary>
    public static IEnumerable<TypeItem> ActiveStatuses => new[] { Ringing, InProgress, OnHold };

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

    public static IEnumerable<TypeItem> All => new[] { Dashboard, Calls, Reports, Agents, Queues, Settings, Personnel };
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

    public static IEnumerable<TypeItem> All => new[]
    {
        DashboardView, DashboardExport,
        CallListen, CallMake, CallRecord,
        ReportView, ReportExport,
        AgentView, AgentManage,
        QueueView, QueueManage,
        SettingsView, SettingsManage,
        PersonnelView, PersonnelManage
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
