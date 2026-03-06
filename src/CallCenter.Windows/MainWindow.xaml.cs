using System.IO;
using System.Net.Http;
using System.Windows;
using CallCenter.Windows.LocalData;
using CallCenter.Windows.LocalData.Entities;
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
#if DEBUG
            .AddJsonFile("wwwroot/appsettings.Development.json", optional: true, reloadOnChange: true)
#endif
            .Build();
        services.AddSingleton<IConfiguration>(config);

        // Secure Storage (localStorage yerine dosya tabanli)
        var secureStorageInstance = new Services.SecureStorage();
        services.AddSingleton(secureStorageInstance);

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
        services.AddSingleton<Services.IncomingCallPipelineService>();

        // Contacts (API-first + lokal buffer)
        services.AddSingleton<Services.ContactService>(sp =>
            new Services.ContactService(
                sp.GetRequiredService<HttpClient>(),
                sp.GetRequiredService<ILocalRepository>()));

        // Hotkeys (global kisayollar)
        _hotkeyService = new Services.HotkeyService();
        services.AddSingleton(_hotkeyService);

        // System Tray
        _trayService = new Services.SystemTrayService();
        services.AddSingleton(_trayService);

        // ── Lokal Dosya Deposu (temp buffer) + Cift Yazim ──
        services.AddLogging();

        var localDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CallCenter", "Data");

        services.AddSingleton<ILocalRepository>(sp =>
        {
            return new FileLocalRepository(localDataPath, machineId: null);
        });
        services.AddSingleton(sp =>
        {
            return new LocalFileStore<LocalSipAccount>(localDataPath, "sip-accounts.json");
        });
        services.AddSingleton<Services.CallSyncService>();
        services.AddSingleton<Services.RecordingUploadService>();
        services.AddSingleton<Services.BackgroundSyncService>();
        services.AddSingleton<Services.LocalReportService>();

        blazorWebView.Services = services.BuildServiceProvider();

        var serviceProvider = blazorWebView.Services as ServiceProvider;

        // System Tray, Hotkeys ve BackgroundSync'i pencere yuklendikten sonra baslat
        Loaded += async (s, e) =>
        {
            _trayService.Initialize(this);
            _hotkeyService.Initialize(this);

            // Lokal DB tablolarini initialize et
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
            var bgSync = serviceProvider?.GetService<Services.BackgroundSyncService>();
            bgSync?.StartAsync();
        };
    }
}
