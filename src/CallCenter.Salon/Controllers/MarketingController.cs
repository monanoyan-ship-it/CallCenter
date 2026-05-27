using Microsoft.AspNetCore.Mvc;
using CallCenter.Shared.Auth;
using CallCenter.Shared.Enums;

namespace CallCenter.Salon.Controllers;

public class MarketingController : SlnBaseController
{
    private static readonly IReadOnlyDictionary<string, string> TabControllers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["campaigns"] = "Campaigns",
        ["email"] = "EmailCampaigns",
        ["winback"] = "Winback",
        ["loyalty"] = "Loyalty",
        ["memberships"] = "Memberships",
        ["giftcards"] = "GiftCards",
        ["reviews"] = "Reviews"
    };

    public IActionResult Index(string? tab = null)
    {
        var availableTabs = MarketingRouteAccess.GetAccessibleTabs(HttpContext, TabControllers);
        if (availableTabs.Count == 0)
            return RedirectToAction("Index", "Home");

        var activeTab = availableTabs.Contains(tab ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            ? tab
            : availableTabs[0];

        ViewData["Title"] = "Salon CRM";
        ViewData["MarketingTab"] = activeTab;
        ViewData["MarketingTabs"] = availableTabs;
        return View();
    }
}

internal static class MarketingRouteAccess
{
    public static bool CanUseConsolidated(HttpContext httpContext) =>
        CanAccessPage(httpContext, "Marketing");

    public static List<string> GetAccessibleTabs(HttpContext httpContext, IReadOnlyDictionary<string, string> tabControllers)
    {
        return tabControllers
            .Where(tab => CanAccessPage(httpContext, tab.Value))
            .Select(tab => tab.Key)
            .ToList();
    }

    private static bool CanAccessPage(HttpContext httpContext, string controllerName)
    {
        var jwt = httpContext.GetJwtIdentity();
        var roleId = jwt.CustomerRoleId > 0 ? jwt.CustomerRoleId : SalonRoles.Ids.SalonOwner;
        if (!SalonRolePermissions.CanAccess(roleId, controllerName))
            return false;

        var modulesCsv = jwt.CustomerModules;
        if (string.IsNullOrWhiteSpace(modulesCsv))
            return true;

        var activeModuleIds = modulesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

        return SalonModuleControllerMap.HasModule(activeModuleIds, controllerName);
    }
}
