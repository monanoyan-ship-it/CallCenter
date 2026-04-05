namespace CallCenter.Api.Services.Email;

/// <summary>
/// Platform seviyesi email gönderimi.
/// Kayıt onayı, şifre sıfırlama, bildirim gibi sistem emailleri için kullanılır.
/// Müşteri email entegrasyonundan (IEmailSendService) bağımsızdır.
/// info@corplynk.com üzerinden Resend SMTP ile gönderir.
/// </summary>
public interface IPlatformEmailService
{
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody);
    Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody);

    /// <summary>
    /// DB'deki email taslagindan gonderi yapar.
    /// EventKey ile taslak bulunur, placeholder'lar replace edilir.
    /// </summary>
    Task<bool> SendTemplatedAsync(string toEmail, string eventKey, Dictionary<string, string>? placeholders = null, string language = "tr");
}
