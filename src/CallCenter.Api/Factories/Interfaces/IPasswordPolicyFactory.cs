namespace CallCenter.Api.Factories.Interfaces;

public interface IPasswordPolicyFactory
{
    (bool IsValid, string[] Errors) ValidatePassword(string password);
    Task<bool> IsPasswordReusedAsync(int userId, string newPassword);
    Task RecordPasswordAsync(int userId, string passwordHash);
    string GenerateSecureTemporaryPassword();
}
