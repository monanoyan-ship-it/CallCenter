using CallCenter.Windows.LocalData.Providers;

namespace CallCenter.Windows.LocalData;

/// <summary>
/// Dosya tabanli ILocalRepository olusturur.
/// basePath: JSON dosyalarinin saklanacagi klasor yolu.
/// Bos/gecersiz yol icin NullLocalRepository dondurur (graceful degradation).
/// </summary>
public static class LocalRepositoryFactory
{
    /// <summary>
    /// Verilen klasor yolunda FileLocalRepository olusturur.
    /// </summary>
    /// <param name="basePath">JSON dosyalarinin saklanacagi klasor (ornek: %LOCALAPPDATA%\CallCenter\Data)</param>
    /// <param name="machineId">Ag modunda PC basina ayri call-records dosyasi icin hostname. Null = lokal mod.</param>
    /// <returns>ILocalRepository implementasyonu</returns>
    public static ILocalRepository Create(string? basePath, string? machineId = null)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            return new NullLocalRepository();

        return new FileLocalRepository(basePath, machineId);
    }
}
