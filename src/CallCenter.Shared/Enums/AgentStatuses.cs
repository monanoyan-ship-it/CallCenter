namespace CallCenter.Shared.Enums;

public static class AgentStatuses
{
    public static readonly TypeItem Offline = new(1, "Offline", "AgentStatus.Offline", "Çevrimdışı", "bi-circle-fill", "offline", 1);
    public static readonly TypeItem Available = new(2, "Available", "AgentStatus.Available", "Müsait", "bi-circle-fill", "online", 2, isDefault: true);
    public static readonly TypeItem Busy = new(3, "Busy", "AgentStatus.Busy", "Meşgul", "bi-circle-fill", "busy", 3);
    public static readonly TypeItem OnBreak = new(4, "OnBreak", "AgentStatus.OnBreak", "Mola", "bi-circle-fill", "break", 4);
    public static readonly TypeItem InCall = new(5, "InCall", "AgentStatus.InCall", "Aramada", "bi-telephone-fill", "busy", 5);
    public static readonly TypeItem AfterCallWork = new(6, "AfterCallWork", "AgentStatus.AfterCallWork", "Arama Sonrası İş", "bi-pencil-fill", "busy", 6);

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
