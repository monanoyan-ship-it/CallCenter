namespace CallCenter.Shared.Enums;

public static class CampaignStatuses
{
    public static readonly TypeItem Draft = new(1, "Draft", "Campaign.Draft", "Taslak", "bi-pencil-fill", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Active = new(2, "Active", "Campaign.Active", "Aktif", "bi-play-fill", "bg-success", 2);
    public static readonly TypeItem Paused = new(3, "Paused", "Campaign.Paused", "Duraklatılmış", "bi-pause-fill", "bg-warning text-dark", 3);
    public static readonly TypeItem Completed = new(4, "Completed", "Campaign.Completed", "Tamamlandı", "bi-check-circle-fill", "bg-primary", 4);
    public static readonly TypeItem Archived = new(5, "Archived", "Campaign.Archived", "Arşivlendi", "bi-archive-fill", "bg-dark", 5);

    public static IEnumerable<TypeItem> All => new[] { Draft, Active, Paused, Completed, Archived };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Draft = 1;
        public const int Active = 2;
        public const int Paused = 3;
        public const int Completed = 4;
        public const int Archived = 5;
    }
}

public static class CampaignContactStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", "CampaignContact.Pending", "Bekliyor", "bi-hourglass-split", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Calling = new(2, "Calling", "CampaignContact.Calling", "Aranıyor", "bi-telephone-fill", "bg-info", 2);
    public static readonly TypeItem Reached = new(3, "Reached", "CampaignContact.Reached", "Ulaşıldı", "bi-check-circle-fill", "bg-success", 3);
    public static readonly TypeItem NotReached = new(4, "NotReached", "CampaignContact.NotReached", "Ulaşılamadı", "bi-x-circle-fill", "bg-danger", 4);
    public static readonly TypeItem Cancelled = new(5, "Cancelled", "CampaignContact.Cancelled", "İptal", "bi-slash-circle-fill", "bg-dark", 5);

    public static IEnumerable<TypeItem> All => new[] { Pending, Calling, Reached, NotReached, Cancelled };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    /// <summary>Tamamlanmis sayilan durumlar</summary>
    public static int[] FinishedIds => new[] { Ids.Reached, Ids.NotReached, Ids.Cancelled };

    public static class Ids
    {
        public const int Pending = 1;
        public const int Calling = 2;
        public const int Reached = 3;
        public const int NotReached = 4;
        public const int Cancelled = 5;
    }
}

public static class CrmContactSources
{
    public static readonly TypeItem Manual = new(1, "Manual", "CrmContactSource.Manual", "Manuel Eklendi", "bi-person-plus", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem LDAP = new(2, "LDAP", "CrmContactSource.LDAP", "LDAP/Active Directory", "bi-diagram-3-fill", "bg-info", 2);
    public static readonly TypeItem CSV = new(3, "CSV", "CrmContactSource.CSV", "CSV Dosyasından İçerildi", "bi-filetype-csv", "bg-success", 3);

    public static IEnumerable<TypeItem> All => new[] { Manual, LDAP, CSV };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Manual = 1;
        public const int LDAP = 2;
        public const int CSV = 3;
    }
}
