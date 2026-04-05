using System.Text.Json;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Services;
using CallCenter.Api.Services.Email;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-email-integrations")]
[Authorize]
public class SlnEmailIntegrationController : ControllerBase
{
    private readonly ISlnEmailIntegrationFactory _factory;
    private readonly GmailOAuthService _gmail;
    private readonly Office365OAuthService _o365;
    private readonly AesEncryptionService _aes;
    private readonly IConfiguration _config;

    public SlnEmailIntegrationController(
        ISlnEmailIntegrationFactory factory,
        GmailOAuthService gmail,
        Office365OAuthService o365,
        AesEncryptionService aes,
        IConfiguration config)
    {
        _factory = factory;
        _gmail = gmail;
        _o365 = o365;
        _aes = aes;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmailIntegrationListDto>>> GetAll()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetIntegrationsAsync(customerId));
    }

    [HttpGet("{uid}")]
    public async Task<ActionResult<EmailIntegrationDetailDto>> GetDetail(Guid uid)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var dto = await _factory.GetIntegrationDetailAsync(customerId, uid);
        return dto != null ? Ok(dto) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateEmailIntegrationRequest dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (id, error) = await _factory.CreateIntegrationAsync(customerId, dto);
        return id > 0 ? Ok(new { id }) : BadRequest(error);
    }

    [HttpPut("{uid}")]
    public async Task<ActionResult> Update(Guid uid, [FromBody] UpdateEmailIntegrationRequest dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.UpdateIntegrationAsync(customerId, uid, dto);
        return success ? Ok() : BadRequest(error);
    }

    [HttpDelete("{uid}")]
    public async Task<ActionResult> Delete(Guid uid)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeleteIntegrationAsync(customerId, uid);
        return success ? Ok() : BadRequest(error);
    }

    [HttpPost("{uid}/test")]
    public async Task<ActionResult<EmailSendResult>> TestSend(Guid uid, [FromBody] EmailTestSendRequest dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.TestSendAsync(customerId, uid, dto));
    }

    // ═══ Gmail OAuth ═══

    [HttpGet("gmail/auth-url")]
    public ActionResult GetGmailAuthUrl()
    {
        if (!_gmail.IsConfigured)
            return BadRequest("Gmail OAuth yapilandirilmamis.");

        var customerId = GetCustomerId();
        var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{customerId}"));
        return Ok(new EmailOAuthUrlResponse { AuthUrl = _gmail.GetAuthorizationUrl(state) });
    }

    [HttpGet("gmail/callback")]
    [AllowAnonymous]
    public ActionResult GmailCallback([FromQuery] string? code, [FromQuery] string? error)
    {
        var salonBaseUrl = _config["Salon:BaseUrl"] ?? "http://localhost:5270";
        if (!string.IsNullOrEmpty(error))
            return Redirect($"{salonBaseUrl}/EmailSettings?error={Uri.EscapeDataString(error)}");
        return Redirect($"{salonBaseUrl}/EmailSettings?provider=gmail&code={Uri.EscapeDataString(code ?? "")}");
    }

    [HttpPost("gmail/exchange-code")]
    public async Task<ActionResult<EmailOAuthExchangeResponse>> GmailExchangeCode([FromBody] EmailOAuthExchangeRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var tokenResult = await _gmail.ExchangeCodeAsync(req.Code);
        if (tokenResult == null)
            return Ok(new EmailOAuthExchangeResponse { Success = false, Error = "Token alinamadi." });

        var email = await _gmail.GetUserEmailAsync(tokenResult.AccessToken);
        if (string.IsNullOrEmpty(email))
            return Ok(new EmailOAuthExchangeResponse { Success = false, Error = "E-posta adresi alinamadi." });

        var creds = new Dictionary<string, string>
        {
            ["RefreshToken"] = tokenResult.RefreshToken,
            ["AccessToken"] = tokenResult.AccessToken,
            ["TokenExpiresAt"] = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn).ToString("O")
        };

        var (id, createError) = await _factory.CreateIntegrationAsync(customerId, new CreateEmailIntegrationRequest
        {
            ProviderTypeId = EmailProviders.Ids.Gmail,
            DisplayName = req.DisplayName ?? $"Gmail ({email})",
            SenderEmail = email,
            IsDefault = req.IsDefault,
            Credentials = creds
        });

        return id > 0
            ? Ok(new EmailOAuthExchangeResponse { Success = true, Email = email })
            : Ok(new EmailOAuthExchangeResponse { Success = false, Error = createError });
    }

    // ═══ Office365 OAuth ═══

    [HttpGet("office365/auth-url")]
    public ActionResult GetOffice365AuthUrl()
    {
        if (!_o365.IsConfigured)
            return BadRequest("Office365 OAuth yapilandirilmamis.");

        var customerId = GetCustomerId();
        var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{customerId}"));
        return Ok(new EmailOAuthUrlResponse { AuthUrl = _o365.GetAuthorizationUrl(state) });
    }

    [HttpGet("office365/callback")]
    [AllowAnonymous]
    public ActionResult Office365Callback([FromQuery] string? code, [FromQuery] string? error)
    {
        var salonBaseUrl = _config["Salon:BaseUrl"] ?? "http://localhost:5270";
        if (!string.IsNullOrEmpty(error))
            return Redirect($"{salonBaseUrl}/EmailSettings?error={Uri.EscapeDataString(error)}");
        return Redirect($"{salonBaseUrl}/EmailSettings?provider=office365&code={Uri.EscapeDataString(code ?? "")}");
    }

    [HttpPost("office365/exchange-code")]
    public async Task<ActionResult<EmailOAuthExchangeResponse>> Office365ExchangeCode([FromBody] EmailOAuthExchangeRequest req)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var tokenResult = await _o365.ExchangeCodeAsync(req.Code);
        if (tokenResult == null)
            return Ok(new EmailOAuthExchangeResponse { Success = false, Error = "Token alinamadi." });

        var email = await _o365.GetUserEmailAsync(tokenResult.AccessToken);
        if (string.IsNullOrEmpty(email))
            return Ok(new EmailOAuthExchangeResponse { Success = false, Error = "E-posta adresi alinamadi." });

        var creds = new Dictionary<string, string>
        {
            ["TenantId"] = _config["Office365Email:TenantId"] ?? "common",
            ["RefreshToken"] = tokenResult.RefreshToken,
            ["AccessToken"] = tokenResult.AccessToken,
            ["TokenExpiresAt"] = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn).ToString("O")
        };

        var (id, createError) = await _factory.CreateIntegrationAsync(customerId, new CreateEmailIntegrationRequest
        {
            ProviderTypeId = EmailProviders.Ids.Office365,
            DisplayName = req.DisplayName ?? $"Office 365 ({email})",
            SenderEmail = email,
            IsDefault = req.IsDefault,
            Credentials = creds
        });

        return id > 0
            ? Ok(new EmailOAuthExchangeResponse { Success = true, Email = email })
            : Ok(new EmailOAuthExchangeResponse { Success = false, Error = createError });
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
