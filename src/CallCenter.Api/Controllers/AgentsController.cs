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
                u.Role,
                u.Status,
                u.Extension
            })
            .ToListAsync();

        return Ok(agents);
    }

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] AgentStatus newStatus)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.Status = newStatus;
        await _db.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("AgentStatusChanged", new AgentStatusUpdate
        {
            AgentId = user.Id,
            AgentName = user.FullName,
            Status = newStatus
        });

        return Ok();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentAgent()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.FullName,
            user.Role,
            user.Status,
            user.Extension,
            user.Email
        });
    }
}
