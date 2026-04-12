using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CallsController : AuditableControllerBase
{
    private readonly ICallFactory _callFactory;
    private readonly ICustomerPersonnelEntityService _personnel;

    public CallsController(
        IAuditFactory auditFactory,
        ICallFactory callFactory,
        ICustomerPersonnelEntityService personnel) : base(auditFactory)
    {
        _callFactory = callFactory;
        _personnel = personnel;
    }

    /// <summary>Operatorun kendi gunluk istatistikleri</summary>
    [HttpGet("my-stats")]
    public async Task<IActionResult> GetMyStats()
    {
        return Ok(await _callFactory.GetMyStatsAsync(GetUserId()));
    }

    /// <summary>Arama gecmisi (tamamlanmis aramalar)</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        return Ok(await _callFactory.GetHistoryAsync(GetUserId(), page, pageSize));
    }

    /// <summary>Aktif aramalar (devam eden)</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        return Ok(await _callFactory.GetActiveAsync(GetUserId()));
    }

    /// <summary>Yeni arama baslat (outbound)</summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartCall([FromBody] StartCallRequest request)
    {
        try
        {
            var (id, uid) = await _callFactory.StartCallAsync(GetUserId(), request);

            await AuditCrudAsync("Create", "CallRecord", id.ToString(),
                $"Arama baslatildi: {request.CalleeNumber}");

            return Ok(new { Id = id, Uid = uid });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("arama yetkisi"))
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Aramayi beklet</summary>
    [HttpPut("{callId}/hold")]
    public async Task<IActionResult> HoldCall(int callId)
    {
        var (success, error) = await _callFactory.HoldCallAsync(callId);
        if (!success) return NotFound();

        await AuditCrudAsync("Hold", "CallRecord", callId.ToString(),
            $"Arama bekletildi: ID={callId}");

        return Ok();
    }

    /// <summary>Aramayi sonlandir</summary>
    [HttpPut("{callId}/end")]
    public async Task<IActionResult> EndCall(int callId)
    {
        var (success, error) = await _callFactory.EndCallAsync(callId, GetUserId());
        if (!success) return NotFound();

        await AuditCrudAsync("End", "CallRecord", callId.ToString(),
            $"Arama sonlandirildi: ID={callId}");

        return Ok();
    }

    /// <summary>Aramayi cevapla</summary>
    [HttpPut("{callId}/answer")]
    public async Task<IActionResult> AnswerCall(int callId)
    {
        var (success, error) = await _callFactory.AnswerCallAsync(callId);
        if (!success) return NotFound();

        await AuditCrudAsync("Answer", "CallRecord", callId.ToString(),
            $"Arama cevaplandi: ID={callId}");

        return Ok();
    }

    /// <summary>Kuyrukta bekleyen aramalar</summary>
    [HttpGet("queued")]
    public async Task<IActionResult> GetQueued([FromQuery] int? customerId = null)
    {
        var items = await _callFactory.GetQueuedAsync(customerId);

        // Sistem adminleri bireysel kayitlari goremez, sadece sayi
        if (IsSystemAdmin)
            return Ok(new List<CallNotification>());

        return Ok(items);
    }

    /// <summary>
    /// Gelen arama kaydi olusturur ve uygun agent'a yonlendirir.
    /// </summary>
    [HttpPost("incoming")]
    public async Task<IActionResult> IncomingCall([FromBody] IncomingCallRequest request)
    {
        var result = await _callFactory.IncomingCallAsync(request);

        await AuditCrudAsync("Incoming", "CallRecord", null,
            $"Gelen arama: {request.CallerNumber} -> kuyruk {request.QueueId}");

        return Ok(result);
    }

    /// <summary>Cagri notunu guncelle</summary>
    [HttpPut("{callId}/notes")]
    public async Task<IActionResult> UpdateNotes(int callId, [FromBody] UpdateCallNotesRequest request)
    {
        var (success, error) = await _callFactory.UpdateNotesAsync(callId, request.Notes);
        if (!success) return NotFound(new { message = error });

        await AuditCrudAsync("UpdateNotes", "CallRecord", callId.ToString(),
            $"Cagri notu guncellendi: ID={callId}");

        return Ok();
    }

    /// <summary>
    /// Windows uygulamasindan lokal DB senkronizasyonu.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncPush([FromBody] CallSyncPushRequest request)
    {
        var result = await _callFactory.SyncPushAsync(GetUserId(), request);

        await AuditCrudAsync("Sync", "CallRecord", request.Uid.ToString(),
            $"Cagri sync: {request.CallerNumber} -> {request.CalleeNumber}");

        return Ok(result);
    }

    /// <summary>Belirli bir numaranin arama gecmisini getirir</summary>
    [HttpGet("number-history")]
    public async Task<IActionResult> GetNumberHistory([FromQuery] string number)
    {
        if (string.IsNullOrWhiteSpace(number)) return BadRequest("Numara gerekli.");
        return Ok(await _callFactory.GetNumberHistoryAsync(GetUserId(), number));
    }

    // ─── Callback (Geri Arama) Yonetimi ───

    /// <summary>Cevapsiz bir aramayi operatore geri arama gorevi olarak ata</summary>
    [HttpPost("{id}/assign-callback")]
    [Authorize(Roles = "Admin,Supervisor,CustomerUser")]
    public async Task<IActionResult> AssignCallback(int id, [FromQuery] int assignedToId, [FromBody] string? note)
    {
        var userId = GetUserId();
        var personnel = await _personnel.GetByUserIdAsync(userId);

        // Sadece FirmaAdmin ve EkipLideri ata yapabilir (veya bizim Admin/Supervisor)
        if (!IsSystemAdmin && personnel?.CustomerRoleId == CustomerRoles.Ids.Operator)
            return Forbid();

        var result = await _callFactory.AssignCallbackAsync(id, assignedToId, note, userId);
        if (!result.Success) return BadRequest(result.Error);

        await AuditCrudAsync("AssignCallback", "CallRecord", id.ToString(), $"Geri arama atandi: OperatorId={assignedToId}");
        return Ok();
    }

    /// <summary>Operatore atanmis veya havuza bırakılmıs bekleyen geri arama gorevlerini listeler</summary>
    [HttpGet("pending-callbacks")]
    public async Task<ActionResult<List<PendingCallbackDto>>> GetPendingCallbacks()
    {
        var userId = GetUserId();
        var customerId = CurrentCustomerId;
        if (customerId == null) return Unauthorized();

        var records = await _callFactory.GetPendingCallbacksAsync(userId, customerId.Value);
        return Ok(records);
    }

    /// <summary>Geri aramayi baslat (Durumu InProgress yapar)</summary>
    [HttpPost("{id}/start-callback")]
    public async Task<IActionResult> StartCallback(int id)
    {
        var result = await _callFactory.StartCallbackAsync(id, GetUserId());
        if (!result.Success) return BadRequest(result.Error);
        return Ok();
    }

    /// <summary>Cevapsiz geri aramayi geri al (InProgress → Todo, havuza birak)</summary>
    [HttpPost("{id}/revert-callback")]
    public async Task<IActionResult> RevertCallback(int id)
    {
        var result = await _callFactory.RevertCallbackAsync(id);
        if (!result.Success) return BadRequest(result.Error);
        return Ok();
    }

    private bool IsSystemAdmin => User.IsInRole("Admin") || User.IsInRole("Supervisor");

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
