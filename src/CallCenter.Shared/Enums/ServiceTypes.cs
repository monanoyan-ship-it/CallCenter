namespace CallCenter.Shared.Enums;

/// <summary>
/// Platform hizmet katalogu. TypeDefinition olarak tanimlandi (DB entity yok).
/// CustomerServiceSubscription.ServiceTypeId bu ID'lere referans verir (FK yok).
/// </summary>
public static class ServiceTypes
{
    // ─── Dahil (Default) Hizmetler ───
    public static readonly TypeItem CagriDagitimi = new(1, "CagriDagitimi", "Service.CagriDagitimi", "Cagri dagitimi (ACD)", "bi-telephone-forward-fill", "bg-success", 1, isDefault: true);
    public static readonly TypeItem CagriKaydi = new(2, "CagriKaydi", "Service.CagriKaydi", "Cagri kaydi (CDR)", "bi-journal-text", "bg-success", 2, isDefault: true);
    public static readonly TypeItem SesKayitlari = new(3, "SesKayitlari", "Service.SesKayitlari", "Ses kayitlari", "bi-record-circle", "bg-success", 3, isDefault: true);

    // ─── Premium (Ucretli) Hizmetler ───
    public static readonly TypeItem IVR = new(4, "IVR", "Service.IVR", "Sesli yonlendirme (IVR)", "bi-telephone-inbound-fill", "bg-warning text-dark", 4);
    public static readonly TypeItem KaliteYonetimi = new(5, "KaliteYonetimi", "Service.KaliteYonetimi", "Kalite degerlendirme", "bi-clipboard-check", "bg-warning text-dark", 5);
    public static readonly TypeItem CRM = new(6, "CRM", "Service.CRM", "CRM modulu (musteri karti, ticket, pipeline)", "bi-person-lines-fill", "bg-warning text-dark", 6);
    public static readonly TypeItem KampanyaModulu = new(7, "KampanyaModulu", "Service.KampanyaModulu", "Kampanya modulu", "bi-megaphone-fill", "bg-warning text-dark", 7);
    public static readonly TypeItem GelismisRaporlama = new(8, "GelismisRaporlama", "Service.GelismisRaporlama", "Gelismis raporlama", "bi-file-earmark-bar-graph", "bg-warning text-dark", 8);
    public static readonly TypeItem SMSMesajlasma = new(9, "SMSMesajlasma", "Service.SMSMesajlasma", "SMS / Mesajlasma", "bi-chat-dots-fill", "bg-warning text-dark", 9);
    public static readonly TypeItem APIErisimi = new(10, "APIErisimi", "Service.APIErisimi", "API erisimi", "bi-braces", "bg-warning text-dark", 10);
    public static readonly TypeItem BulutDepolama = new(11, "BulutDepolama", "Service.BulutDepolama", "Bulut depolama", "bi-cloud-upload", "bg-warning text-dark", 11);
    public static readonly TypeItem EkSipHat = new(12, "EkSipHat", "Service.EkSipHat", "Ek SIP hat", "bi-router-fill", "bg-warning text-dark", 12);
    public static readonly TypeItem EkKullaniciPaketi = new(13, "EkKullaniciPaketi", "Service.EkKullaniciPaketi", "Ek kullanici paketi", "bi-person-plus-fill", "bg-warning text-dark", 13);
    public static readonly TypeItem KVKKPaketi = new(14, "KVKKPaketi", "Service.KVKKPaketi", "KVKK uyumluluk paketi", "bi-shield-check", "bg-warning text-dark", 14);
    public static readonly TypeItem OncelikliDestek = new(15, "OncelikliDestek", "Service.OncelikliDestek", "7/24 oncelikli destek", "bi-headset", "bg-warning text-dark", 15);
    public static readonly TypeItem SesliKarsilama = new(16, "SesliKarsilama", "Service.SesliKarsilama", "Sesli karsilama (Auto-Attendant)", "bi-soundwave", "bg-warning text-dark", 16);
    public static readonly TypeItem WhisperCoaching = new(17, "WhisperCoaching", "Service.WhisperCoaching", "Fisildama ve cagri izleme", "bi-ear-fill", "bg-warning text-dark", 17);
    public static readonly TypeItem OtomatikAramaSistemi = new(18, "OtomatikAramaSistemi", "Service.OtomatikAramaSistemi", "Otomatik arama (Power/Predictive Dialer)", "bi-lightning-fill", "bg-warning text-dark", 18);

