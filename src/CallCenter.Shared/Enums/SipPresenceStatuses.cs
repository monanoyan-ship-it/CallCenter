namespace CallCenter.Shared.Enums;

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
