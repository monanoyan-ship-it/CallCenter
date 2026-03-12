using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface IQualityFactory
{
    // ─── Checklist CRUD ───
    Task<List<QualityChecklistDto>> GetChecklistsAsync(int? customerId);
    Task<QualityChecklistDetailDto?> GetChecklistByUidAsync(Guid uid, int? customerId);
    Task<QualityChecklistDto> CreateChecklistAsync(CreateChecklistRequest req, int? customerId);
    Task<(bool Success, string? Error)> UpdateChecklistAsync(Guid uid, UpdateChecklistRequest req, int? customerId);
    Task<(bool Success, string? Error)> DeleteChecklistAsync(Guid uid, int? customerId);

    // ─── Question CRUD ───
    Task<(bool Success, string? Error, QualityQuestionDto? Question)> AddQuestionAsync(Guid checklistUid, CreateQuestionRequest req, int? customerId);
    Task<(bool Success, string? Error)> UpdateQuestionAsync(int questionId, UpdateQuestionRequest req, int? customerId);
    Task<(bool Success, string? Error)> DeleteQuestionAsync(int questionId, int? customerId);
    Task<(bool Success, string? Error)> ReorderQuestionsAsync(Guid checklistUid, List<int> questionIds, int? customerId);

    // ─── Evaluation Workflow ───
    Task<List<QualityEvaluationDto>> GetEvaluationsAsync(int? customerId, int? evaluatorId = null, int? evaluatedId = null, int? statusId = null);
    Task<QualityEvaluationDetailDto?> GetEvaluationByUidAsync(Guid uid, int? customerId);
    Task<(bool Success, string? Error, QualityEvaluationDto? Evaluation)> CreateEvaluationAsync(CreateEvaluationRequest req, int evaluatorPersonnelId, int? customerId);
    Task<(bool Success, string? Error)> SaveDraftAsync(Guid uid, SubmitEvaluationRequest req, int? customerId);
    Task<(bool Success, string? Error)> SubmitEvaluationAsync(Guid uid, SubmitEvaluationRequest req, int? customerId);
    Task<(bool Success, string? Error)> CancelEvaluationAsync(Guid uid, int? customerId);

    // ─── Score Threshold ───
    Task<List<QualityScoreThresholdDto>> GetThresholdsAsync(int? customerId);
    Task<(bool Success, string? Error)> UpsertThresholdAsync(UpsertScoreThresholdRequest req, int? customerId);
    Task<(bool Success, string? Error)> DeleteThresholdAsync(int thresholdId, int? customerId);

    // ─── Dashboard ───
    Task<QualityDashboardDto> GetDashboardAsync(int? customerId, DateTime? from = null, DateTime? to = null);
}
