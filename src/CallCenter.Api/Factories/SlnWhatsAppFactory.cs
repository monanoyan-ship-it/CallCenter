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
    private readonly IWhatsAppService _whatsApp;
    private readonly IUnitOfWork _uow;

    public SlnWhatsAppFactory(ISlnWhatsAppConfigEntityService configEs, ISlnWhatsAppMessageEntityService messageEs, IWhatsAppService whatsApp, IUnitOfWork uow)
    {
        _configEs = configEs;
        _messageEs = messageEs;
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

    public async Task<object> GetMessagesAsync(int customerId, int page, int pageSize)
    {
        return await _messageEs.GetAllQueryable()
            .Where(m => m.CustomerId == customerId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new
            {
                m.Id, m.DirectionId, m.PhoneNumber, m.TemplateName, m.MessageBody,
                m.StatusId, m.ErrorMessage, m.CreatedAt,
                ClientName = m.SlnClient != null ? m.SlnClient.FullName : null
            }).ToListAsync();
    }

    public async Task<bool> SendTestAsync(int customerId, string phone, string message)
    {
        return await _whatsApp.SendTextMessageAsync(customerId, phone, message);
    }
}
