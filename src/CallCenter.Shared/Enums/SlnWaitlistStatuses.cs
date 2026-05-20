namespace CallCenter.Shared.Enums;

/// <summary>
/// Salon bekleme listesi yasam dongusu.
/// Aktif liste: Waiting, Notified. Arsiv/gecmis: AppointmentBooked, Cancelled, Completed.
/// </summary>
public static class SlnWaitlistStatuses
{
    public static readonly TypeItem Waiting = new(1, "Waiting", "SlnWaitlistStatus.Waiting", "Bekliyor", "bi-hourglass", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem Notified = new(2, "Notified", "SlnWaitlistStatus.Notified", "Bildirildi", "bi-bell", "bg-info", 2);
    public static readonly TypeItem AppointmentBooked = new(3, "AppointmentBooked", "SlnWaitlistStatus.AppointmentBooked", "Randevu Alindi", "bi-calendar-check", "bg-primary", 3);
    public static readonly TypeItem Cancelled = new(4, "Cancelled", "SlnWaitlistStatus.Cancelled", "Iptal", "bi-x-circle", "bg-danger", 4);
    public static readonly TypeItem Completed = new(5, "Completed", "SlnWaitlistStatus.Completed", "Gerceklesti", "bi-check2-circle", "bg-success", 5);

    public static IEnumerable<TypeItem> All => new[] { Waiting, Notified, AppointmentBooked, Cancelled, Completed };
    public static TypeItem Default => Waiting;

    public static IReadOnlySet<int> ActiveIds { get; } = new HashSet<int> { Ids.Waiting, Ids.Notified };
    public static IReadOnlySet<int> ArchivedIds { get; } = new HashSet<int> { Ids.AppointmentBooked, Ids.Cancelled, Ids.Completed };
    public static IReadOnlySet<int> TerminalIds { get; } = new HashSet<int> { Ids.Cancelled, Ids.Completed };

    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static bool IsDefined(int id) => GetById(id) != null;
    public static bool IsActive(int id) => ActiveIds.Contains(id);
    public static bool IsArchived(int id) => ArchivedIds.Contains(id);
    public static bool IsTerminal(int id) => TerminalIds.Contains(id);

    public static class Ids
    {
        public const int Waiting = 1;
        public const int Notified = 2;
        public const int AppointmentBooked = 3;
        public const int Cancelled = 4;
        public const int Completed = 5;
    }
}
