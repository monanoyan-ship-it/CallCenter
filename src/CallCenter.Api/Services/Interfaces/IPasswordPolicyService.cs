namespace CallCenter.Api.Services.Interfaces;

/// <summary>
/// Merkezi sifre politikasi servisi.
/// Sifre dogrulama, gecmis kontrolu ve guvenli gecici sifre uretimi.
/// </summary>
public interface IPasswordPolicyService
{
    /// <summary>Sifre kurallarina uygun mu?</summary>
    (bool IsValid, string[] Errors) ValidatePassword(string password);

    /// <summary>Son N sifrede ayni hash var mi? (true = tekrar, kullanilamaz)</summary>
    Task<bool> IsPasswordReusedAsync(int userId, string newPassword);

    /// <summary>Yeni sifre hash'ini gecmise kaydet.</summary>
    Task RecordPasswordAsync(int userId, string passwordHash);

    /// <summary>Kriptografik olarak guvenli gecici sifre uret.</summary>
    string GenerateSecureTemporaryPassword();
}
