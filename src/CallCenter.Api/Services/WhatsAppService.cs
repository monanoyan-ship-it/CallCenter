using System.Text;
using System.Text.Json;
using CallCenter.Data;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

/// <summary>
/// Meta WhatsApp Business API entegrasyonu
/// </summary>
public interface IWhatsAppService
{
    Task<bool> SendTemplateMessageAsync(int customerId, string phoneNumber, string templateName, string[] parameters, int? clientId = null);
    Task<bool> SendTextMessageAsync(int customerId, string phoneNumber, string message, int? clientId = null);
    Task SendAppointmentReminderAsync(int customerId, string clientName, string clientPhone, string serviceName, DateTime appointmentTime, int? clientId = null);
    Task SendAppointmentConfirmationAsync(int customerId, string clientName, string clientPhone, string serviceName, DateTime appointmentTime, int? clientId = null);
}

public class WhatsAppService : IWhatsAppService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<WhatsAppService> _logger;

    private const string GraphApiUrl = "https://graph.facebook.com/v21.0";

    public WhatsAppService(AppDbContext db, IHttpClientFactory httpFactory, ILogger<WhatsAppService> logger)
    {
        _db = db;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<bool> SendTemplateMessageAsync(int customerId, string phoneNumber, string templateName, string[] parameters, int? clientId = null)
    {
        var config = await GetConfigAsync(customerId);
        if (config == null) return false;

        var phone = NormalizePhone(phoneNumber);
        var body = new
        {
            messaging_product = "whatsapp",
            to = phone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = "tr" },
                components = parameters.Length > 0 ? new[]
                {
                    new
                    {
                        type = "body",
                        parameters = parameters.Select(p => new { type = "text", text = p }).ToArray()
                    }
                } : Array.Empty<object>()
            }
        };

        return await SendRequestAsync(config, phone, templateName, JsonSerializer.Serialize(body), body, clientId);
    }

    public async Task<bool> SendTextMessageAsync(int customerId, string phoneNumber, string message, int? clientId = null)
    {
        var config = await GetConfigAsync(customerId);
        if (config == null) return false;

        var phone = NormalizePhone(phoneNumber);
        var body = new
        {
            messaging_product = "whatsapp",
            to = phone,
            type = "text",
            text = new { body = message }
        };

        return await SendRequestAsync(config, phone, null, message, body, clientId);
    }

    public async Task SendAppointmentReminderAsync(int customerId, string clientName, string clientPhone, string serviceName, DateTime appointmentTime, int? clientId = null)
    {
        var config = await GetConfigAsync(customerId);
        if (config == null || !config.SendAppointmentReminder) return;

        var message = $"Merhaba {clientName}, yarin saat {appointmentTime:HH:mm}'da {serviceName} randevunuz bulunmaktadir. Sizi bekliyoruz!";
        await SendTextMessageAsync(customerId, clientPhone, message, clientId);
    }

    public async Task SendAppointmentConfirmationAsync(int customerId, string clientName, string clientPhone, string serviceName, DateTime appointmentTime, int? clientId = null)
    {
        var config = await GetConfigAsync(customerId);
        if (config == null || !config.SendAppointmentConfirmation) return;

        var message = $"Merhaba {clientName}, {appointmentTime:dd.MM.yyyy} tarihinde saat {appointmentTime:HH:mm}'da {serviceName} randevunuz olusturulmustur. Iyi gunler!";
        await SendTextMessageAsync(customerId, clientPhone, message, clientId);
    }

    private async Task<SlnWhatsAppConfig?> GetConfigAsync(int customerId)
    {
        return await _db.SlnWhatsAppConfigs.FirstOrDefaultAsync(c => c.CustomerId == customerId && c.IsActive);
    }

    private async Task<bool> SendRequestAsync(SlnWhatsAppConfig config, string phone, string? templateName, string messageBody, object requestBody, int? clientId)
    {
        var log = new SlnWhatsAppMessage
        {
            CustomerId = config.CustomerId,
            DirectionId = 1,
            PhoneNumber = phone,
            TemplateName = templateName,
            MessageBody = messageBody,
            SlnClientId = clientId,
            StatusId = 1
        };

        try
        {
            var client = _httpFactory.CreateClient();
            var json = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{GraphApiUrl}/{config.PhoneNumberId}/messages");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AccessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                log.StatusId = 1; // Gonderildi
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("messages", out var msgs) && msgs.GetArrayLength() > 0)
                        log.WhatsAppMessageId = msgs[0].GetProperty("id").GetString();
                }
                catch { }

                _logger.LogInformation("WhatsApp mesaj gonderildi: {Phone}", phone);
            }
            else
            {
                log.StatusId = 4; // Basarisiz
                log.ErrorMessage = responseBody;
                _logger.LogWarning("WhatsApp mesaj gonderilemedi: {Phone} - {Error}", phone, responseBody);
            }

            _db.SlnWhatsAppMessages.Add(log);
            await _db.SaveChangesAsync();
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            log.StatusId = 4;
            log.ErrorMessage = ex.Message;
            _db.SlnWhatsAppMessages.Add(log);
            await _db.SaveChangesAsync();
            _logger.LogError(ex, "WhatsApp mesaj gonderim hatasi: {Phone}", phone);
            return false;
        }
    }

    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = "90" + digits[1..];
        if (!digits.StartsWith("90")) digits = "90" + digits;
        return digits;
    }
}
