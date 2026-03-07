using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Crm.Controllers;

public class AccountController : CrmBaseController
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
        HttpContext.Session.SetString("UserName", root.GetProperty("userName").GetString() ?? "");

        if (root.TryGetProperty("customerName", out var cn))
            HttpContext.Session.SetString("CustomerName", cn.GetString() ?? "");
        if (root.TryGetProperty("customerId", out var ci))
            HttpContext.Session.SetString("CustomerId", ci.GetInt32().ToString());

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
