using System.Security.Claims;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController : AuditableControllerBase
{
    public AgentsController(ServiceFactory factory) : base(factory) { }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var svc = Factory.CreateAgentService();
        return Ok(await svc.GetAllAsync());
    }

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] int newStatusId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var svc = Factory.CreateAgentService();
        var (success, error) = await svc.UpdateStatusAsync(userId, newStatusId);
        if (!success) return BadRequest(error);

        await AuditCrudAsync("StatusChange", "User", userId.ToString(),
            $"Agent durum degisikligi: StatusId={newStatusId}");

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
        var svc = Factory.CreateAgentService();
        return Ok(await svc.GetMyQueuesAsync(userId, role, customerId));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentAgent()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var svc = Factory.CreateAgentService();
        var result = await svc.GetCurrentAgentAsync(userId);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
