namespace CallCenter.Shared.Enums;

/// <summary>
/// Desteklenen e-posta saglaycilari.
/// CustomerEmailIntegration.ProviderTypeId icin kullanilir.
/// </summary>
public static class EmailProviders
{
    public static readonly TypeItem Gmail = new(1, "Gmail", "EmailProvider.Gmail", "Gmail / Google Workspace", "bi-google", "bg-danger", 1);
    public static readonly TypeItem Office365 = new(2, "Office365", "EmailProvider.Office365", "Microsoft 365 / Outlook", "bi-microsoft", "bg-primary", 2);
    public static readonly TypeItem Yandex = new(3, "Yandex", "EmailProvider.Yandex", "Yandex Mail", "bi-cloud-fill", "bg-warning text-dark", 3);
    public static readonly TypeItem SmtpGeneric = new(4, "SmtpGeneric", "EmailProvider.SmtpGeneric", "Genel SMTP", "bi-envelope-fill", "bg-secondary", 4);

    public static IEnumerable<TypeItem> All => new[] { Gmail, Office365, Yandex, SmtpGeneric };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static TypeItem? GetBySystemName(string systemName) => All.FirstOrDefault(x => x.SystemName == systemName);

    public static class Ids
    {
        public const int Gmail = 1;
        public const int Office365 = 2;
        public const int Yandex = 3;
        public const int SmtpGeneric = 4;
    }
}
