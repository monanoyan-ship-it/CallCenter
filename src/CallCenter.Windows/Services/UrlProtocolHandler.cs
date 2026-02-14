using Microsoft.Win32;

namespace CallCenter.Windows.Services;

/// <summary>
/// callto:// ve tel:// URL protocol handler.
/// Windows Registry'ye protokol kaydı yaparak browser'dan gelen
/// callto:+905551234567 veya tel:+905551234567 tiklamalarini yakalar
/// ve Windows client'ta arama baslatir.
/// </summary>
public static class UrlProtocolHandler
{
    private const string CalltoProtocol = "callto";
    private const string TelProtocol = "tel";
    private const string SipProtocol = "sip";

    /// <summary>
    /// callto://, tel://, sip:// protokollerini Windows Registry'ye kaydeder.
    /// Yonetici yetkisi gerektirebilir (HKCU altinda admin gerekmez).
    /// </summary>
    public static void RegisterProtocols(string executablePath)
    {
        try
        {
            RegisterProtocol(CalltoProtocol, "Call Center - Telefon Aramasi", executablePath);
            RegisterProtocol(TelProtocol, "Call Center - Telefon Aramasi", executablePath);
            RegisterProtocol(SipProtocol, "Call Center - SIP Aramasi", executablePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Protocol registration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Tek bir protokolu Registry'ye kaydeder.
    /// HKEY_CURRENT_USER\Software\Classes altinda olusturulur (admin gerektirmez).
    /// </summary>
    private static void RegisterProtocol(string protocol, string description, string executablePath)
    {
        var keyPath = $@"Software\Classes\{protocol}";

        using var key = Registry.CurrentUser.CreateSubKey(keyPath);
        if (key == null) return;

        key.SetValue("", $"URL:{description}");
        key.SetValue("URL Protocol", "");

        // Default Icon
        using var iconKey = key.CreateSubKey("DefaultIcon");
        iconKey?.SetValue("", $"\"{executablePath}\",0");

        // Shell\Open\Command
        using var cmdKey = key.CreateSubKey(@"shell\open\command");
        cmdKey?.SetValue("", $"\"{executablePath}\" \"%1\"");
    }

    /// <summary>
    /// Protokollerin zaten kayitli olup olmadigini kontrol eder.
    /// </summary>
    public static bool IsRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{CalltoProtocol}");
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Protokol kayitlarini siler.
    /// </summary>
    public static void UnregisterProtocols()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{CalltoProtocol}", false);
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{TelProtocol}", false);
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{SipProtocol}", false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Protocol unregistration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Komut satiri argümanından telefon numarasini cikarir.
    /// callto:+905551234567, tel:+905551234567, sip:1001@sip.example.com
    /// </summary>
    public static string? ParsePhoneNumber(string[] args)
    {
        if (args.Length == 0) return null;

        var arg = args[0].Trim();

        // callto: prefix
        if (arg.StartsWith("callto:", StringComparison.OrdinalIgnoreCase))
        {
            return CleanNumber(arg["callto:".Length..]);
        }

        // tel: prefix
        if (arg.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            return CleanNumber(arg["tel:".Length..]);
        }

        // sip: prefix (URI olarak birak)
        if (arg.StartsWith("sip:", StringComparison.OrdinalIgnoreCase))
        {
            return arg; // SIP URI olarak dondur
        }

        return null;
    }

    /// <summary>
    /// Telefon numarasini temizler (URL encoding, bosluklar, vs.).
    /// </summary>
    private static string CleanNumber(string raw)
    {
        // URL decode
        raw = Uri.UnescapeDataString(raw);

        // // prefix varsa kaldir
        if (raw.StartsWith("//"))
            raw = raw[2..];

        // Bosluk ve ozel karakterleri temizle (+ ve rakamlar kalsin)
        var cleaned = new System.Text.StringBuilder();
        foreach (var c in raw)
        {
            if (char.IsDigit(c) || c == '+' || c == '*' || c == '#')
                cleaned.Append(c);
        }

        return cleaned.ToString();
    }
}
