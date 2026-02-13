using System.Security.Claims;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CallsController : AuditableControllerBase
{
    public CallsController(ServiceFactory factory) : base(factory) { }

    /// <summary>Arama gecmisi (tamamlanmis aramalar)</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var svc = Factory.CreateCallService();
        return Ok(await svc.GetHistoryAsync(GetUserId(), page, pageSize));
    }

    /// <summary>Aktif aramalar (devam eden)</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var svc = Factory.CreateCallService();
        return Ok(await svc.GetActiveAsync(GetUserId()));
    }

    /// <summary>Yeni arama baslat (outbound)</summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartCall([FromBody] StartCallRequest request)
    {
        var svc = Factory.CreateCallService();
        var (id, uid) = await svc.StartCallAsync(GetUserId(), request);

        await AuditCrudAsync("Create", "CallRecord", id.ToString(),
            $"Arama baslatildi: {request.CalleeNumber}");

        return Ok(new { Id = id, Uid = uid });
    }

    /// <summary>Aramayi beklet</summary>
    [HttpPut("{callId}/hold")]
    public async Task<IActionResult> HoldCall(int callId)
    {
        var svc = Factory.CreateCallService();
        var (success, error) = await svc.HoldCallAsync(callId);
        if (!success) return NotFound();

        await AuditCrudAsync("Hold", "CallRecord", callId.ToString(),
            $"Arama bekletildi: ID={callId}");

        return Ok();
    }

    /// <summary>Aramayi sonlandir</summary>
    [HttpPut("{callId}/end")]
    public async Task<IActionResult> EndCall(int callId)
    {
        var svc = Factory.CreateCallService();
        var (success, error) = await svc.EndCallAsync(callId, GetUserId());
        if (!success) return NotFound();

        await AuditCrudAsync("End", "CallRecord", callId.ToString(),
            $"Arama sonlandirildi: ID={callId}");

        return Ok();
    }

    /// <summary>Aramayi cevapla</summary>
    [HttpPut("{callId}/answer")]
    public async Task<IActionResult> AnswerCall(int callId)
    {
        var svc = Factory.CreateCallService();
        var (success, error) = await svc.AnswerCallAsync(callId);
        if (!success) return NotFound();

        await AuditCrudAsync("Answer", "CallRecord", callId.ToString(),
            $"Arama cevaplandi: ID={callId}");

        return Ok();
    }

    /// <summary>Kuyrukta bekleyen aramalar</summary>
    [HttpGet("queued")]
    public async Task<IActionResult> GetQueued([FromQuery] int? customerId = null)
    {
        var svc = Factory.CreateCallService();
        return Ok(await svc.GetQueuedAsync(customerId));
    }

    /// <summary>
    /// Gelen arama kaydi olusturur ve uygun agent'a yonlendirir.
    /// PBX webhook veya SIP event'i tarafindan cagrilabilir.
    /// </summary>
    [HttpPost("incoming")]
    public async Task<IActionResult> IncomingCall([FromBody] IncomingCallRequest request)
    {
        var svc = Factory.CreateCallService();
        var result = await svc.IncomingCallAsync(request);

        await AuditCrudAsync("Incoming", "CallRecord", null,
            $"Gelen arama: {request.CallerNumber} -> kuyruk {request.QueueId}");

        return Ok(result);
    }

    /// <summary>
    /// Windows uygulamasindan lokal DB senkronizasyonu.
    /// BackgroundSyncService periyodik olarak senkronlanmamis kayitlari buraya push eder.
    /// Uid bazli idempotent: ayni Uid tekrar gelirse gunceller, yeni olusturmaz.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncPush([FromBody] CallSyncPushRequest request)
    {
        var svc = Factory.CreateCallService();
        var result = await svc.SyncPushAsync(GetUserId(), request);

        await AuditCrudAsync("Sync", "CallRecord", request.Uid.ToString(),
            $"Cagri sync: {request.CallerNumber} -> {request.CalleeNumber}");

        return Ok(result);
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
