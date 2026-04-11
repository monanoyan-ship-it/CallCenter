using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Management.Controllers;

public abstract class MgmtBaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controllerType = context.Controller.GetType();
        var token = HttpContext.Session.GetString("Token");

        // Login sayfasi ve Proxy haric tum controller'lar auth gerektirir
        // Session dustu ama cookie varsa geri yukle
        if (string.IsNullOrEmpty(token) && controllerType != typeof(AccountController))
        {
            var rememberToken = HttpContext.Request.Cookies["RememberToken"];
            if (!string.IsNullOrEmpty(rememberToken))
            {
                HttpContext.Session.SetString("Token", rememberToken);
                // JWT'den rol bilgisini parse et
                try
                {
                    var parts = rememberToken.Split('.');
                    if (parts.Length == 3)
                    {
                        var payload = parts[1].Replace('-', '+').Replace('_', '/');
                        switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
                        using var doc = System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(payload));
                        var root = doc.RootElement;
                        if (root.TryGetProperty("given_name", out var gn))
                            HttpContext.Session.SetString("UserName", gn.ToString());
                        if (root.TryGetProperty("role", out var r))
                            HttpContext.Session.SetString("UserRole", r.ToString());
                    }
                }
                catch { }
                token = rememberToken;
            }
            else
            {
                context.Result = RedirectToAction("Login", "Account");
                return;
            }
        }

        // Sadece Admin rolu girebilir
        if (!string.IsNullOrEmpty(token)
            && controllerType != typeof(AccountController)
            && controllerType != typeof(ProxyController))
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "";
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.Clear();
                context.Result = RedirectToAction("Login", "Account");
                return;
            }
        }

        base.OnActionExecuting(context);
    }

    protected HttpClient CreateApiClient()
    {
        var factory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("ManagementApi");
        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
