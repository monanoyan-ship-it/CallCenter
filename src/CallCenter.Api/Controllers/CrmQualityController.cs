using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CrmQualityController : AuditableControllerBase
{
    private readonly ICrmQualityFactory _qualityFactory;

    public CrmQualityController(IAuditFactory auditFactory, ICrmQualityFactory qualityFactory)
        : base(auditFactory)
    {
        _qualityFactory = qualityFactory;
    }

    // ══════════════════════════════════════════════════════════════
    // Checklist
    // ══════════════════════════════════════════════════════════════

    /// <summary>Checklist listesi (firma bazli)</summary>
    [HttpGet("checklists")]
    public async Task<IActionResult> GetChecklists()
    {
        return Ok(await _qualityFactory.GetChecklistsAsync(GetCustomerId()));
    }

    /// <summary>Checklist detay + sorular</summary>
    [HttpGet("checklists/{uid:guid}")]
    public async Task<IActionResult> GetChecklist(Guid uid)
    {
        var checklist = await _qualityFactory.GetChecklistByUidAsync(uid, GetCustomerId());
        if (checklist == null) return NotFound();
        return Ok(checklist);
    }

    /// <summary>Yeni checklist olustur</summary>
    [HttpPost("checklists")]
    public async Task<IActionResult> CreateChecklist([FromBody] CreateChecklistRequest req)
    {
        var result = await _qualityFactory.CreateChecklistAsync(req, GetCustomerId());

        await AuditCrudAsync("Create", "CrmQualityChecklist", result.Uid.ToString(),
            $"Kalite checklist olusturuldu: {req.Name}");

        return Ok(result);
    }

    /// <summary>Checklist guncelle</summary>
    [HttpPut("checklists/{uid:guid}")]
    public async Task<IActionResult> UpdateChecklist(Guid uid, [FromBody] UpdateChecklistRequest req)
    {
        var (success, error) = await _qualityFactory.UpdateChecklistAsync(uid, req, GetCustomerId());
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "CrmQualityChecklist", uid.ToString(),
            "Kalite checklist guncellendi");

        return Ok();
    }

    /// <summary>Checklist sil</summary>
    [HttpDelete("checklists/{uid:guid}")]
    public async Task<IActionResult> DeleteChecklist(Guid uid)
    {
        var (success, error) = await _qualityFactory.DeleteChecklistAsync(uid, GetCustomerId());
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Delete", "CrmQualityChecklist", uid.ToString(),
            "Kalite checklist silindi");

        return Ok();
    }

    // ══════════════════════════════════════════════════════════════
    // Question
    // ══════════════════════════════════════════════════════════════

    /// <summary>Checklist'e soru ekle</summary>
    [HttpPost("checklists/{uid:guid}/questions")]
    public async Task<IActionResult> AddQuestion(Guid uid, [FromBody] CreateQuestionRequest req)
    {
        var (success, error, question) = await _qualityFactory.AddQuestionAsync(uid, req, GetCustomerId());
        if (!success) return BadRequest(new { message = error });
        return Ok(question);
    }

    /// <summary>Soru guncelle</summary>
    [HttpPut("questions/{id:int}")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] UpdateQuestionRequest req)
    {
        var (success, error) = await _qualityFactory.UpdateQuestionAsync(id, req, GetCustomerId());
        if (!success) return BadRequest(new { message = error });
        return Ok();
    }

    /// <summary>Soru sil</summary>
    [HttpDelete("questions/{id:int}")]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var (success, error) = await _qualityFactory.DeleteQuestionAsync(id, GetCustomerId());
        if (!success) return BadRequest(new { message = error });
        return Ok();
    }

    /// <summary>Soru siralamasini guncelle</summary>
    [HttpPut("checklists/{uid:guid}/questions/reorder")]
    public async Task<IActionResult> ReorderQuestions(Guid uid, [FromBody] List<int> questionIds)
    {
        var (success, error) = await _qualityFactory.ReorderQuestionsAsync(uid, questionIds, GetCustomerId());
        if (!success) return BadRequest(new { message = error });
        return Ok();
    }

    // ══════════════════════════════════════════════════════════════
    // Evaluation
    // ══════════════════════════════════════════════════════════════

    /// <summary>Degerlendirme listesi (filtreleme destekli)</summary>
    [HttpGet("evaluations")]
    public async Task<IActionResult> GetEvaluations(
        [FromQuery] int? evaluatorId = null,
        [FromQuery] int? evaluatedId = null,
        [FromQuery] int? statusId = null)
    {
        return Ok(await _qualityFactory.GetEvaluationsAsync(
            GetCustomerId(), evaluatorId, evaluatedId, statusId));
    }

    /// <summary>Degerlendirme detay + cevaplar</summary>
    [HttpGet("evaluations/{uid:guid}")]
    public async Task<IActionResult> GetEvaluation(Guid uid)
    {
        var evaluation = await _qualityFactory.GetEvaluationByUidAsync(uid, GetCustomerId());
        if (evaluation == null) return NotFound();
        return Ok(evaluation);
    }

    /// <summary>Yeni degerlendirme baslat (draft olarak olustur)</summary>
    [HttpPost("evaluations")]
    public async Task<IActionResult> CreateEvaluation([FromBody] CreateEvaluationRequest req)
    {
        var personnelId = GetPersonnelId();
        if (personnelId == null) return Forbid();

        var (success, error, evaluation) = await _qualityFactory.CreateEvaluationAsync(
            req, personnelId.Value, GetCustomerId());

        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Create", "CrmQualityEvaluation", evaluation!.Uid.ToString(),
            $"Kalite degerlendirmesi baslatildi");

        return Ok(evaluation);
    }

    /// <summary>Degerlendirmeyi taslak olarak kaydet (puan hesaplanmaz, raporlara yansimaz)</summary>
    [HttpPost("evaluations/{uid:guid}/save-draft")]
    public async Task<IActionResult> SaveDraft(Guid uid, [FromBody] SubmitEvaluationRequest req)
    {
        var (success, error) = await _qualityFactory.SaveDraftAsync(uid, req, GetCustomerId());
        if (!success) return BadRequest(new { message = error });
        return Ok();
    }

    /// <summary>Degerlendirmeyi tamamla (puanlari hesapla, raporlara yansit)</summary>
    [HttpPost("evaluations/{uid:guid}/submit")]
    public async Task<IActionResult> SubmitEvaluation(Guid uid, [FromBody] SubmitEvaluationRequest req)
    {
        var (success, error) = await _qualityFactory.SubmitEvaluationAsync(uid, req, GetCustomerId());
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "CrmQualityEvaluation", uid.ToString(),
            "Kalite degerlendirmesi tamamlandi");

        return Ok();
    }

    /// <summary>Degerlendirmeyi iptal et</summary>
    [HttpPost("evaluations/{uid:guid}/cancel")]
    public async Task<IActionResult> CancelEvaluation(Guid uid)
    {
        var (success, error) = await _qualityFactory.CancelEvaluationAsync(uid, GetCustomerId());
        if (!success) return BadRequest(new { message = error });

        await AuditCrudAsync("Update", "CrmQualityEvaluation", uid.ToString(),
            "Kalite degerlendirmesi iptal edildi");

        return Ok();
    }

    // ══════════════════════════════════════════════════════════════
    // Score Threshold
    // ══════════════════════════════════════════════════════════════

    /// <summary>Puan esik degerlerini getir</summary>
    [HttpGet("thresholds")]
    public async Task<IActionResult> GetThresholds()
    {
        return Ok(await _qualityFactory.GetThresholdsAsync(GetCustomerId()));
    }

    /// <summary>Puan esik degeri olustur/guncelle</summary>
    [HttpPost("thresholds")]
    public async Task<IActionResult> UpsertThreshold([FromBody] UpsertScoreThresholdRequest req)
    {
        var (success, error) = await _qualityFactory.UpsertThresholdAsync(req, GetCustomerId());
        if (!success) return BadRequest(new { message = error });
        return Ok();
    }

    /// <summary>Puan esik degeri sil</summary>
    [HttpDelete("thresholds/{id:int}")]
    public async Task<IActionResult> DeleteThreshold(int id)
    {
        var (success, error) = await _qualityFactory.DeleteThresholdAsync(id, GetCustomerId());
        if (!success) return BadRequest(new { message = error });
        return Ok();
    }

    // ══════════════════════════════════════════════════════════════
    // Dashboard
    // ══════════════════════════════════════════════════════════════

    /// <summary>Kalite dashboard (ozet istatistikler)</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        return Ok(await _qualityFactory.GetDashboardAsync(GetCustomerId(), from, to));
    }

    // ─── Helpers ───

    private int? GetCustomerId()
    {
        var claim = User.FindFirstValue("CustomerId");
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    private int? GetPersonnelId()
    {
        var claim = User.FindFirstValue("CustomerPersonnelId");
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
