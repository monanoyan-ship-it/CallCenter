using CallCenter.Shared.Enums;

namespace CallCenter.Crm.Controllers;

public class HomeController : CrmBaseController
{
    public async Task<IActionResult> Index()
    {
        var entitlements = GetCrmEntitlements() ?? await RefreshCrmEntitlementsAsync();
        if (entitlements == null || string.IsNullOrWhiteSpace(entitlements.DefaultScope))
            return RedirectToAction(nameof(NoAccess));

        return entitlements.DefaultScope switch
        {
            CrmScopes.Salon => RedirectToAction(nameof(Salon)),
            CrmScopes.CallCenter => RedirectToAction(nameof(CallCenter)),
            CrmScopes.Overview => RedirectToAction(nameof(Overview)),
            _ => RedirectToAction(nameof(Core))
        };
    }

    public async Task<IActionResult> Core()
    {
        if (RequireCrmScope(CrmScopes.Core) is { } denied) return denied;

        ViewData["Title"] = "CRM Dashboard";
        await LoadPortalSettingsAsync();
        return View("Index");
    }

    public IActionResult Overview()
    {
        ViewData["Title"] = "CRM Kapsamlari";
        return View(GetCrmEntitlements());
    }

    public IActionResult NoAccess(string? scope)
    {
        ViewData["Title"] = "CRM Paketi Aktif Degil";
        ViewBag.Scope = scope;
        return View(GetCrmEntitlements());
    }

    public IActionResult Salon()
    {
        if (RequireCrmScope(CrmScopes.Salon) is { } denied) return denied;

        ViewData["Title"] = "Salon";
        return View();
    }

    public async Task<IActionResult> CallCenter()
    {
        if (RequireCrmScope(CrmScopes.CallCenter) is { } denied) return denied;

        ViewData["Title"] = "Call Center";
        await LoadPortalSettingsAsync();
        return View();
    }

    private async Task LoadPortalSettingsAsync()
    {
        using var client = CreateApiClient();
        var resp = await client.GetAsync("api/portal/settings");
        if (!resp.IsSuccessStatusCode)
        {
            ViewBag.IsCallbackEnabled = false;
            return;
        }

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        ViewBag.IsCallbackEnabled = doc.RootElement.GetProperty("isCallbackManagementEnabled").GetBoolean();
    }
}
