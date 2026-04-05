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
        SetSessionFromLoginResponse(json);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Token")))
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> DoRegister([FromBody] JsonElement body)
    {
        using var client = CreateApiClient();
        var response = await client.PostAsync("api/auth/salon-register",
            new StringContent(body.GetRawText(), Encoding.UTF8, "application/json"));

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!response.IsSuccessStatusCode || !root.TryGetProperty("success", out var s) || !s.GetBoolean())
        {
            var error = root.TryGetProperty("error", out var e) ? e.GetString() : "Kayit sirasinda hata olustu.";
            return Json(new { success = false, error });
        }

        // Kayıt başarılı, session'a token yaz (Login ile aynı claim'ler)
        if (root.TryGetProperty("token", out var token) && token.GetString() is string t && !string.IsNullOrEmpty(t))
        {
            SetSessionFromLoginResponse(json);
        }

        return Json(new { success = true });
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    private void SetSessionFromLoginResponse(string json)
    {
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
                    HttpContext.Session.SetString("CustomerName", cn.ToString());
                if (claimRoot.TryGetProperty("CustomerRole", out var cr))
                    HttpContext.Session.SetString("CustomerRole", cr.ToString());
                if (claimRoot.TryGetProperty("CustomerRoleId", out var cri))
                    HttpContext.Session.SetString("CustomerRoleId", cri.ToString());
                if (claimRoot.TryGetProperty("IsCustomerAdmin", out var ica))
                    HttpContext.Session.SetString("IsCustomerAdmin", ica.ToString());
            }
        }
        catch { }
    }
}
