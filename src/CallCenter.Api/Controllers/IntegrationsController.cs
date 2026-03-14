using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

/// <summary>
/// Entegrasyon yonetimi — baglanti, webhook, API key islemleri.
/// Musteri portali uzerinden kullanilir (CustomerId JWT claim'den alinir).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IntegrationsController : AuditableControllerBase
{
    private readonly IIntegrationFactory _factory;

    public IntegrationsController(IAuditFactory auditFactory, IIntegrationFactory factory)
        : base(auditFactory)
    {
        _factory = factory;
    }

    // ═══════════════════════════════════════════════════════════
    // Connection
    // ═══════════════════════════════════════════════════════════

    /// <summary>Entegrasyon baglantilari listesi</summary>
    [HttpGet("connections")]
    public async Task<IActionResult> GetConnections()
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();
        return Ok(await _factory.GetConnectionsAsync(customerId.Value));
    }

    /// <summary>Entegrasyon baglantisi detayi (webhook + api key dahil)</summary>
    [HttpGet("connections/{uid:guid}")]
    public async Task<IActionResult> GetConnectionDetail(Guid uid)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();
        var result = await _factory.GetConnectionDetailAsync(customerId.Value, uid);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Yeni entegrasyon baglantisi olustur</summary>
    [HttpPost("connections")]
    public async Task<IActionResult> CreateConnection([FromBody] CreateIntegrationConnectionRequest dto)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();

        var (id, error) = await _factory.CreateConnectionAsync(customerId.Value, dto);
        if (id == 0) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "IntegrationConnection", id.ToString(),
            $"Entegrasyon baglantisi olusturuldu: {dto.Name}");
        return Ok(new { id });
    }

    /// <summary>Entegrasyon baglantisi guncelle</summary>
    [HttpPut("connections/{uid:guid}")]
    public async Task<IActionResult> UpdateConnection(Guid uid, [FromBody] UpdateIntegrationConnectionRequest dto)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();

        var (success, error) = await _factory.UpdateConnectionAsync(customerId.Value, uid, dto);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "IntegrationConnection", uid.ToString(),
            $"Entegrasyon baglantisi guncellendi");
        return Ok();
    }

    /// <summary>Entegrasyon baglantisi sil</summary>
    [HttpDelete("connections/{uid:guid}")]
    public async Task<IActionResult> DeleteConnection(Guid uid)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();

        var (success, error) = await _factory.DeleteConnectionAsync(customerId.Value, uid);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Delete", "IntegrationConnection", uid.ToString(),
            $"Entegrasyon baglantisi silindi");
        return Ok();
    }

    /// <summary>Baglanti testi</summary>
    [HttpPost("connections/{uid:guid}/test")]
    public async Task<IActionResult> TestConnection(Guid uid)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();
        return Ok(await _factory.TestConnectionAsync(customerId.Value, uid));
    }

    // ═══════════════════════════════════════════════════════════
    // OAuth2
    // ═══════════════════════════════════════════════════════════

    /// <summary>OAuth2 akisi baslat (Salesforce/HubSpot)</summary>
    [HttpPost("oauth/initiate")]
    public async Task<IActionResult> InitiateOAuth([FromQuery] int platformTypeId, [FromQuery] string? returnUrl = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();

        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/integrations/oauth/callback";
        var result = await _factory.InitiateOAuthAsync(customerId.Value, platformTypeId, callbackUrl);
        if (result == null) return BadRequest(new { message = "Bu platform OAuth2 desteklemiyor." });

        // returnUrl varsa state'e encode edip ekle (CRM/Web portal redirect icin)
        if (!string.IsNullOrEmpty(returnUrl))
        {
            var encodedReturnUrl = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(returnUrl));
            var newState = $"{result.State}:{encodedReturnUrl}";
            result.AuthorizationUrl = result.AuthorizationUrl.Replace($"state={result.State}", $"state={newState}");
            result.State = newState;
        }

        return Ok(result);
    }

    /// <summary>OAuth2 callback (Salesforce/HubSpot redirect sonrasi)</summary>
    [HttpGet("oauth/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> OAuthCallback([FromQuery] string code, [FromQuery] string state)
    {
        // State: "customerId:platformTypeId:guid[:base64ReturnUrl]"
        var parts = state.Split(':');
        if (parts.Length < 2 || !int.TryParse(parts[0], out var customerId))
            return BadRequest("Gecersiz state parametresi.");

        // returnUrl varsa cikar (4. parca base64-encoded)
        string? returnUrl = null;
        if (parts.Length >= 4)
        {
            try { returnUrl = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[3])); }
            catch { /* invalid base64 — ignore */ }
        }

        var (success, error) = await _factory.HandleOAuthCallbackAsync(customerId, new OAuthCallbackRequest
        {
            Code = code,
            State = state
        });

        if (!success)
            return BadRequest(new { message = error });

        // Basarili → CRM veya Web portal'a redirect
        var redirectTo = !string.IsNullOrEmpty(returnUrl)
            ? $"{returnUrl}?oauth=success"
            : "/customer/integrations?oauth=success";
        return Redirect(redirectTo);
    }

    // ═══════════════════════════════════════════════════════════
    // Webhook
    // ═══════════════════════════════════════════════════════════

    /// <summary>Webhook abonelikleri listesi</summary>
    [HttpGet("webhooks")]
    public async Task<IActionResult> GetWebhooks()
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();
        return Ok(await _factory.GetWebhooksAsync(customerId.Value));
    }

    /// <summary>Yeni webhook aboneligi olustur</summary>
    [HttpPost("webhooks")]
    public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookSubscriptionRequest dto)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();

        var (id, error) = await _factory.CreateWebhookAsync(customerId.Value, dto);
        if (id == 0) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "WebhookSubscription", id.ToString(),
            $"Webhook olusturuldu: {dto.Name} -> {dto.TargetUrl}");
        return Ok(new { id });
    }

    /// <summary>Webhook aboneligi guncelle</summary>
    [HttpPut("webhooks/{uid:guid}")]
    public async Task<IActionResult> UpdateWebhook(Guid uid, [FromBody] UpdateWebhookSubscriptionRequest dto)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();

        var (success, error) = await _factory.UpdateWebhookAsync(customerId.Value, uid, dto);
        if (!success) return BadRequest(new { message = error });
        return Ok();
    }

    /// <summary>Webhook aboneligi sil</summary>
    [HttpDelete("webhooks/{uid:guid}")]
    public async Task<IActionResult> DeleteWebhook(Guid uid)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();

        var (success, error) = await _factory.DeleteWebhookAsync(customerId.Value, uid);
        if (!success) return BadRequest(new { message = error });
        return Ok();
    }

    /// <summary>Test webhook gonder</summary>
    [HttpPost("webhooks/{uid:guid}/test")]
    public async Task<IActionResult> SendTestWebhook(Guid uid)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();

        var (success, error) = await _factory.SendTestWebhookAsync(customerId.Value, uid);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Test webhook gonderildi." });
    }

    /// <summary>Webhook gonderim loglari</summary>
    [HttpGet("webhooks/{uid:guid}/deliveries")]
    public async Task<IActionResult> GetDeliveries(Guid uid, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();
        return Ok(await _factory.GetDeliveriesAsync(customerId.Value, uid, page, pageSize));
    }

    // ═══════════════════════════════════════════════════════════
    // API Key
    // ═══════════════════════════════════════════════════════════

    /// <summary>API key listesi</summary>
    [HttpGet("api-keys")]
    public async Task<IActionResult> GetApiKeys([FromQuery] int? connectionId = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();
        return Ok(await _factory.GetApiKeysAsync(customerId.Value, connectionId));
    }

    /// <summary>Yeni API key olustur (key sadece bir kez gosterilir!)</summary>
    [HttpPost("api-keys")]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest dto)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();

        var result = await _factory.CreateApiKeyAsync(customerId.Value, dto);
        if (result == null) return BadRequest(new { message = "Entegrasyon baglantisi bulunamadi." });

        await AuditCrudAsync("Create", "IntegrationApiKey", result.Id.ToString(),
            $"API key olusturuldu: {dto.Name}");
        return Ok(result);
    }

    /// <summary>API key iptal et (revoke)</summary>
    [HttpDelete("api-keys/{uid:guid}")]
    public async Task<IActionResult> RevokeApiKey(Guid uid)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Forbid();

        var (success, error) = await _factory.RevokeApiKeyAsync(customerId.Value, uid);
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Delete", "IntegrationApiKey", uid.ToString(),
            $"API key iptal edildi");
        return Ok();
    }

    // ═══════════════════════════════════════════════════════════
    // Referans veri
    // ═══════════════════════════════════════════════════════════

    /// <summary>Desteklenen platform listesi</summary>
    [HttpGet("platforms")]
    public IActionResult GetPlatforms()
    {
        return Ok(IntegrationPlatforms.All.Select(p => new
        {
            p.Id,
            p.SystemName,
            p.Description,
            p.Icon,
            p.CssClass
        }));
    }

    /// <summary>Webhook event tipleri listesi</summary>
    [HttpGet("event-types")]
    public IActionResult GetEventTypes()
    {
        return Ok(WebhookEventTypes.All.Select(e => new
        {
            e.Id,
            e.SystemName,
            e.Description,
            e.Icon,
            e.CssClass
        }));
    }

    // ─── Helpers ───

    private int? GetCustomerId()
    {
        var claim = User.FindFirstValue("CustomerId");
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
