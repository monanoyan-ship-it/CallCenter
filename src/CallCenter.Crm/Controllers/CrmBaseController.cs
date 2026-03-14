using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CallCenter.Crm.Controllers;

public abstract class CrmBaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrEmpty(token) && context.Controller.GetType() != typeof(AccountController))
        {
            context.Result = RedirectToAction("Login", "Account");
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
                        HttpContext.Session.SetString("CustomerRole", cr.GetString() ?? "");
                    }
                }
            }
            catch { /* Hata olursa yoksay */ }
        }

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
}
