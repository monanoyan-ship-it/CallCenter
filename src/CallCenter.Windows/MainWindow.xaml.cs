using System.Net.Http;
using System.Windows;
using CallCenter.Windows.LocalData;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CallCenter.Windows;

public partial class MainWindow : Window
{
    private Services.SystemTrayService? _trayService;
    private Services.HotkeyService? _hotkeyService;

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
            handler.InnerHandler = new HttpClientHandler
            {
#if DEBUG
                // Development ortaminda self-signed sertifikayi kabul et
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
#endif
            };
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

        // Contacts (lokal rehber)
        services.AddSingleton<Services.ContactService>();

        // Hotkeys (global kisayollar)
        _hotkeyService = new Services.HotkeyService();
        services.AddSingleton(_hotkeyService);

        // System Tray
        _trayService = new Services.SystemTrayService();
        services.AddSingleton(_trayService);

        // ── Lokal DB + Cift Yazim ──
        // SecureStorage'dan DB ayarlarini oku ve dogru provider'i olustur
        services.AddLogging();
        services.AddSingleton<ILocalRepository>(sp =>
        {
            var storage = sp.GetRequiredService<Services.SecureStorage>();
            var dbType = storage.GetAsync("local_db_type").GetAwaiter().GetResult();
            var connStr = storage.GetAsync("local_db_connection").GetAwaiter().GetResult();
            return LocalRepositoryFactory.Create(dbType, connStr);
        });
        services.AddSingleton<Services.CallSyncService>();
        services.AddSingleton<Services.BackgroundSyncService>();
        services.AddSingleton<Services.LocalReportService>();

        blazorWebView.Services = services.BuildServiceProvider();

        var serviceProvider = blazorWebView.Services as ServiceProvider;

        // System Tray, Hotkeys ve BackgroundSync'i pencere yuklendikten sonra baslat
        Loaded += (s, e) =>
        {
            _trayService.Initialize(this);
            _hotkeyService.Initialize(this);

            // Arka plan senkronizasyonunu baslat (lokal DB → backend push)
            var bgSync = serviceProvider?.GetService<Services.BackgroundSyncService>();
            bgSync?.StartAsync();
        };
    }
}
