using System.Security.Cryptography;
using System.Text;

namespace CallCenter.Api.Services;

/// <summary>
/// AES-256-CBC ile sifreleme/cozumleme servisi.
/// SipAccount.Password gibi hassas verileri DB'de sifreli saklamak icin kullanilir.
/// IV her seferde rastgele uretilir ve ciphertext'in basina eklenir.
/// </summary>
public class AesEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key yapilandirilmamis. appsettings.json'a ekleyin.");

        // SHA256 ile 32 byte (256 bit) key turet — key uzunlugu ne olursa olsun sabit boyut
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
    }

    /// <summary>Duz metni AES-256-CBC ile sifreler. Donuş: Base64(IV + ciphertext)</summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // IV (16 byte) + ciphertext birlestirilir
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>Base64(IV + ciphertext) formatindaki sifreli metni cozer</summary>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        // Eger Base64 degilse (eski duz metin veri), oldugu gibi don
        byte[] fullBytes;
        try
        {
            fullBytes = Convert.FromBase64String(cipherText);
        }
        catch (FormatException)
        {
            // Eski sifrenmemis veri — oldugu gibi don (geriye uyumluluk)
            return cipherText;
        }

        // IV 16 byte olmali + en az 1 byte ciphertext
        if (fullBytes.Length < 17)
        {
            // Cok kisa — muhtemelen eski duz metin Base64 decode edilebilmis ama gecersiz
            return cipherText;
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[16];
        Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
        aes.IV = iv;

        var cipherBytes = new byte[fullBytes.Length - 16];
        Buffer.BlockCopy(fullBytes, 16, cipherBytes, 0, cipherBytes.Length);

        try
        {
            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException)
        {
            // Decrypt basarisiz — muhtemelen eski sifrenmemis veri veya farkli key ile sifrenmis
            return cipherText;
        }
    }
}
