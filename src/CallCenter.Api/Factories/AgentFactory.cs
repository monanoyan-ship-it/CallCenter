using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Hubs;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

public class AgentFactory : IAgentFactory
{
    private readonly IUserEntityService _users;
    private readonly IQueueEntityService _queues;
    private readonly IHubContext<CallCenterHub> _hub;
    private readonly IUnitOfWork _uow;

    public AgentFactory(IUserEntityService users, IQueueEntityService queues, IHubContext<CallCenterHub> hub, IUnitOfWork uow)
    {
        _users = users;
        _queues = queues;
        _hub = hub;
        _uow = uow;
    }

    public async Task<object> GetAllAsync()
    {
        return await _users.GetAllQueryable()
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
    }

    public async Task<(bool Success, string? Error)> UpdateStatusAsync(int userId, int newStatusId)
    {
        var statusItem = AgentStatuses.GetById(newStatusId);
        if (statusItem == null) return (false, "Gecersiz durum ID'si.");

        var user = await _users.GetByIdAsync(userId);
        if (user == null) return (false, "Kullanici bulunamadi.");

        user.StatusId = newStatusId;
        await _uow.SaveChangesAsync();

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
        IQueryable<Shared.Entities.Queue> query = _queues.GetAllQueryable().Where(q => q.IsActive);

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
        var user = await _users.GetByIdAsync(userId);
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
