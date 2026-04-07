namespace CallCenter.Shared.Enums;

public static class CallStatuses
{
    public static readonly TypeItem Ringing = new(1, "Ringing", "CallStatus.Ringing", "Çalıyor", "bi-bell-fill", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(2, "InProgress", "CallStatus.InProgress", "Devam Ediyor", "bi-telephone-fill", "bg-success", 2);
    public static readonly TypeItem OnHold = new(3, "OnHold", "CallStatus.OnHold", "Beklemede", "bi-pause-circle-fill", "bg-info", 3);
    public static readonly TypeItem Transferred = new(4, "Transferred", "CallStatus.Transferred", "Transfer Edildi", "bi-arrow-left-right", "bg-secondary", 4);
    public static readonly TypeItem Completed = new(5, "Completed", "CallStatus.Completed", "Tamamlandı", "bi-check-circle-fill", "bg-success", 5);
    public static readonly TypeItem Missed = new(6, "Missed", "CallStatus.Missed", "Cevapsız", "bi-telephone-x-fill", "bg-warning text-dark", 6);
    public static readonly TypeItem Failed = new(7, "Failed", "CallStatus.Failed", "Başarısız", "bi-x-circle-fill", "bg-danger", 7);
    public static readonly TypeItem Queued = new(8, "Queued", "CallStatus.Queued", "Kuyrukta Bekliyor", "bi-hourglass-split", "bg-purple", 8);

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

public static class CallDirections
{
    public static readonly TypeItem Inbound = new(1, "Inbound", "CallDirection.Inbound", "Gelen Arama", "bi-telephone-inbound-fill", "text-success", 1, isDefault: true);
    public static readonly TypeItem Outbound = new(2, "Outbound", "CallDirection.Outbound", "Giden Arama", "bi-telephone-outbound-fill", "text-primary", 2);

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

public static class CallbackStatuses
{
    public static readonly TypeItem Todo = new(1, "Todo", "CallbackStatus.Todo", "Bekliyor", "bi-clock-history", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem InProgress = new(2, "InProgress", "CallbackStatus.InProgress", "Aranıyor", "bi-telephone-outbound", "bg-info", 2);
    public static readonly TypeItem Completed = new(3, "Completed", "CallbackStatus.Completed", "Tamamlandı", "bi-check-circle-fill", "bg-success", 3);
    public static readonly TypeItem Cancelled = new(4, "Cancelled", "CallbackStatus.Cancelled", "İptal edildi", "bi-slash-circle", "bg-secondary", 4);

    public static IEnumerable<TypeItem> All => new[] { Todo, InProgress, Completed, Cancelled };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Todo = 1;
        public const int InProgress = 2;
        public const int Completed = 3;
        public const int Cancelled = 4;
    }
}
