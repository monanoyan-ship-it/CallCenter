namespace CallCenter.Shared.Enums;

public static class MessageTypes
{
    public static readonly TypeItem Text = new(1, "Text", "MessageType.Text", "Metin Mesajı", "bi-chat-dots", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem System = new(2, "System", "MessageType.System", "Sistem Mesajı", "bi-info-circle", "bg-secondary", 2);
    public static readonly TypeItem File = new(3, "File", "MessageType.File", "Dosya Mesajı", "bi-paperclip", "bg-info", 3);

    public static IEnumerable<TypeItem> All => new[] { Text, System, File };
    public static TypeItem Default => All.First(x => x.IsDefault);
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Text = 1;
        public const int System = 2;
        public const int File = 3;
    }
}
