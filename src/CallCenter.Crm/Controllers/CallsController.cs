using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Crm.Controllers;

public class CallsController : CrmBaseController
{
    public async Task<IActionResult> Index()
    {
        if (RequireCallCenterCrmScope() is { } denied) return denied;

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

    [HttpGet]
    public async Task<IActionResult> GetHistory(int page = 1, int pageSize = 20)
    {
        if (RequireCallCenterCrmScope() is { } denied) return denied;

        using var client = CreateApiClient();
        var response = await client.GetAsync($"api/calls/history?page={page}&pageSize={pageSize}");
        if (!response.IsSuccessStatusCode) return Unauthorized();
        
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }

    [HttpPost]
    public async Task<IActionResult> AssignCallback(int id, int assignedToId, [FromBody] string? note)
    {
        if (RequireCallCenterCrmScope() is { } denied) return denied;

        using var client = CreateApiClient();
        var response = await client.PostAsJsonAsync($"api/calls/{id}/assign-callback?assignedToId={assignedToId}", note);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return BadRequest(new { message = error });
        }
        
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetNumberHistory(string number)
    {
        if (RequireCallCenterCrmScope() is { } denied) return denied;

        if (string.IsNullOrWhiteSpace(number)) return BadRequest();
        using var client = CreateApiClient();
        var response = await client.GetAsync($"api/calls/number-history?number={Uri.EscapeDataString(number)}");
        if (!response.IsSuccessStatusCode) return BadRequest();
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }

    [HttpGet]
    public async Task<IActionResult> GetPersonnel()
    {
        if (RequireCallCenterCrmScope() is { } denied) return denied;

        using var client = CreateApiClient();
        var response = await client.GetAsync("api/portal/personnel"); // Dogru route
        if (!response.IsSuccessStatusCode) return BadRequest();
        
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }
}
