using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Factories.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Api.Services;
using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Factories;

/// <summary>
/// PbxService icin ozel API islemleri.
/// CustomerUid ile musteri tespit eder, mevcut EntityService'leri kullanir.
/// </summary>
public class PbxFactory : IPbxFactory
{
    private readonly ICustomerEntityService _customerEs;
    private readonly ISipAccountEntityService _sipEs;
    private readonly IIvrEntityService _ivrEs;
    private readonly IQueueEntityService _queueEs;
    private readonly ICallRecordEntityService _callEs;
    private readonly IUserEntityService _userEs;
    private readonly IUnitOfWork _uow;
    private readonly AesEncryptionService _encryption;
    private readonly ILogger<PbxFactory> _logger;

    public PbxFactory(
        ICustomerEntityService customerEs,
        ISipAccountEntityService sipEs,
        IIvrEntityService ivrEs,
        IQueueEntityService queueEs,
        ICallRecordEntityService callEs,
        IUserEntityService userEs,
        IUnitOfWork uow,
        AesEncryptionService encryption,
        ILogger<PbxFactory> logger)
    {
        _customerEs = customerEs;
        _sipEs = sipEs;
        _ivrEs = ivrEs;
        _queueEs = queueEs;
        _callEs = callEs;
        _userEs = userEs;
        _uow = uow;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<PbxTrunkListResult> GetTrunksAsync(string customerUid)
    {
        var customer = await GetCustomerByUidAsync(customerUid);
        if (customer == null)
            return new PbxTrunkListResult();

        var sips = await _sipEs.GetAllQueryable()
            .Where(s => s.CustomerId == customer.Id && s.IsActive)
            .ToListAsync();

        return new PbxTrunkListResult
        {
            Trunks = sips.Select(s => new PbxTrunkDto
            {
                Id = s.Id,
                Name = s.Name,
                Server = s.Server,
                Port = s.Port,
                Username = s.Username,
                Password = _encryption.Decrypt(s.Password),
                Domain = s.Domain,
                Transport = s.Transport ?? "UDP",
                UseSrtp = s.UseSrtp,
                IsActive = s.IsActive
            }).ToList()
        };
    }

    public async Task<PbxCustomerConfigResult?> GetCustomerConfigAsync(string customerUid)
    {
        var customer = await GetCustomerByUidAsync(customerUid);
        if (customer == null) return null;

        // Varsayilan IVR menu
        var defaultIvr = await _ivrEs.GetMenusQueryable()
            .Where(m => m.CustomerId == customer.Id && m.IsActive)
            .OrderBy(m => m.Id)
            .FirstOrDefaultAsync();

        // Varsayilan kuyruk
        var defaultQueue = await _queueEs.GetAllQueryable()
            .Where(q => q.CustomerId == customer.Id && q.IsActive)
            .OrderBy(q => q.Id)
            .FirstOrDefaultAsync();

        // Mesai saati tanimli mi
        var hasBusinessHours = await _ivrEs.GetBusinessHoursQueryable()
            .AnyAsync(bh => bh.CustomerId == customer.Id);

        return new PbxCustomerConfigResult
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            DefaultIvrMenuId = defaultIvr?.Id,
            DefaultQueueId = defaultQueue?.Id,
            HasBusinessHours = hasBusinessHours
        };
    }

    public async Task<PbxBusinessHoursResult?> GetBusinessHoursAsync(string customerUid)
    {
        var customer = await GetCustomerByUidAsync(customerUid);
        if (customer == null) return null;

        var workdays = await _ivrEs.GetBusinessHoursQueryable()
            .Where(bh => bh.CustomerId == customer.Id)
            .ToListAsync();

        var holidays = await _ivrEs.GetHolidaysQueryable()
            .Where(h => h.CustomerId == customer.Id)
            .ToListAsync();

        return new PbxBusinessHoursResult
        {
            Workdays = workdays.Select(w => new PbxWorkdayDto
            {
                DayOfWeek = w.DayOfWeek,
                StartTime = w.StartTime,
                EndTime = w.EndTime,
                IsWorkday = w.IsWorkday
            }).ToList(),
            Holidays = holidays.Select(h => new PbxHolidayDto
            {
                Date = h.Date,
                Name = h.Name,
                IsRecurring = h.IsRecurring,
                GreetingMessageId = h.GreetingMessageId
            }).ToList(),
            Timezone = "Europe/Istanbul" // TODO: Musteri bazli timezone
        };
    }

    public async Task<PbxIvrMenuResult?> GetIvrMenuAsync(int menuId, string customerUid)
    {
        var customer = await GetCustomerByUidAsync(customerUid);
        if (customer == null) return null;

        var menu = await _ivrEs.GetMenuByIdWithOptionsAsync(menuId, customer.Id);
        if (menu == null) return null;

        return new PbxIvrMenuResult
        {
            Id = menu.Id,
            Name = menu.Name,
            GreetingMessageId = menu.GreetingMessageId,
            MaxRetries = menu.MaxRetries,
            TimeoutSeconds = menu.TimeoutSeconds,
            Options = menu.Options.Select(o => new PbxIvrOptionDto
            {
                Digit = o.Digit,
                ActionType = o.ActionType,
                TargetQueueId = o.TargetQueueId,
                TargetExtension = o.TargetExtension,
                TargetIvrMenuId = o.TargetIvrMenuId,
                TargetGreetingMessageId = o.TargetGreetingMessageId
            }).ToList()
        };
    }

