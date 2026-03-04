using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

/// <summary>
/// Ses kayitlari: bulut config dagitimi, indirme URL'leri.
/// Upload islemi Windows app tarafindan dogrudan bulut'a yapilir.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecordingsController : AuditableControllerBase
{
    public RecordingsController(ServiceFactory factory) : base(factory) { }

    /// <summary>
    /// Musterinin default cloud storage config'ini decrypted dondur.
    /// Windows app bu bilgiyle direkt bulut'a yukler (API proxy yok).
    /// </summary>
    [HttpGet("cloud-config")]
    public async Task<ActionResult<CloudConfigForClientDto>> GetCloudConfig()
    {
        var customerId = CurrentCustomerId;
        if (customerId == null)
            return Unauthorized("CustomerId bulunamadi");

        var service = Factory.CreateCloudStorageService();
        var config = await service.GetConfigForClientAsync(customerId.Value);

        if (config == null)
            return NotFound("Cloud storage yapilandirilmamis");

        return Ok(config);
    }

    /// <summary>
    /// Ses kaydinin gecici indirme URL'ini olustur (30 dakika gecerli).
    /// </summary>
    [HttpGet("{callUid:guid}/url")]
    public async Task<ActionResult<RecordingDownloadUrlDto>> GetDownloadUrl(
        Guid callUid, CancellationToken ct)
    {
        var customerId = CurrentCustomerId;
        if (customerId == null)
            return Unauthorized("CustomerId bulunamadi");

        var service = Factory.CreateCloudStorageService();
        var result = await service.GetCallRecordingUrlAsync(customerId.Value, callUid, ct);

        if (result == null)
            return NotFound("Ses kaydi bulunamadi veya bulut'a yuklenmemis");

        return Ok(result);
    }

    /// <summary>
    /// Musterinin aktif bulut depolama config'i var mi?
    /// </summary>
    [HttpGet("cloud-enabled")]
    public async Task<ActionResult<bool>> IsCloudEnabled()
    {
        var customerId = CurrentCustomerId;
        if (customerId == null)
            return Unauthorized("CustomerId bulunamadi");

        var service = Factory.CreateCloudStorageService();
        var enabled = await service.HasActiveConfigAsync(customerId.Value);
        return Ok(enabled);
    }
}
