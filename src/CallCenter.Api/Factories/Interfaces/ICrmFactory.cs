using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ICrmFactory
{
    // Dashboard
    Task<CrmDashboardDto> GetDashboardAsync(int customerId);
    Task<List<CrmModuleDto>> GetModuleCatalogAsync();

    // CrmContacts
    Task<CrmCallerIdDto?> LookupByPhoneAsync(int customerId, string phoneNumber);
    Task<List<CrmContactDto>> GetContactsAsync(int customerId, string? search);
    Task<CrmContactDto?> GetContactDetailAsync(int contactId, int customerId);
    Task<(bool success, int id, string? error)> CreateContactAsync(CrmContactCreateDto dto, int customerId, int personnelId);
    Task<(bool success, string? error)> UpdateContactAsync(int contactId, CrmContactUpdateDto dto, int customerId);
    Task<(bool success, string? error)> DeleteContactAsync(int contactId, int customerId);

    // Tickets
    Task<List<CrmTicketDto>> GetTicketsAsync(int customerId, int? statusId);
    Task<CrmTicketDto?> GetTicketDetailAsync(int ticketId, int customerId);
    Task<(bool success, int id, string? error)> CreateTicketAsync(CrmTicketCreateDto dto, int customerId, int personnelId);
    Task<(bool success, string? error)> UpdateTicketAsync(int ticketId, CrmTicketUpdateDto dto, int customerId);

    // Deals
    Task<List<CrmDealDto>> GetDealsAsync(int customerId, int? stageId);
    Task<CrmDealDto?> GetDealDetailAsync(int dealId, int customerId);
    Task<(bool success, int id, string? error)> CreateDealAsync(CrmDealCreateDto dto, int customerId, int personnelId);
    Task<(bool success, string? error)> UpdateDealAsync(int dealId, CrmDealUpdateDto dto, int customerId);
    Task<(bool success, string? error)> DeleteDealAsync(int dealId, int customerId);

    // Activities
    Task<List<CrmActivityDto>> GetActivitiesAsync(int customerId, int? contactId, int? ticketId, int? dealId);
    Task<(bool success, int id, string? error)> CreateActivityAsync(CrmActivityCreateDto dto, int customerId, int personnelId);

    // Tasks
    Task<List<CrmTaskDto>> GetTasksAsync(int customerId, int? assignedTo, int? statusId);
    Task<(bool success, int id, string? error)> CreateTaskAsync(CrmTaskCreateDto dto, int customerId, int personnelId);
    Task<(bool success, string? error)> UpdateTaskAsync(int taskId, CrmTaskUpdateDto dto, int customerId);

    // Surveys
    Task<List<CrmSurveyDto>> GetSurveysAsync(int customerId);
    Task<CrmSurveyDetailDto?> GetSurveyDetailAsync(int surveyId, int customerId);
    Task<(bool success, int id, string? error)> CreateSurveyAsync(CrmSurveyCreateDto dto, int customerId, int personnelId);
    Task<(bool success, string? error)> ToggleSurveyAsync(int surveyId, int customerId);
    Task<(bool success, int id, string? error)> SubmitSurveyResponseAsync(CrmSurveyResponseCreateDto dto, int customerId, int personnelId);
    Task<List<CrmSurveyResponseDto>> GetSurveyResponsesAsync(int surveyId, int customerId);

    // Ticket Categories & Comments
    Task<List<CrmTicketCategoryDto>> GetTicketCategoriesAsync(int customerId);
    Task<(bool success, int id, string? error)> CreateTicketCategoryAsync(string name, int customerId);
    Task<List<CrmTicketCommentDto>> GetTicketCommentsAsync(int ticketId, int customerId);
    Task<(bool success, int id, string? error)> CreateTicketCommentAsync(int ticketId, CrmTicketCommentCreateDto dto, int customerId, int personnelId);

    // CrmContact Tags
    Task<List<CrmContactTagDto>> GetContactTagsAsync(int customerId);
    Task<(bool success, int id, string? error)> CreateContactTagAsync(string name, string? color, int customerId);
}
