using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace CallCenter.Shared.Localization;

/// <summary>
/// URL'den {culture} route value'yu okuyarak dil belirler.
/// Ornek: /en/call-center -> "en"
/// </summary>
public class RouteValueCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var culture = httpContext.Request.RouteValues["culture"]?.ToString();

        if (!string.IsNullOrEmpty(culture) && LocalizationConstants.SupportedCodes.Contains(culture))
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture));

        return NullProviderCultureResult;
    }
}
