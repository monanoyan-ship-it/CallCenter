using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<int, GatewayHealthUpdate> _gatewayStates = new();

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

                // Gateway durumunu "kayit disi" olarak guncelle
                if (_gatewayStates.TryGetValue(user.Id, out var gwState))
                {
                    gwState.IsRegistered = false;
                    gwState.ErrorMessage = "Agent baglantisi kesildi";
                    gwState.Timestamp = DateTime.UtcNow;
                    await SendToGroupAndAdminsAsync(groupName, "GatewayHealthChanged", gwState);
                }
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
    // SUPERVISOR DASHBOARD
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tum agent durumlarini getirir (Dashboard ilk yukleme).
    /// Supervisor/Admin rolundeki kullanicilar icin.
    /// </summary>
    public async Task<List<AgentStatusDto>> GetAllAgentStatuses()
    {
        var userId = GetUserId();
        if (userId == null) return new();

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null || (user.RoleId != UserRoles.Ids.Admin && user.RoleId != UserRoles.Ids.Supervisor))
            return new();

        var users = await _db.Users
            .Where(u => u.IsActive && u.RoleId == UserRoles.Ids.Agent)
            .Select(u => new { u.Id, u.FullName, u.Extension, u.RoleId, u.StatusId })
            .ToListAsync();

        var agents = users.Select(u =>
        {
            var role = UserRoles.GetById(u.RoleId);
            var status = AgentStatuses.GetById(u.StatusId);
            return new AgentStatusDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Extension = u.Extension,
                RoleId = u.RoleId,
                RoleName = role?.SystemName ?? "",
                StatusId = u.StatusId,
                StatusName = status?.SystemName ?? "",
                StatusCss = status?.CssClass ?? "",
                StatusIcon = status?.Icon ?? ""
            };
        }).ToList();

        return agents;
    }

    // ═══════════════════════════════════════════════════════════════
    // CONFERENCE (Konferans Odasi)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Konferans odasina katilim bildirimi (SignalR grup).</summary>
    public async Task JoinConferenceRoom(int roomId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var groupName = $"conference_{roomId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        await Clients.Group(groupName).SendAsync("ConferenceParticipantJoined", new
        {
            RoomId = roomId,
            UserId = userId.Value,
            JoinedAt = DateTime.UtcNow
        });
    }

    /// <summary>Konferans odasindan ayrilma bildirimi.</summary>
    public async Task LeaveConferenceRoom(int roomId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var groupName = $"conference_{roomId}";

        await Clients.Group(groupName).SendAsync("ConferenceParticipantLeft", new
        {
            RoomId = roomId,
            UserId = userId.Value,
            LeftAt = DateTime.UtcNow
        });

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    // ═══════════════════════════════════════════════════════════════
    // SIP PRESENCE SENKRONIZASYONU
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// SIP Presence bildirimi geldiginde (Windows client'tan) agent durumunu gunceller.
    /// Windows client SIP NOTIFY aldiginda bu metodu cagirir.
    /// </summary>
    public async Task UpdatePresenceFromSip(string sipUri, string presenceStatus)
    {
        var userId = GetUserId();
        if (userId == null) return;

        // SIP presence → AgentStatuses eslestirmesi
        int agentStatusId = SipPresenceStatuses.ToAgentStatusId(presenceStatus);

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null) return;

        var groupName = await GetGroupNameAsync(userId.Value);

        user.StatusId = agentStatusId;
        await _db.SaveChangesAsync();

        var statusItem = AgentStatuses.GetById(agentStatusId);
        var statusUpdate = new AgentStatusUpdate
        {
            AgentId = user.Id,
            AgentName = user.FullName,
            StatusId = agentStatusId,
            StatusName = statusItem?.SystemName ?? "Unknown"
        };

        await SendToGroupAndAdminsAsync(groupName, "AgentStatusChanged", statusUpdate);

        // SIP presence bilgisini de ayrica yayinla (BLF kullanan client'lar icin)
        await SendToGroupAndAdminsAsync(groupName, "SipPresenceChanged", new
        {
            UserId = userId.Value,
            SipUri = sipUri,
            PresenceStatus = presenceStatus,
            AgentStatusId = agentStatusId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Belirli bir kullanicinin SIP presence bilgisini sorgular.
    /// BLF (Busy Lamp Field) icin.
    /// </summary>
    public async Task<object?> GetUserPresence(int targetUserId)
    {
        var user = await _db.Users.FindAsync(targetUserId);
        if (user == null) return null;

        var sipPresence = SipPresenceStatuses.FromAgentStatus(user.StatusId);
        return new
        {
            UserId = user.Id,
            FullName = user.FullName,
            Extension = user.Extension,
            AgentStatusId = user.StatusId,
            SipPresenceStatus = sipPresence.SystemName,
            SipPresenceDescription = sipPresence.Description
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // INSTANT MESSAGING (Anlik Mesajlasma)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Anlık mesaj gonder — SignalR ile real-time delivery.
    /// Client tarafinda mesaj gonderildiginde bu metot cagirilir.
    /// </summary>
    public async Task SendInstantMessage(int receiverUserId, string content)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var sender = await _db.Users.FindAsync(userId.Value);
        if (sender == null) return;

        // Mesaji aliciya ve gonderene real-time ilet
        var messageEvent = new
        {
            SenderUserId = userId.Value,
            SenderName = sender.FullName,
            ReceiverUserId = receiverUserId,
            Content = content,
            SentAt = DateTime.UtcNow
        };

        // Aliciya bildir
        await Clients.User(receiverUserId.ToString()).SendAsync("NewInstantMessage", messageEvent);

        // Gonderene de onay (baska cihazda aciksa senkron)
        await Clients.User(userId.Value.ToString()).SendAsync("InstantMessageSent", messageEvent);
    }

    /// <summary>Mesaj okundu bildirimi (typing indicator benzeri)</summary>
    public async Task NotifyMessageRead(int senderUserId, int messageId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var notification = new MessageReadNotification
        {
            MessageId = messageId,
            ReadByUserId = userId.Value,
            ReadAt = DateTime.UtcNow
        };

        await Clients.User(senderUserId.ToString()).SendAsync("MessageRead", notification);
    }

    /// <summary>Yazıyor bildirimi (typing indicator)</summary>
    public async Task NotifyTyping(int receiverUserId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        await Clients.User(receiverUserId.ToString()).SendAsync("UserTyping", new
        {
            UserId = userId.Value,
            Timestamp = DateTime.UtcNow
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // GATEWAY HEALTH (SIP Gateway Saglik Monitoru)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Windows client SIP register/unregister oldugunda bu metodu cagirir.
    /// AgentId ve AgentName JWT'den doldurulur (client tarafindan gonderilenleri override eder).
    /// </summary>
    public async Task UpdateGatewayHealth(GatewayHealthUpdate update)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null) return;

        // JWT'den doldur — client manipulasyonunu engelle
        update.AgentId = user.Id;
        update.AgentName = user.FullName;
        update.Timestamp = DateTime.UtcNow;

        // In-memory state guncelle
        _gatewayStates[user.Id] = update;

        var groupName = await GetGroupNameAsync(userId.Value);
        await SendToGroupAndAdminsAsync(groupName, "GatewayHealthChanged", update);
    }

    /// <summary>
    /// Sayfa acildiginda mevcut gateway durumlarini doner (Supervisor/Admin only).
    /// </summary>
    public List<GatewayHealthUpdate> GetAllGatewayStatuses()
    {
        var userId = GetUserId();
        if (userId == null) return new();

        var roleClaim = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (roleClaim != "Admin" && roleClaim != "Supervisor")
            return new();

        return _gatewayStates.Values.ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // MONITORING (Arama Izleme)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Arama izleme baslatildiginda agent ve supervisor'lara bildirim gonderir.
    /// NOT: Gercek ses izleme medya sunucusu gerektirir — bu sadece durum bildirimi.
    /// </summary>
    public async Task NotifyMonitoringStarted(int callId, int supervisorId, int modeId)
    {
        var supervisor = await _db.Users.FindAsync(supervisorId);
        var modeName = MonitoringModes.GetById(modeId)?.SystemName ?? "Unknown";

        var notification = new
        {
            CallId = callId,
            SupervisorId = supervisorId,
            SupervisorName = supervisor?.FullName ?? "",
            ModeId = modeId,
            ModeName = modeName,
            StartedAt = DateTime.UtcNow
        };

        // Agent'a bildir (izlendigini bilmeli — Silent modda bile kayit var)
        var call = await _db.CallRecords.FindAsync(callId);
        if (call?.AgentId != null)
        {
            await Clients.User(call.AgentId.Value.ToString())
                .SendAsync("MonitoringStarted", notification);
        }

        // Admin grubuna bildir
        await Clients.Group("admins").SendAsync("MonitoringStarted", notification);
    }

    /// <summary>Arama izleme durduruldugunda bildirim.</summary>
    public async Task NotifyMonitoringStopped(int callId, int supervisorId)
    {
        var notification = new
        {
            CallId = callId,
            SupervisorId = supervisorId,
            StoppedAt = DateTime.UtcNow
        };

        var call = await _db.CallRecords.FindAsync(callId);
        if (call?.AgentId != null)
        {
            await Clients.User(call.AgentId.Value.ToString())
                .SendAsync("MonitoringStopped", notification);
        }

        await Clients.Group("admins").SendAsync("MonitoringStopped", notification);
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
