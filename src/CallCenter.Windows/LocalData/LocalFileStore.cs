using System.IO;
using System.Text.Json;

namespace CallCenter.Windows.LocalData;

/// <summary>
/// Generic, thread-safe, dosya tabanli JSON depolama sinifi.
/// %LOCALAPPDATA%\CallCenter\Data\ altinda JSON dosyalari tutar.
/// Mevcut CrmContactService ve SecureStorage ile ayni pattern.
/// </summary>
public class LocalFileStore<T> where T : class
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<T>? _cache;
    private DateTime _lastReadWriteTime = DateTime.MinValue;

    private const int WriteRetryCount = 3;
    private const int WriteRetryDelayMs = 200;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public LocalFileStore(string basePath, string fileName)
    {
        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        _filePath = Path.Combine(basePath, fileName);
    }

    /// <summary>Dosya yolunu dondurur (UI gosterim icin)</summary>
    public string FilePath => _filePath;

    /// <summary>Dosya boyutunu byte olarak dondurur (yoksa 0)</summary>
    public long GetFileSize()
    {
        return File.Exists(_filePath) ? new FileInfo(_filePath).Length : 0;
    }

    /// <summary>Tum kayitlari getir</summary>
    public async Task<List<T>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return new List<T>(_cache!);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Kosula uyan ilk kaydi getir</summary>
    public async Task<T?> FindAsync(Func<T, bool> predicate)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return _cache!.FirstOrDefault(predicate);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Kosula uyan tum kayitlari getir</summary>
    public async Task<List<T>> WhereAsync(Func<T, bool> predicate)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return _cache!.Where(predicate).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Yeni kayit ekle</summary>
    public async Task AddAsync(T item)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            _cache!.Add(item);
            await SaveToDiskAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Kosula uyan ilk kaydi guncelle</summary>
    public async Task<bool> UpdateAsync(Func<T, bool> predicate, Action<T> action)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            var item = _cache!.FirstOrDefault(predicate);
            if (item == null) return false;

            action(item);
            await SaveToDiskAsync();
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Kosula uyan ilk kaydi sil</summary>
    public async Task<bool> RemoveAsync(Func<T, bool> predicate)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            var item = _cache!.FirstOrDefault(predicate);
            if (item == null) return false;

            _cache!.Remove(item);
            await SaveToDiskAsync();
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Kosula uyan tum kayitlari sil</summary>
    public async Task<int> RemoveAllAsync(Func<T, bool> predicate)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            var count = _cache!.RemoveAll(new Predicate<T>(predicate));
            if (count > 0)
                await SaveToDiskAsync();
            return count;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Kayit sayisi (opsiyonel filtre)</summary>
    public async Task<int> CountAsync(Func<T, bool>? predicate = null)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return predicate == null ? _cache!.Count : _cache!.Count(predicate);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Tum listeyi degistir (toplu yazma)</summary>
    public async Task SaveAllAsync(List<T> items)
    {
        await _lock.WaitAsync();
        try
        {
            _cache = new List<T>(items);
            await SaveToDiskAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Tum verileri sil</summary>
    public async Task ClearAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _cache = new List<T>();
            await SaveToDiskAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Cache'i sifirla — FileChangeWatcher disaridan degisiklik tespit ettiginde cagirir.
    /// Sonraki okuma dosyayi diskten tekrar yukler.
    /// </summary>
    public void InvalidateCache()
    {
        // lock almadan sadece null'a set — sonraki EnsureLoadedAsync yeniden yukler
        _cache = null;
        _lastReadWriteTime = DateTime.MinValue;
    }

    private async Task EnsureLoadedAsync()
    {
        // Cache varsa, dosyanin disaridan degisip degismedigini kontrol et
        if (_cache != null)
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var currentWriteTime = File.GetLastWriteTimeUtc(_filePath);
                    if (currentWriteTime > _lastReadWriteTime)
                    {
                        // Dosya disaridan degismis, cache'i yenile
                        _cache = null;
                    }
                    else
                    {
                        return; // Cache guncel
                    }
                }
                else
                {
                    return; // Dosya yok, cache'teki veri gecerli
                }
            }
            catch
            {
                return; // Hata durumunda mevcut cache ile devam
            }
        }

        if (File.Exists(_filePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_filePath);
                _cache = JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
                _lastReadWriteTime = File.GetLastWriteTimeUtc(_filePath);
            }
            catch
            {
                if (_cache == null)
                    _cache = new List<T>();
            }
        }
        else
        {
            _cache = new List<T>();
        }
    }

    private async Task SaveToDiskAsync()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_cache, JsonOptions);
        var tmpPath = _filePath + ".tmp";

        for (int attempt = 1; attempt <= WriteRetryCount; attempt++)
        {
            try
            {
                // Atomic write: once .tmp'ye yaz, sonra rename
                await File.WriteAllTextAsync(tmpPath, json);
                File.Move(tmpPath, _filePath, overwrite: true);
                _lastReadWriteTime = File.GetLastWriteTimeUtc(_filePath);
                return;
            }
            catch (IOException) when (attempt < WriteRetryCount)
            {
                await Task.Delay(WriteRetryDelayMs * attempt);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalFileStore] Kayit hatasi ({_filePath}, deneme {attempt}): {ex.Message}");
                if (attempt == WriteRetryCount)
                {
                    // Son deneme: dogrudan yazmayi dene (fallback)
                    try
                    {
                        await File.WriteAllTextAsync(_filePath, json);
                        _lastReadWriteTime = File.GetLastWriteTimeUtc(_filePath);
                    }
                    catch (Exception fallbackEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LocalFileStore] Fallback yazma da basarisiz ({_filePath}): {fallbackEx.Message}");
                    }
                }
            }
        }
    }
}
