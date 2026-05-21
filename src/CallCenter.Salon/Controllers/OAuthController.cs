using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Salon.Controllers;

public class OAuthController : Controller
{
    [HttpGet("oauth/gmail/callback")]
    public IActionResult GmailCallback(
        [FromQuery] string? code,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        if (!string.IsNullOrWhiteSpace(error) || !string.IsNullOrWhiteSpace(errorDescription))
        {
            var message = errorDescription ?? error ?? "OAuth hatasi";
            return Redirect($"/EmailSettings?error={Uri.EscapeDataString(message)}");
        }

        return Redirect($"/EmailSettings?provider=gmail&code={Uri.EscapeDataString(code ?? "")}");
    }
}
