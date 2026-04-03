using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CallCenter.Shared.Localization;

/// <summary>
/// Route constraint — sadece aktif dil kodlarini eslestirir.
/// Controller isimlerinin culture olarak yakalanmasini onler.
/// </summary>
public class CultureRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value) || value is not string culture)
            return false;

        return LocalizationConstants.SupportedCodes.Contains(culture);
    }
}
