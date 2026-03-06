using System.Drawing;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Toolkit.Uwp.Notifications;

namespace CallCenter.Windows.Services;

/// <summary>
/// System Tray ikonu ve Windows toast bildirimleri yonetir.
/// </summary>
public class SystemTrayService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private Window? _mainWindow;

    /// <summary>
    /// System tray ikonunu olusturur ve gosterir.
    /// </summary>
    public void Initialize(Window mainWindow)
    {
        _mainWindow = mainWindow;

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "CorpLynk",
            Visibility = Visibility.Visible
        };

        // Kod ile varsayilan ikon olustur (embedded resource gerektirmez)
        _trayIcon.Icon = SystemIcons.Application;

        // Sag tik menusu
        var contextMenu = new System.Windows.Controls.ContextMenu();

        var showItem = new System.Windows.Controls.MenuItem { Header = "Pencereyi Goster" };
        showItem.Click += (s, e) => ShowWindow();
        contextMenu.Items.Add(showItem);

        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Cikis" };
        exitItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu = contextMenu;

        // Cift tikla pencereyi goster
        _trayIcon.TrayMouseDoubleClick += (s, e) => ShowWindow();

        // Pencere kapatildiginda tray'e kucult
        _mainWindow.Closing += OnMainWindowClosing;
    }

    /// <summary>
    /// Gelen arama icin Windows toast bildirimi gosterir.
    /// </summary>
    public void ShowIncomingCallNotification(string callerNumber, string? callerName = null)
    {
        var displayName = !string.IsNullOrEmpty(callerName) ? callerName : callerNumber;

        new ToastContentBuilder()
            .AddText("Gelen Arama")
            .AddText($"{displayName} arıyor...")
            .SetToastScenario(ToastScenario.IncomingCall)
            .Show();

        // Tray balonunu da goster
        _trayIcon?.ShowBalloonTip(
            "Gelen Arama",
            $"{displayName} arıyor...",
            BalloonIcon.Info);
    }

    /// <summary>
    /// Genel bildirim gosterir.
    /// </summary>
    public void ShowNotification(string title, string message)
    {
        new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .Show();
    }

    /// <summary>
    /// Pencereyi on plana getirir (Blazor componentlerinden cagirilabilir).
    /// </summary>
    public void BringToFront()
    {
        if (_mainWindow == null) return;

        // WPF UI thread'inde calistirilmali
        if (!_mainWindow.Dispatcher.CheckAccess())
        {
            _mainWindow.Dispatcher.Invoke(BringToFront);
            return;
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
        _mainWindow.Topmost = true;
        _mainWindow.Topmost = false;
        _mainWindow.Focus();
    }

    private void ShowWindow()
    {
        BringToFront();
    }

    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Pencereyi kapatmak yerine tray'e kucult
        e.Cancel = true;
        _mainWindow?.Hide();

        _trayIcon?.ShowBalloonTip(
            "CorpLynk",
            "Uygulama arka planda calismaya devam ediyor.",
            BalloonIcon.Info);
    }

    private void ExitApplication()
    {
        // Gercekten cik
        if (_mainWindow != null)
        {
            _mainWindow.Closing -= OnMainWindowClosing;
        }

        Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        // Windows toast temizligi
        ToastNotificationManagerCompat.Uninstall();
    }
}
