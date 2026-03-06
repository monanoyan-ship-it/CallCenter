using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CallsController : AuditableControllerBase
{
    private readonly ICallFactory _callFactory;

    public CallsController(IAuditFactory auditFactory, ICallFactory callFactory) : base(auditFactory)
    {
        _callFactory = callFactory;
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

    private bool IsSystemAdmin => User.IsInRole("Admin") || User.IsInRole("Supervisor");

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
