namespace CallCenter.Shared.Enums;

public static class RecordingAccessActions
{
    public static readonly TypeItem Play = new(1, "Play", "RecordingAccess.Play", "Kayit dinleme", "bi-play-fill", "bg-success", 1);
    public static readonly TypeItem Download = new(2, "Download", "RecordingAccess.Download", "Kayit indirme", "bi-download", "bg-primary", 2);
    public static readonly TypeItem StreamStarted = new(3, "StreamStarted", "RecordingAccess.StreamStarted", "Stream baslatildi", "bi-broadcast", "bg-info", 3);
    public static readonly TypeItem StreamEnded = new(4, "StreamEnded", "RecordingAccess.StreamEnded", "Stream sonlandi", "bi-stop-fill", "bg-secondary", 4);
    public static readonly TypeItem AccessDenied = new(5, "AccessDenied", "RecordingAccess.AccessDenied", "Erisim reddedildi", "bi-shield-x", "bg-danger", 5);
    public static readonly TypeItem HashMismatch = new(6, "HashMismatch", "RecordingAccess.HashMismatch", "Hash uyumsuzlugu", "bi-exclamation-triangle-fill", "bg-warning text-dark", 6);

    public static IEnumerable<TypeItem> All => new[] { Play, Download, StreamStarted, StreamEnded, AccessDenied, HashMismatch };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Play = 1;
        public const int Download = 2;
        public const int StreamStarted = 3;
        public const int StreamEnded = 4;
        public const int AccessDenied = 5;
        public const int HashMismatch = 6;
    }
}
