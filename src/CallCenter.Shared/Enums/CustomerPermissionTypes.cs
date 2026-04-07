namespace CallCenter.Shared.Enums;

public static class CustomerPermissionTypes
{
    // Dashboard
    public static readonly TypeItem DashboardView = new(1, "DashboardView", "CustomerPermission.DashboardView", "Dashboard Görüntüleyebilir", "bi-speedometer2", "bg-primary", 1);
    public static readonly TypeItem DashboardExport = new(2, "DashboardExport", "CustomerPermission.DashboardExport", "Dashboard Verisi Dışarı Aktarabilir", "bi-download", "bg-primary", 2);

    // Call (Arama)
    public static readonly TypeItem CallListen = new(10, "CallListen", "CustomerPermission.CallListen", "Aramaları Dinleyebilir", "bi-ear-fill", "bg-success", 10);
    public static readonly TypeItem CallMake = new(11, "CallMake", "CustomerPermission.CallMake", "Arama Yapabilir", "bi-telephone-outbound-fill", "bg-success", 11);
    public static readonly TypeItem CallRecord = new(12, "CallRecord", "CustomerPermission.CallRecord", "Arama Kayıtlarını Dinleyebilir", "bi-record-circle", "bg-success", 12);

    // Report (Rapor)
    public static readonly TypeItem ReportView = new(20, "ReportView", "CustomerPermission.ReportView", "Raporları Görüntüleyebilir", "bi-file-earmark-bar-graph", "bg-info", 20);
    public static readonly TypeItem ReportExport = new(21, "ReportExport", "CustomerPermission.ReportExport", "Raporları Dışarı Aktarabilir", "bi-download", "bg-info", 21);

    // Agent (Temsilci)
    public static readonly TypeItem AgentView = new(30, "AgentView", "CustomerPermission.AgentView", "Temsilcileri Görüntüleyebilir", "bi-headset", "bg-warning text-dark", 30);
    public static readonly TypeItem AgentManage = new(31, "AgentManage", "CustomerPermission.AgentManage", "Temsilcileri Yönetebilir", "bi-person-gear", "bg-warning text-dark", 31);

    // Queue (Kuyruk)
    public static readonly TypeItem QueueView = new(40, "QueueView", "CustomerPermission.QueueView", "Kuyrukları Görüntüleyebilir", "bi-people-fill", "bg-secondary", 40);
    public static readonly TypeItem QueueManage = new(41, "QueueManage", "CustomerPermission.QueueManage", "Kuyrukları Yönetebilir", "bi-diagram-3-fill", "bg-secondary", 41);

    // Settings (Ayarlar)
    public static readonly TypeItem SettingsView = new(50, "SettingsView", "CustomerPermission.SettingsView", "Ayarları Görüntüleyebilir", "bi-gear", "bg-danger", 50);
    public static readonly TypeItem SettingsManage = new(51, "SettingsManage", "CustomerPermission.SettingsManage", "Ayarları Yönetebilir", "bi-gear-fill", "bg-danger", 51);

    // Personnel (Personel)
    public static readonly TypeItem PersonnelView = new(60, "PersonnelView", "CustomerPermission.PersonnelView", "Personeli Görüntüleyebilir", "bi-people", "bg-dark", 60);
    public static readonly TypeItem PersonnelManage = new(61, "PersonnelManage", "CustomerPermission.PersonnelManage", "Personeli Yönetebilir", "bi-person-plus-fill", "bg-dark", 61);

    // Organizations (Organizasyon)
    public static readonly TypeItem OrgView = new(70, "OrgView", "CustomerPermission.OrgView", "Organizasyonları Görüntüleyebilir", "bi-diagram-3", "bg-indigo", 70);
    public static readonly TypeItem OrgManage = new(71, "OrgManage", "CustomerPermission.OrgManage", "Organizasyonları Yönetebilir", "bi-diagram-3-fill", "bg-indigo", 71);

    // SipSettings (SIP Ayarlari)
    public static readonly TypeItem SipView = new(80, "SipView", "CustomerPermission.SipView", "SIP Hesaplarını Görüntüleyebilir", "bi-router", "bg-teal", 80);
    public static readonly TypeItem SipManage = new(81, "SipManage", "CustomerPermission.SipManage", "SIP Hesaplarını Yönetebilir", "bi-router-fill", "bg-teal", 81);

    // CallRecords (Arama Kayitlari)
    public static readonly TypeItem RecordListen = new(90, "RecordListen", "CustomerPermission.RecordListen", "Arama Kayıtlarını Dinleyebilir", "bi-play-circle", "bg-orange", 90);
    public static readonly TypeItem RecordDownload = new(91, "RecordDownload", "CustomerPermission.RecordDownload", "Arama Kayıtlarını İndirebilir", "bi-download", "bg-orange", 91);
    public static readonly TypeItem RecordDelete = new(92, "RecordDelete", "CustomerPermission.RecordDelete", "Arama Kayıtlarını Silebilir", "bi-trash", "bg-orange", 92);

