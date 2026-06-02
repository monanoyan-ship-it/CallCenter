using Microsoft.AspNetCore.Mvc;
using CallCenter.Shared.Auth;
using CallCenter.Shared.Enums;

namespace CallCenter.Salon.Controllers;

public class MarketingController : SlnBaseController
{
    private static readonly IReadOnlyDictionary<string, string> TabRoutes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["campaigns"] = "/SalonCrm/Campaigns",
        ["email"] = "/SalonCrm/EmailCampaigns",
        ["winback"] = "/SalonCrm/Winback",
        ["memberships"] = "/SalonCrm/Memberships",
        ["giftcards"] = "/SalonCrm/GiftCards",
        ["reviews"] = "/SalonCrm/Reviews",
        ["loyalty"] = "/SalonCrm/Loyalty"
    };

    public IActionResult Index(string? tab = null)
    {
        var route = TabRoutes.TryGetValue(tab ?? string.Empty, out var path)
            ? path
            : "/SalonCrm/Campaigns";

        return MarketingRouteAccess.RedirectToCrm(this, route);
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

    public static RedirectResult RedirectToCrm(Controller controller, string path)
        => controller.Redirect(BuildCrmUrl(controller.HttpContext, path));

    public static string BuildCrmUrl(HttpContext httpContext, string path)
    {
        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var baseUrl = (config["CrmBaseUrl"] ?? "https://crm.corplynk.com").TrimEnd('/');
        return $"{baseUrl}{path}";
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
