using System.Security.Cryptography;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.Entities;

namespace CallCenter.Api.Factories;

public class PasswordPolicyFactory : IPasswordPolicyFactory
{
    private readonly IPasswordHistoryEntityService _history;
    private readonly IUnitOfWork _uow;

    private const int MinLength = 8;
    private const int HistoryCount = 5;
    private const int TempPasswordLength = 12;

    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghjkmnpqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string SpecialChars = "!@#$%&*+-_";

    public PasswordPolicyFactory(IPasswordHistoryEntityService history, IUnitOfWork uow)
    {
        _history = history;
        _uow = uow;
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
        var recentHashes = await _history.GetRecentHashesAsync(userId, HistoryCount);
        return recentHashes.Any(hash => BCrypt.Net.BCrypt.Verify(newPassword, hash));
    }

    public async Task RecordPasswordAsync(int userId, string passwordHash)
    {
        _history.Add(new PasswordHistory
        {
            UserId = userId,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        });
        await _uow.SaveChangesAsync();
    }

    public string GenerateSecureTemporaryPassword()
    {
        var chars = new char[TempPasswordLength];

        chars[0] = PickRandom(UpperChars);
        chars[1] = PickRandom(LowerChars);
        chars[2] = PickRandom(DigitChars);
        chars[3] = PickRandom(SpecialChars);

        var allChars = UpperChars + LowerChars + DigitChars + SpecialChars;
        for (int i = 4; i < TempPasswordLength; i++)
            chars[i] = PickRandom(allChars);

        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static char PickRandom(string source)
        => source[RandomNumberGenerator.GetInt32(source.Length)];
}
