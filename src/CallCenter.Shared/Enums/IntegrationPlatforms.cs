namespace CallCenter.Shared.Enums;

/// <summary>
/// Desteklenen entegrasyon platformlari.
/// IntegrationConnection.PlatformTypeId icin kullanilir.
/// </summary>
public static class IntegrationPlatforms
{
    public static readonly TypeItem GenericWebhook = new(1, "GenericWebhook", "IntegrationPlatform.GenericWebhook", "Genel Webhook", "bi-globe", "bg-secondary", 1);
    public static readonly TypeItem Salesforce = new(2, "Salesforce", "IntegrationPlatform.Salesforce", "Salesforce CRM", "bi-cloud-fill", "bg-primary", 2);
    public static readonly TypeItem HubSpot = new(3, "HubSpot", "IntegrationPlatform.HubSpot", "HubSpot CRM", "bi-hexagon-fill", "bg-warning text-dark", 3);
    public static readonly TypeItem Zendesk = new(4, "Zendesk", "IntegrationPlatform.Zendesk", "Zendesk Support", "bi-chat-square-dots-fill", "bg-success", 4);

    public static IEnumerable<TypeItem> All => new[] { GenericWebhook, Salesforce, HubSpot, Zendesk };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int GenericWebhook = 1;
        public const int Salesforce = 2;
        public const int HubSpot = 3;
        public const int Zendesk = 4;
    }
}

/// <summary>
/// Rehber senkronizasyon yonleri.
/// IntegrationConnection.CrmContactSyncDirectionId icin kullanilir.
/// </summary>
public static class CrmContactSyncDirections
{
    public static readonly TypeItem None = new(0, "None", "CrmContactSyncDirection.None", "Senkronizasyon Yok", "bi-x-circle", "bg-secondary", 0);
    public static readonly TypeItem ToExternal = new(1, "ToExternal", "CrmContactSyncDirection.ToExternal", "Dış Sisteme Gönder", "bi-box-arrow-right", "bg-info", 1);
    public static readonly TypeItem FromExternal = new(2, "FromExternal", "CrmContactSyncDirection.FromExternal", "Dış Sistemden Al", "bi-box-arrow-in-left", "bg-info", 2);
    public static readonly TypeItem Bidirectional = new(3, "Bidirectional", "CrmContactSyncDirection.Bidirectional", "Çift Yönlü Senkronizasyon", "bi-arrow-left-right", "bg-success", 3);

    public static IEnumerable<TypeItem> All => new[] { None, ToExternal, FromExternal, Bidirectional };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int None = 0;
        public const int ToExternal = 1;
        public const int FromExternal = 2;
        public const int Bidirectional = 3;
    }
}
