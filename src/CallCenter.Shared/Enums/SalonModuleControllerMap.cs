namespace CallCenter.Shared.Enums;

/// <summary>
/// Controller adi ile SalonPortalModules ID eslesmesi.
/// Sidebar'da ve SlnBaseController'da modul bazli erisim kontrolu icin kullanilir.
/// GetModuleId null donerse (Account, Proxy, Landing vb.) modul kontrolu atlanir.
/// </summary>
public static class SalonModuleControllerMap
{
    private static readonly Dictionary<string, int> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Home"] = SalonPortalModules.Ids.SlnDashboard,              // 201
        ["Clients"] = SalonPortalModules.Ids.SlnClients,             // 202
        ["Appointments"] = SalonPortalModules.Ids.SlnAppointments,   // 203
        ["Services"] = SalonPortalModules.Ids.SlnServices,           // 204
        ["Products"] = SalonPortalModules.Ids.SlnProducts,           // 205
        ["Invoices"] = SalonPortalModules.Ids.SlnInvoices,           // 206
        ["Cash"] = SalonPortalModules.Ids.SlnCash,                   // 207
        ["Expenses"] = SalonPortalModules.Ids.SlnExpenses,           // 208
        ["Staff"] = SalonPortalModules.Ids.SlnStaff,                 // 209
        ["Suppliers"] = SalonPortalModules.Ids.SlnSuppliers,         // 210
        ["Reports"] = SalonPortalModules.Ids.SlnReports,             // 211
        ["Marketing"] = SalonPortalModules.Ids.SlnCampaigns,         // 212 (Musteri Sadakati / Pazarlama ortak ekrani)
        ["Campaigns"] = SalonPortalModules.Ids.SlnCampaigns,         // 212
        ["Branches"] = SalonPortalModules.Ids.SlnBranches,           // 213
        ["Sales"] = SalonPortalModules.Ids.SlnSales,                 // 214
        ["Recipes"] = SalonPortalModules.Ids.SlnRecipes,             // 215
        ["GiftCards"] = SalonPortalModules.Ids.SlnGiftCards,         // 216
        ["LoyaltyPackages"] = SalonPortalModules.Ids.SlnLoyaltyPackages,    // 217
        ["Memberships"] = SalonPortalModules.Ids.SlnMemberships,     // 218
        ["Loyalty"] = SalonPortalModules.Ids.SlnLoyalty,             // 219
        ["Profile"] = SalonPortalModules.Ids.SlnProfile,             // 220
        ["Waitlist"] = SalonPortalModules.Ids.SlnWaitlist,           // 221
        ["EmailCampaigns"] = SalonPortalModules.Ids.SlnEmailCampaigns, // 222
        ["Reviews"] = SalonPortalModules.Ids.SlnReviews,             // 223
        ["NoShowPolicy"] = SalonPortalModules.Ids.SlnNoShowPolicy,   // 224
        ["ConsentForms"] = SalonPortalModules.Ids.SlnConsentForms,   // 225
        ["BeforeAfter"] = SalonPortalModules.Ids.SlnBeforeAfter,     // 226
        ["Winback"] = SalonPortalModules.Ids.SlnWinback,             // 227
        ["PersonnelPrices"] = SalonPortalModules.Ids.SlnPersonnelPrices, // 228
    };

    /// <summary>Controller adina karsilik gelen modul ID'sini dondurur. null = modul kontrolune tabi degil.</summary>
    public static int? GetModuleId(string controllerName) =>
        Map.TryGetValue(controllerName, out var id) ? id : null;

    /// <summary>Verilen aktif modul listesinde ilgili controller'in modulu var mi?</summary>
    public static bool HasModule(IEnumerable<int> activeModuleIds, string controllerName)
    {
        var moduleId = GetModuleId(controllerName);
        if (moduleId == null) return true; // Modul kontrolune tabi degil
        return activeModuleIds.Contains(moduleId.Value);
    }
}
