namespace CallCenter.Shared.Enums;

/// <summary>
/// Salon portal modulleri. Musteri bazli acilir/kapanir.
/// ID'ler 201+ ile baslar (CallCenter PortalModules ile cakismamasi icin).
/// </summary>
public static class SalonPortalModules
{
    public const int ProductTypeId = 2; // Salon

    public static readonly TypeItem SlnDashboard = new(201, "SlnDashboard", "SalonModule.Dashboard", "Gösterge Paneli", "bi-speedometer2", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem SlnClients = new(202, "SlnClients", "SalonModule.Clients", "Müşteri Yönetimi", "bi-people-fill", "bg-success", 2, isDefault: true);
    public static readonly TypeItem SlnAppointments = new(203, "SlnAppointments", "SalonModule.Appointments", "Randevu Yönetimi", "bi-calendar-check", "bg-info", 3, isDefault: true);
    public static readonly TypeItem SlnServices = new(204, "SlnServices", "SalonModule.Services", "Hizmet Tanımları", "bi-list-check", "bg-warning text-dark", 4, isDefault: true);
    public static readonly TypeItem SlnProducts = new(205, "SlnProducts", "SalonModule.Products", "Ürün ve Stok", "bi-box-seam", "bg-secondary", 5, isDefault: true);
    public static readonly TypeItem SlnInvoices = new(206, "SlnInvoices", "SalonModule.Invoices", "Adisyon ve Satış", "bi-receipt", "bg-danger", 6, isDefault: true);
    public static readonly TypeItem SlnCash = new(207, "SlnCash", "SalonModule.Cash", "Kasa Yönetimi", "bi-cash-stack", "bg-success", 7, isDefault: true);
    public static readonly TypeItem SlnExpenses = new(208, "SlnExpenses", "SalonModule.Expenses", "Masraf Takibi", "bi-credit-card", "bg-dark", 8);
    public static readonly TypeItem SlnStaff = new(209, "SlnStaff", "SalonModule.Staff", "Personel ve Maaş", "bi-person-badge", "bg-indigo", 9, isDefault: true);
    public static readonly TypeItem SlnSuppliers = new(210, "SlnSuppliers", "SalonModule.Suppliers", "Tedarikçi ve Cari", "bi-truck", "bg-teal", 10);
    public static readonly TypeItem SlnReports = new(211, "SlnReports", "SalonModule.Reports", "Raporlama", "bi-bar-chart-line", "bg-orange", 11);
    public static readonly TypeItem SlnCampaigns = new(212, "SlnCampaigns", "SalonModule.Campaigns", "Pazarlama ve SMS", "bi-megaphone-fill", "bg-pink", 12);
    public static readonly TypeItem SlnBranches = new(213, "SlnBranches", "SalonModule.Branches", "Şube Yönetimi", "bi-building", "bg-cyan", 13, isDefault: true);

    // ─── Yeni modüller (Faz A-D) ───
    public static readonly TypeItem SlnSales = new(214, "SlnSales", "SalonModule.Sales", "Hızlı Satış POS", "bi-cart-check-fill", "bg-success", 14, isDefault: true);
    public static readonly TypeItem SlnRecipes = new(215, "SlnRecipes", "SalonModule.Recipes", "Reçete Tanımları", "bi-journal-bookmark", "bg-purple", 15, isDefault: true);
    public static readonly TypeItem SlnGiftCards = new(216, "SlnGiftCards", "SalonModule.GiftCards", "Hediye Kartları", "bi-gift", "bg-warning text-dark", 16);
    public static readonly TypeItem SlnPackages = new(217, "SlnPackages", "SalonModule.Packages", "Seans Paketleri", "bi-box-seam", "bg-info", 17, isDefault: true);
    public static readonly TypeItem SlnMemberships = new(218, "SlnMemberships", "SalonModule.Memberships", "Üyelik Planları", "bi-award", "bg-primary", 18);
    public static readonly TypeItem SlnLoyalty = new(219, "SlnLoyalty", "SalonModule.Loyalty", "Sadakat Programı", "bi-star", "bg-warning", 19);
    public static readonly TypeItem SlnProfile = new(220, "SlnProfile", "SalonModule.Profile", "Salon Profili", "bi-shop", "bg-dark", 20, isDefault: true);
    public static readonly TypeItem SlnWaitlist = new(221, "SlnWaitlist", "SalonModule.Waitlist", "Bekleme Listesi", "bi-clock-history", "bg-secondary", 21, isDefault: true);
    public static readonly TypeItem SlnEmailCampaigns = new(222, "SlnEmailCampaigns", "SalonModule.EmailCampaigns", "E-posta Kampanyaları", "bi-envelope", "bg-info", 22);
    public static readonly TypeItem SlnReviews = new(223, "SlnReviews", "SalonModule.Reviews", "Yorum Yönetimi", "bi-chat-square-text", "bg-success", 23);
    public static readonly TypeItem SlnNoShowPolicy = new(224, "SlnNoShowPolicy", "SalonModule.NoShowPolicy", "No-Show Politikası", "bi-shield-exclamation", "bg-danger", 24, isDefault: true);
    public static readonly TypeItem SlnConsentForms = new(225, "SlnConsentForms", "SalonModule.ConsentForms", "Onay Formları", "bi-file-earmark-text", "bg-secondary", 25, isDefault: true);
    public static readonly TypeItem SlnBeforeAfter = new(226, "SlnBeforeAfter", "SalonModule.BeforeAfter", "Önce/Sonra Fotoğrafları", "bi-images", "bg-pink", 26);
    public static readonly TypeItem SlnWinback = new(227, "SlnWinback", "SalonModule.Winback", "Kayıp Müşteri Geri Kazanım", "bi-arrow-repeat", "bg-orange", 27);
    public static readonly TypeItem SlnPersonnelPrices = new(228, "SlnPersonnelPrices", "SalonModule.PersonnelPrices", "Personel Fiyatları ve Hasılat", "bi-currency-exchange", "bg-teal", 28, isDefault: true);

    public static IEnumerable<TypeItem> All => new[]
    {
        SlnDashboard, SlnClients, SlnAppointments, SlnServices, SlnProducts, SlnInvoices, SlnCash, SlnExpenses,
        SlnStaff, SlnSuppliers, SlnReports, SlnCampaigns, SlnBranches,
        SlnSales, SlnRecipes, SlnGiftCards, SlnPackages, SlnMemberships, SlnLoyalty, SlnProfile,
        SlnWaitlist, SlnEmailCampaigns, SlnReviews, SlnNoShowPolicy, SlnConsentForms, SlnBeforeAfter, SlnWinback, SlnPersonnelPrices
    };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static IEnumerable<TypeItem> Defaults => All.Where(x => x.IsDefault);

    public static readonly HashSet<int> NotImplementedIds = new();
    public static bool IsImplemented(int moduleId) => !NotImplementedIds.Contains(moduleId);

    public static class Ids
    {
        public const int SlnDashboard = 201;
        public const int SlnClients = 202;
        public const int SlnAppointments = 203;
        public const int SlnServices = 204;
        public const int SlnProducts = 205;
        public const int SlnInvoices = 206;
        public const int SlnCash = 207;
        public const int SlnExpenses = 208;
        public const int SlnStaff = 209;
        public const int SlnSuppliers = 210;
        public const int SlnReports = 211;
        public const int SlnCampaigns = 212;
        public const int SlnBranches = 213;
        public const int SlnSales = 214;
        public const int SlnRecipes = 215;
        public const int SlnGiftCards = 216;
        public const int SlnPackages = 217;
        public const int SlnMemberships = 218;
        public const int SlnLoyalty = 219;
        public const int SlnProfile = 220;
        public const int SlnWaitlist = 221;
        public const int SlnEmailCampaigns = 222;
        public const int SlnReviews = 223;
        public const int SlnNoShowPolicy = 224;
        public const int SlnConsentForms = 225;
        public const int SlnBeforeAfter = 226;
        public const int SlnWinback = 227;
        public const int SlnPersonnelPrices = 228;
    }
}
