using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-personnel-prices")]
[Authorize]
public class SlnPersonnelPriceController : ControllerBase
{
    private readonly ISlnPersonnelPriceFactory _factory;

    public SlnPersonnelPriceController(ISlnPersonnelPriceFactory factory) => _factory = factory;

    // ═══ Personel Bazli Fiyatlandirma ═══

    [HttpGet]
    public async Task<ActionResult<List<SlnPersonnelServicePriceDto>>> GetPrices()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetPricesAsync(customerId));
    }

    [HttpPost]
    public async Task<ActionResult<SlnPersonnelServicePriceDto>> CreateOrUpdatePrice([FromBody] SlnPersonnelServicePriceCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.CreateOrUpdatePriceAsync(dto, customerId));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePrice(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeletePriceAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    // ═══ Hasilat Paylasimi ═══

    [HttpGet("revenue-shares")]
    public async Task<ActionResult<List<SlnRevenueShareDto>>> GetRevenueShares()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.GetRevenueSharesAsync(customerId));
    }

    [HttpPost("revenue-shares")]
    public async Task<ActionResult<SlnRevenueShareDto>> SaveRevenueShare([FromBody] SlnRevenueShareCreateDto dto)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        return Ok(await _factory.SaveRevenueShareAsync(dto, customerId));
    }

    [HttpDelete("revenue-shares/{id}")]
    public async Task<ActionResult> DeleteRevenueShare(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();
        var (success, error) = await _factory.DeleteRevenueShareAsync(id, customerId);
        return success ? Ok() : BadRequest(error);
    }

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
}
