using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnEmailIntegrationFactory
{
    Task<List<EmailIntegrationListDto>> GetIntegrationsAsync(int customerId);
    Task<EmailIntegrationDetailDto?> GetIntegrationDetailAsync(int customerId, Guid uid);
    Task<(int Id, string? Error)> CreateIntegrationAsync(int customerId, CreateEmailIntegrationRequest dto);
    Task<(bool Success, string? Error)> UpdateIntegrationAsync(int customerId, Guid uid, UpdateEmailIntegrationRequest dto);
    Task<(bool Success, string? Error)> DeleteIntegrationAsync(int customerId, Guid uid);
    Task<EmailSendResult> TestSendAsync(int customerId, Guid uid, EmailTestSendRequest dto);
}