    // CrmQualityManagement (Kalite Yonetimi)
    public static readonly TypeItem CrmQualityView = new(100, "CrmQualityView", "CustomerPermission.CrmQualityView", "Kalite Değerlendirmelerini Görüntüleyebilir", "bi-clipboard-data", "bg-pink", 100);
    public static readonly TypeItem CrmQualityManage = new(101, "CrmQualityManage", "CustomerPermission.CrmQualityManage", "Kalite Formlarını Yönetebilir", "bi-clipboard-check", "bg-pink", 101);
    public static readonly TypeItem CrmQualityScore = new(102, "CrmQualityScore", "CustomerPermission.CrmQualityScore", "Kalite Puanlaması Yapabilir", "bi-star-fill", "bg-pink", 102);

    // KnowledgeBase (Bilgi Bankasi)
    public static readonly TypeItem KBView = new(110, "KBView", "CustomerPermission.KBView", "Bilgi Bankasını Görüntüleyebilir", "bi-book", "bg-cyan", 110);
    public static readonly TypeItem KBManage = new(111, "KBManage", "CustomerPermission.KBManage", "Bilgi Bankasını Yönetebilir", "bi-book-fill", "bg-cyan", 111);

    // Integrations (Entegrasyonlar)
    public static readonly TypeItem IntegrationView = new(120, "IntegrationView", "CustomerPermission.IntegrationView", "Entegrasyonları Görüntüleyebilir", "bi-plug", "bg-purple", 120);
    public static readonly TypeItem IntegrationManage = new(121, "IntegrationManage", "CustomerPermission.IntegrationManage", "Entegrasyonları Yönetebilir", "bi-plug-fill", "bg-purple", 121);

    // Campaigns (Kampanyalar)
    public static readonly TypeItem CampaignView = new(130, "CampaignView", "CustomerPermission.CampaignView", "Kampanyaları Görüntüleyebilir", "bi-megaphone", "bg-yellow text-dark", 130);
    public static readonly TypeItem CampaignManage = new(131, "CampaignManage", "CustomerPermission.CampaignManage", "Kampanyaları Yönetebilir", "bi-megaphone-fill", "bg-yellow text-dark", 131);
    public static readonly TypeItem CampaignExecute = new(132, "CampaignExecute", "CustomerPermission.CampaignExecute", "Kampanya Çalıştırabilir", "bi-play-fill", "bg-yellow text-dark", 132);

    // KVKK Compliance (KVKK Uyumluluk)
    public static readonly TypeItem KvkkView = new(140, "KvkkView", "CustomerPermission.KvkkView", "KVKK Verilerini Görüntüleyebilir", "bi-shield", "bg-dark", 140);
    public static readonly TypeItem KvkkManage = new(141, "KvkkManage", "CustomerPermission.KvkkManage", "KVKK Ayarlarını Yönetebilir", "bi-shield-check", "bg-dark", 141);
    public static readonly TypeItem PrivacyNoticeManage = new(142, "PrivacyNoticeManage", "CustomerPermission.PrivacyNoticeManage", "Aydınlatma Metinlerini Yönetebilir", "bi-file-earmark-text", "bg-dark", 142);

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
        CrmQualityView, CrmQualityManage, CrmQualityScore,
        KBView, KBManage,
        IntegrationView, IntegrationManage,
        CampaignView, CampaignManage, CampaignExecute,
        KvkkView, KvkkManage, PrivacyNoticeManage
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
            PortalModules.Ids.CrmQualityManagement => new[] { CrmQualityView, CrmQualityManage, CrmQualityScore },
            PortalModules.Ids.KnowledgeBase => new[] { KBView, KBManage },
            PortalModules.Ids.Integrations => new[] { IntegrationView, IntegrationManage },
            PortalModules.Ids.Campaigns => new[] { CampaignView, CampaignManage, CampaignExecute },
            PortalModules.Ids.KvkkCompliance => new[] { KvkkView, KvkkManage, PrivacyNoticeManage },
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
            >= 100 and <= 109 => PortalModules.Ids.CrmQualityManagement,
            >= 110 and <= 119 => PortalModules.Ids.KnowledgeBase,
            >= 120 and <= 129 => PortalModules.Ids.Integrations,
            >= 130 and <= 139 => PortalModules.Ids.Campaigns,
            >= 140 and <= 149 => PortalModules.Ids.KvkkCompliance,
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
        // CrmQualityManagement
        public const int CrmQualityView = 100;
        public const int CrmQualityManage = 101;
        public const int CrmQualityScore = 102;
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
        // KVKK Compliance
        public const int KvkkView = 140;
        public const int KvkkManage = 141;
        public const int PrivacyNoticeManage = 142;
    }
}
