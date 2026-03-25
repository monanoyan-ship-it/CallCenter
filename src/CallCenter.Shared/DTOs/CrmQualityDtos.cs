namespace CallCenter.Shared.DTOs;

// ══════════════════════════════════════════════════════════════
// Checklist DTOs
// ══════════════════════════════════════════════════════════════

public class CrmQualityChecklistDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsScored { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public int ScoringMethodId { get; set; }
    public string ScoringMethodName { get; set; } = string.Empty;
    public decimal MaxTotalPoints { get; set; }
    public string? Code { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public int QuestionCount { get; set; }
    public int EvaluationCount { get; set; }
}

public class CrmQualityChecklistDetailDto : CrmQualityChecklistDto
{
    public bool HideGroupNames { get; set; }
    public List<CrmQualityQuestionDto> Questions { get; set; } = new();
}

public class CreateChecklistRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsScored { get; set; } = true;
    public int ScoringMethodId { get; set; } = 1;
    public decimal MaxTotalPoints { get; set; } = 100;
    public string? Code { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool HideGroupNames { get; set; }
}

public class UpdateChecklistRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsScored { get; set; }
    public bool? IsActive { get; set; }
    public int? ScoringMethodId { get; set; }
    public decimal? MaxTotalPoints { get; set; }
    public string? Code { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public bool? HideGroupNames { get; set; }
}

// ══════════════════════════════════════════════════════════════
// Question DTOs
// ══════════════════════════════════════════════════════════════

public class CrmQualityQuestionDto
{
    public int Id { get; set; }
    public int ChecklistId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public int ScoringTypeId { get; set; }
    public string ScoringTypeName { get; set; } = string.Empty;
    public decimal WeightPoints { get; set; }
    public int MaxPoints { get; set; }
    public int PenaltyTypeId { get; set; }
    public string PenaltyTypeName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? HelpText { get; set; }
    public string? GroupName { get; set; }
    public bool ShowScoreInput { get; set; }
    public bool AllowComment { get; set; }
    public bool IsActive { get; set; }
    public List<CrmQualitySubCriteriaDto> SubCriteria { get; set; } = new();
}

public class CreateQuestionRequest
{
    public string Text { get; set; } = string.Empty;
    public int? Order { get; set; }
    public int ScoringTypeId { get; set; } = 1;
    public decimal WeightPoints { get; set; } = 10;
    public int MaxPoints { get; set; } = 5;
    public int PenaltyTypeId { get; set; } = 0;
    public bool IsRequired { get; set; } = true;
    public string? HelpText { get; set; }
    public string? GroupName { get; set; }
    public bool ShowScoreInput { get; set; } = true;
    public bool AllowComment { get; set; } = true;
    public List<CreateSubCriteriaRequest>? SubCriteria { get; set; }
}

public class UpdateQuestionRequest
{
    public string? Text { get; set; }
    public int? Order { get; set; }
    public int? ScoringTypeId { get; set; }
    public decimal? WeightPoints { get; set; }
    public int? MaxPoints { get; set; }
    public int? PenaltyTypeId { get; set; }
    public bool? IsRequired { get; set; }
    public string? HelpText { get; set; }
    public string? GroupName { get; set; }
    public bool? ShowScoreInput { get; set; }
    public bool? AllowComment { get; set; }
    public bool? IsActive { get; set; }
}

// ══════════════════════════════════════════════════════════════
// SubCriteria DTOs
// ══════════════════════════════════════════════════════════════

public class CrmQualitySubCriteriaDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public decimal WeightPoints { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSubCriteriaRequest
{
    public string Description { get; set; } = string.Empty;
    public int? Order { get; set; }
    public decimal WeightPoints { get; set; } = 1;
}

// ══════════════════════════════════════════════════════════════
// Evaluation DTOs
// ══════════════════════════════════════════════════════════════

public class CrmQualityEvaluationDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public int ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;
    public int CallRecordId { get; set; }
    public string? CallRecordNumber { get; set; }
    public int EvaluatorPersonnelId { get; set; }
    public string EvaluatorName { get; set; } = string.Empty;
    public int? EvaluatedPersonnelId { get; set; }
    public string? EvaluatedName { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? ScorePercentage { get; set; }
    public int YellowCardCount { get; set; }
    public int RedCardCount { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrmQualityEvaluationDetailDto : CrmQualityEvaluationDto
{
    public string? EvaluationComment { get; set; }
    public string? Notes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FormOpenedAt { get; set; }
    public List<CrmQualityAnswerDto> Answers { get; set; } = new();
}

public class CreateEvaluationRequest
{
    public int ChecklistId { get; set; }
    public int CallRecordId { get; set; }
    public int? EvaluatedPersonnelId { get; set; }
}

public class SubmitEvaluationRequest
{
    public string? EvaluationComment { get; set; }
    public string? Notes { get; set; }
    public List<SubmitAnswerRequest> Answers { get; set; } = new();
}

public class SubmitAnswerRequest
{
    public int QuestionId { get; set; }
    public decimal? GivenPoints { get; set; }
    public string? AnswerText { get; set; }
    public string? Notes { get; set; }
    public string? RecommendationNotes { get; set; }
    public bool IsNotApplicable { get; set; }
    public bool IsPenaltyApplied { get; set; }
    public int AppliedPenaltyTypeId { get; set; }
    public List<int>? SelectedSubCriteriaIds { get; set; }
}

// ══════════════════════════════════════════════════════════════
// Answer DTOs
// ══════════════════════════════════════════════════════════════

public class CrmQualityAnswerDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public int ScoringTypeId { get; set; }
    public decimal? GivenPoints { get; set; }
    public decimal? EarnedPoints { get; set; }
    public decimal WeightPoints { get; set; }
    public int MaxPoints { get; set; }
    public string? AnswerText { get; set; }
    public string? Notes { get; set; }
    public string? RecommendationNotes { get; set; }
    public bool IsPenaltyApplied { get; set; }
    public int AppliedPenaltyTypeId { get; set; }
    public string? AppliedPenaltyTypeName { get; set; }
    public bool IsNotApplicable { get; set; }
    public List<int> SelectedSubCriteriaIds { get; set; } = new();
}

// ══════════════════════════════════════════════════════════════
// ScoreThreshold DTOs
// ══════════════════════════════════════════════════════════════

public class CrmQualityScoreThresholdDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal SuccessThreshold { get; set; }
    public decimal WarningThreshold { get; set; }
    public int? ChecklistId { get; set; }
    public string? ChecklistName { get; set; }
}

public class UpsertScoreThresholdRequest
{
    public decimal SuccessThreshold { get; set; } = 80;
    public decimal WarningThreshold { get; set; } = 60;
    public int? ChecklistId { get; set; }
}

// ══════════════════════════════════════════════════════════════
// Dashboard / Summary DTOs
// ══════════════════════════════════════════════════════════════

public class CrmQualityDashboardDto
{
    public int TotalEvaluations { get; set; }
    public int CompletedEvaluations { get; set; }
    public int PendingEvaluations { get; set; }
    public decimal? AverageScore { get; set; }
    public int YellowCardTotal { get; set; }
    public int RedCardTotal { get; set; }
    public List<CrmQualityAgentScoreDto> TopAgents { get; set; } = new();
    public List<CrmQualityAgentScoreDto> BottomAgents { get; set; } = new();
}

public class CrmQualityAgentScoreDto
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public int EvaluationCount { get; set; }
    public decimal AverageScore { get; set; }
    public int YellowCardCount { get; set; }
    public int RedCardCount { get; set; }
}
