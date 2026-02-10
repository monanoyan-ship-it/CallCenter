using System.Security.Claims;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController : ControllerBase
{
    private readonly ServiceFactory _factory;

    public AgentsController(ServiceFactory factory)
    {
        _factory = factory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var svc = _factory.CreateAgentService();
        return Ok(await svc.GetAllAsync());
    }

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] int newStatusId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var svc = _factory.CreateAgentService();
        var (success, error) = await svc.UpdateStatusAsync(userId, newStatusId);
        if (!success) return BadRequest(error);
        return Ok();
    }

    /// <summary>
    /// Agent kendi kuyruklarini gorur. Admin/Supervisor tum kuyruklari (customerId filtreli) gorur.
    /// </summary>
    [HttpGet("my/queues")]
    public async Task<ActionResult<List<MyQueueDto>>> GetMyQueues([FromQuery] int? customerId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var svc = _factory.CreateAgentService();
        return Ok(await svc.GetMyQueuesAsync(userId, role, customerId));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentAgent()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var svc = _factory.CreateAgentService();
        var result = await svc.GetCurrentAgentAsync(userId);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
