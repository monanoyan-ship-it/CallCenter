using CallCenter.Windows.LocalData.Providers;

namespace CallCenter.Windows.LocalData;

/// <summary>
/// Veritabani tipine gore dogru ILocalRepository implementasyonunu olusturur.
///
/// Kullanim:
///   var repo = LocalRepositoryFactory.Create("PostgreSQL", connectionString);
///   await repo.TestConnectionAsync();  // baglanti kontrolu
///   await repo.InitializeAsync();      // tablolari olustur
///
/// Desteklenen tipler:
///   - "PostgreSQL" → PostgresLocalRepository (Npgsql)
///   - "MSSQL"      → MssqlLocalRepository (Microsoft.Data.SqlClient)
///   - "MongoDB"    → MongoLocalRepository (MongoDB.Driver)
///   - Diger/bos    → NullLocalRepository (no-op, hata vermez)
///
/// Bu factory, ayarlar ekraninda kullanicinin sectigi DB tipine gore
/// cagrilir ve sonuc DI container'a singleton olarak kaydedilir.
/// </summary>
public static class LocalRepositoryFactory
{
    /// <summary>
    /// Verilen DB tipi ve connection string ile uygun repository olusturur.
    /// Gecersiz/bos degerler icin NullLocalRepository dondurur (graceful degradation).
    /// </summary>
    /// <param name="dbType">Veritabani tipi: "PostgreSQL", "MSSQL", "MongoDB"</param>
    /// <param name="connectionString">Baglanti dizesi</param>
    /// <returns>ILocalRepository implementasyonu</returns>
    public static ILocalRepository Create(string? dbType, string? connectionString)
    {
        // Connection string yoksa → NullRepository (DB ayarlanmamis demek)
        if (string.IsNullOrWhiteSpace(connectionString))
            return new NullLocalRepository();

        return dbType?.Trim() switch
        {
            "PostgreSQL" => new PostgresLocalRepository(connectionString),
            "MSSQL"      => new MssqlLocalRepository(connectionString),
            "MongoDB"    => new MongoLocalRepository(connectionString),
            _            => new NullLocalRepository()
        };
    }

    /// <summary>
    /// Desteklenen DB tiplerini dondurur.
    /// Ayarlar ekranindaki dropdown icin kullanilir.
    /// </summary>
    public static string[] SupportedTypes => ["PostgreSQL", "MSSQL", "MongoDB"];
}
