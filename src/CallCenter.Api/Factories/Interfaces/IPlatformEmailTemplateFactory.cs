using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IPlatformEmailTemplateFactory
{
    // Event CRUD
    Task<List<PlatformEmailEventDto>> GetAllEventsAsync();
    Task<PlatformEmailEventDto?> GetEventByIdAsync(int id);
    Task<PlatformEmailEventDto> CreateEventAsync(PlatformEmailEventCreateDto dto);
    Task<(bool Success, string? Error)> UpdateEventAsync(int id, PlatformEmailEventUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteEventAsync(int id);

    // Template CRUD (event altinda, dil bazli)
    Task<PlatformEmailTemplateDto> AddTemplateAsync(int eventId, PlatformEmailTemplateCreateDto dto);
    Task<(bool Success, string? Error)> UpdateTemplateAsync(int templateId, PlatformEmailTemplateUpdateDto dto);
    Task<(bool Success, string? Error)> DeleteTemplateAsync(int templateId);
    Task<EmailSendResult> SendTestTemplateAsync(int templateId, PlatformEmailTemplateTestSendDto dto);
}
