using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/sln-module-requests")]
[Authorize]
public class SlnModuleRequestController : ControllerBase
{
    private readonly IModuleRequestFactory _factory;

    public SlnModuleRequestController(IModuleRequestFactory factory) => _factory = factory;

    /// <summary>Firmanin modul talepleri</summary>
    [HttpGet]
    public async Task<IActionResult> GetRequests()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var result = await _factory.GetCustomerRequestsAsync(customerId);
        return Ok(result);
    }

    /// <summary>Firmanin aktif modulleri (fiyat bilgisi ile)</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveModules()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var factory = HttpContext.RequestServices.GetRequiredService<ICustomerFactory>();
        var modules = await factory.GetCustomerModulesAsync(customerId);
        return Ok(modules);
    }

    /// <summary>Talep edilebilir moduller (aktif olmayan, pending talep olmayan)</summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableModules()
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        var result = await _factory.GetAvailableModulesAsync(customerId);
        return Ok(result);
    }

    /// <summary>Yeni modul talebi olustur</summary>
    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateModuleRequestDto dto)
    {
        var customerId = GetCustomerId();
        var personnelId = GetPersonnelId();
        if (customerId == 0) return Unauthorized();

        try
        {
            var result = await _factory.CreateRequestAsync(customerId, personnelId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Talep iptal et (sadece Pending)</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelRequest(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == 0) return Unauthorized();

        try
        {
            await _factory.CancelRequestAsync(id, customerId);
            return Ok(new { success = true });
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or UnauthorizedAccessException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Talep onayla (Admin)</summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveRequest(int id, [FromBody] ReviewModuleRequestDto? dto)
    {
        var userId = GetUserId();
        try
        {
            var result = await _factory.ApproveRequestAsync(id, userId, dto?.Notes);
            return Ok(result);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Talep reddet (Admin)</summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectRequest(int id, [FromBody] ReviewModuleRequestDto? dto)
    {
        var userId = GetUserId();
        try
        {
            var result = await _factory.RejectRequestAsync(id, userId, dto?.Notes);
            return Ok(result);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int GetUserId()
        => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private int GetCustomerId()
        => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

    private int GetPersonnelId()
        => int.Parse(User.FindFirst("CustomerPersonnelId")?.Value ?? "0");
}
