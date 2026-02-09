using System.Security.Claims;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CallCenter.Api.Hubs;

namespace CallCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<CallCenterHub> _hub;

    public AgentsController(AppDbContext db, IHubContext<CallCenterHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var agents = await _db.Users
            .Where(u => u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.FullName,
                u.RoleId,
                RoleName = UserRoles.GetById(u.RoleId) != null ? UserRoles.GetById(u.RoleId)!.SystemName : "Agent",
                u.StatusId,
                StatusName = AgentStatuses.GetById(u.StatusId) != null ? AgentStatuses.GetById(u.StatusId)!.SystemName : "Offline",
                u.Extension
            })
            .ToListAsync();

        return Ok(agents);
    }

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] int newStatusId)
    {
        var statusItem = AgentStatuses.GetById(newStatusId);
        if (statusItem == null) return BadRequest("Gecersiz durum ID'si.");

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.StatusId = newStatusId;
        await _db.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("AgentStatusChanged", new AgentStatusUpdate
        {
            AgentId = user.Id,
            AgentName = user.FullName,
            StatusId = newStatusId,
            StatusName = statusItem.SystemName
        });

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

        IQueryable<Shared.Entities.Queue> query = _db.Queues.Where(q => q.IsActive);

        if (role is "Admin" or "Supervisor")
        {
            // Admin/Supervisor: tum kuyruklar, customerId filtreli
            if (customerId.HasValue && customerId.Value > 0)
                query = query.Where(q => q.CustomerId == customerId.Value);
        }
        else
        {
            // Agent: sadece kendi atandigi kuyruklar
            query = query.Where(q => q.QueueAgents.Any(qa => qa.AgentId == userId));
        }

        var result = await query
            .OrderBy(q => q.Customer.Name).ThenBy(q => q.Name)
            .Select(q => new MyQueueDto
            {
                Id = q.Id,
                Name = q.Name,
                IsActive = q.IsActive,
                CustomerName = q.Customer.Name,
                AgentCount = q.QueueAgents.Count,
                WaitingCount = q.CallRecords.Count(c => c.StatusId == CallStatuses.Ids.Ringing),
                ActiveCount = q.CallRecords.Count(c => c.StatusId == CallStatuses.Ids.InProgress || c.StatusId == CallStatuses.Ids.OnHold)
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentAgent()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.FullName,
            RoleId = user.RoleId,
            RoleName = UserRoles.GetById(user.RoleId)?.SystemName ?? "Agent",
            StatusId = user.StatusId,
            StatusName = AgentStatuses.GetById(user.StatusId)?.SystemName ?? "Offline",
            user.Extension,
            user.Email
        });
    }
}
