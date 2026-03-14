namespace CallCenter.Crm.Controllers;

public class HomeController : CrmBaseController
{
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Dashboard";

        // Musteri ayarlarini çek (Geri arama yonetimi aktif mi?)
        using var client = CreateApiClient();
        var resp = await client.GetAsync("api/portal/settings");
        if (resp.IsSuccessStatusCode)
        {
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            ViewBag.IsCallbackEnabled = doc.RootElement.GetProperty("isCallbackManagementEnabled").GetBoolean();
        }
        else
        {
            ViewBag.IsCallbackEnabled = false;
        }

        return View();
    }
}
