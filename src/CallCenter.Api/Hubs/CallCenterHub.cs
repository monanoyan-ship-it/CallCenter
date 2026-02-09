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
                user.StatusId = AgentStatuses.Ids.Available;
                await _db.SaveChangesAsync();

                await Clients.All.SendAsync("AgentStatusChanged", new AgentStatusUpdate
                {
                    AgentId = user.Id,
                    AgentName = user.FullName,
                    StatusId = AgentStatuses.Ids.Available,
                    StatusName = AgentStatuses.Available.SystemName
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
                user.StatusId = AgentStatuses.Ids.Offline;
                await _db.SaveChangesAsync();

                await Clients.All.SendAsync("AgentStatusChanged", new AgentStatusUpdate
                {
                    AgentId = user.Id,
                    AgentName = user.FullName,
                    StatusId = AgentStatuses.Ids.Offline,
                    StatusName = AgentStatuses.Offline.SystemName
                });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdateMyStatus(int statusId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var statusItem = AgentStatuses.GetById(statusId);
        if (statusItem == null) return;

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null) return;

        user.StatusId = statusId;
        await _db.SaveChangesAsync();

        await Clients.All.SendAsync("AgentStatusChanged", new AgentStatusUpdate
        {
            AgentId = user.Id,
            AgentName = user.FullName,
            StatusId = statusId,
            StatusName = statusItem.SystemName
        });
    }

    public async Task NotifyIncomingCall(CallNotification notification)
    {
        await Clients.All.SendAsync("IncomingCall", notification);
    }

    public async Task NotifyCallEnded(int callId)
    {
        await Clients.All.SendAsync("CallEnded", callId);
    }

    /// <summary>Belirli bir agent'a gelen arama bildirimi gönderir.</summary>
    public async Task NotifySpecificAgent(int agentId, CallNotification notification)
    {
        await Clients.User(agentId.ToString()).SendAsync("IncomingCall", notification);
    }

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null ? int.Parse(claim) : null;
    }
}
