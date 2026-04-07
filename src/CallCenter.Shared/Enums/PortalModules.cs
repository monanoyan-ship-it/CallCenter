namespace CallCenter.Shared.Enums;

/// <summary>
/// Musteri portalindaki modul tanimlari.
/// Her modul bir yetki kategorisine karsilik gelir.
/// CustomerPortalModule DB tablosu ile hangi musteriye hangi modul acik tutulur.
/// </summary>
public static class PortalModules
{
    public const int ProductTypeId = 1; // CallCenter

    public static readonly TypeItem Dashboard = new(1, "Dashboard", "PortalModule.Dashboard", "Gösterge Paneli", "bi-speedometer2", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem Calls = new(2, "Calls", "PortalModule.Calls", "Arama Yönetimi", "bi-telephone-fill", "bg-success", 2, isDefault: true);
    public static readonly TypeItem Reports = new(3, "Reports", "PortalModule.Reports", "Raporlama", "bi-file-earmark-bar-graph", "bg-info", 3);
    public static readonly TypeItem Agents = new(4, "Agents", "PortalModule.Agents", "Temsilci Yönetimi", "bi-headset", "bg-warning text-dark", 4);
    public static readonly TypeItem Queues = new(5, "Queues", "PortalModule.Queues", "Kuyruk Yönetimi", "bi-people-fill", "bg-secondary", 5);
    public static readonly TypeItem Settings = new(6, "Settings", "PortalModule.Settings", "Ayarlar", "bi-gear-fill", "bg-danger", 6, isDefault: true);
    public static readonly TypeItem Personnel = new(7, "Personnel", "PortalModule.Personnel", "Personel Yönetimi", "bi-person-badge", "bg-dark", 7, isDefault: true);
    public static readonly TypeItem Organizations = new(8, "Organizations", "PortalModule.Organizations", "Organizasyon Yönetimi", "bi-diagram-3-fill", "bg-indigo", 8);

    // Yeni modüller (Faz 4 — sektörel araştırma sonucu)
    public static readonly TypeItem SipSettings = new(9, "SipSettings", "PortalModule.SipSettings", "SIP/VoIP Yapılandırması", "bi-router-fill", "bg-teal", 9, isDefault: true);
    public static readonly TypeItem CallRecords = new(10, "CallRecords", "PortalModule.CallRecords", "Arama Kaydı Dinleme/Yönetimi", "bi-record-circle", "bg-orange", 10, isDefault: true);
    public static readonly TypeItem CrmQualityManagement = new(11, "CrmQualityManagement", "PortalModule.CrmQualityManagement", "Kalite Değerlendirme Formları", "bi-clipboard-check", "bg-pink", 11);
    public static readonly TypeItem KnowledgeBase = new(12, "KnowledgeBase", "PortalModule.KnowledgeBase", "Bilgi Bankası, Agent Senaryoları", "bi-book", "bg-cyan", 12);
    public static readonly TypeItem Integrations = new(13, "Integrations", "PortalModule.Integrations", "API/Webhook/CRM Entegrasyonları", "bi-plug-fill", "bg-purple", 13);
    public static readonly TypeItem Campaigns = new(14, "Campaigns", "PortalModule.Campaigns", "Giden Arama Kampanyaları", "bi-megaphone-fill", "bg-yellow text-dark", 14);
    public static readonly TypeItem KvkkCompliance = new(15, "KvkkCompliance", "PortalModule.KvkkCompliance", "KVKK Uyumluluk Yönetimi", "bi-shield-check", "bg-dark", 15);
    public static readonly TypeItem Messaging = new(16, "Messaging", "PortalModule.Messaging", "Mesajlaşma", "bi-chat-dots-fill", "bg-info", 16);

    public static IEnumerable<TypeItem> All => new[] { Dashboard, Calls, Reports, Agents, Queues, Settings, Personnel, Organizations, SipSettings, CallRecords, CrmQualityManagement, KnowledgeBase, Integrations, Campaigns, KvkkCompliance, Messaging };
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
        public const int CrmQualityManagement = 11;
        public const int KnowledgeBase = 12;
        public const int Integrations = 13;
        public const int Campaigns = 14;
        public const int KvkkCompliance = 15;
        public const int Messaging = 16;
    }
}
