using System.Net.Http;
using System.Windows;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.Windows;

public partial class MainWindow : Window
{
    private Services.SystemTrayService? _trayService;

    public MainWindow()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        // Configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("wwwroot/appsettings.json", optional: false)
            .Build();
        services.AddSingleton<IConfiguration>(config);

        // Secure Storage (localStorage yerine dosya tabanli)
        services.AddSingleton<Services.SecureStorage>();

        // Auth
        services.AddSingleton<Services.WindowsAuthStateProvider>();
        services.AddSingleton<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<Services.WindowsAuthStateProvider>());
        services.AddSingleton<Services.WindowsAuthService>();
        services.AddAuthorizationCore();

        // HttpClient + Bearer token
        var apiBaseUrl = config["ApiBaseUrl"] ?? "https://localhost:7147";
        services.AddSingleton<Services.WindowsAuthHeaderHandler>();
        services.AddSingleton(sp =>
        {
            var handler = sp.GetRequiredService<Services.WindowsAuthHeaderHandler>();
            handler.InnerHandler = new HttpClientHandler();
            return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
        });

        // SignalR
        services.AddSingleton<Services.WindowsHubService>();

        // Permissions
        services.AddSingleton<Services.WindowsPermissionService>();

        // Toast
        services.AddSingleton<Services.ToastService>();

        // SIP (native SIPSorcery)
        services.AddSingleton<Services.ISipService, Services.NativeSipService>();

        // System Tray
        _trayService = new Services.SystemTrayService();
        services.AddSingleton(_trayService);

        blazorWebView.Services = services.BuildServiceProvider();

        // System Tray'i pencere yuklendikten sonra baslat
        Loaded += (s, e) => _trayService.Initialize(this);
    }
}
