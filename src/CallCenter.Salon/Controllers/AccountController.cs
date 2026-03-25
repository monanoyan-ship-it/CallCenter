using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class AccountController : SlnBaseController
{
    [HttpGet]
    public IActionResult Login()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Token")))
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        using var client = CreateApiClient();
        var payload = JsonSerializer.Serialize(new { username, password });
        var response = await client.PostAsync("api/auth/login",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = "Kullanici adi veya sifre hatali.";
            return View();
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        HttpContext.Session.SetString("Token", root.GetProperty("token").GetString() ?? "");
        HttpContext.Session.SetString("UserName", root.GetProperty("fullName").GetString() ?? "");
        HttpContext.Session.SetString("UserRole", root.GetProperty("role").GetString() ?? "");

        try
        {
            var jwtToken = root.GetProperty("token").GetString() ?? "";
            var jwtParts = jwtToken.Split('.');
            if (jwtParts.Length == 3)
            {
                var jwtPayload = jwtParts[1].Replace('-', '+').Replace('_', '/');
                switch (jwtPayload.Length % 4)
                {
                    case 2: jwtPayload += "=="; break;
                    case 3: jwtPayload += "="; break;
                }
                var payloadBytes = Convert.FromBase64String(jwtPayload);
                using var claims = JsonDocument.Parse(payloadBytes);
                var claimRoot = claims.RootElement;

                if (claimRoot.TryGetProperty("CustomerName", out var cn))
                    HttpContext.Session.SetString("CustomerName", cn.GetString() ?? "");
                if (claimRoot.TryGetProperty("CustomerRole", out var cr))
                    HttpContext.Session.SetString("CustomerRole", cr.GetString() ?? "");
                if (claimRoot.TryGetProperty("IsCustomerAdmin", out var ica))
                    HttpContext.Session.SetString("IsCustomerAdmin", ica.GetString() ?? "false");
            }
        }
        catch { }

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
