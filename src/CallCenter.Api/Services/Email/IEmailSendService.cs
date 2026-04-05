using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services.Email;

/// <summary>
/// Moduler e-posta gonderim servisi.
/// Kampanya, bildirim, fatura, randevu hatirlatma gibi
/// her yerden cagirilabilir.
/// </summary>
public interface IEmailSendService
{
    /// <summary>Musterinin kayitli email entegrasyonuyla mail gonderir.</summary>
    Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken ct = default);

    /// <summary>Belirtilen credential'larla direkt gonderim (test icin).</summary>
    Task<EmailSendResult> SendWithCredentialsAsync(
        int providerTypeId,
        Dictionary<string, string> credentials,
        string senderEmail,
        string? senderName,
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken ct = default);
}
