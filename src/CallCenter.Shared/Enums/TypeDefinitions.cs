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
