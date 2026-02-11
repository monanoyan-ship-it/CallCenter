using System.Net;
using CallCenter.Shared.DTOs;
using CallCenter.Windows.Models;
using NAudio.Wave;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Windows;

namespace CallCenter.Windows.Services;

/// <summary>
/// SIPSorcery + SIPSorceryMedia.Windows tabanli native SIP servisi.
/// Register, arama yapma/alma, hold, DTMF, transfer destegi.
/// WindowsAudioEndPoint ile gercek mikrofon/hoparlor kullanir.
/// </summary>
public class NativeSipService : ISipService
{
    private SIPTransport? _sipTransport;
    private SIPRegistrationUserAgent? _regAgent;
    private SIPUserAgent? _userAgent;
    private VoIPMediaSession? _mediaSession;
    private SIPServerUserAgent? _pendingUas;

    private SipConnectionInfoDto? _config;
    private int _inputDeviceIndex = -1;  // -1 = default
    private int _outputDeviceIndex = -1; // -1 = default

    // ─── State ───
    public bool IsRegistered { get; private set; }
    public bool IsInCall { get; private set; }
    public bool IsOnHold { get; private set; }

    // ─── Events ───
    public event Func<Task>? OnRegistered;
    public event Func<string, Task>? OnRegistrationFailed;
    public event Func<string, string, Task>? OnIncomingCall;
    public event Func<Task>? OnCallAnswered;
    public event Func<Task>? OnCallEnded;
    public event Func<string, Task>? OnCallFailed;

    /// <summary>
    /// SIP transport ve registration baslatir.
    /// </summary>
    public async Task<bool> InitializeAsync(SipConnectionInfoDto config)
    {
        try
        {
            _config = config;

            // SIP URI parse
            var sipUri = SIPURI.ParseSIPURI(config.SipUri);
            if (sipUri == null)
            {
                await (OnRegistrationFailed?.Invoke("Gecersiz SIP URI") ?? Task.CompletedTask);
                return false;
            }

            // Transport olustur
            _sipTransport = new SIPTransport();

            // Transport kanali ekle (config.Transport'a gore)
            var transport = config.Transport?.ToUpperInvariant() ?? "UDP";
            switch (transport)
            {
                case "TCP":
                    _sipTransport.AddSIPChannel(new SIPTCPChannel(new IPEndPoint(IPAddress.Any, 0)));
                    break;
                case "TLS":
                    _sipTransport.AddSIPChannel(new SIPTCPChannel(new IPEndPoint(IPAddress.Any, 0)));
                    break;
                default: // UDP
                    _sipTransport.AddSIPChannel(new SIPUDPChannel(new IPEndPoint(IPAddress.Any, 0)));
                    break;
            }

            // Registration
            var regUri = new SIPURI(sipUri.User, sipUri.Host, null, SIPSchemesEnum.sip, SIPProtocolsEnum.udp);
            _regAgent = new SIPRegistrationUserAgent(
                _sipTransport,
                config.AuthUsername,
                config.AuthPassword,
                regUri.ToString(),
                120); // 120 saniye expiry

            _regAgent.RegistrationSuccessful += (uri, response) =>
            {
                IsRegistered = true;
                _ = OnRegistered?.Invoke() ?? Task.CompletedTask;
            };

            _regAgent.RegistrationFailed += (uri, response, message) =>
            {
                IsRegistered = false;
                _ = OnRegistrationFailed?.Invoke(message ?? "Registration basarisiz") ?? Task.CompletedTask;
            };

            // Gelen arama dinle
            _sipTransport.SIPTransportRequestReceived += OnSipRequestReceived;

            // Registration baslat
            _regAgent.Start();

            return true;
        }
        catch (Exception ex)
        {
            await (OnRegistrationFailed?.Invoke($"SIP init hatasi: {ex.Message}") ?? Task.CompletedTask);
            return false;
        }
    }

