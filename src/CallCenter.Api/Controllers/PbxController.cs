using CallCenter.Api.Factories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

/// <summary>
/// PbxService icin ozel API endpoint'leri.
/// PbxService bu endpoint'ler uzerinden tum veriyi alir.
/// Auth: PbxService icin API key veya service-to-service auth (simdilik AllowAnonymous).
/// </summary>
[ApiController]
[Route("api/pbx")]
public class PbxController : ControllerBase
{
    private readonly IPbxFactory _pbxFactory;

    public PbxController(IPbxFactory pbxFactory)
    {
        _pbxFactory = pbxFactory;
    }

    // ─── Konfigürasyon ───

    /// <summary>Musteri SIP trunk bilgileri (sifre cozulmus)</summary>
    [HttpGet("trunks")]
    public async Task<IActionResult> GetTrunks([FromQuery] string customerUid)
    {
        if (string.IsNullOrEmpty(customerUid))
            return BadRequest("customerUid gerekli");

        var result = await _pbxFactory.GetTrunksAsync(customerUid);
        return Ok(result.Trunks);
    }

    /// <summary>Musteri PBX yapilandirmasi</summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig([FromQuery] string customerUid)
    {
        if (string.IsNullOrEmpty(customerUid))
            return BadRequest("customerUid gerekli");

        var result = await _pbxFactory.GetCustomerConfigAsync(customerUid);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Mesai saatleri ve tatiller</summary>
    [HttpGet("business-hours")]
    public async Task<IActionResult> GetBusinessHours([FromQuery] string customerUid)
    {
        if (string.IsNullOrEmpty(customerUid))
            return BadRequest("customerUid gerekli");

        var result = await _pbxFactory.GetBusinessHoursAsync(customerUid);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>IVR menu detayi (seçeneklerle birlikte)</summary>
    [HttpGet("ivr/{menuId:int}")]
    public async Task<IActionResult> GetIvrMenu(int menuId, [FromQuery] string customerUid)
    {
        if (string.IsNullOrEmpty(customerUid))
            return BadRequest("customerUid gerekli");

        var result = await _pbxFactory.GetIvrMenuAsync(menuId, customerUid);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // ─── Kuyruk ───

    /// <summary>Kuyruk yapilandirmasi (bekleme muzigi, timeout vb.)</summary>
    [HttpGet("queues/{queueId:int}/config")]
    public async Task<IActionResult> GetQueueConfig(int queueId)
    {
        var result = await _pbxFactory.GetQueueConfigAsync(queueId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Kuyrukta musait agent bul (ACD)</summary>
    [HttpGet("queues/{queueId:int}/available-agent")]
    public async Task<IActionResult> GetAvailableAgent(int queueId)
    {
        var result = await _pbxFactory.GetAvailableAgentAsync(queueId);
        if (result == null) return NoContent();
        return Ok(result);
    }

    // ─── Çağrı Yönetimi ───

    /// <summary>Gelen cagri kaydi olustur</summary>
    [HttpPost("calls/incoming")]
    public async Task<IActionResult> CreateIncomingCall([FromBody] PbxIncomingCallRequest request)
    {
        var result = await _pbxFactory.CreateIncomingCallAsync(request);
        if (result == null) return BadRequest("Cagri kaydi olusturulamadi");
        return Ok(result);
    }

    /// <summary>Cagri kaydi guncelle</summary>
    [HttpPut("calls/{callRecordId:int}")]
    public async Task<IActionResult> UpdateCallRecord(int callRecordId, [FromBody] PbxCallUpdateRequest request)
    {
        var success = await _pbxFactory.UpdateCallRecordAsync(callRecordId, request);
        if (!success) return NotFound();
        return Ok();
    }

    // ─── Ses Dosyası ───

    /// <summary>Ses dosyasi (karsilama, bekleme muzigi vb.)</summary>
    [HttpGet("audio/{greetingMessageId:int}")]
    public async Task<IActionResult> GetAudio(int greetingMessageId)
    {
        var result = await _pbxFactory.GetAudioFileAsync(greetingMessageId);
        if (result == null || result.Value.Data == null)
            return NotFound();

        return File(result.Value.Data, result.Value.ContentType ?? "audio/wav");
    }
}
