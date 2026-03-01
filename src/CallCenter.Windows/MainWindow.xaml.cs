using System.IO;
using System.Net.Http;
using System.Windows;
using CallCenter.Windows.LocalData;
using CallCenter.Windows.LocalData.Providers;
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
                // HTTP ve self-signed sertifika destegi (test/on-premise ortami)
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
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

        // ── Lokal Dosya Deposu + Cift Yazim ──
        services.AddLogging();
        services.AddSingleton<ILocalRepository>(sp =>
        {
            var basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CallCenter", "Data");
            return new FileLocalRepository(basePath);
        });
        services.AddSingleton<Services.CallSyncService>();
        services.AddSingleton<Services.BackgroundSyncService>();
        services.AddSingleton<Services.LocalReportService>();

        blazorWebView.Services = services.BuildServiceProvider();

        var serviceProvider = blazorWebView.Services as ServiceProvider;

        // System Tray, Hotkeys ve BackgroundSync'i pencere yuklendikten sonra baslat
        Loaded += async (s, e) =>
        {
            _trayService.Initialize(this);
            _hotkeyService.Initialize(this);

            // Lokal DB tablolarini initialize et — BEFORE anything accesses the DB
            var localRepo = serviceProvider?.GetService<ILocalRepository>();
            if (localRepo != null)
            {
                try
                {
                    await localRepo.InitializeAsync();
                    System.Diagnostics.Debug.WriteLine("✓ Lokal DB initialized successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Lokal DB initialization ERROR: {ex.GetType().Name}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"  Stack: {ex.StackTrace}");
                }
            }

            // Arka plan senkronizasyonunu baslat (lokal DB → backend push)
            // AFTER DB is initialized
            var bgSync = serviceProvider?.GetService<Services.BackgroundSyncService>();
            bgSync?.StartAsync();
        };
    }
}
