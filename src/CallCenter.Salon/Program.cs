using CallCenter.Shared.Auth;
using CallCenter.Shared.Localization;
using CallCenter.Shared.Security;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// DataProtection: Cloud Run container restart larinda key kaybolmasin
// /tmp dizini her instance da bos baslar ama instance yasadigi surece kalir
var dpKeysPath = Path.Combine(Path.GetTempPath(), "dp-keys-salon");
Directory.CreateDirectory(dpKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("CallCenter.Salon")
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

builder.Services.AddControllersWithViews();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl yapilandirilmamis. appsettings.json kontrol edin.");

builder.Services.AddHttpClient("SalonApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddAppLocalization(apiBaseUrl, module: "salon", useAcceptLanguageHeader: false);

builder.Services.Configure<JwtAuthCookieOptions>(o => o.CookieName = "CorpLynk.Salon.Auth");

// Session kaldirildi — JWT cookie tabanli stateless akis (Shared.Auth.JwtIdentity)

builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options =>
{
    options.ConstraintMap.Add("culture", typeof(CultureRouteConstraint));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
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
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
