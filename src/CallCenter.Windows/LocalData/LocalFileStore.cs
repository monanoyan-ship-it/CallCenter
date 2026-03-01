using System.IO;
using System.Text.Json;

namespace CallCenter.Windows.LocalData;

/// <summary>
/// Generic, thread-safe, dosya tabanli JSON depolama sinifi.
/// %LOCALAPPDATA%\CallCenter\Data\ altinda JSON dosyalari tutar.
/// Mevcut ContactService ve SecureStorage ile ayni pattern.
/// </summary>
public class LocalFileStore<T> where T : class
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<T>? _cache;

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

    private async Task EnsureLoadedAsync()
    {
        if (_cache != null) return;

        if (File.Exists(_filePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_filePath);
                _cache = JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
            }
            catch
            {
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
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_cache, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalFileStore] Kayit hatasi ({_filePath}): {ex.Message}");
        }
    }
}
