using CallCenter.Api.EntityServices;
using CallCenter.Api.Factories;
using CallCenter.Api.Infrastructure;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Tests.Factories;

public class QualityFactoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly QualityFactory _sut;

    public QualityFactoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        var checklistEs = new QualityChecklistEntityService(_db);
        var questionEs = new QualityQuestionEntityService(_db);
        var subCriteriaEs = new QualitySubCriteriaEntityService(_db);
        var evaluationEs = new QualityEvaluationEntityService(_db);
        var answerEs = new QualityAnswerEntityService(_db);
        var selectionEs = new QualityAnswerSubCriteriaSelectionEntityService(_db);
        var thresholdEs = new QualityScoreThresholdEntityService(_db);
        var callRecordEs = new CallRecordEntityService(_db);
        var personnelEs = new CustomerPersonnelEntityService(_db);
        var uow = new UnitOfWork(_db);

        _sut = new QualityFactory(
            checklistEs, questionEs, subCriteriaEs,
            evaluationEs, answerEs, selectionEs,
            thresholdEs, callRecordEs, personnelEs, uow);

        SeedBaseData();
    }

    public void Dispose() => _db.Dispose();

    // ═══════════════════════════════════════════════════════════════
    // SEED DATA
    // ═══════════════════════════════════════════════════════════════

    private const int CustomerId = 1;
    private const int EvaluatorPersonnelId = 1;
    private const int EvaluatedPersonnelId = 2;

    private void SeedBaseData()
    {
        var customer = new Customer
        {
            Id = CustomerId, Uid = Guid.NewGuid(), Name = "Test Firma",
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);

        var user1 = new User
        {
            Id = 10, Uid = Guid.NewGuid(), UserName = "evaluator",
            FullName = "Kalite Uzmani", Email = "kalite@test.local",
            PasswordHash = "$2a$11$test", RoleId = UserRoles.Ids.Supervisor,
            StatusId = AgentStatuses.Ids.Available, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        var user2 = new User
        {
            Id = 11, Uid = Guid.NewGuid(), UserName = "agent1",
            FullName = "Agent Bir", Email = "agent1@test.local",
            PasswordHash = "$2a$11$test", Extension = "1001",
            RoleId = UserRoles.Ids.Agent, StatusId = AgentStatuses.Ids.Available,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.Users.AddRange(user1, user2);

        _db.CustomerPersonnel.AddRange(
            new CustomerPersonnel { Id = EvaluatorPersonnelId, CustomerId = CustomerId, UserId = 10, IsActive = true, CreatedAt = DateTime.UtcNow },
            new CustomerPersonnel { Id = EvaluatedPersonnelId, CustomerId = CustomerId, UserId = 11, IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        _db.CallRecords.Add(new CallRecord
        {
            Id = 100, Uid = Guid.NewGuid(), CallerNumber = "05551112233",
            CalleeNumber = "02121234567", DirectionId = CallDirections.Ids.Inbound,
            StatusId = CallStatuses.Ids.Completed, StartedAt = DateTime.UtcNow.AddHours(-1),
            AnsweredAt = DateTime.UtcNow.AddHours(-1).AddSeconds(5),
            EndedAt = DateTime.UtcNow.AddMinutes(-30), DurationSeconds = 1800, AgentId = 11
        });

        _db.SaveChanges();
    }

    // ═══════════════════════════════════════════════════════════════
    // CHECKLIST CRUD
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateChecklist_ValidRequest_CreatesSuccessfully()
    {
        var req = new CreateChecklistRequest
        {
            Name = "Genel Kalite Formu",
            Description = "Temel kalite kontrol formu",
            ScoringMethodId = QualityScoringMethods.Ids.Maximum,
            MaxTotalPoints = 100,
            IsScored = true
        };

        var result = await _sut.CreateChecklistAsync(req, CustomerId);

        result.Should().NotBeNull();
        result.Name.Should().Be("Genel Kalite Formu");
        result.ScoringMethodId.Should().Be(QualityScoringMethods.Ids.Maximum);
        result.MaxTotalPoints.Should().Be(100);
        result.QuestionCount.Should().Be(0);
        result.Uid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetChecklists_ReturnsCustomerChecklists()
    {
        await CreateTestChecklist("Form A");
        await CreateTestChecklist("Form B");

        var result = await _sut.GetChecklistsAsync(CustomerId);

        result.Should().HaveCount(2);
        result.Select(c => c.Name).Should().Contain("Form A").And.Contain("Form B");
    }

    [Fact]
    public async Task GetChecklists_OtherCustomer_ReturnsEmpty()
    {
        await CreateTestChecklist("Form A");

        var result = await _sut.GetChecklistsAsync(999);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateChecklist_ChangesFields()
    {
        var cl = await CreateTestChecklist("Eski Ad");

        var (success, error) = await _sut.UpdateChecklistAsync(cl.Uid, new UpdateChecklistRequest
        {
            Name = "Yeni Ad",
            MaxTotalPoints = 200
        }, CustomerId);

        success.Should().BeTrue();
        error.Should().BeNull();

        var updated = await _sut.GetChecklistByUidAsync(cl.Uid, CustomerId);
        updated!.Name.Should().Be("Yeni Ad");
        updated.MaxTotalPoints.Should().Be(200);
    }

    [Fact]
    public async Task DeleteChecklist_RemovesSuccessfully()
    {
        var cl = await CreateTestChecklist("Silinecek");

        var (success, error) = await _sut.DeleteChecklistAsync(cl.Uid, CustomerId);

        success.Should().BeTrue();
        var list = await _sut.GetChecklistsAsync(CustomerId);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteChecklist_OtherCustomer_Fails()
    {
        var cl = await CreateTestChecklist("Baska Firma");

        var (success, error) = await _sut.DeleteChecklistAsync(cl.Uid, 999);

        success.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════
    // QUESTION MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddQuestion_IncreasesQuestionCount()
    {
        var cl = await CreateTestChecklist("Form");

        var (success, error, question) = await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Musteri selamlandi mi?",
            WeightPoints = 10,
            MaxPoints = 5,
            ScoringTypeId = QualityScoringTypes.Ids.Scored
        }, CustomerId);

        success.Should().BeTrue();
        question.Should().NotBeNull();
        question!.Text.Should().Be("Musteri selamlandi mi?");

        var detail = await _sut.GetChecklistByUidAsync(cl.Uid, CustomerId);
        detail!.Questions.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddQuestion_AutoAssignsOrder()
    {
        var cl = await CreateTestChecklist("Form");

        await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest { Text = "Soru 1", WeightPoints = 10, MaxPoints = 5 }, CustomerId);
        await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest { Text = "Soru 2", WeightPoints = 10, MaxPoints = 5 }, CustomerId);
        await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest { Text = "Soru 3", WeightPoints = 10, MaxPoints = 5 }, CustomerId);

        var detail = await _sut.GetChecklistByUidAsync(cl.Uid, CustomerId);
        detail!.Questions.Should().HaveCount(3);
        detail.Questions.Select(q => q.Order).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task AddQuestion_WithSubCriteria_CreatesSubCriteria()
    {
        var cl = await CreateTestChecklist("Form");

        var (success, _, question) = await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Iletisim kalitesi",
            WeightPoints = 20,
            MaxPoints = 5,
            ScoringTypeId = QualityScoringTypes.Ids.Scored,
            SubCriteria = new List<CreateSubCriteriaRequest>
            {
                new() { Description = "Ses tonu uygun", WeightPoints = 5 },
                new() { Description = "Net konusma", WeightPoints = 5 }
            }
        }, CustomerId);

        success.Should().BeTrue();
        question!.SubCriteria.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateQuestion_ChangesFields()
    {
        var cl = await CreateTestChecklist("Form");
        var (_, _, q) = await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Eski soru", WeightPoints = 10, MaxPoints = 5
        }, CustomerId);

        var (success, _) = await _sut.UpdateQuestionAsync(q!.Id, new UpdateQuestionRequest
        {
            Text = "Yeni soru",
            WeightPoints = 20
        }, CustomerId);

        success.Should().BeTrue();
        var detail = await _sut.GetChecklistByUidAsync(cl.Uid, CustomerId);
        detail!.Questions.First().Text.Should().Be("Yeni soru");
        detail.Questions.First().WeightPoints.Should().Be(20);
    }

    [Fact]
    public async Task DeleteQuestion_RemovesFromChecklist()
    {
        var cl = await CreateTestChecklist("Form");
        var (_, _, q) = await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Silinecek", WeightPoints = 10, MaxPoints = 5
        }, CustomerId);

        var (success, _) = await _sut.DeleteQuestionAsync(q!.Id, CustomerId);

        success.Should().BeTrue();
        var detail = await _sut.GetChecklistByUidAsync(cl.Uid, CustomerId);
        detail!.Questions.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════
    // EVALUATION WORKFLOW: Create → SaveDraft → Submit
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateEvaluation_CreatesAsDraft()
    {
        var cl = await CreateTestChecklistWithQuestions();

        var (success, error, ev) = await _sut.CreateEvaluationAsync(new CreateEvaluationRequest
        {
            ChecklistId = cl.Id,
            CallRecordId = 100,
            EvaluatedPersonnelId = EvaluatedPersonnelId
        }, EvaluatorPersonnelId, CustomerId);

        success.Should().BeTrue();
        ev.Should().NotBeNull();
        ev!.StatusId.Should().Be(QualityEvaluationStatuses.Ids.Draft);
        ev.TotalScore.Should().BeNull();
        ev.ScorePercentage.Should().BeNull();
    }

    [Fact]
    public async Task SaveDraft_SavesAnswersWithoutScoring()
    {
        var (ev, cl) = await CreateTestEvaluation();

        var answers = cl.Questions.Select(q => new SubmitAnswerRequest
        {
            QuestionId = q.Id,
            GivenPoints = 3,
            Notes = "Test notu"
        }).ToList();

        var (success, error) = await _sut.SaveDraftAsync(ev.Uid, new SubmitEvaluationRequest
        {
            Answers = answers,
            EvaluationComment = "Taslak yorum"
        }, CustomerId);

        success.Should().BeTrue();

        // Draft sonrasi puan hesaplanmamis olmali
        var detail = await _sut.GetEvaluationByUidAsync(ev.Uid, CustomerId);
        detail.Should().NotBeNull();
        detail!.StatusId.Should().Be(QualityEvaluationStatuses.Ids.Draft);
        detail.TotalScore.Should().BeNull();
        detail.Answers.Should().HaveCount(cl.Questions.Count);
    }

    [Fact]
    public async Task SaveDraft_MultipleTimes_ReplacesAnswers()
    {
        var (ev, cl) = await CreateTestEvaluation();
        var questionId = cl.Questions.First().Id;

        // Ilk draft
        await _sut.SaveDraftAsync(ev.Uid, new SubmitEvaluationRequest
        {
            Answers = new List<SubmitAnswerRequest>
            {
                new() { QuestionId = questionId, GivenPoints = 2, Notes = "Ilk" }
            }
        }, CustomerId);

        // Ikinci draft - cevabi guncelle
        await _sut.SaveDraftAsync(ev.Uid, new SubmitEvaluationRequest
        {
            Answers = new List<SubmitAnswerRequest>
            {
                new() { QuestionId = questionId, GivenPoints = 4, Notes = "Guncellendi" }
            }
        }, CustomerId);

        var detail = await _sut.GetEvaluationByUidAsync(ev.Uid, CustomerId);
        detail!.Answers.Should().HaveCount(1); // Duplike degil
        detail.Answers.First().GivenPoints.Should().Be(4);
        detail.Answers.First().Notes.Should().Be("Guncellendi");
    }

    [Fact]
    public async Task SubmitEvaluation_CalculatesScores_Maximum()
    {
        var (ev, cl) = await CreateTestEvaluation();

        var answers = cl.Questions.Select(q => new SubmitAnswerRequest
        {
            QuestionId = q.Id,
            GivenPoints = 4 // 4/5 * 10 = 8 puan per soru
        }).ToList();

        var (success, error) = await _sut.SubmitEvaluationAsync(ev.Uid, new SubmitEvaluationRequest
        {
            Answers = answers,
            EvaluationComment = "Final degerlendirme"
        }, CustomerId);

        success.Should().BeTrue();

        var detail = await _sut.GetEvaluationByUidAsync(ev.Uid, CustomerId);
        detail!.StatusId.Should().Be(QualityEvaluationStatuses.Ids.Completed);
        detail.TotalScore.Should().BeGreaterThan(0);
        detail.MaxScore.Should().BeGreaterThan(0);
        detail.ScorePercentage.Should().BeGreaterThan(0);
        detail.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitEvaluation_MaximumMethod_CorrectCalculation()
    {
        // Maximum: EarnedPoints = (GivenPoints / MaxPoints) * WeightPoints
        var cl = await CreateTestChecklist("Form", QualityScoringMethods.Ids.Maximum);

        // 1 soru: MaxPoints=5, WeightPoints=10
        await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Soru", WeightPoints = 10, MaxPoints = 5,
            ScoringTypeId = QualityScoringTypes.Ids.Scored
        }, CustomerId);

        var updatedCl = await _sut.GetChecklistByUidAsync(cl.Uid, CustomerId);
        var questionId = updatedCl!.Questions.First().Id;

        var (_, _, ev) = await _sut.CreateEvaluationAsync(new CreateEvaluationRequest
        {
            ChecklistId = cl.Id, CallRecordId = 100
        }, EvaluatorPersonnelId, CustomerId);

        // GivenPoints=4, MaxPoints=5, Weight=10 -> Earned = (4/5)*10 = 8
        await _sut.SubmitEvaluationAsync(ev!.Uid, new SubmitEvaluationRequest
        {
            Answers = new List<SubmitAnswerRequest>
            {
                new() { QuestionId = questionId, GivenPoints = 4 }
            }
        }, CustomerId);

        var detail = await _sut.GetEvaluationByUidAsync(ev.Uid, CustomerId);
        detail!.TotalScore.Should().Be(8m);
        detail.MaxScore.Should().Be(10m);
        detail.ScorePercentage.Should().Be(80m);
    }

    [Fact]
    public async Task SubmitEvaluation_NotApplicable_ExcludedFromScore()
    {
        var cl = await CreateTestChecklist("Form");
        await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Soru 1", WeightPoints = 10, MaxPoints = 5
        }, CustomerId);
        await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Soru 2", WeightPoints = 10, MaxPoints = 5
        }, CustomerId);

        var updatedCl = await _sut.GetChecklistByUidAsync(cl.Uid, CustomerId);
        var q1 = updatedCl!.Questions[0];
        var q2 = updatedCl.Questions[1];

        var (_, _, ev) = await _sut.CreateEvaluationAsync(new CreateEvaluationRequest
        {
            ChecklistId = cl.Id, CallRecordId = 100
        }, EvaluatorPersonnelId, CustomerId);

        await _sut.SubmitEvaluationAsync(ev!.Uid, new SubmitEvaluationRequest
        {
            Answers = new List<SubmitAnswerRequest>
            {
                new() { QuestionId = q1.Id, GivenPoints = 5 },           // Tam puan: 10
                new() { QuestionId = q2.Id, IsNotApplicable = true }      // N/A - skordan haric
            }
        }, CustomerId);

        var detail = await _sut.GetEvaluationByUidAsync(ev.Uid, CustomerId);
        detail!.TotalScore.Should().Be(10m);
        // MaxScore sadece applicable sorulari icermeli: 10 (q1)
        detail.MaxScore.Should().Be(10m);
        detail.ScorePercentage.Should().Be(100m);
    }

    [Fact]
    public async Task SubmitEvaluation_PenaltyCards_Counted()
    {
        var cl = await CreateTestChecklist("Form");
        await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Hakaret kontrol", WeightPoints = 0, MaxPoints = 1,
            ScoringTypeId = QualityScoringTypes.Ids.Penalty,
            PenaltyTypeId = QualityPenaltyTypes.Ids.RedCard
        }, CustomerId);
        await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Yanlis bilgi", WeightPoints = 0, MaxPoints = 1,
            ScoringTypeId = QualityScoringTypes.Ids.Penalty,
            PenaltyTypeId = QualityPenaltyTypes.Ids.YellowCard
        }, CustomerId);

        var updatedCl = await _sut.GetChecklistByUidAsync(cl.Uid, CustomerId);
        var qRed = updatedCl!.Questions.First(q => q.Text == "Hakaret kontrol");
        var qYellow = updatedCl.Questions.First(q => q.Text == "Yanlis bilgi");

        var (_, _, ev) = await _sut.CreateEvaluationAsync(new CreateEvaluationRequest
        {
            ChecklistId = cl.Id, CallRecordId = 100
        }, EvaluatorPersonnelId, CustomerId);

        await _sut.SubmitEvaluationAsync(ev!.Uid, new SubmitEvaluationRequest
        {
            Answers = new List<SubmitAnswerRequest>
            {
                new() { QuestionId = qRed.Id, IsPenaltyApplied = true, AppliedPenaltyTypeId = QualityPenaltyTypes.Ids.RedCard },
                new() { QuestionId = qYellow.Id, IsPenaltyApplied = true, AppliedPenaltyTypeId = QualityPenaltyTypes.Ids.YellowCard }
            }
        }, CustomerId);

        var detail = await _sut.GetEvaluationByUidAsync(ev.Uid, CustomerId);
        detail!.RedCardCount.Should().Be(1);
        detail.YellowCardCount.Should().Be(1);
    }

    [Fact]
    public async Task CancelEvaluation_ChangesStatus()
    {
        var (ev, _) = await CreateTestEvaluation();

        var (success, _) = await _sut.CancelEvaluationAsync(ev.Uid, CustomerId);

        success.Should().BeTrue();
        var detail = await _sut.GetEvaluationByUidAsync(ev.Uid, CustomerId);
        detail!.StatusId.Should().Be(QualityEvaluationStatuses.Ids.Cancelled);
    }

    [Fact]
    public async Task SubmitEvaluation_AlreadyCompleted_Fails()
    {
        var (ev, cl) = await CreateTestEvaluation();

        // Submit once
        await _sut.SubmitEvaluationAsync(ev.Uid, new SubmitEvaluationRequest
        {
            Answers = cl.Questions.Select(q => new SubmitAnswerRequest
            {
                QuestionId = q.Id, GivenPoints = 3
            }).ToList()
        }, CustomerId);

        // Submit again should fail
        var (success, error) = await _sut.SubmitEvaluationAsync(ev.Uid, new SubmitEvaluationRequest
        {
            Answers = cl.Questions.Select(q => new SubmitAnswerRequest
            {
                QuestionId = q.Id, GivenPoints = 5
            }).ToList()
        }, CustomerId);

        success.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════
    // SCORE THRESHOLDS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpsertThreshold_CreatesNew()
    {
        var (success, _) = await _sut.UpsertThresholdAsync(new UpsertScoreThresholdRequest
        {
            SuccessThreshold = 80,
            WarningThreshold = 60
        }, CustomerId);

        success.Should().BeTrue();

        var thresholds = await _sut.GetThresholdsAsync(CustomerId);
        thresholds.Should().HaveCount(1);
        thresholds.First().SuccessThreshold.Should().Be(80);
        thresholds.First().WarningThreshold.Should().Be(60);
    }

    [Fact]
    public async Task UpsertThreshold_UpdatesExisting()
    {
        await _sut.UpsertThresholdAsync(new UpsertScoreThresholdRequest
        {
            SuccessThreshold = 80, WarningThreshold = 60
        }, CustomerId);

        await _sut.UpsertThresholdAsync(new UpsertScoreThresholdRequest
        {
            SuccessThreshold = 90, WarningThreshold = 70
        }, CustomerId);

        var thresholds = await _sut.GetThresholdsAsync(CustomerId);
        thresholds.Should().HaveCount(1); // Upsert - duplike degil
        thresholds.First().SuccessThreshold.Should().Be(90);
    }

    [Fact]
    public async Task DeleteThreshold_RemovesSuccessfully()
    {
        await _sut.UpsertThresholdAsync(new UpsertScoreThresholdRequest
        {
            SuccessThreshold = 80, WarningThreshold = 60
        }, CustomerId);

        var thresholds = await _sut.GetThresholdsAsync(CustomerId);
        var id = thresholds.First().Id;

        var (success, _) = await _sut.DeleteThresholdAsync(id, CustomerId);

        success.Should().BeTrue();
        (await _sut.GetThresholdsAsync(CustomerId)).Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════
    // DASHBOARD
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDashboard_EmptyDb_ReturnsZeros()
    {
        var result = await _sut.GetDashboardAsync(CustomerId);

        result.TotalEvaluations.Should().Be(0);
        result.CompletedEvaluations.Should().Be(0);
        result.AverageScore.Should().BeNull();
        result.YellowCardTotal.Should().Be(0);
        result.RedCardTotal.Should().Be(0);
    }

    [Fact]
    public async Task GetDashboard_OnlyCompletedInStats()
    {
        var (ev, cl) = await CreateTestEvaluation();

        // Draft kaydet — dashboard a yansimamali
        await _sut.SaveDraftAsync(ev.Uid, new SubmitEvaluationRequest
        {
            Answers = cl.Questions.Select(q => new SubmitAnswerRequest
            {
                QuestionId = q.Id, GivenPoints = 5
            }).ToList()
        }, CustomerId);

        var result = await _sut.GetDashboardAsync(CustomerId);
        result.CompletedEvaluations.Should().Be(0);
        result.AverageScore.Should().BeNull();

        // Simdi submit et
        await _sut.SubmitEvaluationAsync(ev.Uid, new SubmitEvaluationRequest
        {
            Answers = cl.Questions.Select(q => new SubmitAnswerRequest
            {
                QuestionId = q.Id, GivenPoints = 5
            }).ToList()
        }, CustomerId);

        var resultAfter = await _sut.GetDashboardAsync(CustomerId);
        resultAfter.CompletedEvaluations.Should().Be(1);
        resultAfter.AverageScore.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private async Task<QualityChecklistDto> CreateTestChecklist(string name = "Test Form", int scoringMethod = 1)
    {
        return await _sut.CreateChecklistAsync(new CreateChecklistRequest
        {
            Name = name,
            ScoringMethodId = scoringMethod,
            MaxTotalPoints = 100,
            IsScored = true
        }, CustomerId);
    }

    private async Task<QualityChecklistDetailDto> CreateTestChecklistWithQuestions()
    {
        var cl = await CreateTestChecklist();
        await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Musteri selamlandi mi?", WeightPoints = 10, MaxPoints = 5,
            ScoringTypeId = QualityScoringTypes.Ids.Scored
        }, CustomerId);
        await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Sorun cozuldu mu?", WeightPoints = 15, MaxPoints = 5,
            ScoringTypeId = QualityScoringTypes.Ids.Scored
        }, CustomerId);
        await _sut.AddQuestionAsync(cl.Uid, new CreateQuestionRequest
        {
            Text = "Kapanista veda edildi mi?", WeightPoints = 5, MaxPoints = 5,
            ScoringTypeId = QualityScoringTypes.Ids.Scored
        }, CustomerId);

        return (await _sut.GetChecklistByUidAsync(cl.Uid, CustomerId))!;
    }

    private async Task<(QualityEvaluationDto Evaluation, QualityChecklistDetailDto Checklist)> CreateTestEvaluation()
    {
        var cl = await CreateTestChecklistWithQuestions();
        var (_, _, ev) = await _sut.CreateEvaluationAsync(new CreateEvaluationRequest
        {
            ChecklistId = cl.Id,
            CallRecordId = 100,
            EvaluatedPersonnelId = EvaluatedPersonnelId
        }, EvaluatorPersonnelId, CustomerId);

        return (ev!, cl);
    }
}
