using SIPSorcery.SIP;

namespace CallCenter.PbxService.Services;

/// <summary>
/// Gelen cagri akisi:
/// INVITE -> 100 Trying -> Mesai kontrolu -> IVR/Kuyruk -> Agent bridge
/// </summary>
public class InboundCallHandler
{
    private readonly ILogger<InboundCallHandler> _logger;
    private readonly IApiClient _apiClient;
    private readonly ICallSessionManager _sessionManager;
    private readonly ISipTransportService _transport;

    // Startup'ta API'den yuklenir, cache'lenir
    private BusinessHoursInfo? _businessHours;
    private CustomerPbxConfig? _customerConfig;

    public InboundCallHandler(
        ILogger<InboundCallHandler> logger,
        IApiClient apiClient,
        ICallSessionManager sessionManager,
        ISipTransportService transport)
    {
        _logger = logger;
        _apiClient = apiClient;
        _sessionManager = sessionManager;
        _transport = transport;
    }

    /// <summary>Startup'ta API'den mesai/config bilgilerini yukle</summary>
    public async Task LoadConfigAsync(string customerUid)
    {
        _customerConfig = await _apiClient.GetCustomerPbxConfigAsync(customerUid);
        _businessHours = await _apiClient.GetBusinessHoursAsync(customerUid);

        _logger.LogInformation("PBX Config yuklendi - IVR: {IvrId}, Kuyruk: {QueueId}, Mesai: {HasBH}",
            _customerConfig?.DefaultIvrMenuId,
            _customerConfig?.DefaultQueueId,
            _businessHours != null);
    }

    /// <summary>Gelen INVITE isleme</summary>
    public async Task HandleInviteAsync(SIPEndPoint localEp, SIPEndPoint remoteEp, SIPRequest invite)
    {
        var callId = invite.Header.CallId;
        var callerUri = invite.Header.From.FromURI;
        var calleeUri = invite.Header.To.ToURI;

        // Arayan/aranan numaralari cikar
        var callerNumber = ExtractPhoneNumber(callerUri);
        var calleeNumber = ExtractPhoneNumber(calleeUri);

        _logger.LogInformation("Gelen cagri: {Caller} -> {Callee} [CallId={CallId}]",
            callerNumber, calleeNumber, callId);

        // 1. 100 Trying
        var trying = SIPResponse.GetResponse(invite, SIPResponseStatusCodesEnum.Trying, null);
        await _transport.SendResponseAsync(trying);

        // 2. Oturum olustur
        var session = _sessionManager.CreateSession(callId, callerUri.ToString(), calleeUri.ToString());

        // 3. Mesai kontrolu
        if (!IsWithinBusinessHours())
        {
            _logger.LogInformation("Mesai disi cagri: {Caller}", callerNumber);
            // TODO 11.5: Mesai disi karsilama mesaji cal
            var unavailable = SIPResponse.GetResponse(invite,
                SIPResponseStatusCodesEnum.TemporarilyUnavailable, "Mesai saatleri disinda");
            await _transport.SendResponseAsync(unavailable);
            _sessionManager.RemoveSession(callId);
            return;
        }

        // 4. API'ye cagri kaydi olustur
        var callRecordId = await _apiClient.CreateIncomingCallRecordAsync(new IncomingCallRequest
        {
            CallerNumber = callerNumber,
            CalleeNumber = calleeNumber,
            SipCallId = callId,
            QueueId = _customerConfig?.DefaultQueueId
        });

        if (callRecordId.HasValue)
        {
            session.CallRecordId = callRecordId.Value;
        }

        // 5. 180 Ringing
        var ringing = SIPResponse.GetResponse(invite, SIPResponseStatusCodesEnum.Ringing, null);
        await _transport.SendResponseAsync(ringing);

        // 6. IVR veya direkt kuyruga yonlendir
        if (_customerConfig?.DefaultIvrMenuId != null)
        {
            session.State = CallSessionState.IvrPlaying;
            _logger.LogInformation("IVR menusune yonlendiriliyor: MenuId={MenuId}",
                _customerConfig.DefaultIvrMenuId);
            // TODO 11.6: IVR motor cagirilacak
        }
        else if (_customerConfig?.DefaultQueueId != null)
        {
            session.State = CallSessionState.Queued;
            session.QueueId = _customerConfig.DefaultQueueId;
            _logger.LogInformation("Kuyruga yonlendiriliyor: QueueId={QueueId}",
                _customerConfig.DefaultQueueId);
            // TODO 11.7+11.8: Kuyruk bekleme + ACD
        }
        else
        {
            // Ne IVR ne kuyruk tanimli - reject
            _logger.LogWarning("IVR veya kuyruk tanimli degil, cagri reddediliyor");
            var busy = SIPResponse.GetResponse(invite,
                SIPResponseStatusCodesEnum.BusyHere, "Yapilandirma eksik");
            await _transport.SendResponseAsync(busy);
            _sessionManager.RemoveSession(callId);
        }
    }

    /// <summary>BYE isleme - cagri sonlandirma</summary>
    public async Task HandleByeAsync(SIPRequest bye)
    {
        var callId = bye.Header.CallId;
        var session = _sessionManager.GetSession(callId);

        if (session?.CallRecordId != null)
        {
            var duration = session.AnsweredAt.HasValue
                ? (int)(DateTime.UtcNow - session.AnsweredAt.Value).TotalSeconds
                : 0;

            await _apiClient.UpdateCallRecordAsync(session.CallRecordId.Value, new CallRecordUpdate
            {
                StatusId = 6, // Completed
                EndedAt = DateTime.UtcNow,
                DurationSeconds = duration
            });
        }

        _sessionManager.RemoveSession(callId);

        var ok = SIPResponse.GetResponse(bye, SIPResponseStatusCodesEnum.Ok, null);
        await _transport.SendResponseAsync(ok);
    }

    private bool IsWithinBusinessHours()
    {
        if (_businessHours == null || _businessHours.Workdays.Count == 0)
            return true; // Mesai tanimli degilse her zaman acik

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById(_businessHours.Timezone));
        var today = DateOnly.FromDateTime(now);

        // Tatil kontrolu
        foreach (var holiday in _businessHours.Holidays)
        {
            if (holiday.Date == today)
                return false;
            if (holiday.IsRecurring && holiday.Date.Month == today.Month && holiday.Date.Day == today.Day)
                return false;
        }

        // Gun kontrolu
        var dayOfWeek = (int)now.DayOfWeek;
        var workday = _businessHours.Workdays.FirstOrDefault(w => w.DayOfWeek == dayOfWeek);

        if (workday == null || !workday.IsWorkday)
            return false;

        var currentTime = now.TimeOfDay;
        return currentTime >= workday.StartTime && currentTime <= workday.EndTime;
    }

    private static string ExtractPhoneNumber(SIPURI uri)
    {
        var user = uri.User;
        if (string.IsNullOrEmpty(user)) return uri.ToString();

        // sip:+905551234567@server -> 05551234567
        if (user.StartsWith('+'))
            user = "0" + user[3..]; // +90 kaldir, 0 ekle

        return user;
    }
}
