using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Crm.Controllers;

public class AccountController : CrmBaseController
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Token")))
            return RedirectAfterLogin(returnUrl);

        ViewBag.ReturnUrl = returnUrl ?? "";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl ?? "";

        using var client = CreateApiClient();
        var payload = JsonSerializer.Serialize(new { username, password });
        var response = await client.PostAsync("api/auth/login",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = ExtractMessage(json, "Kullanıcı adı veya şifre hatalı.");
            return View();
        }
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        HttpContext.Session.Clear();
        HttpContext.Session.SetString("Token", root.GetProperty("token").GetString() ?? "");
        HttpContext.Session.SetString("UserName", root.GetProperty("fullName").GetString() ?? "");
        HttpContext.Session.SetString("UserRole", root.GetProperty("role").GetString() ?? "");

        // JWT token'dan customer bilgilerini coz
        var jwtToken = root.GetProperty("token").GetString() ?? "";
        try
        {
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
                if (claimRoot.TryGetProperty("IsCustomerAdmin", out var ica))
                    HttpContext.Session.SetString("IsCustomerAdmin", ica.ToString());

            }
        }
        catch { /* JWT parse hatasi olursa login akisini engelleme */ }

        await RefreshCrmEntitlementsAsync();

        return RedirectAfterLogin(returnUrl);
    }

    private IActionResult RedirectAfterLogin(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Success = false;
            ViewBag.Message = "Doğrulama bağlantısı geçersiz.";
            return View();
        }

        using var client = CreateApiClient();
        var response = await client.GetAsync($"api/auth/verify-email?token={Uri.EscapeDataString(token)}");
        var json = await response.Content.ReadAsStringAsync();

        ViewBag.Success = response.IsSuccessStatusCode;
        ViewBag.Message = ExtractMessage(json, response.IsSuccessStatusCode
            ? "Email başarıyla doğrulandı."
            : "Doğrulama başarısız.");
        return View();
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string username)
    {
        using var client = CreateApiClient();
        var payload = JsonSerializer.Serialize(new { userName = username });
        await client.PostAsync("api/auth/forgot-password",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        ViewBag.Submitted = true;
        ViewBag.Message = "Eğer hesap kayıtlıysa, şifre sıfırlama bağlantısı e-posta adresine gönderildi.";
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string? token)
    {
        ViewBag.Token = token ?? "";
        if (string.IsNullOrWhiteSpace(token))
            ViewBag.Error = "Sıfırlama bağlantısı geçersiz.";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
    {
        ViewBag.Token = token;
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Error = "Sıfırlama bağlantısı geçersiz.";
            return View();
        }
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            ViewBag.Error = "Şifre en az 6 karakter olmalı.";
            return View();
        }
        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "Şifreler eşleşmiyor.";
            return View();
        }

        using var client = CreateApiClient();
        var payload = JsonSerializer.Serialize(new { token, newPassword });
        var response = await client.PostAsync("api/auth/reset-password",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = ExtractMessage(json, "Şifre sıfırlama başarısız.");
            return View();
        }

        ViewBag.Success = true;
        ViewBag.Message = "Şifreniz başarıyla güncellendi. Giriş yapabilirsiniz.";
        return View();
    }

    private static string ExtractMessage(string json, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var m) && m.GetString() is { } msg)
                return msg;
        }
        catch { }
        return fallback;
    }
}
