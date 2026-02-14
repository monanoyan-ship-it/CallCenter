using System.Security.Cryptography;

namespace CallCenter.Shared.Services;

/// <summary>
/// Stream bazli AES-256-CBC dosya sifreleme servisi.
/// Ses kayitlarini (WAV) diskte sifreli (.enc) olarak saklamak icin.
/// Dosya formati: [16 byte IV][AES-CBC ciphertext]
/// </summary>
public static class FileEncryptionService
{
    private const int BufferSize = 81920; // 80KB buffer

    /// <summary>
    /// Dosyayi AES-256-CBC ile sifreler.
    /// Kaynak dosya degistirilmez, sifreli icerik destPath'e yazilir.
    /// </summary>
    public static async Task EncryptFileAsync(string sourcePath, string destPath, byte[] key)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePath);
        ArgumentException.ThrowIfNullOrEmpty(destPath);
        if (key.Length != 32)
            throw new ArgumentException("AES-256 icin 32 byte key gerekli.", nameof(key));

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        // Hedef dizin yoksa olustur
        var dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await using var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);

        // Ilk 16 byte: IV
        await destStream.WriteAsync(aes.IV);

        // Geri kalan: sifrelenmis icerik
        using var encryptor = aes.CreateEncryptor();
        await using var cryptoStream = new CryptoStream(destStream, encryptor, CryptoStreamMode.Write);
        await using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);

        await sourceStream.CopyToAsync(cryptoStream, BufferSize);
    }

    /// <summary>
    /// Sifreli dosyayi cozer, sonucu destPath'e yazar.
    /// </summary>
    public static async Task DecryptFileAsync(string sourcePath, string destPath, byte[] key)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePath);
        ArgumentException.ThrowIfNullOrEmpty(destPath);
        if (key.Length != 32)
            throw new ArgumentException("AES-256 icin 32 byte key gerekli.", nameof(key));

        await using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);

        // Ilk 16 byte: IV
        var iv = new byte[16];
        var bytesRead = await sourceStream.ReadAsync(iv);
        if (bytesRead != 16)
            throw new InvalidDataException("Sifreli dosya IV okunamadi.");

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Hedef dizin yoksa olustur
        var dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var decryptor = aes.CreateDecryptor();
        await using var cryptoStream = new CryptoStream(sourceStream, decryptor, CryptoStreamMode.Read);
        await using var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);

        await cryptoStream.CopyToAsync(destStream, BufferSize);
    }

    /// <summary>
    /// Sifreli dosyayi MemoryStream'e cozer (playback icin).
    /// </summary>
    public static async Task<MemoryStream> DecryptToStreamAsync(string sourcePath, byte[] key)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourcePath);
        if (key.Length != 32)
            throw new ArgumentException("AES-256 icin 32 byte key gerekli.", nameof(key));

        await using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);

        var iv = new byte[16];
        var bytesRead = await sourceStream.ReadAsync(iv);
        if (bytesRead != 16)
            throw new InvalidDataException("Sifreli dosya IV okunamadi.");

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        await using var cryptoStream = new CryptoStream(sourceStream, decryptor, CryptoStreamMode.Read);

        var result = new MemoryStream();
        await cryptoStream.CopyToAsync(result, BufferSize);
        result.Position = 0;
        return result;
    }

    /// <summary>
    /// Dosyanin SHA-256 hash'ini hesaplar (butunluk dogrulamasi).
    /// </summary>
    public static async Task<string> ComputeFileHashAsync(string filePath)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, true);
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Config string'inden 32-byte AES key turetir (SHA256).
    /// AesEncryptionService ile ayni turetim yontemi.
    /// </summary>
    public static byte[] DeriveKey(string keyString)
    {
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(keyString));
    }
}
