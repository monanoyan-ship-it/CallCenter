using CallCenter.Api.EntityServices.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CallCenter.Api.Services.Email;

public class ResendPlatformEmailService : IPlatformEmailService
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ResendPlatformEmailService> _logger;

    public ResendPlatformEmailService(
        IConfiguration config,
        IServiceProvider serviceProvider,
        ILogger<ResendPlatformEmailService> logger)
    {
        _config = config;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task<bool> SendAsync(string toEmail, string subject, string htmlBody)
        => SendAsync(toEmail, toEmail, subject, htmlBody);

    public async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var host = _config["PlatformEmail:Host"] ?? "smtp.resend.com";
            var port = int.TryParse(_config["PlatformEmail:Port"], out var p) ? p : 587;
            var username = _config["PlatformEmail:Username"] ?? "resend";
            var password = _config["PlatformEmail:Password"] ?? "";
            var fromEmail = _config["PlatformEmail:FromEmail"] ?? "info@corplynk.com";
            var fromName = _config["PlatformEmail:FromName"] ?? "CorpLynk";
            var useSsl = _config["PlatformEmail:UseSsl"] == "true";

            if (string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("PlatformEmail:Password yapilandirilmamis. Email gonderilemedi.");
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            var socketOptions = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await client.ConnectAsync(host, port, socketOptions);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Platform email gonderildi. To={To}, Subject={Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Platform email gonderim hatasi. To={To}, Subject={Subject}", toEmail, subject);
            return false;
        }
    }

    public async Task<bool> SendTemplatedAsync(string toEmail, string eventKey, Dictionary<string, string>? placeholders = null, string language = "tr")
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var templateEs = scope.ServiceProvider.GetRequiredService<IPlatformEmailTemplateEntityService>();

            var template = await templateEs.GetByEventKeyAsync(eventKey, language);
            if (template == null)
            {
                _logger.LogWarning("Email taslagi bulunamadi. EventKey={EventKey}, Language={Language}", eventKey, language);
                return false;
            }

            var subject = ReplacePlaceholders(template.Subject, placeholders);
            var htmlBody = ReplacePlaceholders(template.HtmlBody, placeholders);

            return await SendAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Templated email gonderim hatasi. EventKey={EventKey}, To={To}", eventKey, toEmail);
            return false;
        }
    }

    private static string ReplacePlaceholders(string text, Dictionary<string, string>? placeholders)
    {
        if (placeholders == null || placeholders.Count == 0)
            return text;

        foreach (var (key, value) in placeholders)
            text = text.Replace($"{{{{{key}}}}}", value);

        return text;
    }
}
