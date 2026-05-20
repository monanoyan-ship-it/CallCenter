namespace CallCenter.Shared.Enums;

/// <summary>
/// Salon bekleme listesi yasam dongusu.
/// Aktif/aksiyon bekleyen liste: Waiting, Notified, AppointmentBooked. Arsiv/gecmis: Cancelled, Completed.
/// </summary>
public static class SlnWaitlistStatuses
{
    public const string ScopeAll = "all";
    public const string ScopeActive = "active";
    public const string ScopeArchive = "archive";
    public const string ScopeHistory = "history";

    public static readonly TypeItem Waiting = new(1, "Waiting", TranslationKeys.Waiting, "Bekliyor", "bi-hourglass", "bg-warning text-dark", 1, isDefault: true);
    public static readonly TypeItem Notified = new(2, "Notified", TranslationKeys.Notified, "Bildirildi", "bi-bell", "bg-info", 2);
    public static readonly TypeItem AppointmentBooked = new(3, "AppointmentBooked", TranslationKeys.AppointmentBooked, "Randevu Alındı", "bi-calendar-check", "bg-primary", 3);
    public static readonly TypeItem Cancelled = new(4, "Cancelled", TranslationKeys.Cancelled, "İptal", "bi-x-circle", "bg-danger", 4);
    public static readonly TypeItem Completed = new(5, "Completed", TranslationKeys.Completed, "Gerçekleşti", "bi-check2-circle", "bg-success", 5);

    public static IEnumerable<TypeItem> All => new[] { Waiting, Notified, AppointmentBooked, Cancelled, Completed };
    public static TypeItem Default => Waiting;

    public static IReadOnlySet<int> ActiveIds { get; } = new HashSet<int> { Ids.Waiting, Ids.Notified, Ids.AppointmentBooked };
    public static IReadOnlySet<int> ArchivedIds { get; } = new HashSet<int> { Ids.Cancelled, Ids.Completed };
    public static IReadOnlySet<int> TerminalIds { get; } = new HashSet<int> { Ids.Cancelled, Ids.Completed };

    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static bool IsDefined(int id) => GetById(id) != null;
    public static bool IsActive(int id) => ActiveIds.Contains(id);
    public static bool IsArchived(int id) => ArchivedIds.Contains(id);
    public static bool IsTerminal(int id) => TerminalIds.Contains(id);

    public static string? NormalizeScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope)) return ScopeAll;
        return scope.Trim().ToLowerInvariant() switch
        {
            ScopeAll => ScopeAll,
            ScopeActive => ScopeActive,
            ScopeArchive => ScopeArchive,
            ScopeHistory => ScopeArchive,
            _ => null
        };
    }

    public static bool CanTransition(int fromStatusId, int toStatusId)
    {
        if (!IsDefined(fromStatusId) || !IsDefined(toStatusId)) return false;
        if (fromStatusId == toStatusId) return true;

        return fromStatusId switch
        {
            Ids.Waiting => toStatusId is Ids.Notified or Ids.AppointmentBooked or Ids.Cancelled,
            Ids.Notified => toStatusId is Ids.AppointmentBooked or Ids.Cancelled,
            Ids.AppointmentBooked => toStatusId is Ids.Completed or Ids.Cancelled,
            _ => false
        };
    }

    public static class Ids
    {
        public const int Waiting = 1;
        public const int Notified = 2;
        public const int AppointmentBooked = 3;
        public const int Cancelled = 4;
        public const int Completed = 5;
    }

    public static class TranslationKeys
    {
        public const string Waiting = "salon.waitlist.status.waiting";
        public const string Notified = "salon.waitlist.status.notified";
        public const string AppointmentBooked = "salon.waitlist.status.appointment_booked";
        public const string Cancelled = "salon.waitlist.status.cancelled";
        public const string Completed = "salon.waitlist.status.completed";
    }
}
