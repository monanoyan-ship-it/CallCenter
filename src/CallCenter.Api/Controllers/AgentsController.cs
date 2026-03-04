using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController : AuditableControllerBase
{
    private readonly IAgentFactory _agentFactory;

    public AgentsController(IAuditFactory auditFactory, IAgentFactory agentFactory) : base(auditFactory)
    {
        _agentFactory = agentFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _agentFactory.GetAllAsync());
    }

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] int newStatusId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _agentFactory.UpdateStatusAsync(userId, newStatusId);
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
        return Ok(await _agentFactory.GetMyQueuesAsync(userId, role, customerId));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentAgent()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _agentFactory.GetCurrentAgentAsync(userId);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
