using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace CallCenter.Windows.Services;

/// <summary>
/// Global klavye kisayollari servisi — MicroSIP benzeri.
/// Uygulama arka plandayken de calisir.
/// </summary>
public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    // Hotkey ID'leri
    public const int HK_ANSWER = 1;
    public const int HK_HANGUP = 2;
    public const int HK_HOLD = 3;
    public const int HK_MUTE = 4;
    public const int HK_DND = 5;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Modifier key constants
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CTRL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_NOREPEAT = 0x4000;

    // Virtual key codes
    private const uint VK_F1 = 0x70;
    private const uint VK_F2 = 0x71;
    private const uint VK_F3 = 0x72;
    private const uint VK_F4 = 0x73;
    private const uint VK_F5 = 0x74;

    private IntPtr _windowHandle;
    private HwndSource? _source;

    // Events
    public event Action? OnAnswerHotkey;
    public event Action? OnHangupHotkey;
    public event Action? OnHoldHotkey;
    public event Action? OnMuteHotkey;
    public event Action? OnDndHotkey;

    /// <summary>
    /// WPF penceresi yuklendiginde cagrilir.
    /// </summary>
    public void Initialize(System.Windows.Window window)
    {
        var helper = new WindowInteropHelper(window);
        _windowHandle = helper.Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(WndProc);

        // Varsayilan kisayollar: Ctrl+F1..F5
        RegisterHotKey(_windowHandle, HK_ANSWER, MOD_CTRL | MOD_NOREPEAT, VK_F1); // Ctrl+F1 = Cevapla
        RegisterHotKey(_windowHandle, HK_HANGUP, MOD_CTRL | MOD_NOREPEAT, VK_F2); // Ctrl+F2 = Kapat
        RegisterHotKey(_windowHandle, HK_HOLD, MOD_CTRL | MOD_NOREPEAT, VK_F3);   // Ctrl+F3 = Beklet
        RegisterHotKey(_windowHandle, HK_MUTE, MOD_CTRL | MOD_NOREPEAT, VK_F4);   // Ctrl+F4 = Sustur
        RegisterHotKey(_windowHandle, HK_DND, MOD_CTRL | MOD_NOREPEAT, VK_F5);    // Ctrl+F5 = DND
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            switch (id)
            {
                case HK_ANSWER: OnAnswerHotkey?.Invoke(); break;
                case HK_HANGUP: OnHangupHotkey?.Invoke(); break;
                case HK_HOLD: OnHoldHotkey?.Invoke(); break;
                case HK_MUTE: OnMuteHotkey?.Invoke(); break;
                case HK_DND: OnDndHotkey?.Invoke(); break;
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, HK_ANSWER);
            UnregisterHotKey(_windowHandle, HK_HANGUP);
            UnregisterHotKey(_windowHandle, HK_HOLD);
            UnregisterHotKey(_windowHandle, HK_MUTE);
            UnregisterHotKey(_windowHandle, HK_DND);
        }
        _source?.RemoveHook(WndProc);
    }
}
