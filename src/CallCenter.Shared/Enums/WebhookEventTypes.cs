namespace CallCenter.Shared.Enums;

/// <summary>
/// Webhook event tipleri.
/// Dis sistemlere push edilen cagri/agent/kuyruk olaylari.
/// </summary>
public static class WebhookEventTypes
{
    // ─── Call Events (1-9) ───
    public static readonly TypeItem CallStarted = new(1, "call.started", "WebhookEvent.CallStarted", "Çağrı Başladı", "bi-telephone-fill", "bg-success", 1);
    public static readonly TypeItem CallAnswered = new(2, "call.answered", "WebhookEvent.CallAnswered", "Çağrı Cevaplandı", "bi-telephone-inbound-fill", "bg-success", 2);
    public static readonly TypeItem CallEnded = new(3, "call.ended", "WebhookEvent.CallEnded", "Çağrı Sonlandı", "bi-telephone-x-fill", "bg-danger", 3);
    public static readonly TypeItem CallMissed = new(4, "call.missed", "WebhookEvent.CallMissed", "Çağrı Kaçtı", "bi-telephone-minus-fill", "bg-warning text-dark", 4);
    public static readonly TypeItem CallTransferred = new(5, "call.transferred", "WebhookEvent.CallTransferred", "Çağrı Transfer Edildi", "bi-arrow-left-right", "bg-info", 5);
    public static readonly TypeItem CallHeld = new(6, "call.held", "WebhookEvent.CallHeld", "Çağrı Beklemeye Alındı", "bi-pause-circle-fill", "bg-secondary", 6);

    // ─── Agent Events (10-19) ───
    public static readonly TypeItem AgentStatusChanged = new(10, "agent.status_changed", "WebhookEvent.AgentStatusChanged", "Agent Durumu Değişti", "bi-person-fill-gear", "bg-primary", 10);
    public static readonly TypeItem AgentLoggedIn = new(11, "agent.logged_in", "WebhookEvent.AgentLoggedIn", "Agent Giriş Yaptı", "bi-box-arrow-in-right", "bg-success", 11);
    public static readonly TypeItem AgentLoggedOut = new(12, "agent.logged_out", "WebhookEvent.AgentLoggedOut", "Agent Çıkış Yaptı", "bi-box-arrow-left", "bg-danger", 12);

    // ─── Queue Events (20-29) ───
    public static readonly TypeItem QueueUpdated = new(20, "queue.updated", "WebhookEvent.QueueUpdated", "Kuyruk Güncellendi", "bi-people-fill", "bg-info", 20);
    public static readonly TypeItem QueueThresholdExceeded = new(21, "queue.threshold_exceeded", "WebhookEvent.QueueThresholdExceeded", "Kuyruk Eşiği Aşıldı", "bi-exclamation-triangle-fill", "bg-danger", 21);

    // ─── CRM Events (30-49) ───
    public static readonly TypeItem CrmContactCreated = new(30, "contact.created", "WebhookEvent.CrmContactCreated", "Kişi Oluşturuldu", "bi-person-plus-fill", "bg-success", 30);
    public static readonly TypeItem CrmContactUpdated = new(31, "contact.updated", "WebhookEvent.CrmContactUpdated", "Kişi Güncellendi", "bi-person-fill-check", "bg-info", 31);
    public static readonly TypeItem TicketCreated = new(40, "ticket.created", "WebhookEvent.TicketCreated", "Talep Oluşturuldu", "bi-ticket-detailed-fill", "bg-warning text-dark", 40);
    public static readonly TypeItem TicketUpdated = new(41, "ticket.updated", "WebhookEvent.TicketUpdated", "Talep Güncellendi", "bi-ticket-fill", "bg-info", 41);

    public static IEnumerable<TypeItem> All => new[]
    {
        CallStarted, CallAnswered, CallEnded, CallMissed, CallTransferred, CallHeld,
        AgentStatusChanged, AgentLoggedIn, AgentLoggedOut,
        QueueUpdated, QueueThresholdExceeded,
        CrmContactCreated, CrmContactUpdated, TicketCreated, TicketUpdated
    };

    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    /// <summary>Event kategorilerine gore grupla (frontend filtre icin)</summary>
    public static IEnumerable<TypeItem> CallEvents => new[] { CallStarted, CallAnswered, CallEnded, CallMissed, CallTransferred, CallHeld };
    public static IEnumerable<TypeItem> AgentEvents => new[] { AgentStatusChanged, AgentLoggedIn, AgentLoggedOut };
    public static IEnumerable<TypeItem> QueueEvents => new[] { QueueUpdated, QueueThresholdExceeded };
    public static IEnumerable<TypeItem> CrmEvents => new[] { CrmContactCreated, CrmContactUpdated, TicketCreated, TicketUpdated };

    public static class Ids
    {
        // Call
        public const int CallStarted = 1;
        public const int CallAnswered = 2;
        public const int CallEnded = 3;
        public const int CallMissed = 4;
        public const int CallTransferred = 5;
        public const int CallHeld = 6;
        // Agent
        public const int AgentStatusChanged = 10;
        public const int AgentLoggedIn = 11;
        public const int AgentLoggedOut = 12;
        // Queue
        public const int QueueUpdated = 20;
        public const int QueueThresholdExceeded = 21;
        // CRM
        public const int CrmContactCreated = 30;
        public const int CrmContactUpdated = 31;
        public const int TicketCreated = 40;
        public const int TicketUpdated = 41;
    }
}
