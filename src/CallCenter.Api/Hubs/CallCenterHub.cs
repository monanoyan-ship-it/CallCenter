using System.Security.Claims;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Hubs;

[Authorize]
public class CallCenterHub : Hub
{
    private readonly AppDbContext _db;

    public CallCenterHub(AppDbContext db)
    {
        _db = db;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != null)
        {
            var user = await _db.Users.FindAsync(userId.Value);
            if (user != null)
            {
                user.Status = AgentStatus.Available;
                await _db.SaveChangesAsync();

                await Clients.All.SendAsync("AgentStatusChanged", new AgentStatusUpdate
                {
                    AgentId = user.Id,
                    AgentName = user.FullName,
                    Status = AgentStatus.Available
                });
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != null)
        {
            var user = await _db.Users.FindAsync(userId.Value);
            if (user != null)
            {
                user.Status = AgentStatus.Offline;
                await _db.SaveChangesAsync();

                await Clients.All.SendAsync("AgentStatusChanged", new AgentStatusUpdate
                {
                    AgentId = user.Id,
                    AgentName = user.FullName,
                    Status = AgentStatus.Offline
                });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdateMyStatus(AgentStatus status)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null) return;

        user.Status = status;
        await _db.SaveChangesAsync();

        await Clients.All.SendAsync("AgentStatusChanged", new AgentStatusUpdate
        {
            AgentId = user.Id,
            AgentName = user.FullName,
            Status = status
        });
    }

    public async Task NotifyIncomingCall(CallNotification notification)
    {
        await Clients.All.SendAsync("IncomingCall", notification);
    }

    public async Task NotifyCallEnded(Guid callId)
    {
        await Clients.All.SendAsync("CallEnded", callId);
    }

    private Guid? GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null ? Guid.Parse(claim) : null;
    }
}
