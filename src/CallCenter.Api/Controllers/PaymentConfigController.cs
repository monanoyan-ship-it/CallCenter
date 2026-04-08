using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/payment-config")]
[Authorize(Roles = "Admin")]
public class PaymentConfigController : ControllerBase
{
    private readonly IPaymentConfigFactory _factory;

    public PaymentConfigController(IPaymentConfigFactory factory) => _factory = factory;

    /// <summary>Tum odeme yapilandirmalarini listele</summary>
    [HttpGet]
    public async Task<ActionResult<List<PaymentConfigListDto>>> GetAll()
    {
        return Ok(await _factory.GetAllAsync());
    }

    /// <summary>Yapilandirma detayi (credential'lar maskelenmis)</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentConfigDetailDto>> GetById(int id)
    {
        var dto = await _factory.GetByIdAsync(id);
        if (dto == null) return NotFound(new { message = "Yapilandirma bulunamadi." });
        return Ok(dto);
    }

    /// <summary>Aktif yapilandirma</summary>
    [HttpGet("active")]
    public async Task<ActionResult<PaymentConfigDetailDto>> GetActive()
    {
        var dto = await _factory.GetActiveAsync();
        if (dto == null) return NotFound(new { message = "Aktif odeme yapilandirmasi bulunamadi." });
        return Ok(dto);
    }

    /// <summary>Havale banka bilgileri (Salon/Public erisimi icin AllowAnonymous olabilir)</summary>
    [HttpGet("bank-info")]
    [AllowAnonymous]
    public async Task<ActionResult<PaymentBankInfoDto>> GetBankInfo()
    {
        var dto = await _factory.GetBankInfoAsync();
        if (dto == null) return NotFound(new { message = "Banka bilgisi tanimlanmamis." });
        return Ok(dto);
    }

    /// <summary>Desteklenen odeme provider'lari</summary>
    [HttpGet("providers")]
    public ActionResult<List<PaymentProviderInfoDto>> GetProviders()
    {
        return Ok(_factory.GetAvailableProviders());
    }

    /// <summary>Yeni yapilandirma olustur</summary>
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] PaymentConfigSaveDto dto)
    {
        var (success, id, error) = await _factory.CreateAsync(dto);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { id, message = "Odeme yapilandirmasi olusturuldu." });
    }

    /// <summary>Yapilandirma guncelle</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] PaymentConfigSaveDto dto)
    {
        var (success, error) = await _factory.UpdateAsync(id, dto);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Yapilandirma guncellendi." });
    }

    /// <summary>Yapilandirma sil</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var (success, error) = await _factory.DeleteAsync(id);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Yapilandirma silindi." });
    }

    /// <summary>Baglanti testi</summary>
    [HttpPost("{id:int}/test")]
    public async Task<ActionResult<PaymentConfigTestResultDto>> TestConnection(int id, CancellationToken ct)
    {
        var result = await _factory.TestConnectionAsync(id, ct);
        return Ok(result);
    }
}
