using CallCenter.Shared.Entities;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnWhatsAppFactory
{
    Task<SlnWhatsAppConfig> GetConfigAsync(int customerId);
    Task SaveConfigAsync(int customerId, SlnWhatsAppConfig dto);
    Task<object> GetMessagesAsync(int customerId, int page, int pageSize);
    Task<bool> SendTestAsync(int customerId, string phone, string message);
    Task<bool> SendMessageAsync(int customerId, string phone, string message);
    Task<(bool Success, string? Error)> RecordIncomingMessageAsync(string phoneNumberId, string fromPhone, string message, string? whatsAppMessageId);
}
