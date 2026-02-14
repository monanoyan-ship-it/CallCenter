using System.Security.Cryptography;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

/// <summary>
/// Sifre politikasi servisi.
/// - Min 8 karakter, buyuk/kucuk/rakam/ozel karakter zorunlu
/// - Son 5 sifre tekrar kullanilamaz (BCrypt.Verify ile kontrol)
/// - Kriptografik olarak guvenli gecici sifre uretimi (RandomNumberGenerator)
/// </summary>
public class PasswordPolicyService : IPasswordPolicyService
{
    private readonly AppDbContext _db;

    private const int MinLength = 8;
    private const int HistoryCount = 5;
    private const int TempPasswordLength = 12;

    // Karakter setleri (benzer karakterler haric: 0/O, 1/I/l)
    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghjkmnpqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string SpecialChars = "!@#$%&*+-_";

    public PasswordPolicyService(AppDbContext db)
    {
        _db = db;
    }

    public (bool IsValid, string[] Errors) ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Şifre boş olamaz.");
            return (false, errors.ToArray());
        }

        if (password.Length < MinLength)
            errors.Add($"Şifre en az {MinLength} karakter olmalıdır.");

        if (!password.Any(char.IsUpper))
            errors.Add("Şifre en az bir büyük harf içermelidir.");

        if (!password.Any(char.IsLower))
            errors.Add("Şifre en az bir küçük harf içermelidir.");

        if (!password.Any(char.IsDigit))
            errors.Add("Şifre en az bir rakam içermelidir.");

        if (!password.Any(c => SpecialChars.Contains(c)))
            errors.Add("Şifre en az bir özel karakter içermelidir (!@#$%&*+-_).");

        return (errors.Count == 0, errors.ToArray());
    }

    public async Task<bool> IsPasswordReusedAsync(int userId, string newPassword)
    {
        // Son N sifre hash'ini al
        var recentHashes = await _db.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(HistoryCount)
            .Select(ph => ph.PasswordHash)
            .ToListAsync();

        // BCrypt.Verify ile her birini kontrol et
        return recentHashes.Any(hash => BCrypt.Net.BCrypt.Verify(newPassword, hash));
    }

    public async Task RecordPasswordAsync(int userId, string passwordHash)
    {
        _db.PasswordHistories.Add(new PasswordHistory
        {
            UserId = userId,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public string GenerateSecureTemporaryPassword()
    {
        var chars = new char[TempPasswordLength];

        // Ilk 4 karakter: her turden en az birer tane (zorunlu)
        chars[0] = PickRandom(UpperChars);
        chars[1] = PickRandom(LowerChars);
        chars[2] = PickRandom(DigitChars);
        chars[3] = PickRandom(SpecialChars);

        // Kalan karakterleri tumunden rastgele sec
        var allChars = UpperChars + LowerChars + DigitChars + SpecialChars;
        for (int i = 4; i < TempPasswordLength; i++)
            chars[i] = PickRandom(allChars);

        // Karistir (Fisher-Yates shuffle — kriptografik random ile)
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static char PickRandom(string source)
    {
        return source[RandomNumberGenerator.GetInt32(source.Length)];
    }
}
