using System.Collections.Concurrent;
using System.Security.Claims;
using CallCenter.Api.Factories.Interfaces;
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
    private static readonly ConcurrentDictionary<int, int> _userConnectionCounts = new();

    private readonly AppDbContext _db;
    private readonly CallDistributionService _distribution;
    private readonly ISipAccountFactory _sipFactory;

    public CallCenterHub(AppDbContext db, CallDistributionService distribution, ISipAccountFactory sipFactory)
    {
        _db = db;
        _distribution = distribution;
        _sipFactory = sipFactory;
    }

    public static bool HasActiveConnection(int userId)
        => _userConnectionCounts.TryGetValue(userId, out var count) && count > 0;

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != null)
        {
            var user = await _db.Users.FindAsync(userId.Value);
            if (user != null)
            {
                _userConnectionCounts.AddOrUpdate(user.Id, 1, (_, count) => count + 1);

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

                // Dashboard KPI guncelle (musait temsilci sayisi degisti)
                await BroadcastKpiUpdateAsync(groupName);
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
                var remainingConnections = _userConnectionCounts.AddOrUpdate(user.Id, 0, (_, count) => Math.Max(0, count - 1));
                if (remainingConnections == 0)
                    _userConnectionCounts.TryRemove(user.Id, out _);

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

                // Dashboard KPI guncelle (musait temsilci sayisi degisti)
                await BroadcastKpiUpdateAsync(groupName);

                // Dinamik hat tahsisini serbest birak
                var personnelIdClaim = Context.User?.FindFirst("CustomerPersonnelId")?.Value;
                if (!string.IsNullOrEmpty(personnelIdClaim) && int.TryParse(personnelIdClaim, out var personnelId))
                {
                    try { await _sipFactory.ReleaseLineAsync(personnelId); }
                    catch { /* best effort */ }
                }

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

        // Dashboard KPI guncelle
        await BroadcastKpiUpdateAsync(groupName);

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
        if (!await CanAccessTargetUserAsync(agentId)) return;

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

        // Agent ve CustomerUser rollerini dahil et (portal'dan olusturulan operatorler CustomerUser rolunde)
        var usersQuery = _db.Users
            .Where(u => u.IsActive && (u.RoleId == UserRoles.Ids.Agent || u.RoleId == UserRoles.Ids.CustomerUser))
            .AsQueryable();

        if (user.RoleId != UserRoles.Ids.Admin)
        {
            var scope = await GetCurrentUserScopeAsync(userId.Value);
            if (scope == null) return new();

            var scopedUserIds = _db.CustomerPersonnel
                .Where(cp => cp.IsActive && cp.CustomerId == scope.Value.CustomerId);

            if (scope.Value.BranchId.HasValue)
            {
                var branchId = scope.Value.BranchId.Value;
                scopedUserIds = scopedUserIds.Where(cp => cp.BranchId == branchId);
            }

            usersQuery = usersQuery.Where(u => scopedUserIds.Select(cp => cp.UserId).Contains(u.Id));
        }

        var users = await usersQuery
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

        // Dashboard KPI guncelle
        await BroadcastKpiUpdateAsync(groupName);

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
        if (!await CanAccessTargetUserAsync(targetUserId)) return null;

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
        if (!await CanAccessTargetUserAsync(receiverUserId)) return;

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
        if (!await CanAccessTargetUserAsync(senderUserId)) return;

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
        if (!await CanAccessTargetUserAsync(receiverUserId)) return;

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
    public async Task<List<GatewayHealthUpdate>> GetAllGatewayStatuses()
    {
        var userId = GetUserId();
        if (userId == null) return new();

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null || (user.RoleId != UserRoles.Ids.Admin && user.RoleId != UserRoles.Ids.Supervisor))
            return new();

        if (user.RoleId == UserRoles.Ids.Admin)
            return _gatewayStates.Values.ToList();

        var result = new List<GatewayHealthUpdate>();
        foreach (var state in _gatewayStates.Values)
        {
            if (await CanAccessTargetUserAsync(state.AgentId))
                result.Add(state);
        }

        return result;
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

    private static int? GetCustomerIdFromGroupName(string? groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName) || !groupName.StartsWith("customer_", StringComparison.Ordinal))
            return null;

        return int.TryParse(groupName["customer_".Length..], out var customerId) && customerId > 0
            ? customerId
            : null;
    }

    private int? GetClaimInt(string claimType)
    {
        var value = Context.User?.FindFirst(claimType)?.Value;
        return int.TryParse(value, out var id) && id > 0 ? id : null;
    }

    private async Task<UserScope?> GetCurrentUserScopeAsync(int userId)
    {
        var customerId = GetClaimInt("CustomerId");
        if (customerId.HasValue)
            return new UserScope(customerId.Value, GetClaimInt("BranchId"));

        return await GetUserScopeFromDbAsync(userId);
    }

    private async Task<UserScope?> GetUserScopeFromDbAsync(int userId)
    {
        var scope = await _db.CustomerPersonnel
            .AsNoTracking()
            .Where(cp => cp.IsActive && cp.UserId == userId)
            .Select(cp => new { cp.CustomerId, cp.BranchId })
            .FirstOrDefaultAsync();

        return scope == null ? null : new UserScope(scope.CustomerId, scope.BranchId);
    }

    private async Task<bool> CanAccessTargetUserAsync(int targetUserId)
    {
        var currentUserId = GetUserId();
        if (currentUserId == null) return false;
        if (currentUserId.Value == targetUserId) return true;

        var currentUser = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == currentUserId.Value)
            .Select(u => new { u.RoleId })
            .FirstOrDefaultAsync();

        if (currentUser == null) return false;
        if (currentUser.RoleId == UserRoles.Ids.Admin) return true;

        var currentScope = await GetCurrentUserScopeAsync(currentUserId.Value);
        if (currentScope == null) return false;

        var targetScope = await GetUserScopeFromDbAsync(targetUserId);
        if (targetScope == null) return false;
        if (currentScope.Value.CustomerId != targetScope.Value.CustomerId) return false;

        return !currentScope.Value.BranchId.HasValue ||
               targetScope.Value.BranchId == currentScope.Value.BranchId;
    }

    /// <summary>
    /// Agent durum degisikliklerinde Dashboard KPI guncellemesini broadcast eder.
    /// </summary>
    private async Task BroadcastKpiUpdateAsync(string? groupName)
    {
        try
        {
            var activeStatusIds = CallStatuses.ActiveStatuses.Select(s => s.Id).ToList();
            var customerId = GetCustomerIdFromGroupName(groupName);

            var callQuery = _db.CallRecords.AsQueryable();
            var userQuery = _db.Users
                .Where(u => u.IsActive && (u.RoleId == UserRoles.Ids.Agent || u.RoleId == UserRoles.Ids.CustomerUser));

            if (customerId.HasValue)
            {
                var scopedCustomerId = customerId.Value;
                var scopedUserIds = _db.CustomerPersonnel
                    .Where(cp => cp.IsActive && cp.CustomerId == scopedCustomerId)
                    .Select(cp => cp.UserId);

                callQuery = callQuery.Where(c => c.CustomerId == scopedCustomerId);
                userQuery = userQuery.Where(u => scopedUserIds.Contains(u.Id));
            }

            var activeCallCount = await callQuery
                .Where(c => activeStatusIds.Contains(c.StatusId))
                .CountAsync();

            var queueWaitingCount = await callQuery
                .Where(c => c.StatusId == CallStatuses.Ids.Queued || c.StatusId == CallStatuses.Ids.Ringing)
                .CountAsync();

            var availableAgentCount = await userQuery
                .Where(u => u.StatusId == AgentStatuses.Ids.Available)
                .CountAsync();

            var today = DateTime.UtcNow.Date;
            var todayCalls = await callQuery
                .Where(c => c.StartedAt >= today)
                .Select(c => c.StatusId)
                .ToListAsync();

            var kpi = new DashboardKpiUpdate
            {
                ActiveCallCount = activeCallCount,
                AvailableAgentCount = availableAgentCount,
                QueueWaitingCount = queueWaitingCount,
                TodayTotalCallCount = todayCalls.Count,
                TodayAnsweredCount = todayCalls.Count(s => s == CallStatuses.Ids.Completed),
                TodayMissedCount = todayCalls.Count(s => s == CallStatuses.Ids.Missed)
            };

            await SendToGroupAndAdminsAsync(groupName, "DashboardKpiUpdated", kpi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Hub] KPI broadcast hatasi: {ex.Message}");
        }
    }

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null ? int.Parse(claim) : null;
    }

    private readonly record struct UserScope(int CustomerId, int? BranchId);
}
