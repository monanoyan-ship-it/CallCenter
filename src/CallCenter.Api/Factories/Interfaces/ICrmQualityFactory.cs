using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ICrmQualityFactory
{
    // ─── Checklist CRUD ───
    Task<List<CrmQualityChecklistDto>> GetChecklistsAsync(int? customerId);
    Task<CrmQualityChecklistDetailDto?> GetChecklistByUidAsync(Guid uid, int? customerId);
    Task<CrmQualityChecklistDto> CreateChecklistAsync(CreateChecklistRequest req, int? customerId);
    Task<(bool Success, string? Error)> UpdateChecklistAsync(Guid uid, UpdateChecklistRequest req, int? customerId);
    Task<(bool Success, string? Error)> DeleteChecklistAsync(Guid uid, int? customerId);

    // ─── Question CRUD ───
    Task<(bool Success, string? Error, CrmQualityQuestionDto? Question)> AddQuestionAsync(Guid checklistUid, CreateQuestionRequest req, int? customerId);
    Task<(bool Success, string? Error)> UpdateQuestionAsync(int questionId, UpdateQuestionRequest req, int? customerId);
    Task<(bool Success, string? Error)> DeleteQuestionAsync(int questionId, int? customerId);
    Task<(bool Success, string? Error)> ReorderQuestionsAsync(Guid checklistUid, List<int> questionIds, int? customerId);

    // ─── Evaluation Workflow ───
    Task<List<CrmQualityEvaluationDto>> GetEvaluationsAsync(int? customerId, int? evaluatorId = null, int? evaluatedId = null, int? statusId = null);
    Task<CrmQualityEvaluationDetailDto?> GetEvaluationByUidAsync(Guid uid, int? customerId);
    Task<(bool Success, string? Error, CrmQualityEvaluationDto? Evaluation)> CreateEvaluationAsync(CreateEvaluationRequest req, int evaluatorPersonnelId, int? customerId);
    Task<(bool Success, string? Error)> SaveDraftAsync(Guid uid, SubmitEvaluationRequest req, int? customerId);
    Task<(bool Success, string? Error)> SubmitEvaluationAsync(Guid uid, SubmitEvaluationRequest req, int? customerId);
    Task<(bool Success, string? Error)> CancelEvaluationAsync(Guid uid, int? customerId);

    // ─── Score Threshold ───
    Task<List<CrmQualityScoreThresholdDto>> GetThresholdsAsync(int? customerId);
    Task<(bool Success, string? Error)> UpsertThresholdAsync(UpsertScoreThresholdRequest req, int? customerId);
    Task<(bool Success, string? Error)> DeleteThresholdAsync(int thresholdId, int? customerId);

    // ─── Dashboard ───
    Task<CrmQualityDashboardDto> GetDashboardAsync(int? customerId, DateTime? from = null, DateTime? to = null);
}