    /// <summary>
    /// Dis arama yapar (outbound INVITE).
    /// </summary>
    public async Task<bool> MakeCallAsync(string destination)
    {
        if (_sipTransport == null || _config == null || IsInCall) return false;

        try
        {
            _userAgent = new SIPUserAgent(_sipTransport, null);
            _userAgent.ClientCallFailed += (uac, error, response) =>
            {
                IsInCall = false;
                _ = OnCallFailed?.Invoke(error ?? "Arama basarisiz") ?? Task.CompletedTask;
            };
            _userAgent.OnCallHungup += (dialog) =>
            {
                CleanupCall();
                _ = OnCallEnded?.Invoke() ?? Task.CompletedTask;
            };

            // Medya oturumu (ses) — gercek mikrofon + hoparlor
            _mediaSession = CreateMediaSession();

            // Hedef URI
            var destUri = destination.StartsWith("sip:")
                ? SIPURI.ParseSIPURI(destination)
                : SIPURI.ParseSIPURI($"sip:{destination}@{SIPURI.ParseSIPURI(_config.SipUri)?.Host}");

            if (destUri == null)
            {
                await (OnCallFailed?.Invoke("Gecersiz hedef numara") ?? Task.CompletedTask);
                return false;
            }

            var result = await _userAgent.Call(destUri.ToString(), _config.AuthUsername, _config.AuthPassword, _mediaSession);

            if (result)
            {
                IsInCall = true;
                await (OnCallAnswered?.Invoke() ?? Task.CompletedTask);
            }
            else
            {
                CleanupCall();
                await (OnCallFailed?.Invoke("Arama baglanamiyor") ?? Task.CompletedTask);
            }

            return result;
        }
        catch (Exception ex)
        {
            CleanupCall();
            await (OnCallFailed?.Invoke(ex.Message) ?? Task.CompletedTask);
            return false;
        }
    }

