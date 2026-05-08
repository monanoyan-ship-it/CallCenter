using System.Text;
using System.Text.Json;
using CallCenter.Shared.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Management.Controllers;

public class AccountController : MgmtBaseController
{
    [HttpGet]
    public IActionResult Login()
    {
        // Manuel logout (?loggedOut=1) veya AJAX 401 -> Layout handler ?returnUrl ile yonlendirdi.
        // Iki durumda da kullanici login formunu gormeli; cookie'yi temizle ve IsAuthenticated kontrolunu atla.
        var loggedOut = string.Equals(Request.Query["loggedOut"], "1", StringComparison.Ordinal);
        var hasReturnUrl = !string.IsNullOrEmpty(Request.Query["returnUrl"]);
        if (loggedOut || hasReturnUrl)
        {
            HttpContext.ClearAuthCookie();
            return View();
        }

        if (HttpContext.GetJwtIdentity().IsAuthenticated)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, bool rememberMe = false)
    {
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

        var role = root.TryGetProperty("role", out var r) ? r.GetString() : "";
        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            ViewBag.Error = "Bu panele sadece Admin rolu ile giris yapilabilir.";
            return View();
        }

        var token = root.GetProperty("token").GetString() ?? "";
        HttpContext.SetAuthCookie(token, rememberMe ? 30 : 1);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.ClearAuthCookie();
        return RedirectToAction("Login", "Account", new { loggedOut = 1 });
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
    public async Task<IActionResult> ForgotPassword(string username)
    {
        using var client = CreateApiClient();
        var payload = JsonSerializer.Serialize(new { userName = username });
        await client.PostAsync("api/auth/forgot-password",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        ViewBag.Submitted = true;
        ViewBag.Message = "Eğer hesap kayıtlıysa, şifre sıfırlama bağlantısı email adresine gönderildi.";
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
