using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class QualityFactory : IQualityFactory
{
    private readonly IQualityChecklistEntityService _checklistEs;
    private readonly IQualityQuestionEntityService _questionEs;
    private readonly IQualitySubCriteriaEntityService _subCriteriaEs;
    private readonly IQualityEvaluationEntityService _evaluationEs;
    private readonly IQualityAnswerEntityService _answerEs;
    private readonly IQualityAnswerSubCriteriaSelectionEntityService _selectionEs;
    private readonly IQualityScoreThresholdEntityService _thresholdEs;
    private readonly ICallRecordEntityService _callRecordEs;
    private readonly ICustomerPersonnelEntityService _personnelEs;
    private readonly IUnitOfWork _uow;

    public QualityFactory(
        IQualityChecklistEntityService checklistEs,
        IQualityQuestionEntityService questionEs,
        IQualitySubCriteriaEntityService subCriteriaEs,
        IQualityEvaluationEntityService evaluationEs,
        IQualityAnswerEntityService answerEs,
        IQualityAnswerSubCriteriaSelectionEntityService selectionEs,
        IQualityScoreThresholdEntityService thresholdEs,
        ICallRecordEntityService callRecordEs,
        ICustomerPersonnelEntityService personnelEs,
        IUnitOfWork uow)
    {
        _checklistEs = checklistEs;
        _questionEs = questionEs;
        _subCriteriaEs = subCriteriaEs;
        _evaluationEs = evaluationEs;
        _answerEs = answerEs;
        _selectionEs = selectionEs;
        _thresholdEs = thresholdEs;
        _callRecordEs = callRecordEs;
        _personnelEs = personnelEs;
        _uow = uow;
    }

    // ══════════════════════════════════════════════════════════════
    // Checklist CRUD
    // ══════════════════════════════════════════════════════════════

    public async Task<List<QualityChecklistDto>> GetChecklistsAsync(int? customerId)
    {
        var query = _checklistEs.GetAllQueryable()
            .Include(c => c.Questions)
            .Include(c => c.Evaluations)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);

        var checklists = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return checklists.Select(MapChecklistToDto).ToList();
    }

    public async Task<QualityChecklistDetailDto?> GetChecklistByUidAsync(Guid uid, int? customerId)
    {
        var query = _checklistEs.GetAllQueryable()
            .Include(c => c.Questions.Where(q => q.IsActive).OrderBy(q => q.Order))
                .ThenInclude(q => q.SubCriteria.Where(sc => sc.IsActive).OrderBy(sc => sc.Order))
            .Where(c => c.Uid == uid);

        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);

        var checklist = await query.FirstOrDefaultAsync();
        if (checklist == null) return null;

        return new QualityChecklistDetailDto
        {
            Id = checklist.Id,
            Uid = checklist.Uid,
            Name = checklist.Name,
            Description = checklist.Description,
            IsScored = checklist.IsScored,
            IsActive = checklist.IsActive,
            Version = checklist.Version,
            ScoringMethodId = checklist.ScoringMethodId,
            ScoringMethodName = QualityScoringMethods.GetById(checklist.ScoringMethodId)?.Description ?? "",
            MaxTotalPoints = checklist.MaxTotalPoints,
            Code = checklist.Code,
            ValidFrom = checklist.ValidFrom,
            ValidUntil = checklist.ValidUntil,
            HideGroupNames = checklist.HideGroupNames,
            CreatedAt = checklist.CreatedAt,
            QuestionCount = checklist.Questions.Count,
            Questions = checklist.Questions.Select(MapQuestionToDto).ToList()
        };
    }

    public async Task<QualityChecklistDto> CreateChecklistAsync(CreateChecklistRequest req, int? customerId)
    {
        var checklist = new QualityChecklist
        {
            CustomerId = customerId ?? 0,
            Name = req.Name,
            Description = req.Description,
            IsScored = req.IsScored,
            ScoringMethodId = req.ScoringMethodId,
            MaxTotalPoints = req.MaxTotalPoints,
            Code = req.Code,
            ValidFrom = req.ValidFrom.HasValue ? DateTime.SpecifyKind(req.ValidFrom.Value, DateTimeKind.Utc) : null,
            ValidUntil = req.ValidUntil.HasValue ? DateTime.SpecifyKind(req.ValidUntil.Value, DateTimeKind.Utc) : null,
            HideGroupNames = req.HideGroupNames
        };

        _checklistEs.Add(checklist);
        await _uow.SaveChangesAsync();

        return MapChecklistToDto(checklist);
    }

    public async Task<(bool Success, string? Error)> UpdateChecklistAsync(Guid uid, UpdateChecklistRequest req, int? customerId)
    {
        var checklist = await GetChecklistByUidAndCustomer(uid, customerId);
        if (checklist == null) return (false, "Checklist bulunamadi");

        if (req.Name != null) checklist.Name = req.Name;
        if (req.Description != null) checklist.Description = req.Description;
        if (req.IsScored.HasValue) checklist.IsScored = req.IsScored.Value;
        if (req.IsActive.HasValue) checklist.IsActive = req.IsActive.Value;
        if (req.ScoringMethodId.HasValue) checklist.ScoringMethodId = req.ScoringMethodId.Value;
        if (req.MaxTotalPoints.HasValue) checklist.MaxTotalPoints = req.MaxTotalPoints.Value;
        if (req.Code != null) checklist.Code = req.Code;
        if (req.ValidFrom.HasValue) checklist.ValidFrom = DateTime.SpecifyKind(req.ValidFrom.Value, DateTimeKind.Utc);
        if (req.ValidUntil.HasValue) checklist.ValidUntil = DateTime.SpecifyKind(req.ValidUntil.Value, DateTimeKind.Utc);
        if (req.HideGroupNames.HasValue) checklist.HideGroupNames = req.HideGroupNames.Value;
        checklist.UpdatedAt = DateTime.UtcNow;
        checklist.Version++;

        _checklistEs.Update(checklist);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteChecklistAsync(Guid uid, int? customerId)
    {
        var checklist = await _checklistEs.GetAllQueryable()
            .Include(c => c.Evaluations)
            .Where(c => c.Uid == uid)
            .FirstOrDefaultAsync();

        if (checklist == null) return (false, "Checklist bulunamadi");
        if (customerId.HasValue && checklist.CustomerId != customerId.Value) return (false, "Yetki hatasi");

        if (checklist.Evaluations.Any())
            return (false, "Bu checklist ile yapilmis degerlendirmeler var. Silinemiyor. Deaktif edebilirsiniz.");

        _checklistEs.Delete(checklist);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ══════════════════════════════════════════════════════════════
    // Question CRUD
    // ══════════════════════════════════════════════════════════════

    public async Task<(bool Success, string? Error, QualityQuestionDto? Question)> AddQuestionAsync(
        Guid checklistUid, CreateQuestionRequest req, int? customerId)
    {
        var checklist = await _checklistEs.GetAllQueryable()
            .Include(c => c.Questions)
            .Where(c => c.Uid == checklistUid)
            .FirstOrDefaultAsync();

        if (checklist == null) return (false, "Checklist bulunamadi", null);
        if (customerId.HasValue && checklist.CustomerId != customerId.Value) return (false, "Yetki hatasi", null);

        var maxOrder = checklist.Questions.Any() ? checklist.Questions.Max(q => q.Order) : 0;

        var question = new QualityQuestion
        {
            ChecklistId = checklist.Id,
            Text = req.Text,
            Order = req.Order ?? maxOrder + 1,
            ScoringTypeId = req.ScoringTypeId,
            WeightPoints = req.WeightPoints,
            MaxPoints = req.MaxPoints,
            PenaltyTypeId = req.PenaltyTypeId,
            IsRequired = req.IsRequired,
            HelpText = req.HelpText,
            GroupName = req.GroupName,
            ShowScoreInput = req.ShowScoreInput,
            AllowComment = req.AllowComment
        };

        _questionEs.Add(question);
        await _uow.SaveChangesAsync();

        // Alt kriterleri ekle
        if (req.SubCriteria?.Any() == true)
        {
            var subCriteria = req.SubCriteria.Select((sc, i) => new QualityQuestionSubCriteria
            {
                QuestionId = question.Id,
                Description = sc.Description,
                Order = sc.Order ?? i + 1,
                WeightPoints = sc.WeightPoints
            }).ToList();

            _subCriteriaEs.AddRange(subCriteria);
            await _uow.SaveChangesAsync();
        }

        // Reload with sub-criteria
        var loaded = await _questionEs.GetAllQueryable()
            .Include(q => q.SubCriteria.OrderBy(sc => sc.Order))
            .FirstAsync(q => q.Id == question.Id);

        return (true, null, MapQuestionToDto(loaded));
    }

    public async Task<(bool Success, string? Error)> UpdateQuestionAsync(int questionId, UpdateQuestionRequest req, int? customerId)
    {
        var question = await _questionEs.GetAllQueryable()
            .Include(q => q.Checklist)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null) return (false, "Soru bulunamadi");
        if (customerId.HasValue && question.Checklist.CustomerId != customerId.Value) return (false, "Yetki hatasi");

        if (req.Text != null) question.Text = req.Text;
        if (req.Order.HasValue) question.Order = req.Order.Value;
        if (req.ScoringTypeId.HasValue) question.ScoringTypeId = req.ScoringTypeId.Value;
        if (req.WeightPoints.HasValue) question.WeightPoints = req.WeightPoints.Value;
        if (req.MaxPoints.HasValue) question.MaxPoints = req.MaxPoints.Value;
        if (req.PenaltyTypeId.HasValue) question.PenaltyTypeId = req.PenaltyTypeId.Value;
        if (req.IsRequired.HasValue) question.IsRequired = req.IsRequired.Value;
        if (req.HelpText != null) question.HelpText = req.HelpText;
        if (req.GroupName != null) question.GroupName = req.GroupName;
        if (req.ShowScoreInput.HasValue) question.ShowScoreInput = req.ShowScoreInput.Value;
        if (req.AllowComment.HasValue) question.AllowComment = req.AllowComment.Value;
        if (req.IsActive.HasValue) question.IsActive = req.IsActive.Value;

        _questionEs.Update(question);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteQuestionAsync(int questionId, int? customerId)
    {
        var question = await _questionEs.GetAllQueryable()
            .Include(q => q.Checklist)
            .Include(q => q.Answers)
            .Include(q => q.SubCriteria)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null) return (false, "Soru bulunamadi");
        if (customerId.HasValue && question.Checklist.CustomerId != customerId.Value) return (false, "Yetki hatasi");

        if (question.Answers.Any())
            return (false, "Bu soru cevaplanmis degerlendirmelerde var. Silinemiyor. Deaktif edebilirsiniz.");

        if (question.SubCriteria.Any())
            _subCriteriaEs.DeleteRange(question.SubCriteria);

        _questionEs.Delete(question);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ReorderQuestionsAsync(Guid checklistUid, List<int> questionIds, int? customerId)
    {
        var checklist = await GetChecklistByUidAndCustomer(checklistUid, customerId);
        if (checklist == null) return (false, "Checklist bulunamadi");

        var questions = await _questionEs.GetAllQueryable()
            .Where(q => q.ChecklistId == checklist.Id && questionIds.Contains(q.Id))
            .ToListAsync();

        for (int i = 0; i < questionIds.Count; i++)
        {
            var question = questions.FirstOrDefault(q => q.Id == questionIds[i]);
            if (question != null)
                question.Order = i + 1;
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ══════════════════════════════════════════════════════════════
    // Evaluation Workflow
    // ══════════════════════════════════════════════════════════════

    public async Task<List<QualityEvaluationDto>> GetEvaluationsAsync(
        int? customerId, int? evaluatorId = null, int? evaluatedId = null, int? statusId = null)
    {
        var query = _evaluationEs.GetAllQueryable()
            .Include(e => e.Checklist)
            .Include(e => e.CallRecord)
            .Include(e => e.EvaluatorPersonnel).ThenInclude(p => p.User)
            .Include(e => e.EvaluatedPersonnel!).ThenInclude(p => p!.User)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(e => e.CustomerId == customerId.Value);
        if (evaluatorId.HasValue)
            query = query.Where(e => e.EvaluatorPersonnelId == evaluatorId.Value);
        if (evaluatedId.HasValue)
            query = query.Where(e => e.EvaluatedPersonnelId == evaluatedId.Value);
        if (statusId.HasValue)
            query = query.Where(e => e.StatusId == statusId.Value);

        var evaluations = await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return evaluations.Select(MapEvaluationToDto).ToList();
    }

    public async Task<QualityEvaluationDetailDto?> GetEvaluationByUidAsync(Guid uid, int? customerId)
    {
        var query = _evaluationEs.GetAllQueryable()
            .Include(e => e.Checklist)
            .Include(e => e.CallRecord)
            .Include(e => e.EvaluatorPersonnel).ThenInclude(p => p.User)
            .Include(e => e.EvaluatedPersonnel!).ThenInclude(p => p!.User)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
            .Where(e => e.Uid == uid);

        if (customerId.HasValue)
            query = query.Where(e => e.CustomerId == customerId.Value);

        var evaluation = await query.FirstOrDefaultAsync();
        if (evaluation == null) return null;

        var dto = new QualityEvaluationDetailDto
        {
            Id = evaluation.Id,
            Uid = evaluation.Uid,
            ChecklistId = evaluation.ChecklistId,
            ChecklistName = evaluation.Checklist?.Name ?? "",
            CallRecordId = evaluation.CallRecordId,
            CallRecordNumber = evaluation.CallRecord?.CalleeNumber,
            EvaluatorPersonnelId = evaluation.EvaluatorPersonnelId,
            EvaluatorName = evaluation.EvaluatorPersonnel?.User?.FullName ?? "",
            EvaluatedPersonnelId = evaluation.EvaluatedPersonnelId,
            EvaluatedName = evaluation.EvaluatedPersonnel?.User?.FullName,
            StatusId = evaluation.StatusId,
            StatusName = QualityEvaluationStatuses.GetById(evaluation.StatusId)?.Description ?? "",
            TotalScore = evaluation.TotalScore,
            MaxScore = evaluation.MaxScore,
            ScorePercentage = evaluation.ScorePercentage,
            YellowCardCount = evaluation.YellowCardCount,
            RedCardCount = evaluation.RedCardCount,
            CompletedAt = evaluation.CompletedAt,
            CreatedAt = evaluation.CreatedAt,
            EvaluationComment = evaluation.EvaluationComment,
            Notes = evaluation.Notes,
            StartedAt = evaluation.StartedAt,
            FormOpenedAt = evaluation.FormOpenedAt,
            Answers = evaluation.Answers
                .OrderBy(a => a.Question?.Order ?? 0)
                .Select(a => new QualityAnswerDto
                {
                    Id = a.Id,
                    QuestionId = a.QuestionId,
                    QuestionText = a.Question?.Text ?? "",
                    GroupName = a.Question?.GroupName,
                    ScoringTypeId = a.Question?.ScoringTypeId ?? QualityScoringTypes.Ids.Scored,
                    GivenPoints = a.GivenPoints,
                    EarnedPoints = a.EarnedPoints,
                    WeightPoints = a.Question?.WeightPoints ?? 0,
                    MaxPoints = a.Question?.MaxPoints ?? 0,
                    AnswerText = a.AnswerText,
                    Notes = a.Notes,
                    RecommendationNotes = a.RecommendationNotes,
                    IsPenaltyApplied = a.IsPenaltyApplied,
                    AppliedPenaltyTypeId = a.AppliedPenaltyTypeId,
                    AppliedPenaltyTypeName = QualityPenaltyTypes.GetById(a.AppliedPenaltyTypeId)?.Description,
                    IsNotApplicable = a.IsNotApplicable,
                    SelectedSubCriteriaIds = a.SubCriteriaSelections.Select(s => s.SubCriteriaId).ToList()
                }).ToList()
        };

        return dto;
    }

    public async Task<(bool Success, string? Error, QualityEvaluationDto? Evaluation)> CreateEvaluationAsync(
        CreateEvaluationRequest req, int evaluatorPersonnelId, int? customerId)
    {
        // Checklist var mi?
        var checklist = await _checklistEs.GetByIdAsync(req.ChecklistId);
        if (checklist == null) return (false, "Checklist bulunamadi", null);
        if (customerId.HasValue && checklist.CustomerId != customerId.Value)
            return (false, "Bu checklist firmaniza ait degil", null);
        if (!checklist.IsActive)
            return (false, "Bu checklist deaktif", null);

        // Cagri kaydi var mi?
        var callRecord = await _callRecordEs.GetByIdAsync(req.CallRecordId);
        if (callRecord == null) return (false, "Cagri kaydi bulunamadi", null);

        var evaluation = new QualityEvaluation
        {
            ChecklistId = req.ChecklistId,
            CallRecordId = req.CallRecordId,
            EvaluatorPersonnelId = evaluatorPersonnelId,
            EvaluatedPersonnelId = req.EvaluatedPersonnelId,
            CustomerId = customerId ?? checklist.CustomerId,
            StatusId = QualityEvaluationStatuses.Ids.Draft,
            FormOpenedAt = DateTime.UtcNow
        };

        _evaluationEs.Add(evaluation);
        await _uow.SaveChangesAsync();

        // Reload with navigation
        var loaded = await _evaluationEs.GetAllQueryable()
            .Include(e => e.Checklist)
            .Include(e => e.CallRecord)
            .Include(e => e.EvaluatorPersonnel).ThenInclude(p => p.User)
            .Include(e => e.EvaluatedPersonnel!).ThenInclude(p => p!.User)
            .FirstAsync(e => e.Id == evaluation.Id);

        return (true, null, MapEvaluationToDto(loaded));
    }

    public async Task<(bool Success, string? Error)> SaveDraftAsync(
        Guid uid, SubmitEvaluationRequest req, int? customerId)
    {
        var (success, error, evaluation) = await LoadEvaluationForEdit(uid, customerId);
        if (!success) return (false, error);

        // Cevaplari kaydet (eski varsa sil + yeni yaz)
        await ReplaceAnswersAsync(evaluation!, req);

        // Draft olarak kalir — puan hesaplanmaz, raporlara yansimaz
        evaluation!.StatusId = QualityEvaluationStatuses.Ids.Draft;
        evaluation.EvaluationComment = req.EvaluationComment;
        evaluation.Notes = req.Notes;
        evaluation.UpdatedAt = DateTime.UtcNow;

        if (evaluation.StartedAt == null)
            evaluation.StartedAt = DateTime.UtcNow;

        _evaluationEs.Update(evaluation);
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SubmitEvaluationAsync(
        Guid uid, SubmitEvaluationRequest req, int? customerId)
    {
        var (success, error, evaluation) = await LoadEvaluationForEdit(uid, customerId);
        if (!success) return (false, error);

        // Cevaplari kaydet (eski varsa sil + yeni yaz)
        var answers = await ReplaceAnswersAsync(evaluation!, req);

        // Final puan hesaplama
        var questions = evaluation!.Checklist.Questions.ToDictionary(q => q.Id);
        var scoringMethod = evaluation.Checklist.ScoringMethodId;

        decimal totalEarned = 0;
        decimal totalMaxWeight = 0;
        int yellowCards = 0;
        int redCards = 0;

        foreach (var answer in answers)
        {
            if (!questions.TryGetValue(answer.QuestionId, out var question))
                continue;

            // Puan hesaplama
            if (!answer.IsNotApplicable && question.ScoringTypeId == QualityScoringTypes.Ids.Scored)
            {
                if (scoringMethod == QualityScoringMethods.Ids.Maximum)
                {
                    // Maximum: (GivenPoints / MaxPoints) * WeightPoints
                    if (answer.GivenPoints.HasValue && question.MaxPoints > 0)
                    {
                        answer.EarnedPoints = (answer.GivenPoints.Value / question.MaxPoints) * question.WeightPoints;
                    }
                    else
                    {
                        answer.EarnedPoints = 0;
                    }
                }
                else if (scoringMethod == QualityScoringMethods.Ids.CriteriaTotal)
                {
                    // CriteriaTotal: GivenPoints direkt sayilir
                    answer.EarnedPoints = answer.GivenPoints ?? 0;
                }

                totalEarned += answer.EarnedPoints ?? 0;
                totalMaxWeight += question.WeightPoints;
            }

            // Ceza kartlari
            if (answer.IsPenaltyApplied)
            {
                if (answer.AppliedPenaltyTypeId == QualityPenaltyTypes.Ids.YellowCard)
                    yellowCards++;
                else if (answer.AppliedPenaltyTypeId == QualityPenaltyTypes.Ids.RedCard)
                    redCards++;
            }
        }

        // EarnedPoints guncelle (hesaplanan degerler answer'lara yazildi)
        await _uow.SaveChangesAsync();

        // Evaluation toplamlarini guncelle — SADECE submit'te
        evaluation.TotalScore = totalEarned;
        evaluation.MaxScore = totalMaxWeight;
        evaluation.ScorePercentage = totalMaxWeight > 0
            ? Math.Round((totalEarned / totalMaxWeight) * 100, 2)
            : null;
        evaluation.YellowCardCount = yellowCards;
        evaluation.RedCardCount = redCards;
        evaluation.StatusId = QualityEvaluationStatuses.Ids.Completed;
        evaluation.CompletedAt = DateTime.UtcNow;
        evaluation.EvaluationComment = req.EvaluationComment;
        evaluation.Notes = req.Notes;
        evaluation.UpdatedAt = DateTime.UtcNow;

        if (evaluation.StartedAt == null)
            evaluation.StartedAt = evaluation.FormOpenedAt ?? evaluation.CreatedAt;

        _evaluationEs.Update(evaluation);
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    /// <summary>
    /// Degerlendirmeyi edit icin yukle + validasyon yap.
    /// Draft/Pending/InProgress kabul edilir, Completed ve Cancelled reddedilir.
    /// </summary>
    private async Task<(bool Success, string? Error, QualityEvaluation? Evaluation)> LoadEvaluationForEdit(
        Guid uid, int? customerId)
    {
        var evaluation = await _evaluationEs.GetAllQueryable()
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
            .Include(e => e.Checklist)
                .ThenInclude(c => c.Questions.Where(q => q.IsActive))
            .FirstOrDefaultAsync(e => e.Uid == uid);

        if (evaluation == null) return (false, "Degerlendirme bulunamadi", null);
        if (customerId.HasValue && evaluation.CustomerId != customerId.Value)
            return (false, "Yetki hatasi", null);
        if (evaluation.StatusId == QualityEvaluationStatuses.Ids.Completed)
            return (false, "Bu degerlendirme zaten tamamlanmis", null);
        if (evaluation.StatusId == QualityEvaluationStatuses.Ids.Cancelled)
            return (false, "Bu degerlendirme iptal edilmis", null);

        return (true, null, evaluation);
    }

    /// <summary>
    /// Mevcut cevaplari sil, yeni cevaplari + alt kriter secimlerini kaydet.
    /// Hem SaveDraft hem Submit tarafindan kullanilir.
    /// Puan hesaplamayi YAPMAZ — sadece ham veriyi yazar.
    /// </summary>
    private async Task<List<QualityAnswer>> ReplaceAnswersAsync(
        QualityEvaluation evaluation, SubmitEvaluationRequest req)
    {
        // Eski cevaplari temizle
        if (evaluation.Answers.Any())
        {
            foreach (var answer in evaluation.Answers)
            {
                if (answer.SubCriteriaSelections.Any())
                    _selectionEs.DeleteRange(answer.SubCriteriaSelections);
            }
            _answerEs.DeleteRange(evaluation.Answers);
            await _uow.SaveChangesAsync();
        }

        // Yeni cevaplari olustur
        var answers = new List<QualityAnswer>();
        foreach (var answerReq in req.Answers)
        {
            var answer = new QualityAnswer
            {
                EvaluationId = evaluation.Id,
                QuestionId = answerReq.QuestionId,
                GivenPoints = answerReq.GivenPoints,
                AnswerText = answerReq.AnswerText,
                Notes = answerReq.Notes,
                RecommendationNotes = answerReq.RecommendationNotes,
                IsNotApplicable = answerReq.IsNotApplicable,
                IsPenaltyApplied = answerReq.IsPenaltyApplied,
                AppliedPenaltyTypeId = answerReq.AppliedPenaltyTypeId
            };
            answers.Add(answer);
        }

        _answerEs.AddRange(answers);
        await _uow.SaveChangesAsync();

        // Alt kriter secimleri
        var selections = new List<QualityAnswerSubCriteriaSelection>();
        foreach (var answerReq in req.Answers)
        {
            if (answerReq.SelectedSubCriteriaIds?.Any() != true) continue;

            var answer = answers.First(a => a.QuestionId == answerReq.QuestionId);
            foreach (var scId in answerReq.SelectedSubCriteriaIds)
            {
                selections.Add(new QualityAnswerSubCriteriaSelection
                {
                    AnswerId = answer.Id,
                    SubCriteriaId = scId
                });
            }
        }

        if (selections.Any())
        {
            _selectionEs.AddRange(selections);
            await _uow.SaveChangesAsync();
        }

        return answers;
    }

    public async Task<(bool Success, string? Error)> CancelEvaluationAsync(Guid uid, int? customerId)
    {
        var evaluation = await _evaluationEs.GetAllQueryable()
            .FirstOrDefaultAsync(e => e.Uid == uid);

        if (evaluation == null) return (false, "Degerlendirme bulunamadi");
        if (customerId.HasValue && evaluation.CustomerId != customerId.Value) return (false, "Yetki hatasi");
        if (evaluation.StatusId == QualityEvaluationStatuses.Ids.Completed)
            return (false, "Tamamlanmis degerlendirme iptal edilemez");

        evaluation.StatusId = QualityEvaluationStatuses.Ids.Cancelled;
        evaluation.UpdatedAt = DateTime.UtcNow;

        _evaluationEs.Update(evaluation);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ══════════════════════════════════════════════════════════════
    // Score Threshold
    // ══════════════════════════════════════════════════════════════

    public async Task<List<QualityScoreThresholdDto>> GetThresholdsAsync(int? customerId)
    {
        var query = _thresholdEs.GetAllQueryable()
            .Include(t => t.Checklist)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(t => t.CustomerId == customerId.Value);

        var thresholds = await query.ToListAsync();
        return thresholds.Select(t => new QualityScoreThresholdDto
        {
            Id = t.Id,
            CustomerId = t.CustomerId,
            SuccessThreshold = t.SuccessThreshold,
            WarningThreshold = t.WarningThreshold,
            ChecklistId = t.ChecklistId,
            ChecklistName = t.Checklist?.Name
        }).ToList();
    }

    public async Task<(bool Success, string? Error)> UpsertThresholdAsync(UpsertScoreThresholdRequest req, int? customerId)
    {
        if (!customerId.HasValue) return (false, "Musteri bilgisi gerekli");
        if (req.SuccessThreshold <= req.WarningThreshold)
            return (false, "Basari esigi uyari esiginden buyuk olmali");

        // Ayni checklist icin mevcut var mi?
        var existing = await _thresholdEs.GetAllQueryable()
            .FirstOrDefaultAsync(t => t.CustomerId == customerId.Value
                && t.ChecklistId == req.ChecklistId);

        if (existing != null)
        {
            existing.SuccessThreshold = req.SuccessThreshold;
            existing.WarningThreshold = req.WarningThreshold;
            existing.UpdatedAt = DateTime.UtcNow;
            _thresholdEs.Update(existing);
        }
        else
        {
            var threshold = new QualityScoreThreshold
            {
                CustomerId = customerId.Value,
                SuccessThreshold = req.SuccessThreshold,
                WarningThreshold = req.WarningThreshold,
                ChecklistId = req.ChecklistId
            };
            _thresholdEs.Add(threshold);
        }

        await _uow.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteThresholdAsync(int thresholdId, int? customerId)
    {
        var threshold = await _thresholdEs.GetByIdAsync(thresholdId);
        if (threshold == null) return (false, "Esik degeri bulunamadi");
        if (customerId.HasValue && threshold.CustomerId != customerId.Value) return (false, "Yetki hatasi");

        _thresholdEs.Delete(threshold);
        await _uow.SaveChangesAsync();
        return (true, null);
    }

    // ══════════════════════════════════════════════════════════════
    // Dashboard
    // ══════════════════════════════════════════════════════════════

    public async Task<QualityDashboardDto> GetDashboardAsync(int? customerId, DateTime? from = null, DateTime? to = null)
    {
        var query = _evaluationEs.GetAllQueryable()
            .Include(e => e.EvaluatedPersonnel!).ThenInclude(p => p!.User)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(e => e.CustomerId == customerId.Value);

        if (from.HasValue)
            query = query.Where(e => e.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedAt <= to.Value);

        var evaluations = await query.ToListAsync();

        var completed = evaluations.Where(e => e.StatusId == QualityEvaluationStatuses.Ids.Completed).ToList();

        // Agent bazli istatistikler
        var agentStats = completed
            .Where(e => e.EvaluatedPersonnelId.HasValue)
            .GroupBy(e => e.EvaluatedPersonnelId!.Value)
            .Select(g => new QualityAgentScoreDto
            {
                PersonnelId = g.Key,
                PersonnelName = g.First().EvaluatedPersonnel?.User?.FullName ?? "",
                EvaluationCount = g.Count(),
                AverageScore = g.Average(e => e.ScorePercentage ?? 0),
                YellowCardCount = g.Sum(e => e.YellowCardCount),
                RedCardCount = g.Sum(e => e.RedCardCount)
            })
            .ToList();

        return new QualityDashboardDto
        {
            TotalEvaluations = evaluations.Count,
            CompletedEvaluations = completed.Count,
            PendingEvaluations = evaluations.Count(e =>
                e.StatusId == QualityEvaluationStatuses.Ids.Pending ||
                e.StatusId == QualityEvaluationStatuses.Ids.Draft ||
                e.StatusId == QualityEvaluationStatuses.Ids.InProgress),
            AverageScore = completed.Any() ? Math.Round(completed.Average(e => e.ScorePercentage ?? 0), 2) : null,
            YellowCardTotal = completed.Sum(e => e.YellowCardCount),
            RedCardTotal = completed.Sum(e => e.RedCardCount),
            TopAgents = agentStats.OrderByDescending(a => a.AverageScore).Take(5).ToList(),
            BottomAgents = agentStats.OrderBy(a => a.AverageScore).Take(5).ToList()
        };
    }

    // ══════════════════════════════════════════════════════════════
    // Helpers & Mappers
    // ══════════════════════════════════════════════════════════════

    private async Task<QualityChecklist?> GetChecklistByUidAndCustomer(Guid uid, int? customerId)
    {
        var query = _checklistEs.GetAllQueryable().Where(c => c.Uid == uid);
        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);
        return await query.FirstOrDefaultAsync();
    }

    private static QualityChecklistDto MapChecklistToDto(QualityChecklist c) => new()
    {
        Id = c.Id,
        Uid = c.Uid,
        Name = c.Name,
        Description = c.Description,
        IsScored = c.IsScored,
        IsActive = c.IsActive,
        Version = c.Version,
        ScoringMethodId = c.ScoringMethodId,
        ScoringMethodName = QualityScoringMethods.GetById(c.ScoringMethodId)?.Description ?? "",
        MaxTotalPoints = c.MaxTotalPoints,
        Code = c.Code,
        ValidFrom = c.ValidFrom,
        ValidUntil = c.ValidUntil,
        CreatedAt = c.CreatedAt,
        QuestionCount = c.Questions?.Count ?? 0,
        EvaluationCount = c.Evaluations?.Count ?? 0
    };

    private static QualityQuestionDto MapQuestionToDto(QualityQuestion q) => new()
    {
        Id = q.Id,
        ChecklistId = q.ChecklistId,
        Text = q.Text,
        Order = q.Order,
        ScoringTypeId = q.ScoringTypeId,
        ScoringTypeName = QualityScoringTypes.GetById(q.ScoringTypeId)?.Description ?? "",
        WeightPoints = q.WeightPoints,
        MaxPoints = q.MaxPoints,
        PenaltyTypeId = q.PenaltyTypeId,
        PenaltyTypeName = QualityPenaltyTypes.GetById(q.PenaltyTypeId)?.Description ?? "",
        IsRequired = q.IsRequired,
        HelpText = q.HelpText,
        GroupName = q.GroupName,
        ShowScoreInput = q.ShowScoreInput,
        AllowComment = q.AllowComment,
        IsActive = q.IsActive,
        SubCriteria = q.SubCriteria?.Select(sc => new QualitySubCriteriaDto
        {
            Id = sc.Id,
            QuestionId = sc.QuestionId,
            Description = sc.Description,
            Order = sc.Order,
            WeightPoints = sc.WeightPoints,
            IsActive = sc.IsActive
        }).ToList() ?? new()
    };

    private static QualityEvaluationDto MapEvaluationToDto(QualityEvaluation e) => new()
    {
        Id = e.Id,
        Uid = e.Uid,
        ChecklistId = e.ChecklistId,
        ChecklistName = e.Checklist?.Name ?? "",
        CallRecordId = e.CallRecordId,
        CallRecordNumber = e.CallRecord?.CalleeNumber,
        EvaluatorPersonnelId = e.EvaluatorPersonnelId,
        EvaluatorName = e.EvaluatorPersonnel?.User?.FullName ?? "",
        EvaluatedPersonnelId = e.EvaluatedPersonnelId,
        EvaluatedName = e.EvaluatedPersonnel?.User?.FullName,
        StatusId = e.StatusId,
        StatusName = QualityEvaluationStatuses.GetById(e.StatusId)?.Description ?? "",
        TotalScore = e.TotalScore,
        MaxScore = e.MaxScore,
        ScorePercentage = e.ScorePercentage,
        YellowCardCount = e.YellowCardCount,
        RedCardCount = e.RedCardCount,
        CompletedAt = e.CompletedAt,
        CreatedAt = e.CreatedAt
    };
}