    public static IEnumerable<TypeItem> All => new[]
    {
        CagriDagitimi, CagriKaydi, SesKayitlari,
        IVR, KaliteYonetimi, CRM, KampanyaModulu, GelismisRaporlama,
        SMSMesajlasma, APIErisimi, BulutDepolama, EkSipHat, EkKullaniciPaketi, KVKKPaketi, OncelikliDestek,
        SesliKarsilama, WhisperCoaching, OtomatikAramaSistemi
    };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    /// <summary>Yeni musteri olusturuldiginda varsayilan olarak atanacak hizmetler</summary>
    public static IEnumerable<TypeItem> Defaults => All.Where(x => x.IsDefault);

    /// <summary>Premium (ucretli) hizmetler</summary>
    public static IEnumerable<TypeItem> Premium => All.Where(x => !x.IsDefault);

    /// <summary>Hizmetin kategori ID'si (1=Default, 2=Premium)</summary>
    public static int GetCategoryId(int id) => GetById(id)?.IsDefault == true
        ? ServiceCategories.Ids.Default
        : ServiceCategories.Ids.Premium;

    /// <summary>Varsayilan aylik fiyatlar (TL)</summary>
    private static readonly Dictionary<int, decimal> _defaultPrices = new()
    {
        { 1, 0 }, { 2, 0 }, { 3, 0 },                         // Dahil
        { 4, 500 }, { 5, 300 }, { 6, 400 }, { 7, 350 },       // Premium
        { 8, 250 }, { 9, 200 }, { 10, 450 }, { 11, 300 },
        { 12, 150 }, { 13, 200 }, { 14, 500 }, { 15, 750 },
        { 16, 200 }, { 17, 350 }, { 18, 600 }
    };

    public static decimal GetDefaultPrice(int id) =>
        _defaultPrices.TryGetValue(id, out var price) ? price : 0;

    public static class Ids
    {
        public const int CagriDagitimi = 1;
        public const int CagriKaydi = 2;
        public const int SesKayitlari = 3;
        public const int IVR = 4;
        public const int KaliteYonetimi = 5;
        public const int CRM = 6;
        public const int KampanyaModulu = 7;
        public const int GelismisRaporlama = 8;
        public const int SMSMesajlasma = 9;
        public const int APIErisimi = 10;
        public const int BulutDepolama = 11;
        public const int EkSipHat = 12;
        public const int EkKullaniciPaketi = 13;
        public const int KVKKPaketi = 14;
        public const int OncelikliDestek = 15;
        public const int SesliKarsilama = 16;
        public const int WhisperCoaching = 17;
        public const int OtomatikAramaSistemi = 18;
    }
}

public static class ServiceCategories
{
    public static readonly TypeItem Default = new(1, "Default", "ServiceCategory.Default", "Standart dahil hizmet", "bi-check-circle-fill", "bg-success", 1, isDefault: true);
    public static readonly TypeItem Premium = new(2, "Premium", "ServiceCategory.Premium", "Ucretli ek hizmet", "bi-star-fill", "bg-warning text-dark", 2);

    public static IEnumerable<TypeItem> All => new[] { Default, Premium };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Default = 1;
        public const int Premium = 2;
    }
}

public static class SubscriptionStatuses
{
    public static readonly TypeItem Active = new(1, "Active", "SubscriptionStatus.Active", "Aktif abonelik", "bi-check-circle-fill", "bg-success", 1, isDefault: true);
    public static readonly TypeItem Suspended = new(2, "Suspended", "SubscriptionStatus.Suspended", "Askiya alinmis", "bi-pause-circle-fill", "bg-warning text-dark", 2);
    public static readonly TypeItem Cancelled = new(3, "Cancelled", "SubscriptionStatus.Cancelled", "Iptal edilmis", "bi-x-circle-fill", "bg-danger", 3);

    public static IEnumerable<TypeItem> All => new[] { Active, Suspended, Cancelled };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Active = 1;
        public const int Suspended = 2;
        public const int Cancelled = 3;
    }
}

public static class BillingItemStatuses
{
    public static readonly TypeItem Pending = new(1, "Pending", "BillingItemStatus.Pending", "Beklemede", "bi-clock-fill", "bg-secondary", 1, isDefault: true);
    public static readonly TypeItem Invoiced = new(2, "Invoiced", "BillingItemStatus.Invoiced", "Faturalanmis", "bi-receipt", "bg-info", 2);
    public static readonly TypeItem Paid = new(3, "Paid", "BillingItemStatus.Paid", "Odenmis", "bi-check-circle-fill", "bg-success", 3);
    public static readonly TypeItem Overdue = new(4, "Overdue", "BillingItemStatus.Overdue", "Gecmis", "bi-exclamation-triangle-fill", "bg-danger", 4);

    public static IEnumerable<TypeItem> All => new[] { Pending, Invoiced, Paid, Overdue };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);

    public static class Ids
    {
        public const int Pending = 1;
        public const int Invoiced = 2;
        public const int Paid = 3;
        public const int Overdue = 4;
    }
}
