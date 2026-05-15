using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class SlnWhatsAppFactory : ISlnWhatsAppFactory
{
    private readonly ISlnWhatsAppConfigEntityService _configEs;
    private readonly ISlnWhatsAppMessageEntityService _messageEs;
    private readonly ISlnClientEntityService _clients;
    private readonly IWhatsAppService _whatsApp;
    private readonly IUnitOfWork _uow;

    public SlnWhatsAppFactory(ISlnWhatsAppConfigEntityService configEs, ISlnWhatsAppMessageEntityService messageEs, ISlnClientEntityService clients, IWhatsAppService whatsApp, IUnitOfWork uow)
    {
        _configEs = configEs;
        _messageEs = messageEs;
        _clients = clients;
        _whatsApp = whatsApp;
        _uow = uow;
    }

    public async Task<SlnWhatsAppConfig> GetConfigAsync(int customerId)
    {
        var config = await _configEs.GetAllQueryable().FirstOrDefaultAsync(c => c.CustomerId == customerId);
        return config ?? new SlnWhatsAppConfig();
    }

    public async Task SaveConfigAsync(int customerId, SlnWhatsAppConfig dto)
    {
        var config = await _configEs.GetAllQueryable().FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (config == null)
        {
            config = new SlnWhatsAppConfig { CustomerId = customerId };
            _configEs.Add(config);
        }

        config.BusinessAccountId = dto.BusinessAccountId;
        config.PhoneNumberId = dto.PhoneNumberId;
        config.AccessToken = dto.AccessToken;
        config.WebhookVerifyToken = dto.WebhookVerifyToken;
        config.IsActive = dto.IsActive;
        config.SendAppointmentReminder = dto.SendAppointmentReminder;
        config.SendAppointmentConfirmation = dto.SendAppointmentConfirmation;
        config.SendBirthdayGreeting = dto.SendBirthdayGreeting;
        config.ReminderHoursBefore = dto.ReminderHoursBefore;

        await _uow.SaveChangesAsync();
    }

    public async Task<object> GetMessagesAsync(int customerId, int page, int pageSize, int? branchId = null)
    {
        return await SalonBranchScope.ApplyToWhatsAppMessages(
                _messageEs.GetAllQueryable().Where(m => m.CustomerId == customerId),
                branchId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new
            {
                m.Id, m.DirectionId, m.PhoneNumber, m.TemplateName, m.MessageBody,
                m.StatusId, m.ErrorMessage, m.CreatedAt,
                DirectionText = m.DirectionId == 1 ? "Giden" : "Gelen",
                StatusText = m.StatusId == 4 ? "Basarisiz" : (m.StatusId == 3 ? "Okundu" : (m.StatusId == 2 ? "Teslim edildi" : "Gonderildi")),
                ClientName = m.SlnClient != null ? m.SlnClient.FullName : null
            }).ToListAsync();
    }

    public async Task<bool> SendTestAsync(int customerId, string phone, string message, int? branchId = null)
    {
        return await _whatsApp.SendTextMessageAsync(customerId, phone, message, branchId: branchId);
    }

    public async Task<bool> SendMessageAsync(int customerId, string phone, string message, int? branchId = null)
    {
        var client = await FindClientByPhoneAsync(customerId, phone, branchId);
        return await _whatsApp.SendTextMessageAsync(customerId, phone, message, client?.Id, branchId);
    }

    public async Task<(bool Success, string? Error)> RecordIncomingMessageAsync(string phoneNumberId, string fromPhone, string message, string? whatsAppMessageId)
    {
        var config = await _configEs.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.PhoneNumberId == phoneNumberId && c.IsActive);
        if (config == null) return (false, "WhatsApp konfigurasyonu bulunamadi");

        var client = await FindClientByPhoneAsync(config.CustomerId, fromPhone);
        var existing = !string.IsNullOrWhiteSpace(whatsAppMessageId)
            && await _messageEs.GetAllQueryable().AnyAsync(m => m.WhatsAppMessageId == whatsAppMessageId);
        if (existing) return (true, null);

        _messageEs.Add(new SlnWhatsAppMessage
        {
            CustomerId = config.CustomerId,
            DirectionId = 2,
            PhoneNumber = NormalizePhone(fromPhone),
            MessageBody = message,
            StatusId = 2,
            WhatsAppMessageId = whatsAppMessageId,
            SlnClientId = client?.Id,
            BranchId = client?.BranchId,
            CreatedAt = DateTime.UtcNow
        });
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    private async Task<SlnClient?> FindClientByPhoneAsync(int customerId, string phone, int? branchId = null)
    {
        var normalized = NormalizePhone(phone);
        var local = normalized.StartsWith("90") ? "0" + normalized[2..] : normalized;
        return await SalonBranchScope.ApplyToClients(
                _clients.GetAllQueryable()
                    .Where(c => c.CustomerId == customerId && (c.Phone == normalized || c.Phone == local || c.Phone2 == normalized || c.Phone2 == local)),
                branchId)
            .FirstOrDefaultAsync();
    }

    private static string NormalizePhone(string phone)
    {
        var digits = new string((phone ?? "").Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0")) digits = "90" + digits[1..];
        if (!digits.StartsWith("90")) digits = "90" + digits;
        return digits;
    }
}
