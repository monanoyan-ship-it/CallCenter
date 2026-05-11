using System.IO;
using System.Threading;

namespace CallCenter.Windows.Services;

/// <summary>
/// Windows uygulamasinda auth oturumu gecersiz kaldiginda UI ve servislerin tek yerden temizlenmesini tetikler.
/// </summary>
public sealed class WindowsSessionEvents
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CorpLynk",
        "session-debug.log");
    private static readonly object LogLock = new();
    private int _invalidating;

    public bool IsInvalidated { get; private set; }

    public event Func<string, Task>? SessionInvalidated;

    public void MarkSessionActive()
    {
        IsInvalidated = false;
        Interlocked.Exchange(ref _invalidating, 0);
        Log("Session marked active.");
    }

    public void MarkSessionEnded()
    {
        IsInvalidated = true;
        Interlocked.Exchange(ref _invalidating, 0);
        Log("Session marked ended.");
    }

    public async Task InvalidateAsync(string reason)
    {
        if (IsInvalidated || Interlocked.Exchange(ref _invalidating, 1) == 1)
            return;

        IsInvalidated = true;
        Log("Session invalidated: " + reason);

        var handlers = SessionInvalidated?
            .GetInvocationList()
            .Cast<Func<string, Task>>()
            .ToArray();

        if (handlers == null || handlers.Length == 0)
        {
            Interlocked.Exchange(ref _invalidating, 0);
            return;
        }

        foreach (var handler in handlers)
            await handler(reason);

        Interlocked.Exchange(ref _invalidating, 0);
    }

    private static void Log(string message)
    {
        try
        {
            lock (LogLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Session logging is best effort.
        }
    }
}
