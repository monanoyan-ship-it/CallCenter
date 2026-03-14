using System.Security.Claims;
using System.Text.Json;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

/// <summary>
/// Dis sistemlerin bizim API'mize eristigi gateway.
/// Authentication: X-Api-Key header (ApiKeyAuthMiddleware tarafindan dogrulanir).
/// Tum endpoint'ler /api/integration/v1/ prefix'i ile baslar.
/// </summary>
[ApiController]
[Route("api/integration/v1")]
public class IntegrationApiController : ControllerBase
{
    private readonly ICrmFactory _crmFactory;
    private readonly IAgentFactory _agentFactory;

    public IntegrationApiController(
        ICrmFactory crmFactory,
        IAgentFactory agentFactory)
    {
        _crmFactory = crmFactory;
        _agentFactory = agentFactory;
    }

    /// <summary>Kisi arama (telefon numarasi ile) — Screen pop icin kullanilir</summary>
    [HttpGet("contacts/lookup")]
    public async Task<IActionResult> LookupContact([FromQuery] string phone)
    {
        if (!HasScope("contacts:read")) return Forbid();
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized();

        var contact = await _crmFactory.LookupByPhoneAsync(customerId.Value, phone);
        if (contact == null) return NotFound(new { message = "Bu telefon numarasi ile kayitli kisi bulunamadi." });
        return Ok(contact);
    }

    /// <summary>Click-to-dial — dis sistemden arama baslatma istegi</summary>
    [HttpPost("calls/dial")]
    public Task<IActionResult> ClickToDial([FromBody] ClickToDialRequest dto)
    {
        if (!HasScope("calls:write")) return Task.FromResult<IActionResult>(Forbid());
        var customerId = GetCustomerId();
        if (customerId == null) return Task.FromResult<IActionResult>(Unauthorized());

        // Not: Gercek arama baslatma PBX + SignalR ile calisir.
        // Bu endpoint arama istegi olusturur, ilgili agent'a SignalR bildirimi gider.
        // Connector adapter'lar eklendiginde (Adim 5-6) detaylandirilacak.
        return Task.FromResult<IActionResult>(Ok(new
        {
            message = "Arama istegi olusturuldu.",
            phoneNumber = dto.PhoneNumber,
            agentUid = dto.AgentUid
        }));
    }

    /// <summary>Agent durumlari — dis sistemin agent musaitligini sorgulamasi</summary>
    [HttpGet("agents/status")]
    public async Task<IActionResult> GetAgentStatuses()
    {
        if (!HasScope("agents:read")) return Forbid();
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized();

        // Mevcut AgentFactory.GetAllAsync tum agentlari getirir
        // Customer-scoped filtreleme connector entegrasyonunda detaylandirilacak
        var agents = await _agentFactory.GetAllAsync();
        return Ok(agents);
    }

    /// <summary>Kisi listesi</summary>
    [HttpGet("contacts")]
    public async Task<IActionResult> GetContacts([FromQuery] string? search = null)
    {
        if (!HasScope("contacts:read")) return Forbid();
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized();

        var contacts = await _crmFactory.GetContactsAsync(customerId.Value, search);
        return Ok(contacts);
    }

    /// <summary>API durum kontrolu (health check)</summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "ok",
            version = "v1",
            timestamp = DateTime.UtcNow
        });
    }

    // ─── Helpers ───

    private int? GetCustomerId()
    {
        var claim = User.FindFirstValue("CustomerId");
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    private bool HasScope(string scope)
    {
        var scopesClaim = User.FindFirstValue("ApiKeyScopes");
        if (string.IsNullOrEmpty(scopesClaim)) return false;

        try
        {
            var scopes = JsonSerializer.Deserialize<List<string>>(scopesClaim);
            return scopes != null && (scopes.Contains(scope) || scopes.Contains("*"));
        }
        catch
        {
            return false;
        }
    }
}
