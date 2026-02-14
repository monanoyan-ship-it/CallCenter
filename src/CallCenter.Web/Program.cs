using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using CallCenter.Web;
using CallCenter.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base URL
// - Development: API ayri portta calisir (https://localhost:7147)
// - Docker/Nginx: Bos veya null → ayni origin uzerinden proxy ile erisim
var configuredUrl = builder.Configuration["ApiBaseUrl"];
var apiBaseUrl = string.IsNullOrWhiteSpace(configuredUrl)
    ? builder.HostEnvironment.BaseAddress.TrimEnd('/')
    : configuredUrl;

// Auth servisleri
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthorizationCore();

// HttpClient + Bearer token interceptor
builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthHeaderHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
});

// SignalR Hub servisi
builder.Services.AddScoped<HubService>();

// SIP/VoIP servisi (SIP.js JS Interop wrapper)
builder.Services.AddScoped<SipService>();

// Coklu dil (Translation) servisi
builder.Services.AddScoped<TranslationService>();

// Toast bildirim servisi
builder.Services.AddScoped<ToastService>();

// Portal yetki servisi (JWT claim parse)
builder.Services.AddScoped<PermissionService>();

await builder.Build().RunAsync();
