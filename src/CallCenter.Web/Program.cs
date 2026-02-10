using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using CallCenter.Web;
using CallCenter.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base URL (development: API ayri portta calisir)
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7147";

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

// Toast bildirim servisi
builder.Services.AddScoped<ToastService>();

// Portal yetki servisi (JWT claim parse)
builder.Services.AddScoped<PermissionService>();

await builder.Build().RunAsync();