    /// <summary>
    /// Gelen aramayi cevaplar.
    /// </summary>
    public async Task<bool> AnswerCallAsync()
    {
        if (_userAgent == null || _pendingUas == null) return false;

        try
        {
            _mediaSession = CreateMediaSession();
            var result = await _userAgent.Answer(_pendingUas, _mediaSession);
            _pendingUas = null;

            if (result)
            {
                IsInCall = true;
                await (OnCallAnswered?.Invoke() ?? Task.CompletedTask);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Answer hatasi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Aktif aramayi sonlandirir (BYE).
    /// </summary>
    public async Task<bool> HangupAsync()
    {
        if (_userAgent == null && _pendingUas == null) return false;

        try
        {
            // Cevaplanmamis gelen aramayi reject et
            if (_pendingUas != null)
            {
                _pendingUas.Reject(SIPResponseStatusCodesEnum.BusyHere, null);
                _pendingUas = null;
            }

            if (_userAgent?.IsCallActive == true)
            {
                _userAgent.Hangup();
            }

            CleanupCall();
            await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Hangup hatasi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Aramayi bekletir (re-INVITE ile sendonly SDP).
    /// </summary>
    public async Task<bool> HoldAsync()
    {
        if (_userAgent == null || !IsInCall || IsOnHold) return false;

        try
        {
            if (_mediaSession != null)
            {
                await _mediaSession.PutOnHold();
            }
            IsOnHold = true;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Hold hatasi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Bekletmeden devam eder (re-INVITE ile sendrecv SDP).
    /// </summary>
    public Task<bool> UnholdAsync()
    {
        if (_userAgent == null || !IsOnHold) return Task.FromResult(false);

        try
        {
            if (_mediaSession != null)
            {
                // TakeOffHold void doner (Task degil)
                _mediaSession.TakeOffHold();
            }
            IsOnHold = false;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Unhold hatasi: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// DTMF tonu gonderir (RFC 2833).
    /// </summary>
    public async Task<bool> SendDtmfAsync(string tone)
    {
        if (_userAgent == null || !IsInCall || _mediaSession == null) return false;

        try
        {
            byte dtmfEvent;

            if (byte.TryParse(tone, out var dtmfByte))
            {
                dtmfEvent = dtmfByte;
            }
            else
            {
                // *, #, A-D
                dtmfEvent = tone switch
                {
                    "*" => 10,
                    "#" => 11,
                    "A" or "a" => 12,
                    "B" or "b" => 13,
                    "C" or "c" => 14,
                    "D" or "d" => 15,
                    _ => byte.MaxValue
                };
                if (dtmfEvent == byte.MaxValue) return false;
            }

            await _userAgent.SendDtmf(dtmfEvent);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] DTMF hatasi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Blind transfer (REFER).
    /// </summary>
    public async Task<bool> TransferAsync(string target)
    {
        if (_userAgent == null || !IsInCall) return false;

        try
        {
            var targetUri = target.StartsWith("sip:")
                ? SIPURI.ParseSIPURI(target)
                : SIPURI.ParseSIPURI($"sip:{target}@{SIPURI.ParseSIPURI(_config!.SipUri)?.Host}");

            if (targetUri == null) return false;

            var result = await _userAgent.BlindTransfer(targetUri, TimeSpan.FromSeconds(10), default);

            if (result)
            {
                CleanupCall();
                await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Transfer hatasi: {ex.Message}");
            return false;
        }
    }

    // ═══════════════════════════════════════════════════
    // SES CIHAZI YONETIMI
    // ═══════════════════════════════════════════════════

    public Task<List<AudioDeviceInfo>> GetAudioInputDevicesAsync()
    {
        var devices = new List<AudioDeviceInfo>();
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var caps = WaveInEvent.GetCapabilities(i);
            devices.Add(new AudioDeviceInfo { DeviceIndex = i, DeviceName = caps.ProductName, Kind = "input" });
        }
        return Task.FromResult(devices);
    }

    public Task<List<AudioDeviceInfo>> GetAudioOutputDevicesAsync()
    {
        var devices = new List<AudioDeviceInfo>();
        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            var caps = WaveOut.GetCapabilities(i);
            devices.Add(new AudioDeviceInfo { DeviceIndex = i, DeviceName = caps.ProductName, Kind = "output" });
        }
        return Task.FromResult(devices);
    }

    public Task<bool> SetAudioInputDeviceAsync(int deviceIndex)
    {
        _inputDeviceIndex = deviceIndex;
        return Task.FromResult(true);
    }

    public Task<bool> SetAudioOutputDeviceAsync(int deviceIndex)
    {
        _outputDeviceIndex = deviceIndex;
        return Task.FromResult(true);
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════

    /// <summary>Gelen SIP isteklerini isler (INVITE = gelen arama).</summary>
    private async Task OnSipRequestReceived(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
    {
        if (sipRequest.Method == SIPMethodsEnum.INVITE)
        {
            // Zaten aramadaysak reject et
            if (IsInCall)
            {
                var busyResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.BusyHere, null);
                await _sipTransport!.SendResponseAsync(busyResponse);
                return;
            }

            // SIPUserAgent olustur
            _userAgent = new SIPUserAgent(_sipTransport!, null);
            _userAgent.OnCallHungup += (dialog) =>
            {
                CleanupCall();
                _ = OnCallEnded?.Invoke() ?? Task.CompletedTask;
            };

            // AcceptCall ile SIPServerUserAgent olustur (180 Ringing gonderir)
            _pendingUas = _userAgent.AcceptCall(sipRequest);

            // Arayan bilgilerini event ile bildir
            var callerUri = sipRequest.Header.From?.FromURI?.ToString() ?? "Bilinmeyen";
            var callerDisplay = sipRequest.Header.From?.FromName ?? callerUri;
            await (OnIncomingCall?.Invoke(callerUri, callerDisplay) ?? Task.CompletedTask);
        }
    }

    /// <summary>
    /// Medya oturumu olusturur — WindowsAudioEndPoint ile gercek mikrofon/hoparlor.
    /// </summary>
    private VoIPMediaSession CreateMediaSession()
    {
        var winAudio = new WindowsAudioEndPoint(new AudioEncoder(), _outputDeviceIndex, _inputDeviceIndex);
        winAudio.RestrictFormats(x => x.Codec == AudioCodecsEnum.PCMU || x.Codec == AudioCodecsEnum.PCMA);

        var mediaSession = new VoIPMediaSession(winAudio.ToMediaEndPoints());
        mediaSession.AcceptRtpFromAny = true;

        return mediaSession;
    }

    /// <summary>Arama sonrasi temizlik.</summary>
    private void CleanupCall()
    {
        IsInCall = false;
        IsOnHold = false;

        if (_mediaSession != null)
        {
            _mediaSession.Close(null);
            _mediaSession = null;
        }

        _pendingUas = null;
        _userAgent = null;
    }

    /// <summary>Tum kaynaklari serbest birakir.</summary>
    public ValueTask DisposeAsync()
    {
        CleanupCall();

        if (_regAgent != null)
        {
            _regAgent.Stop();
            _regAgent = null;
        }

        if (_sipTransport != null)
        {
            _sipTransport.Shutdown();
            _sipTransport = null;
        }

        IsRegistered = false;
        return ValueTask.CompletedTask;
    }
}
