using System.Text.Json;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CallCenter.Api.Services.Email;

public class MailKitEmailSendService : IEmailSendService
{
    private readonly ICustomerEmailIntegrationEntityService _integrationEs;
    private readonly IUnitOfWork _uow;
    private readonly AesEncryptionService _aes;
    private readonly GmailOAuthService _gmail;
    private readonly Office365OAuthService _o365;
    private readonly ILogger<MailKitEmailSendService> _logger;

    public MailKitEmailSendService(
        ICustomerEmailIntegrationEntityService integrationEs,
        IUnitOfWork uow,
        AesEncryptionService aes,
        GmailOAuthService gmail,
        Office365OAuthService o365,
        ILogger<MailKitEmailSendService> logger)
    {
        _integrationEs = integrationEs;
        _uow = uow;
        _aes = aes;
        _gmail = gmail;
        _o365 = o365;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken ct = default)
    {
        try
        {
            var integration = request.IntegrationId.HasValue
                ? await _integrationEs.GetByIdAsync(request.IntegrationId.Value)
                : await _integrationEs.GetDefaultAsync(request.CustomerId);

            if (integration == null)
                return new EmailSendResult { Success = false, Error = "E-posta entegrasyonu bulunamadi." };

            if (!integration.IsActive)
                return new EmailSendResult { Success = false, Error = "E-posta entegrasyonu aktif degil." };

            var credsJson = _aes.Decrypt(integration.EncryptedCredentials);
            var creds = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson) ?? new();

            var message = BuildMessage(
                integration.SenderEmail, integration.SenderName,
                request.ToAddress, request.ToName,
                request.Subject, request.HtmlBody, request.PlainTextBody, request.ReplyTo);

            var result = await SendViaProviderAsync(integration.ProviderTypeId, creds,
                integration.SenderEmail, message, integration, ct);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta gonderim hatasi. CustomerId={CustomerId}", request.CustomerId);
            return new EmailSendResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<EmailSendResult> SendWithCredentialsAsync(
        int providerTypeId,
        Dictionary<string, string> credentials,
        string senderEmail,
        string? senderName,
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        try
        {
            var message = BuildMessage(senderEmail, senderName, toAddress, null, subject, htmlBody, null, null);
            return await SendViaProviderAsync(providerTypeId, credentials, senderEmail, message, null, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test e-posta gonderim hatasi. Provider={Provider}", providerTypeId);
            return new EmailSendResult { Success = false, Error = ex.Message };
        }
    }

    private async Task<EmailSendResult> SendViaProviderAsync(
        int providerTypeId,
        Dictionary<string, string> creds,
        string senderEmail,
        MimeMessage message,
        Shared.Entities.CustomerEmailIntegration? integration,
        CancellationToken ct)
    {
        using var client = new SmtpClient();

        switch (providerTypeId)
        {
            case EmailProviders.Ids.Gmail:
            {
                var accessToken = await EnsureGmailTokenAsync(creds, integration);
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, ct);
                var oauth2 = new SaslMechanismOAuth2(senderEmail, accessToken);
                await client.AuthenticateAsync(oauth2, ct);
                break;
            }
            case EmailProviders.Ids.Office365:
            {
                var accessToken = await EnsureOffice365TokenAsync(creds, integration);
                await client.ConnectAsync("smtp.office365.com", 587, SecureSocketOptions.StartTls, ct);
                var oauth2 = new SaslMechanismOAuth2(senderEmail, accessToken);
                await client.AuthenticateAsync(oauth2, ct);
                break;
            }
            case EmailProviders.Ids.Yandex:
            {
                var email = creds.GetValueOrDefault("Email", senderEmail);
                var password = creds.GetValueOrDefault("AppPassword", "");
                await client.ConnectAsync("smtp.yandex.com", 465, SecureSocketOptions.SslOnConnect, ct);
                await client.AuthenticateAsync(email, password, ct);
                break;
            }
            case EmailProviders.Ids.SmtpGeneric:
            {
                var host = creds.GetValueOrDefault("Host", "");
                var port = int.TryParse(creds.GetValueOrDefault("Port", "587"), out var p) ? p : 587;
                var useSsl = creds.GetValueOrDefault("UseSsl", "true") == "true";
                var username = creds.GetValueOrDefault("Username", "");
                var password = creds.GetValueOrDefault("Password", "");
                var socketOptions = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                await client.ConnectAsync(host, port, socketOptions, ct);
                if (!string.IsNullOrEmpty(username))
                    await client.AuthenticateAsync(username, password, ct);
                break;
            }
            default:
                return new EmailSendResult { Success = false, Error = "Desteklenmeyen provider." };
        }

        var response = await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        return new EmailSendResult { Success = true, MessageId = response };
    }

    private async Task<string> EnsureGmailTokenAsync(
        Dictionary<string, string> creds,
        Shared.Entities.CustomerEmailIntegration? integration)
    {
        var accessToken = creds.GetValueOrDefault("AccessToken", "");
        var expiresAt = creds.GetValueOrDefault("TokenExpiresAt", "");

        if (!string.IsNullOrEmpty(expiresAt) && DateTime.TryParse(expiresAt, out var exp) && exp > DateTime.UtcNow.AddMinutes(2))
            return accessToken;

        var refreshToken = creds.GetValueOrDefault("RefreshToken", "");
        if (string.IsNullOrEmpty(refreshToken))
            return accessToken;

        var result = await _gmail.RefreshTokenAsync(refreshToken);
        if (result == null)
            return accessToken;

        creds["AccessToken"] = result.AccessToken;
        creds["TokenExpiresAt"] = DateTime.UtcNow.AddSeconds(result.ExpiresIn).ToString("O");

        if (integration != null)
        {
            integration.EncryptedCredentials = _aes.Encrypt(JsonSerializer.Serialize(creds));
            integration.UpdatedAt = DateTime.UtcNow;
            _integrationEs.Update(integration);
            await _uow.SaveChangesAsync();
        }

        return result.AccessToken;
    }

    private async Task<string> EnsureOffice365TokenAsync(
        Dictionary<string, string> creds,
        Shared.Entities.CustomerEmailIntegration? integration)
    {
        var accessToken = creds.GetValueOrDefault("AccessToken", "");
        var expiresAt = creds.GetValueOrDefault("TokenExpiresAt", "");

        if (!string.IsNullOrEmpty(expiresAt) && DateTime.TryParse(expiresAt, out var exp) && exp > DateTime.UtcNow.AddMinutes(2))
            return accessToken;

        var refreshToken = creds.GetValueOrDefault("RefreshToken", "");
        if (string.IsNullOrEmpty(refreshToken))
            return accessToken;

        var result = await _o365.RefreshTokenAsync(refreshToken);
        if (result == null)
            return accessToken;

        creds["AccessToken"] = result.AccessToken;
        creds["RefreshToken"] = result.RefreshToken;
        creds["TokenExpiresAt"] = DateTime.UtcNow.AddSeconds(result.ExpiresIn).ToString("O");

        if (integration != null)
        {
            integration.EncryptedCredentials = _aes.Encrypt(JsonSerializer.Serialize(creds));
            integration.UpdatedAt = DateTime.UtcNow;
            _integrationEs.Update(integration);
            await _uow.SaveChangesAsync();
        }

        return result.AccessToken;
    }

    private static MimeMessage BuildMessage(
        string fromEmail, string? fromName,
        string toEmail, string? toName,
        string subject, string htmlBody, string? plainTextBody, string? replyTo)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName ?? fromEmail, fromEmail));
        message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
        message.Subject = subject;

        if (!string.IsNullOrEmpty(replyTo))
            message.ReplyTo.Add(new MailboxAddress("", replyTo));

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        if (!string.IsNullOrEmpty(plainTextBody))
            builder.TextBody = plainTextBody;

        message.Body = builder.ToMessageBody();
        return message;
    }
}
