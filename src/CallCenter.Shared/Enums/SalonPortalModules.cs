namespace CallCenter.Shared.Enums;

/// <summary>
/// Salon portal modulleri. Musteri bazli acilir/kapanir.
/// ID'ler 201+ ile baslar (CallCenter PortalModules ile cakismamasi icin).
/// </summary>
public static class SalonPortalModules
{
    public static readonly TypeItem SlnDashboard = new(201, "SlnDashboard", "SalonModule.Dashboard", "Dashboard", "bi-speedometer2", "bg-primary", 1, isDefault: true);
    public static readonly TypeItem SlnClients = new(202, "SlnClients", "SalonModule.Clients", "Musteri yonetimi", "bi-people-fill", "bg-success", 2, isDefault: true);
    public static readonly TypeItem SlnAppointments = new(203, "SlnAppointments", "SalonModule.Appointments", "Randevu yonetimi", "bi-calendar-check", "bg-info", 3, isDefault: true);
    public static readonly TypeItem SlnServices = new(204, "SlnServices", "SalonModule.Services", "Hizmet tanimlari", "bi-list-check", "bg-warning text-dark", 4, isDefault: true);
    public static readonly TypeItem SlnProducts = new(205, "SlnProducts", "SalonModule.Products", "Urun ve stok", "bi-box-seam", "bg-secondary", 5);
    public static readonly TypeItem SlnInvoices = new(206, "SlnInvoices", "SalonModule.Invoices", "Adisyon ve satis", "bi-receipt", "bg-danger", 6, isDefault: true);
    public static readonly TypeItem SlnCash = new(207, "SlnCash", "SalonModule.Cash", "Kasa yonetimi", "bi-cash-stack", "bg-success", 7);
    public static readonly TypeItem SlnExpenses = new(208, "SlnExpenses", "SalonModule.Expenses", "Masraf takibi", "bi-credit-card", "bg-dark", 8);
    public static readonly TypeItem SlnStaff = new(209, "SlnStaff", "SalonModule.Staff", "Personel ve maas", "bi-person-badge", "bg-indigo", 9);
    public static readonly TypeItem SlnSuppliers = new(210, "SlnSuppliers", "SalonModule.Suppliers", "Tedarikci ve cari", "bi-truck", "bg-teal", 10);
    public static readonly TypeItem SlnReports = new(211, "SlnReports", "SalonModule.Reports", "Raporlama", "bi-bar-chart-line", "bg-orange", 11);
    public static readonly TypeItem SlnCampaigns = new(212, "SlnCampaigns", "SalonModule.Campaigns", "Pazarlama ve SMS", "bi-megaphone-fill", "bg-pink", 12);
    public static readonly TypeItem SlnBranches = new(213, "SlnBranches", "SalonModule.Branches", "Sube yonetimi", "bi-building", "bg-cyan", 13);

    public static IEnumerable<TypeItem> All => new[] { SlnDashboard, SlnClients, SlnAppointments, SlnServices, SlnProducts, SlnInvoices, SlnCash, SlnExpenses, SlnStaff, SlnSuppliers, SlnReports, SlnCampaigns, SlnBranches };
    public static TypeItem? GetById(int id) => All.FirstOrDefault(x => x.Id == id);
    public static IEnumerable<TypeItem> Defaults => All.Where(x => x.IsDefault);

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
    }
}
