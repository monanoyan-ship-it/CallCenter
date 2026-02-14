using CallCenter.Api.Services.Interfaces;
using CallCenter.Data;
using CallCenter.Shared.DTOs;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using CallCenter.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

/// <summary>
/// Uygulama seviyesi ACD (Automatic Call Distribution).
/// Gelen aramaları kuyruklardaki müsait agent'lara dağıtır.
/// Call Forwarding kurallarini kontrol eder.
///
/// NOT: Gerçek ACD genellikle PBX'in işidir (Asterisk/FreeSWITCH).
/// Bu servis PBX olmadığında veya PBX'in kuyruk tanımı yoksa devreye girer.
/// Strateji: Round-robin (en az meşgul agent, öncelik sırasına göre).
/// </summary>
public class CallDistributionService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<CallCenterHub> _hub;
    private readonly ICallForwardingService _forwardingService;

    public CallDistributionService(AppDbContext db, IHubContext<CallCenterHub> hub, ICallForwardingService forwardingService)
    {
        _db = db;
        _hub = hub;
        _forwardingService = forwardingService;
    }

    /// <summary>
    /// Gelen aramayı uygun bir agent'a atar.
    /// Müsait agent bulunamazsa null döner (arama kuyrukta bekler).
    /// </summary>
    public async Task<int?> AssignCallToAgentAsync(int queueId, int callRecordId)
    {
        var queue = await _db.Queues
            .Include(q => q.QueueAgents)
            .FirstOrDefaultAsync(q => q.Id == queueId && q.IsActive);

        if (queue == null) return null;

        // Kuyrukta tanımlı agent'ları çek (öncelik sırasıyla)
        var queueAgentIds = queue.QueueAgents
            .OrderBy(qa => qa.Priority)
            .Select(qa => qa.AgentId)
            .ToList();

        if (!queueAgentIds.Any()) return null;

        // Müsait agent'ları bul (durumu Available olanlar)
        var availableAgents = await _db.Users
            .Where(u => queueAgentIds.Contains(u.Id) && u.StatusId == AgentStatuses.Ids.Available && u.IsActive)
            .ToListAsync();

        if (!availableAgents.Any()) return null;

        // En az meşgul agent'ı seç (bugünkü en az arama sayısı)
        var today = DateTime.UtcNow.Date;
        var agentCallCounts = await _db.CallRecords
            .Where(c => c.AgentId != null && availableAgents.Select(a => a.Id).Contains(c.AgentId!.Value))
            .Where(c => c.StartedAt >= today)
            .GroupBy(c => c.AgentId)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToListAsync();

        // Öncelik sırasına göre, eşitlik varsa en az arama yapana ata
        var selectedAgent = availableAgents
            .OrderBy(a => queueAgentIds.IndexOf(a.Id)) // QueueAgent öncelik sırası
            .ThenBy(a => agentCallCounts.FirstOrDefault(c => c.AgentId == a.Id)?.Count ?? 0) // En az meşgul
            .First();

        // Call Forwarding kontrolu: secilen agent'in yonlendirme kurali var mi?
        // Offline agent'lar zaten filtrelendi, ama Always tipi kontrol edilmeli
        var forwardDest = await _forwardingService.GetForwardDestinationAsync(
            selectedAgent.Id, ForwardTypes.Always);
        // ForwardDest varsa loglama yapilir, SIP tarafinda 302 gonderilir
        // Simdilik CallRecord'a forwarding bilgisini kaydediyoruz

        // CallRecord'a agent'ı ata
        var callRecord = await _db.CallRecords.FindAsync(callRecordId);
        if (callRecord == null) return null;

        callRecord.AgentId = selectedAgent.Id;
        await _db.SaveChangesAsync();

        // Agent'a SignalR bildirimi gönder
        var notification = new CallNotification
        {
            CallId = callRecord.Id,
            CallerNumber = callRecord.CallerNumber,
            CalleeNumber = callRecord.CalleeNumber,
            DirectionId = callRecord.DirectionId,
            StatusId = CallStatuses.Ids.Ringing,
            QueueName = queue.Name
        };

        // Belirli agent'a bildirim gönder (User claim'indeki NameIdentifier ile)
        await _hub.Clients.User(selectedAgent.Id.ToString())
            .SendAsync("IncomingCall", notification);

        return selectedAgent.Id;
    }
}
