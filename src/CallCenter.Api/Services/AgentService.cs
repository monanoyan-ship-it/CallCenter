using CallCenter.Api.Hubs;
using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

public class AgentService : IAgentService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<CallCenterHub> _hub;

    public AgentService(AppDbContext db, IHubContext<CallCenterHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<object> GetAllAsync()
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

        return agents;
    }

    public async Task<(bool Success, string? Error)> UpdateStatusAsync(int userId, int newStatusId)
    {
        var statusItem = AgentStatuses.GetById(newStatusId);
        if (statusItem == null) return (false, "Gecersiz durum ID'si.");

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return (false, "Kullanici bulunamadi.");

        user.StatusId = newStatusId;
        await _db.SaveChangesAsync();

        await _hub.Clients.All.SendAsync("AgentStatusChanged", new AgentStatusUpdate
        {
            AgentId = user.Id,
            AgentName = user.FullName,
            StatusId = newStatusId,
            StatusName = statusItem.SystemName
        });

        return (true, null);
    }

    public async Task<List<MyQueueDto>> GetMyQueuesAsync(int userId, string role, int? customerId)
    {
        IQueryable<Shared.Entities.Queue> query = _db.Queues.Where(q => q.IsActive);

        if (role is "Admin" or "Supervisor")
        {
            if (customerId.HasValue && customerId.Value > 0)
                query = query.Where(q => q.CustomerId == customerId.Value);
        }
        else
        {
            query = query.Where(q => q.QueueAgents.Any(qa => qa.AgentId == userId));
        }

        return await query
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
    }

    public async Task<object?> GetCurrentAgentAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        return new
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
        };
    }
}
