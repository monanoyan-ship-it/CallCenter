using System.Security.Claims;
using CallCenter.Api.Services;
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
    private readonly CallDistributionService _distribution;

    public CallCenterHub(AppDbContext db, CallDistributionService distribution)
    {
        _db = db;
        _distribution = distribution;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != null)
        {
            var user = await _db.Users.FindAsync(userId.Value);
            if (user != null)
            {
                // Kullaniciyi musteri grubuna ekle
                var groupName = await GetGroupNameAsync(userId.Value);
                if (groupName != null)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                }

                // Admin'leri ayrica admin grubuna ekle (tum musterileri gorebilmeli)
                if (user.RoleId == UserRoles.Ids.Admin)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
                }

                user.StatusId = AgentStatuses.Ids.Available;
                await _db.SaveChangesAsync();

                var statusUpdate = new AgentStatusUpdate
                {
                    AgentId = user.Id,
                    AgentName = user.FullName,
                    StatusId = AgentStatuses.Ids.Available,
                    StatusName = AgentStatuses.Available.SystemName
                };

                // Grup + Admin'lere bildir
                await SendToGroupAndAdminsAsync(groupName, "AgentStatusChanged", statusUpdate);
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
                var groupName = await GetGroupNameAsync(userId.Value);

                user.StatusId = AgentStatuses.Ids.Offline;
                await _db.SaveChangesAsync();

                var statusUpdate = new AgentStatusUpdate
                {
                    AgentId = user.Id,
                    AgentName = user.FullName,
                    StatusId = AgentStatuses.Ids.Offline,
                    StatusName = AgentStatuses.Offline.SystemName
                };

                await SendToGroupAndAdminsAsync(groupName, "AgentStatusChanged", statusUpdate);
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

        var groupName = await GetGroupNameAsync(userId.Value);

        user.StatusId = statusId;
        await _db.SaveChangesAsync();

        var statusUpdate = new AgentStatusUpdate
        {
            AgentId = user.Id,
            AgentName = user.FullName,
            StatusId = statusId,
            StatusName = statusItem.SystemName
        };

        await SendToGroupAndAdminsAsync(groupName, "AgentStatusChanged", statusUpdate);

        // Agent musait oldugunda kuyrukta bekleyen aramalar varsa otomatik ata
        if (statusId == AgentStatuses.Ids.Available)
        {
            await TryAssignQueuedCallAsync(userId.Value);
        }
    }

    /// <summary>
    /// Kuyrukta bekleyen en eski aramayi bu agent'a atar.
    /// Agent'in bagli oldugu kuyruklardan birinde bekleyen varsa atanir.
    /// </summary>
    private async Task TryAssignQueuedCallAsync(int agentId)
    {
        // Agent'in bagli oldugu kuyruklari bul
        var agentQueueIds = await _db.QueueAgents
            .Where(qa => qa.AgentId == agentId)
            .Select(qa => qa.QueueId)
            .ToListAsync();

        if (!agentQueueIds.Any()) return;

        // Bu kuyrukta bekleyen en eski aramayi bul
        var queuedCall = await _db.CallRecords
            .Where(c => c.StatusId == CallStatuses.Ids.Queued)
            .Where(c => c.QueueId.HasValue && agentQueueIds.Contains(c.QueueId.Value))
            .OrderBy(c => c.StartedAt)
            .FirstOrDefaultAsync();

        if (queuedCall == null) return;

        // Aramayi bu agent'a ata
        queuedCall.AgentId = agentId;
        queuedCall.StatusId = CallStatuses.Ids.Ringing;
        await _db.SaveChangesAsync();

        // Agent'a bildirim gonder
        var notification = new CallNotification
        {
            CallId = queuedCall.Id,
            CallerNumber = queuedCall.CallerNumber,
            CalleeNumber = queuedCall.CalleeNumber,
            DirectionId = queuedCall.DirectionId,
            StatusId = CallStatuses.Ids.Ringing,
            QueueName = queuedCall.Queue?.Name
        };

        await Clients.User(agentId.ToString()).SendAsync("IncomingCall", notification);
    }

    public async Task NotifyIncomingCall(CallNotification notification)
    {
        var groupName = GetCallerGroupName();
        await SendToGroupAndAdminsAsync(groupName, "IncomingCall", notification);
    }

    public async Task NotifyCallEnded(int callId)
    {
        var groupName = GetCallerGroupName();
        await SendToGroupAndAdminsAsync(groupName, "CallEnded", callId);
    }

    /// <summary>Belirli bir agent'a gelen arama bildirimi gonderir.</summary>
    public async Task NotifySpecificAgent(int agentId, CallNotification notification)
    {
        await Clients.User(agentId.ToString()).SendAsync("IncomingCall", notification);
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPER METODLAR
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Musteri grubuna ve admin grubuna ayni anda mesaj gonderir.
    /// Admin'ler her musterinin mesajlarini gorebilir.
    /// </summary>
    private async Task SendToGroupAndAdminsAsync(string? groupName, string method, object arg)
    {
        if (groupName != null)
        {
            await Clients.Group(groupName).SendAsync(method, arg);
        }
        // Admin'ler her zaman gorur (musteri grubunda degilse de)
        await Clients.Group("admins").SendAsync(method, arg);
    }

    /// <summary>
    /// Kullanicinin CustomerId'sine gore musteri grup adini olusturur.
    /// CustomerPersonnel tablosu uzerinden CustomerId bulunur.
    /// Admin'lerin CustomerId'si yoksa null doner.
    /// </summary>
    private async Task<string?> GetGroupNameAsync(int userId)
    {
        // Oncelikle JWT claim'den CustomerId dene
        var customerIdClaim = Context.User?.FindFirst("CustomerId")?.Value;
        if (!string.IsNullOrEmpty(customerIdClaim))
        {
            return $"customer_{customerIdClaim}";
        }

        // CustomerPersonnel tablosundan bul
        var customerId = await _db.CustomerPersonnel
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.CustomerId)
            .FirstOrDefaultAsync();

        return customerId > 0 ? $"customer_{customerId}" : null;
    }

    /// <summary>Mevcut baglantinin CustomerId claim'inden grup adini alir.</summary>
    private string? GetCallerGroupName()
    {
        var customerIdClaim = Context.User?.FindFirst("CustomerId")?.Value;
        return !string.IsNullOrEmpty(customerIdClaim) ? $"customer_{customerIdClaim}" : null;
    }

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null ? int.Parse(claim) : null;
    }
}