    public async Task<PbxQueueConfigResult?> GetQueueConfigAsync(int queueId)
    {
        var queue = await _queueEs.GetByIdAsync(queueId);
        if (queue == null) return null;

        // Kuyruk icin bekleme muzigi: once kuyruga ozel, yoksa genel default
        var holdMusic = await _ivrEs.GetHoldMusicsQueryable()
            .Where(hm => hm.CustomerId == queue.CustomerId && hm.IsActive &&
                   (hm.QueueId == queueId || hm.QueueId == null))
            .OrderByDescending(hm => hm.QueueId.HasValue) // Kuyruga ozel once
            .ThenBy(hm => hm.IsDefault ? 0 : 1)
            .FirstOrDefaultAsync();

        return new PbxQueueConfigResult
        {
            QueueId = queue.Id,
            Name = queue.Name,
            HoldMusicId = holdMusic?.Id,
            MaxWaitTimeSeconds = queue.MaxWaitTimeSeconds,
            MaxQueueLength = 50
        };
    }

    public async Task<PbxAvailableAgentResult?> GetAvailableAgentAsync(int queueId)
    {
        var queue = await _queueEs.GetByIdWithAgentsAsync(queueId);
        if (queue == null) return null;

        // Oncelik ve en az mesgul stratejisi
        var availableAgent = queue.QueueAgents
            .OrderBy(qa => qa.Priority)
            .Select(qa => qa.Agent)
            .FirstOrDefault(a => a.StatusId == AgentStatuses.Ids.Available && a.IsActive);

        if (availableAgent == null) return null;

        return new PbxAvailableAgentResult
        {
            AgentId = availableAgent.Id,
            AgentName = availableAgent.FullName,
            SipExtension = availableAgent.Extension ?? availableAgent.Id.ToString()
        };
    }

    public async Task<PbxCallCreatedResult?> CreateIncomingCallAsync(PbxIncomingCallRequest request)
    {
        var callRecord = new CallRecord
        {
            CallerNumber = request.CallerNumber,
            CalleeNumber = request.CalleeNumber,
            DirectionId = CallDirections.Ids.Inbound,
            StatusId = CallStatuses.Ids.Ringing,
            StartedAt = DateTime.UtcNow,
            QueueId = request.QueueId
        };

        _callEs.Add(callRecord);
        await _uow.SaveChangesAsync();

        return new PbxCallCreatedResult { Id = callRecord.Id };
    }

    public async Task<bool> UpdateCallRecordAsync(int callRecordId, PbxCallUpdateRequest request)
    {
        var record = await _callEs.GetByIdAsync(callRecordId);
        if (record == null) return false;

        if (request.StatusId.HasValue) record.StatusId = request.StatusId.Value;
        if (request.AgentId.HasValue) record.AgentId = request.AgentId.Value;
        if (request.AnsweredAt.HasValue) record.AnsweredAt = request.AnsweredAt.Value;
        if (request.EndedAt.HasValue) record.EndedAt = request.EndedAt.Value;
        if (request.DurationSeconds.HasValue) record.DurationSeconds = request.DurationSeconds.Value;

        _callEs.Update(record);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<(byte[]? Data, string? ContentType)?> GetAudioFileAsync(int greetingMessageId)
    {
        // Oncelik: GreetingMessage'dan dosya yolunu al
        var greeting = await _ivrEs.GetGreetingsQueryable()
            .FirstOrDefaultAsync(g => g.Id == greetingMessageId);

        string? filePath = greeting?.AudioFilePath;

        // GreetingMessage yoksa HoldMusic'ten dene
        if (filePath == null)
        {
            var holdMusic = await _ivrEs.GetHoldMusicsQueryable()
                .FirstOrDefaultAsync(hm => hm.Id == greetingMessageId);
            filePath = holdMusic?.AudioFilePath;
        }

        if (string.IsNullOrEmpty(filePath))
            return null;

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Ses dosyasi bulunamadi: {Path}", filePath);
            return null;
        }

        var data = await File.ReadAllBytesAsync(filePath);
        return (data, "audio/wav");
    }

    private async Task<Customer?> GetCustomerByUidAsync(string customerUid)
    {
        if (!Guid.TryParse(customerUid, out var uid))
        {
            _logger.LogWarning("Gecersiz CustomerUid: {Uid}", customerUid);
            return null;
        }

        return await _customerEs.GetAllQueryable()
            .FirstOrDefaultAsync(c => c.Uid == uid && c.IsActive);
    }
}
