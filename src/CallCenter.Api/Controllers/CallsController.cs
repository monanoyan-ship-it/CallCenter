using System.Security.Claims;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CallsController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public CallsController(ServiceFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Arama gecmisi (tamamlanmis aramalar)</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var svc = _factory.CreateCallService();
        return Ok(await svc.GetHistoryAsync(GetUserId(), page, pageSize));
    }

    /// <summary>Aktif aramalar (devam eden)</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var svc = _factory.CreateCallService();
        return Ok(await svc.GetActiveAsync(GetUserId()));
    }

    /// <summary>Yeni arama baslat (outbound)</summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartCall([FromBody] StartCallRequest request)
    {
        var svc = _factory.CreateCallService();
        var (id, uid) = await svc.StartCallAsync(GetUserId(), request);
        return Ok(new { Id = id, Uid = uid });
    }

    /// <summary>Aramayi beklet</summary>
    [HttpPut("{callId}/hold")]
    public async Task<IActionResult> HoldCall(int callId)
    {
        var svc = _factory.CreateCallService();
        var (success, error) = await svc.HoldCallAsync(callId);
        if (!success) return NotFound();
        return Ok();
    }

    /// <summary>Aramayi sonlandir</summary>
    [HttpPut("{callId}/end")]
    public async Task<IActionResult> EndCall(int callId)
    {
        var svc = _factory.CreateCallService();
        var (success, error) = await svc.EndCallAsync(callId, GetUserId());
        if (!success) return NotFound();
        return Ok();
    }

    /// <summary>Aramayi cevapla</summary>
    [HttpPut("{callId}/answer")]
    public async Task<IActionResult> AnswerCall(int callId)
    {
        var svc = _factory.CreateCallService();
        var (success, error) = await svc.AnswerCallAsync(callId);
        if (!success) return NotFound();
        return Ok();
    }

    /// <summary>Kuyrukta bekleyen aramalar</summary>
    [HttpGet("queued")]
    public async Task<IActionResult> GetQueued([FromQuery] int? customerId = null)
    {
        var svc = _factory.CreateCallService();
        return Ok(await svc.GetQueuedAsync(customerId));
    }

    /// <summary>
    /// Gelen arama kaydı oluşturur ve uygun agent'a yönlendirir.
    /// PBX webhook veya SIP event'i tarafından çağrılabilir.
    /// </summary>
    [HttpPost("incoming")]
    public async Task<IActionResult> IncomingCall([FromBody] IncomingCallRequest request)
    {
        var svc = _factory.CreateCallService();
        return Ok(await svc.IncomingCallAsync(request));
    }

    /// <summary>
    /// Windows uygulamasindan lokal DB senkronizasyonu.
    /// BackgroundSyncService periyodik olarak senkronlanmamis kayitlari buraya push eder.
    /// Uid bazli idempotent: ayni Uid tekrar gelirse gunceller, yeni olusturmaz.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncPush([FromBody] CallSyncPushRequest request)
    {
        var svc = _factory.CreateCallService();
        var result = await svc.SyncPushAsync(GetUserId(), request);
        return Ok(result);
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
