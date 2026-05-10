using System.Globalization;
using CallCenter.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.Shared.Localization;

public static class LocalizationExtensions
{
    /// <summary>
    /// MVC/RazorPages uygulamalari icin lokalizasyon servislerini register eder.
    /// </summary>
    public static IServiceCollection AddAppLocalization(this IServiceCollection services, string apiBaseUrl, string? module = null, bool useAcceptLanguageHeader = true)
    {
        services.AddSingleton(sp =>
        {
            var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/") };
            return new ServerTranslationCache(http, module);
        });

        services.AddScoped<ScopedTranslationService>();
        services.AddScoped<ITranslationService>(sp => sp.GetRequiredService<ScopedTranslationService>());

        var supportedCultures = LocalizationConstants.SupportedCodes
            .Select(c => new CultureInfo(c)).ToList();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(LocalizationConstants.DefaultLanguage);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(new RouteValueCultureProvider());
            options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
            if (useAcceptLanguageHeader)
                options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
        });

        return services;
    }

    /// <summary>
    /// Lokalizasyon middleware'ini ekler ve ScopedTranslationService'i her request icin yukler.
    /// Route value tabanli kultur icin UseRouting'den sonra cagrilmali.
    /// </summary>
    public static IApplicationBuilder UseAppLocalization(this IApplicationBuilder app)
    {
        app.UseRequestLocalization();

        app.Use(async (context, next) =>
        {
            var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var translationService = context.RequestServices.GetRequiredService<ScopedTranslationService>();
            translationService.CurrentLanguage = culture;
            await translationService.LoadForCurrentLanguageAsync();
            await next();
        });

        return app;
    }
}
