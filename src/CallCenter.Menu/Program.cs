using CallCenter.Shared.Auth;
using CallCenter.Shared.Localization;
using CallCenter.Shared.Security;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var dpKeysPath = Path.Combine(Path.GetTempPath(), "dp-keys-menu");
Directory.CreateDirectory(dpKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("CallCenter.Menu")
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

builder.Services.AddControllersWithViews();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl yapilandirilmamis. appsettings.json kontrol edin.");

builder.Services.AddHttpClient("MenuApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddAppLocalization(apiBaseUrl, module: "menu", useAcceptLanguageHeader: false);
builder.Services.Configure<JwtAuthCookieOptions>(o => o.CookieName = "CorpLynk.Menu.Auth");

builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options =>
{
    options.ConstraintMap.Add("culture", typeof(CultureRouteConstraint));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCorpLynkSecurityHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseAppLocalization();

app.MapControllerRoute(
    name: "localized",
    pattern: "{culture:culture}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "public-menu",
    pattern: "m/{slug}",
    defaults: new { controller = "PublicMenu", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
