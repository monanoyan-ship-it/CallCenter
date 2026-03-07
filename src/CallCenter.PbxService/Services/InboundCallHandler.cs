using System.Net;
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
    private readonly QueueManager _queueManager;

    // Startup'ta API'den yuklenir, cache'lenir
    private BusinessHoursInfo? _businessHours;
    private CustomerPbxConfig? _customerConfig;

    public InboundCallHandler(
        ILogger<InboundCallHandler> logger,
        IApiClient apiClient,
        ICallSessionManager sessionManager,
        ISipTransportService transport,
        QueueManager queueManager)
    {
        _logger = logger;
        _apiClient = apiClient;
        _sessionManager = sessionManager;
        _transport = transport;
        _queueManager = queueManager;
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

            // Arayanin RTP endpoint bilgisini SDP'den cikar
            var callerRtpEp = ExtractRtpEndpointFromSdp(invite.Body);
            if (callerRtpEp == null)
            {
                _logger.LogWarning("SDP'den RTP endpoint cikarilmadi, cagri reddediliyor: CallId={CallId}", callId);
                var noSdp = SIPResponse.GetResponse(invite,
                    SIPResponseStatusCodesEnum.NotAcceptable, "SDP gerekli");
                await _transport.SendResponseAsync(noSdp);
                _sessionManager.RemoveSession(callId);
                return;
            }

            // Arayana RTP gondermek icin sender olustur
            var callerEndpoint = ParseEndpoint(callerRtpEp);
            var rtpSender = new CallerRtpSender(callerEndpoint);

            // Session'a caller RTP bilgisi kaydet (ACD bridge icin)
            session.CallerRtpEndpoint = callerRtpEp;
            session.CallerNumber = callerNumber;

            // 200 OK + SDP answer gonder (medya akisi baslasin)
            var localIp = localEp.GetIPEndPoint().Address.ToString();
            var sdpAnswer = BuildSdpAnswer(localIp, rtpSender.LocalPort);
            var ok = SIPResponse.GetResponse(invite, SIPResponseStatusCodesEnum.Ok, null);
            ok.Header.ContentType = "application/sdp";
            ok.Body = sdpAnswer;
            await _transport.SendResponseAsync(ok);

            _logger.LogInformation(
                "Kuyruga yonlendiriliyor: QueueId={QueueId}, CallerRtp={CallerRtp}, LocalRtp=:{LocalPort}",
                _customerConfig.DefaultQueueId, callerRtpEp, rtpSender.LocalPort);

            // Kuyruga ekle - bekleme muzigi otomatik baslar
            _ = _queueManager.EnqueueAsync(callId, _customerConfig.DefaultQueueId.Value, rtpSender, CancellationToken.None);
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

        // Kuyrukta bekliyorsa cikar
        _queueManager.RemoveByCallId(callId);

        // RTP Bridge aktifse durdur
        session?.Bridge?.Dispose();

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

    /// <summary>SDP'den RTP endpoint cikar (c= IP, m=audio port)</summary>
    private static string? ExtractRtpEndpointFromSdp(string? sdp)
    {
        if (string.IsNullOrEmpty(sdp)) return null;

        string? ip = null;
        int? port = null;

        foreach (var line in sdp.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("c=IN IP4 "))
            {
                ip = trimmed["c=IN IP4 ".Length..].Trim();
            }
            else if (trimmed.StartsWith("m=audio "))
            {
                var parts = trimmed.Split(' ');
                if (parts.Length >= 2 && int.TryParse(parts[1], out var p))
                    port = p;
            }
        }

        return ip != null && port != null ? $"{ip}:{port}" : null;
    }

    /// <summary>SDP answer olustur</summary>
    private static string BuildSdpAnswer(string localIp, int rtpPort)
    {
        return $"""
            v=0
            o=PbxService 0 0 IN IP4 {localIp}
            s=CallCenter PBX
            c=IN IP4 {localIp}
            t=0 0
            m=audio {rtpPort} RTP/AVP 0 8
            a=rtpmap:0 PCMU/8000
            a=rtpmap:8 PCMA/8000
            a=ptime:20
            a=sendrecv
            """.Replace("            ", "");
    }

    private static IPEndPoint ParseEndpoint(string endpoint)
    {
        var parts = endpoint.Split(':');
        var ip = IPAddress.Parse(parts[0]);
        var port = parts.Length > 1 ? int.Parse(parts[1]) : 0;
        return new IPEndPoint(ip, port);
    }
}
