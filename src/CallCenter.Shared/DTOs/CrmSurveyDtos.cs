namespace CallCenter.Shared.DTOs;

public class CrmSurveyDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TypeId { get; set; }
    public string? TypeName { get; set; }
    public bool IsActive { get; set; }
    public int QuestionCount { get; set; }
    public int ResponseCount { get; set; }
    public decimal? AverageScore { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrmSurveyDetailDto : CrmSurveyDto
{
    public List<CrmSurveyQuestionDto> Questions { get; set; } = new();
}

public class CrmSurveyQuestionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int QuestionTypeId { get; set; }
    public string? QuestionTypeName { get; set; }
    public string? Options { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
}

public class CrmSurveyCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TypeId { get; set; } = 1;
    public List<CrmSurveyQuestionCreateDto> Questions { get; set; } = new();
}

public class CrmSurveyQuestionCreateDto
{
    public string Text { get; set; } = string.Empty;
    public int QuestionTypeId { get; set; } = 1;
    public string? Options { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; } = true;
}

public class CrmSurveyResponseCreateDto
{
    public int SurveyId { get; set; }
    public int? ContactId { get; set; }
    public int? CallRecordId { get; set; }
    public string? RespondentPhone { get; set; }
    public string? RespondentName { get; set; }
    public List<CrmSurveyAnswerDto> Answers { get; set; } = new();
}

public class CrmSurveyAnswerDto
{
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }
    public int? AnswerScore { get; set; }
}

public class CrmSurveyResponseDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }
    public string? ContactName { get; set; }
    public string? RespondentPhone { get; set; }
    public string? RespondentName { get; set; }
    public decimal? OverallScore { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public List<CrmSurveyAnswerDto> Answers { get; set; } = new();
}

public class CrmTicketCommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrmTicketCommentCreateDto
{
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}

public class CrmContactTagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public class CrmTicketCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
