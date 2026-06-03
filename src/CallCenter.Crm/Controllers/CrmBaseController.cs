using System.Text.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Crm.Controllers;

public abstract class CrmBaseController : Controller
{
    protected const string CrmEntitlementsSessionKey = "CrmEntitlements";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrEmpty(token) && context.Controller.GetType() != typeof(AccountController))
        {
            var returnUrl = $"{Request.PathBase}{Request.Path}{Request.QueryString}";
            context.Result = RedirectToAction("Login", "Account", new { returnUrl });
            return;
        }

        // Session'da musteri rolu yoksa (eski oturumlar icin) token'dan cikarip ekleyelim
        if (!string.IsNullOrEmpty(token) && string.IsNullOrEmpty(HttpContext.Session.GetString("CustomerRole")))
        {
            try
            {
                var jwtParts = token.Split('.');
                if (jwtParts.Length == 3)
                {
                    var jwtPayload = jwtParts[1].Replace('-', '+').Replace('_', '/');
                    switch (jwtPayload.Length % 4)
                    {
                        case 2: jwtPayload += "=="; break;
                        case 3: jwtPayload += "="; break;
                    }
                    var payloadBytes = Convert.FromBase64String(jwtPayload);
                    using var claims = System.Text.Json.JsonDocument.Parse(payloadBytes);
                    if (claims.RootElement.TryGetProperty("CustomerRole", out var cr))
                    {
                        HttpContext.Session.SetString("CustomerRole", cr.ToString());
                    }
                }
            }
            catch { /* Hata olursa yoksay */ }
        }

        ViewData[CrmEntitlementsSessionKey] = GetCrmEntitlements();
        base.OnActionExecuting(context);
    }

    protected HttpClient CreateApiClient()
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("CrmApi");
        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected CrmEntitlementsDto? GetCrmEntitlements()
    {
        var json = HttpContext.Session.GetString(CrmEntitlementsSessionKey);
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<CrmEntitlementsDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    protected async Task<CrmEntitlementsDto?> RefreshCrmEntitlementsAsync()
    {
        using var client = CreateApiClient();
        var response = await client.GetAsync("api/crm/entitlements");
        if (!response.IsSuccessStatusCode) return GetCrmEntitlements();

        var json = await response.Content.ReadAsStringAsync();
        HttpContext.Session.SetString(CrmEntitlementsSessionKey, json);
        return GetCrmEntitlements();
    }

    protected bool HasCrmScope(string scopeKey)
    {
        var entitlements = GetCrmEntitlements();
        if (entitlements == null) return true;

        return scopeKey switch
        {
            CrmScopes.Core => entitlements.HasStandaloneCrm,
            CrmScopes.Salon => entitlements.HasSalonCrm,
            CrmScopes.CallCenter => entitlements.HasCallCenterCrm,
            _ => false
        };
    }

    protected IActionResult? RequireCrmScope(string scopeKey)
        => HasCrmScope(scopeKey) ? null : RedirectToAction("NoAccess", "Home", new { scope = scopeKey });

    protected IActionResult? RequireCoreCrmScope()
        => RequireCrmScope(CrmScopes.Core);

    protected IActionResult? RequireSalonCrmScope()
        => RequireCrmScope(CrmScopes.Salon);

    protected IActionResult? RequireCallCenterCrmScope()
        => RequireCrmScope(CrmScopes.CallCenter);

    protected IActionResult? RequireCoreOrCallCenterCrmScope()
        => HasCrmScope(CrmScopes.Core) || HasCrmScope(CrmScopes.CallCenter)
            ? null
            : RedirectToAction("NoAccess", "Home", new { scope = CrmScopes.Core });
}
