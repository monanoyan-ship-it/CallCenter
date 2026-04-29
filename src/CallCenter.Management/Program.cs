using CallCenter.Shared.Auth;
using CallCenter.Shared.Localization;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// DataProtection: Cloud Run container restart larinda key kaybolmasin
var dpKeysPath = Path.Combine(Path.GetTempPath(), "dp-keys-mng");
Directory.CreateDirectory(dpKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("CallCenter.Management")
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

builder.Services.AddControllersWithViews();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl yapilandirilmamis. appsettings.json kontrol edin.");

builder.Services.AddHttpClient("ManagementApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddAppLocalization(apiBaseUrl);

// localhost: Management ile Salon ayni hostta farkli port — ayri JWT cookie adi zorunlu
builder.Services.Configure<JwtAuthCookieOptions>(o => o.CookieName = "CorpLynk.Mgmt.Auth");

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
app.UseStaticFiles();
app.UseAppLocalization();
app.UseRouting();

app.MapControllerRoute(
    name: "localized",
    pattern: "{culture:culture}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
