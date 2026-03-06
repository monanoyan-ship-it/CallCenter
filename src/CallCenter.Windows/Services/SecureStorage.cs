using System.IO;
using System.Text.Json;

namespace CallCenter.Windows.Services;

/// <summary>
/// Windows icin guvenli depolama servisi.
/// Token ve ayarlari %LOCALAPPDATA%\CorpLynk\ klasorunde JSON dosyasinda saklar.
/// </summary>
public class SecureStorage
{
    private readonly string _storagePath;
    private readonly string _filePath;
    private Dictionary<string, string> _cache = new();
    private readonly object _lock = new();

    public SecureStorage()
    {
        _storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CorpLynk");

        _filePath = Path.Combine(_storagePath, "storage.json");

        // Klasoru olustur
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }

        // Mevcut verileri oku
        LoadFromDisk();
    }

    public Task<string?> GetAsync(string key)
    {
        lock (_lock)
        {
            return Task.FromResult(_cache.TryGetValue(key, out var value) ? value : null);
        }
    }

    public Task SetAsync(string key, string value)
    {
        lock (_lock)
        {
            _cache[key] = value;
            SaveToDisk();
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        lock (_lock)
        {
            _cache.Remove(key);
            SaveToDisk();
        }
        return Task.CompletedTask;
    }

    private void LoadFromDisk()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
        }
        catch
        {
            _cache = new();
        }
    }

    private void SaveToDisk()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // I/O hatasi — sessizce devam et
        }
    }
}
