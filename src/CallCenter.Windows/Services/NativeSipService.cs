using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using CallCenter.Shared.DTOs;
using CallCenter.Windows.Models;
using Concentus.Enums;
using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Windows;

namespace CallCenter.Windows.Services;

/// <summary>
/// SIPSorcery tabanli native SIP servisi — MicroSIP feature parity.
/// Attended/Blind transfer, multi-line, DND, auto-answer, mute, volume,
/// recording, codec secimi, SRTP, STUN/ICE, voicemail (MWI), ringtone.
/// </summary>
public class NativeSipService : ISipService
{
    // ─── Dosya Logger ───
    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CorpLynk", "sip-debug.log");
    private static readonly object _logLock = new();

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        try
        {
            lock (_logLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }
        catch { }
    }

    // ═══════════════════════════════════════════════════
    // ALTYAPI
    // ═══════════════════════════════════════════════════

    private SIPTransport? _sipTransport;
    private SIPRegistrationUserAgent? _regAgent;
    private SipConnectionInfoDto? _config;

    // ─── Multi-Line ───
    private const int MaxLines = 4;
    private readonly CallLine[] _lines = new CallLine[MaxLines];
    private int _activeLineIndex;

    // ─── Ses cihazi ───
    private int _inputDeviceIndex = -1;  // -1 = default
    private int _outputDeviceIndex = -1;

    // ─── Volume (0.0 - 1.0) ───
    private float _micVolume = 1.0f;
    private float _speakerVolume = 1.0f;

    // ─── Codec ───
    private List<string> _enabledCodecNames = new() { "OPUS", "G722", "PCMU", "PCMA" };
    private OpusDecoder? _opusDecoder;
    private OpusEncoder? _opusEncoder;

    // ─── Jitter Buffer ───
    private int _jitterBufferMinMs;
    private int _jitterBufferMaxMs;

    // ─── Network Change Detection ───
    private bool _networkListenerActive;

    // ─── Ozellikler ───
    private bool _dndEnabled;
    private bool _autoAnswerEnabled;
    private bool _muted;
    private bool _srtpEnabled;
    private string? _stunServer;
    private string? _turnServer;
    private string? _turnUsername;
    private string? _turnPassword;
    private string? _ringtonePath;
    private IPAddress? _stunPublicAddress;

    // ─── Recording (iki yonlu mono mix) ───
    private WaveFileWriter? _waveWriter;
    private bool _isRecording;
    private string? _recordingWavPath; // Sifrelemeden onceki WAV yolu
    private int _recordingPayloadType; // Aktif codec: 0=PCMU, 8=PCMA, 9=G722
    private WindowsAudioEndPoint? _winAudioEndPoint; // Mikrofon event'i icin referans
    private readonly System.Collections.Concurrent.ConcurrentQueue<byte[]> _recInQueue = new(); // Gelen ses (karsi taraf)
    private readonly System.Collections.Concurrent.ConcurrentQueue<byte[]> _recOutQueue = new(); // Giden ses (mikrofon)
    private readonly object _recWriteLock = new();
    private const int MaxRecordQueueDepth = 50; // ~1sn (20ms chunk basina)

    // ─── RTP Timeout (karsi taraf BYE gondermeden kapatirsa) ───
    private DateTime _lastRtpReceived;
    private System.Timers.Timer? _rtpTimeoutTimer;
    private const int RtpTimeoutSeconds = 8; // 8sn RTP gelmezse cagri bitti say

    // ─── Voicemail (MWI) ───
    private int _voicemailCount;
    private string? _voicemailNumber;

    // ─── SIP Presence (SUBSCRIBE/NOTIFY) ───
    private readonly Dictionary<string, string> _buddyPresence = new(); // SIP URI → presence status
    private bool _presenceEnabled;
    private string? _currentPresenceStatus; // "open", "closed", "busy", etc.

    // ─── Attended Transfer ───
    private int? _transferSourceLineIndex;

    // ─── Ringtone oynatici ───
    private WaveOutEvent? _ringtonePlayer;
    private AudioFileReader? _ringtoneReader;

    // ─── Hold Music ───
    private bool _holdMusicEnabled = true;

    // ─── Recording sifreleme ───
    private string? _encryptionKey;

    public NativeSipService()
    {
        for (int i = 0; i < MaxLines; i++)
            _lines[i] = new CallLine { Index = i };
    }

    /// <summary>Kayit sifreleme anahtarini ayarla (DI veya config'den).</summary>
    public void SetEncryptionKey(string key) => _encryptionKey = key;

    /// <summary>
    /// SIP event callback'lerini guvenli sekilde calistirir.
    /// Hata olursa loglar, uygulama crash etmez.
    /// </summary>
    private void SafeCallback(Action action, [System.Runtime.CompilerServices.CallerMemberName] string? caller = null)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Log($"[SIP] SafeCallback hatasi ({caller}): {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Async SIP event callback'lerini guvenli sekilde calistirir.
    /// </summary>
    private async void SafeCallbackAsync(Func<Task> action, [System.Runtime.CompilerServices.CallerMemberName] string? caller = null)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Log($"[SIP] SafeCallbackAsync hatasi ({caller}): {ex.Message}\n{ex.StackTrace}");
        }
    }

    // ═══════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════

    public bool IsRegistered { get; private set; }
    public bool IsInCall => ActiveLine.State is LineState.Connected or LineState.OnHold or LineState.Connecting;
    public bool IsOnHold => ActiveLine.State == LineState.OnHold;
    public bool IsDndEnabled => _dndEnabled;
    public bool IsAutoAnswerEnabled => _autoAnswerEnabled;
    public bool IsRecording => _isRecording;
    public string? LastRecordingPath => _recordingWavPath;
    public bool IsMuted => _muted;
    public int ActiveLineIndex => _activeLineIndex;
    public int LineCount => MaxLines;
    public float MicrophoneVolume => _micVolume;
    public float SpeakerVolume => _speakerVolume;
    public bool IsSrtpEnabled => _srtpEnabled;
    public string? StunServer => _stunServer;
    public string? TurnServer => _turnServer;
    public string? TurnUsername => _turnUsername;
    public int VoicemailCount => _voicemailCount;
    public string? RingtonePath => _ringtonePath;

    private CallLine ActiveLine => _lines[_activeLineIndex];

    // ═══════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════

    public event Func<Task>? OnRegistered;
    public event Func<string, Task>? OnRegistrationFailed;
    public event Func<string, string, Task>? OnIncomingCall;
    public event Func<Task>? OnCallAnswered;
    public event Func<Task>? OnCallEnded;
    public event Func<string, Task>? OnCallFailed;
    public event Func<int, Task>? OnLineChanged;
    public event Func<bool, Task>? OnMuteChanged;
    public event Func<int, Task>? OnVoicemailCountChanged;
    public event Func<string, string, Task>? OnBuddyPresenceChanged; // (sipUri, presenceStatus)
    public event Func<byte, int, Task>? OnDtmfReceived; // (tone 0-15, durationMs)

    // ═══════════════════════════════════════════════════
    // INITIALIZE & REGISTER
    // ═══════════════════════════════════════════════════

    public async Task<bool> InitializeAsync(SipConnectionInfoDto config)
    {
        try
        {
            _config = config;

            // SRTP ayarini config'den al
            _srtpEnabled = config.UseSrtp;

            // ICE/TURN bilgilerini config'den al
            if (!string.IsNullOrEmpty(config.StunServer) || !string.IsNullOrEmpty(config.TurnServer))
            {
                SetIceServers(config.StunServer, config.TurnServer, config.TurnUsername, config.TurnPassword);
            }

            var sipUri = SIPURI.ParseSIPURI(config.SipUri);
            if (sipUri == null)
            {
                await (OnRegistrationFailed?.Invoke("Gecersiz SIP URI") ?? Task.CompletedTask);
                return false;
            }

            _sipTransport = new SIPTransport();

            // Transport kanali
            var transport = config.Transport?.ToUpperInvariant() ?? "UDP";
            switch (transport)
            {
                case "WSS":
                case "WS":
                    _sipTransport.AddSIPChannel(new SIPWebSocketChannel(new IPEndPoint(IPAddress.Any, 0), null));
                    Log("[SIP] WebSocket (WSS) transport kanali eklendi");
                    break;
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

            // STUN ile NAT arkasindaki public IP'yi kesfet
            // SIP sinyallesme WSS ile gidse bile RTP medya UDP uzerinden gider — STUN her zaman gerekli
            if (!string.IsNullOrEmpty(_stunServer))
            {
                try
                {
                    var stunAddr = _stunServer!.Replace("stun:", "").Replace("stuns:", "");
                    var stunParts = stunAddr.Split(':');
                    var stunHost = stunParts[0];
                    var stunPort = stunParts.Length > 1 && int.TryParse(stunParts[1], out var p) ? p : 3478;

                    var publicEp = STUNClient.GetPublicIPEndPoint(stunHost, stunPort);
                    if (publicEp != null)
                    {
                        _stunPublicAddress = publicEp.Address;
                        // WSS'de SIP ContactHost degistirmeyiz (sinyallesme WSS uzerinden)
                        // Ama UDP/TCP/TLS'de ContactHost'u STUN adresiyle set ediyoruz
                        if (transport is not "WSS" and not "WS")
                            _sipTransport.ContactHost = publicEp.Address.ToString();
                        Log($"[SIP] STUN public address: {publicEp} (transport={transport})");
                    }
                }
                catch (Exception ex)
                {
                    Log($"[SIP] STUN discovery hatasi: {ex.Message}");
                }
            }

            // Registration — transport'a gore protokol ve host belirle
            var sipProtocol = transport switch
            {
                "WSS" => SIPProtocolsEnum.wss,
                "WS" => SIPProtocolsEnum.ws,
                "TCP" => SIPProtocolsEnum.tcp,
                "TLS" => SIPProtocolsEnum.tls,
                _ => SIPProtocolsEnum.udp
            };
            var sipScheme = sipProtocol is SIPProtocolsEnum.wss or SIPProtocolsEnum.tls
                ? SIPSchemesEnum.sips : SIPSchemesEnum.sip;

            // WSS icin WsUri'den host:port al (SIP sunucusu farkli portta dinliyor olabilir)
            var regHost = sipUri.Host;
            if (transport is "WSS" or "WS" && !string.IsNullOrEmpty(config.WsUri))
            {
                try
                {
                    var wsUri = new Uri(config.WsUri);
                    regHost = wsUri.Port > 0 && wsUri.Port != 443
                        ? $"{wsUri.Host}:{wsUri.Port}"
                        : wsUri.Host;
                    Log($"[SIP] WSS registration hedefi: {regHost} (WsUri: {config.WsUri})");
                }
                catch (Exception ex)
                {
                    Log($"[SIP] WsUri parse hatasi, SIP host kullaniliyor: {ex.Message}");
                }
            }

            var regUri = new SIPURI(sipUri.User, regHost, null, sipScheme, sipProtocol);
            _regAgent = new SIPRegistrationUserAgent(
                _sipTransport,
                config.AuthUsername,
                config.AuthPassword,
                regUri.ToString(),
                120);

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

            // Gelen SIP istekleri (INVITE, SUBSCRIBE/NOTIFY MWI)
            _sipTransport.SIPTransportRequestReceived += OnSipRequestReceived;

            // MWI icin SUBSCRIBE gonder (voicemail count)
            _voicemailNumber = config.AuthUsername; // Varsayilan: kendi extension'imiz

            _regAgent.Start();

            // Ag degisikligi algilama (VPN, Wi-Fi degisimi vb.)
            StartNetworkChangeDetection();

            return true;
        }
        catch (Exception ex)
        {
            // Baglanti basarisiz olsa bile ag dinlemeyi baslat
            // VPN sonradan baglaninca re-register denenecek
            StartNetworkChangeDetection();

            await (OnRegistrationFailed?.Invoke($"SIP init hatasi: {ex.Message}") ?? Task.CompletedTask);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════
    // CORE CALL METHODS
    // ═══════════════════════════════════════════════════

    public async Task<bool> MakeCallAsync(string destination)
    {
        // Aktif hat bossa onu kullan, degilse otomatik bos hat bul
        if (ActiveLine.State == LineState.Idle)
            return await MakeCallOnLineAsync(_activeLineIndex, destination);

        var freeLine = FindFreeLine();
        if (freeLine == null)
        {
            Log("[SIP] Tum hatlar dolu, arama baslatilemiyor");
            return false;
        }

        _activeLineIndex = freeLine.Index;
        return await MakeCallOnLineAsync(freeLine.Index, destination);
    }

    public async Task<bool> AnswerCallAsync()
    {
        var line = FindRingingLine();
        Log($"[SIP] AnswerCallAsync cagirildi. RingingLine={line?.Index.ToString() ?? "null"}");
        if (line == null || _sipTransport == null)
        {
            Log("[SIP] AnswerCallAsync: Ringing hat yok veya transport null");
            return false;
        }

        try
        {
            // Mevcut aktif arama varsa hold'a al
            if (ActiveLine.State == LineState.Connected && ActiveLine.Index != line.Index)
            {
                Log($"[SIP] Mevcut aktif hat {ActiveLine.Index} hold'a aliniyor");
                await HoldLineAsync(ActiveLine);
            }

            line.MediaSession = CreateMediaSession();
            Log($"[SIP] Cevaplaniyor: hat={line.Index}, remote={line.RemoteUri}");
            var result = await line.UserAgent!.Answer(line.PendingUas!, line.MediaSession);
            line.PendingUas = null;

            if (result)
            {
                line.State = LineState.Connected;
                line.StartTime = DateTime.Now;
                _activeLineIndex = line.Index;
                StopRingtone();
                Log($"[SIP] Gelen arama cevaplandi! Hat {line.Index} -> Connected");
                await (OnCallAnswered?.Invoke() ?? Task.CompletedTask);
                await (OnLineChanged?.Invoke(line.Index) ?? Task.CompletedTask);
            }
            else
            {
                Log("[SIP] Answer basarisiz (Answer() false dondu)");
            }

            return result;
        }
        catch (Exception ex)
        {
            Log($"[SIP] Answer hatasi: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> HangupAsync()
    {
        var line = ActiveLine;
        Log($"[SIP] HangupAsync cagirildi. ActiveLine={line.Index}, State={line.State}");

        // Connecting hattı varsa (giden arama calıyor) onu kapat
        if (line.State == LineState.Idle)
        {
            // Ringing (gelen) veya Connecting (giden) hat ara
            var ringing = FindRingingLine();
            var connecting = Array.Find(_lines, l => l.State == LineState.Connecting);
            line = ringing ?? connecting ?? line;
            if (ringing != null) Log($"[SIP] Ringing hat bulundu: {ringing.Index}");
            if (connecting != null) Log($"[SIP] Connecting hat bulundu: {connecting.Index}");
        }

        if (line.State == LineState.Idle && line.PendingUas == null)
        {
            Log("[SIP] Hangup: Hat idle ve PendingUas yok, iptal");
            return false;
        }

        try
        {
            if (line.PendingUas != null)
            {
                Log("[SIP] Gelen arama reddediliyor (BusyHere)");
                line.PendingUas.Reject(SIPResponseStatusCodesEnum.BusyHere, null);
                line.PendingUas = null;
                StopRingtone();
            }

            if (line.UserAgent != null)
            {
                // Connecting veya Connected — CANCEL veya BYE gonder
                Log($"[SIP] UserAgent.Cancel/Hangup cagirildi (IsCallActive={line.UserAgent.IsCallActive})");
                if (line.UserAgent.IsCallActive)
                    line.UserAgent.Hangup();
                else
                    line.UserAgent.Cancel();
            }

            StopRtpTimeoutTimer();
            CleanupLine(line);
            await (OnCallEnded?.Invoke() ?? Task.CompletedTask);

            // Baska aktif hat varsa ona gec
            var nextActive = Array.FindIndex(_lines, l => l.State != LineState.Idle);
            if (nextActive >= 0)
            {
                _activeLineIndex = nextActive;
                Log($"[SIP] Sonraki aktif hat: {nextActive}");
                await (OnLineChanged?.Invoke(_activeLineIndex) ?? Task.CompletedTask);
            }

            Log("[SIP] Hangup basarili");
            return true;
        }
        catch (Exception ex)
        {
            Log($"[SIP] Hangup hatasi: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> HoldAsync()
    {
        Log($"[SIP] HoldAsync cagirildi. ActiveLine.State={ActiveLine.State}, MediaSession null={ActiveLine.MediaSession == null}");
        if (ActiveLine.State != LineState.Connected) return false;
        return await HoldLineAsync(ActiveLine);
    }

    public async Task<bool> UnholdAsync()
    {
        Log($"[SIP] UnholdAsync cagirildi. ActiveLine.State={ActiveLine.State}");
        if (ActiveLine.State != LineState.OnHold) return false;

        try
        {
            // 1. Mikrofonu tekrar ac (mute degilse)
            if (ActiveLine.MediaSession?.AudioExtrasSource != null)
            {
                var source = _muted ? AudioSourcesEnum.Silence : AudioSourcesEnum.None;
                ActiveLine.MediaSession.AudioExtrasSource.SetSource(source);
                Log($"[SIP] AudioExtrasSource -> {source}");
            }

            // 2. SDP hold'u kaldir
            ActiveLine.MediaSession?.TakeOffHold();
            ActiveLine.State = LineState.Connected;
            Log("[SIP] Unhold basarili, State -> Connected");
            await (OnLineChanged?.Invoke(ActiveLine.Index) ?? Task.CompletedTask);
            return true;
        }
        catch (Exception ex)
        {
            Log($"[SIP] Unhold hatasi: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendDtmfAsync(string tone)
    {
        if (ActiveLine.UserAgent == null || ActiveLine.State != LineState.Connected) return false;

        try
        {
            byte dtmfEvent;
            if (byte.TryParse(tone, out var dtmfByte))
                dtmfEvent = dtmfByte;
            else
            {
                dtmfEvent = tone switch
                {
                    "*" => 10, "#" => 11,
                    "A" or "a" => 12, "B" or "b" => 13,
                    "C" or "c" => 14, "D" or "d" => 15,
                    _ => byte.MaxValue
                };
                if (dtmfEvent == byte.MaxValue) return false;
            }

            await ActiveLine.UserAgent.SendDtmf(dtmfEvent);
            return true;
        }
        catch (Exception ex)
        {
            Log($"[SIP] DTMF hatasi: {ex.Message}");
            return false;
        }
    }

    // ═══════════════════════════════════════════════════
    // TRANSFER
    // ═══════════════════════════════════════════════════

    public async Task<bool> BlindTransferAsync(string target)
    {
        if (ActiveLine.UserAgent == null || ActiveLine.State != LineState.Connected) return false;

        try
        {
            var targetUri = BuildTargetUri(target);
            if (targetUri == null) return false;

            var result = await ActiveLine.UserAgent.BlindTransfer(targetUri, TimeSpan.FromSeconds(10), default);
            if (result)
            {
                CleanupLine(ActiveLine);
                await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
            }
            return result;
        }
        catch (Exception ex)
        {
            Log($"[SIP] Blind transfer hatasi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Attended (danismali) transfer baslat:
    /// 1. Mevcut aramayi hold'a al
    /// 2. Yeni hatta hedefi ara
    /// 3. Konustuktan sonra CompleteAttendedTransferAsync ile transferi tamamla
    /// </summary>
    public async Task<bool> AttendedTransferAsync(string target)
    {
        if (ActiveLine.State != LineState.Connected) return false;

        try
        {
            // Kaynak hatti hatirla ve hold'a al
            _transferSourceLineIndex = _activeLineIndex;
            await HoldLineAsync(ActiveLine);
            ActiveLine.State = LineState.TransferPending;

            // Bos hat bul
            var freeLine = FindFreeLine();
            if (freeLine == null)
            {
                await (OnCallFailed?.Invoke("Bos hat yok — attended transfer yapilamiyor") ?? Task.CompletedTask);
                return false;
            }

            // Yeni hatta hedefi ara
            _activeLineIndex = freeLine.Index;
            await (OnLineChanged?.Invoke(_activeLineIndex) ?? Task.CompletedTask);
            return await MakeCallOnLineAsync(freeLine.Index, target);
        }
        catch (Exception ex)
        {
            Log($"[SIP] Attended transfer hatasi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Attended transfer'i tamamla — kaynak hattaki aramayi hedef hattaki kisiye bagla.
    /// </summary>
    public async Task<bool> CompleteAttendedTransferAsync()
    {
        if (_transferSourceLineIndex == null) return false;

        var sourceLine = _lines[_transferSourceLineIndex.Value];
        var targetLine = ActiveLine;

        if (sourceLine.UserAgent == null || targetLine.UserAgent == null) return false;

        try
        {
            // SIPSorcery attended transfer: kaynak UserAgent.AttendedTransfer(hedef dialog)
            var targetDialog = targetLine.UserAgent.Dialogue;
            if (targetDialog == null) return false;

            var result = await sourceLine.UserAgent.AttendedTransfer(targetDialog, TimeSpan.FromSeconds(10), default);

            if (result)
            {
                CleanupLine(sourceLine);
                CleanupLine(targetLine);
                _transferSourceLineIndex = null;
                await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
            }

            return result;
        }
        catch (Exception ex)
        {
            Log($"[SIP] Attended transfer tamamlama hatasi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Attended transfer iptal — kaynak hata geri don.
    /// </summary>
    public async Task<bool> CancelAttendedTransferAsync()
    {
        if (_transferSourceLineIndex == null) return false;

        // Hedef hatti kapat
        if (ActiveLine.UserAgent?.IsCallActive == true)
            ActiveLine.UserAgent.Hangup();
        CleanupLine(ActiveLine);

        // Kaynak hata geri don
        var sourceLine = _lines[_transferSourceLineIndex.Value];
        _activeLineIndex = sourceLine.Index;
        sourceLine.State = LineState.OnHold;
        _transferSourceLineIndex = null;

        // Unhold
        sourceLine.MediaSession?.TakeOffHold();
        sourceLine.State = LineState.Connected;

        await (OnLineChanged?.Invoke(_activeLineIndex) ?? Task.CompletedTask);
        return true;
    }

    // Eski TransferAsync uyumluluk icin (ISipService'de artik BlindTransferAsync)
    // Mevcut Dialer/TransferDialog bunu kullaniyor olabilir, yönlendir
    // Not: ISipService'den kaldirildi, sadece BlindTransferAsync var

    // ═══════════════════════════════════════════════════
    // MULTI-LINE
    // ═══════════════════════════════════════════════════

    public Task<List<LineInfo>> GetLinesAsync()
    {
        var list = _lines.Select(l => new LineInfo
        {
            Index = l.Index,
            IsActive = l.Index == _activeLineIndex,
            IsOnHold = l.State == LineState.OnHold,
            IsRinging = l.State == LineState.Ringing,
            RemoteParty = l.RemoteUri,
            RemoteDisplayName = l.RemoteDisplayName,
            StartTime = l.StartTime,
            State = l.State
        }).ToList();
        return Task.FromResult(list);
    }

    public async Task<bool> SwitchLineAsync(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= MaxLines) return false;
        if (lineIndex == _activeLineIndex) return true;

        // Mevcut aktif hat connected ise hold'a al
        if (ActiveLine.State == LineState.Connected)
            await HoldLineAsync(ActiveLine);

        _activeLineIndex = lineIndex;

        // Yeni hat hold'daysa unhold
        if (ActiveLine.State == LineState.OnHold)
        {
            ActiveLine.MediaSession?.TakeOffHold();
            ActiveLine.State = LineState.Connected;
        }

        await (OnLineChanged?.Invoke(lineIndex) ?? Task.CompletedTask);
        return true;
    }

    public async Task<bool> MakeCallOnNewLineAsync(string destination)
    {
        var freeLine = FindFreeLine();
        if (freeLine == null) return false;

        // Mevcut hatti hold'a al
        if (ActiveLine.State == LineState.Connected)
            await HoldLineAsync(ActiveLine);

        _activeLineIndex = freeLine.Index;
        await (OnLineChanged?.Invoke(freeLine.Index) ?? Task.CompletedTask);
        return await MakeCallOnLineAsync(freeLine.Index, destination);
    }

    // ═══════════════════════════════════════════════════
    // DND & AUTO-ANSWER
    // ═══════════════════════════════════════════════════

    public void SetDnd(bool enabled) => _dndEnabled = enabled;

    public void SetAutoAnswer(bool enabled) => _autoAnswerEnabled = enabled;

    // ═══════════════════════════════════════════════════
    // MUTE
    // ═══════════════════════════════════════════════════

    public async Task<bool> MuteAsync()
    {
        Log($"[SIP] MuteAsync cagirildi. ActiveLine.State={ActiveLine.State}, AudioExtrasSource null={ActiveLine.MediaSession?.AudioExtrasSource == null}");
        _muted = true;
        // Sadece mikrofonu kapat — karsi tarafin sesi devam etsin
        if (ActiveLine.MediaSession?.AudioExtrasSource != null)
        {
            ActiveLine.MediaSession.AudioExtrasSource.SetSource(AudioSourcesEnum.Silence);
            Log("[SIP] Mute: AudioExtrasSource -> Silence");
        }
        else
        {
            Log("[SIP] UYARI: Mute icin AudioExtrasSource bulunamadi");
        }
        await (OnMuteChanged?.Invoke(true) ?? Task.CompletedTask);
        return true;
    }

    public async Task<bool> UnmuteAsync()
    {
        Log($"[SIP] UnmuteAsync cagirildi. ActiveLine.State={ActiveLine.State}");
        _muted = false;
        // Mikrofonu tekrar ac (hold'da degilse)
        if (ActiveLine.MediaSession?.AudioExtrasSource != null)
        {
            if (ActiveLine.State == LineState.OnHold)
            {
                Log("[SIP] Unmute: Hat hold'da, sessizlik devam ediyor");
            }
            else
            {
                ActiveLine.MediaSession.AudioExtrasSource.SetSource(AudioSourcesEnum.None);
                Log("[SIP] Unmute: AudioExtrasSource -> None (mikrofon acildi)");
            }
        }
        else
        {
            Log("[SIP] UYARI: Unmute icin AudioExtrasSource bulunamadi");
        }
        await (OnMuteChanged?.Invoke(false) ?? Task.CompletedTask);
        return true;
    }

    // ═══════════════════════════════════════════════════
    // VOLUME
    // ═══════════════════════════════════════════════════

    public void SetMicrophoneVolume(float volume)
    {
        _micVolume = Math.Clamp(volume, 0f, 1f);
    }

    public void SetSpeakerVolume(float volume)
    {
        _speakerVolume = Math.Clamp(volume, 0f, 1f);
    }

    // ═══════════════════════════════════════════════════
    // RECORDING
    // ═══════════════════════════════════════════════════

    public Task<bool> StartRecordingAsync(string? filePath = null)
    {
        if (_isRecording) return Task.FromResult(false);

        try
        {
            var recordingsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CorpLynk", "Recordings");
            var path = filePath ?? Path.Combine(recordingsDir, $"call_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception dirEx)
                {
                    Log($"[SIP] Kayit klasoru olusturulamadi: {dir} — {dirEx.Message}");
                    // Ag yolu erisilemediyse varsayilan lokale dusur
                    var fallbackDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "CorpLynk", "Recordings");
                    Directory.CreateDirectory(fallbackDir);
                    path = Path.Combine(fallbackDir, Path.GetFileName(path));
                    Log($"[SIP] Varsayilan klasore dusuldu: {path}");
                }
            }

            // Aktif aramanin codec'ini tespit et (SDP'den)
            _recordingPayloadType = DetectActiveCodecPayloadType();
            int sampleRate = AudioCodecDecoder.GetSampleRate(_recordingPayloadType);

            _waveWriter = new WaveFileWriter(path, new WaveFormat(sampleRate, 16, 1));
            _recordingWavPath = path;
            _isRecording = true;

            // Kuyruklari temizle
            while (_recInQueue.TryDequeue(out _)) { }
            while (_recOutQueue.TryDequeue(out _)) { }

            Log($"[SIP] Recording baslatildi (iki yonlu mono mix) — codec PT={_recordingPayloadType}, sampleRate={sampleRate}Hz, dosya={path}");

            // Gelen ses: RTP paketlerinden (karsi taraf)
            if (ActiveLine.MediaSession != null)
            {
                ActiveLine.MediaSession.OnRtpPacketReceived += OnRtpPacketForRecording;
            }

            // Giden ses: Mikrofon encode edilmis sample'lar
            if (_winAudioEndPoint != null)
            {
                _winAudioEndPoint.OnAudioSourceEncodedSample += OnMicSampleForRecording;
                Log("[SIP] Mikrofon kaydi aktif (OnAudioSourceEncodedSample)");
            }
            else
            {
                Log("[SIP] UYARI: WindowsAudioEndPoint null — sadece gelen ses kaydedilecek!");
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Log($"[SIP] Recording baslatilamadi: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Aktif aramanin uzlasilmis ses codec'ini SDP'den tespit eder.
    /// Bulunamazsa varsayilan PCMU (0) doner.
    /// </summary>
    private int DetectActiveCodecPayloadType()
    {
        try
        {
            var session = ActiveLine.MediaSession;
            var capabilities = session?.AudioLocalTrack?.Capabilities;
            if (capabilities != null && capabilities.Count > 0)
            {
                // Uzlasilmis (negotiated) ilk codec'in payload type'ini al
                return capabilities[0].ID; // RTP payload type (0=PCMU, 8=PCMA, 9=G722)
            }
        }
        catch (Exception ex)
        {
            Log($"[SIP] Codec tespit hatasi: {ex.Message}");
        }

        // Varsayilan: PCMU
        return 0;
    }

    public async Task<bool> StopRecordingAsync()
    {
        if (!_isRecording) return false;

        try
        {
            if (ActiveLine.MediaSession != null)
            {
                ActiveLine.MediaSession.OnRtpPacketReceived -= OnRtpPacketForRecording;
            }

            if (_winAudioEndPoint != null)
            {
                _winAudioEndPoint.OnAudioSourceEncodedSample -= OnMicSampleForRecording;
            }

            // Kuyruklardaki kalan veriyi yaz
            FlushRemainingRecordingQueues();

            _waveWriter?.Flush();
            _waveWriter?.Dispose();
            _waveWriter = null;
            _isRecording = false;

            // WAV dosyasini AES-256 ile sifrele
            if (!string.IsNullOrEmpty(_recordingWavPath) && File.Exists(_recordingWavPath))
            {
                try
                {
                    var encPath = Path.ChangeExtension(_recordingWavPath, ".enc");
                    var key = CallCenter.Shared.Services.FileEncryptionService.DeriveKey(
                        _encryptionKey ?? "DefaultEncryptionKey");
                    await CallCenter.Shared.Services.FileEncryptionService.EncryptFileAsync(
                        _recordingWavPath, encPath, key);

                    // Orijinal WAV'i sil — sadece sifreli .enc kalsin
                    File.Delete(_recordingWavPath);
                    _recordingWavPath = encPath;

                    Log($"[SIP] Recording sifrelendi: {encPath}");
                }
                catch (Exception encEx)
                {
                    Log($"[SIP] Recording sifreleme hatasi (WAV korundu): {encEx.Message}");
                    // Sifreleme basarisiz olursa WAV oldugu gibi kalir
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log($"[SIP] Recording durdurulamadi: {ex.Message}");
            return false;
        }
    }

    /// <summary>Gelen ses (karsi taraf) — RTP paketlerinden PCM'e decode edip kuyruga ekler.</summary>
    private void OnRtpPacketForRecording(IPEndPoint remoteEP, SDPMediaTypesEnum mediaType, RTPPacket rtpPacket)
    {
        if (mediaType != SDPMediaTypesEnum.audio || _waveWriter == null) return;

        try
        {
            int pt = rtpPacket.Header.PayloadType;
            if (pt >= 96) pt = _recordingPayloadType;

            var pcm = AudioCodecDecoder.Decode(rtpPacket.Payload, pt);
            _recInQueue.Enqueue(pcm);
            // Mix islemini ayri thread'de yap - RTP alma pipeline'ini bloklama
            ThreadPool.QueueUserWorkItem(_ => TryWriteMixedAudio());
        }
        catch { /* Kayit hatasi sessizce gecilebilir */ }
    }

    /// <summary>Giden ses (mikrofon) — encode edilmis sample'i PCM'e decode edip kuyruga ekler.</summary>
    private void OnMicSampleForRecording(uint durationRtpUnits, byte[] sample)
    {
        if (_waveWriter == null) return;

        // Mikrofon pipeline'ini bloklamamak icin tamamen asenkron
        var payload = sample;
        var pt = _recordingPayloadType;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                var pcm = AudioCodecDecoder.Decode(payload, pt);
                _recOutQueue.Enqueue(pcm);
                TryWriteMixedAudio();
            }
            catch { }
        });
    }

    /// <summary>
    /// Iki kuyruktan eslestirilmis chunk'lari alip mono mix yaparak WAV'a yazar.
    /// Her iki taraftan da veri varsa sample-by-sample toplanir.
    /// Bir taraf sessizse ve kuyruk doluysa tek tarafli yazilir.
    /// </summary>
    private void TryWriteMixedAudio()
    {
        lock (_recWriteLock)
        {
            if (_waveWriter == null) return;

            // 1. Eslestirilmis mix: Her iki kuyrukta veri varsa birlikte yaz
            while (_recInQueue.TryPeek(out _) && _recOutQueue.TryPeek(out _))
            {
                _recInQueue.TryDequeue(out var inPcm);
                _recOutQueue.TryDequeue(out var outPcm);
                if (inPcm == null || outPcm == null) break;

                var mixed = MixPcmMono(inPcm, outPcm);
                _waveWriter.Write(mixed, 0, mixed.Length);
            }

            // 2. Tasmasi onle: Bir taraf sessizse (ornegin hold) kuyruk buyumesin
            while (_recInQueue.Count > MaxRecordQueueDepth && _recInQueue.TryDequeue(out var overflow))
                _waveWriter.Write(overflow, 0, overflow.Length);
            while (_recOutQueue.Count > MaxRecordQueueDepth && _recOutQueue.TryDequeue(out var overflow))
                _waveWriter.Write(overflow, 0, overflow.Length);
        }
    }

    /// <summary>Recording durdurulurken kuyruklardaki kalan veriyi yazar.</summary>
    private void FlushRemainingRecordingQueues()
    {
        lock (_recWriteLock)
        {
            if (_waveWriter == null) return;

            // Once eslestirebilenler
            while (_recInQueue.TryPeek(out _) && _recOutQueue.TryPeek(out _))
            {
                _recInQueue.TryDequeue(out var inPcm);
                _recOutQueue.TryDequeue(out var outPcm);
                if (inPcm == null || outPcm == null) break;
                var mixed = MixPcmMono(inPcm, outPcm);
                _waveWriter.Write(mixed, 0, mixed.Length);
            }

            // Kalanlar tek tarafli
            while (_recInQueue.TryDequeue(out var remaining))
                _waveWriter.Write(remaining, 0, remaining.Length);
            while (_recOutQueue.TryDequeue(out var remaining))
                _waveWriter.Write(remaining, 0, remaining.Length);
        }
    }

    /// <summary>
    /// Iki PCM16 byte dizisini sample-by-sample toplayip mono mix uretir.
    /// Clipping korumasıyla short tasmasi onlenir.
    /// </summary>
    private static byte[] MixPcmMono(byte[] a, byte[] b)
    {
        int len = Math.Max(a.Length, b.Length);
        var result = new byte[len];
        int samples = len / 2;

        for (int i = 0; i < samples; i++)
        {
            int offset = i * 2;
            short sA = offset + 1 < a.Length
                ? (short)(a[offset] | (a[offset + 1] << 8))
                : (short)0;
            short sB = offset + 1 < b.Length
                ? (short)(b[offset] | (b[offset + 1] << 8))
                : (short)0;

            // Toplama + clipping
            int mixed = sA + sB;
            if (mixed > short.MaxValue) mixed = short.MaxValue;
            if (mixed < short.MinValue) mixed = short.MinValue;

            result[offset] = (byte)(mixed & 0xFF);
            result[offset + 1] = (byte)((mixed >> 8) & 0xFF);
        }

        return result;
    }

    // ═══════════════════════════════════════════════════
    // CODEC
    // ═══════════════════════════════════════════════════

    public List<CodecInfo> GetAvailableCodecs()
    {
        return new List<CodecInfo>
        {
            new() { Name = "OPUS", PayloadType = 111, SampleRate = 48000, IsEnabled = _enabledCodecNames.Contains("OPUS"), Priority = 0 },
            new() { Name = "G722", PayloadType = 9, SampleRate = 16000, IsEnabled = _enabledCodecNames.Contains("G722"), Priority = 1 },
            new() { Name = "PCMU", PayloadType = 0, SampleRate = 8000, IsEnabled = _enabledCodecNames.Contains("PCMU"), Priority = 2 },
            new() { Name = "PCMA", PayloadType = 8, SampleRate = 8000, IsEnabled = _enabledCodecNames.Contains("PCMA"), Priority = 3 },
            new() { Name = "G726", PayloadType = 2, SampleRate = 8000, IsEnabled = _enabledCodecNames.Contains("G726"), Priority = 4 },
            new() { Name = "SPEEX", PayloadType = 97, SampleRate = 8000, IsEnabled = _enabledCodecNames.Contains("SPEEX"), Priority = 5 },
            new() { Name = "ILBC", PayloadType = 98, SampleRate = 8000, IsEnabled = _enabledCodecNames.Contains("ILBC"), Priority = 6 },
        };
    }

    /// <summary>SipAccount'tan gelen codec tercihlerini uygular (JSON array).</summary>
    public void ApplyCodecPreferences(string? preferredCodecsJson)
    {
        if (string.IsNullOrWhiteSpace(preferredCodecsJson)) return;
        try
        {
            var codecs = JsonSerializer.Deserialize<List<string>>(preferredCodecsJson);
            if (codecs != null && codecs.Count > 0)
            {
                _enabledCodecNames = codecs.Select(c => c.ToUpperInvariant()).ToList();
            }
        }
        catch { /* Gecersiz JSON — varsayilani koru */ }
    }

    /// <summary>Jitter buffer parametrelerini ayarlar.</summary>
    public void SetJitterBuffer(int minMs, int maxMs)
    {
        _jitterBufferMinMs = minMs;
        _jitterBufferMaxMs = maxMs;
    }

    /// <summary>Opus decoder/encoder'i baslat.</summary>
    private void EnsureOpusInitialized()
    {
#pragma warning disable CS0618 // Concentus managed fallback — native desteklenmiyor
        _opusDecoder ??= new OpusDecoder(48000, 1);
        _opusEncoder ??= new OpusEncoder(48000, 1, OpusApplication.OPUS_APPLICATION_VOIP);
#pragma warning restore CS0618
    }

    public List<CodecInfo> GetEnabledCodecs()
    {
        return GetAvailableCodecs().Where(c => c.IsEnabled).ToList();
    }

    public void SetEnabledCodecs(List<string> codecNames)
    {
        _enabledCodecNames = codecNames;
    }

    // ═══════════════════════════════════════════════════
    // SRTP & STUN
    // ═══════════════════════════════════════════════════

    public void SetSrtp(bool enabled) => _srtpEnabled = enabled;

    public void SetStunServer(string? stunServer) => _stunServer = stunServer;

    public void SetIceServers(string? stunServer, string? turnServer, string? turnUsername, string? turnPassword)
    {
        _stunServer = stunServer;
        _turnServer = turnServer;
        _turnUsername = turnUsername;
        _turnPassword = turnPassword;
    }

    // ═══════════════════════════════════════════════════
    // NETWORK CHANGE DETECTION
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Ag degisikligini dinler. Baglanti kesilirse SIP re-register yapar.
    /// </summary>
    public void StartNetworkChangeDetection()
    {
        if (_networkListenerActive) return;
        _networkListenerActive = true;

        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
        Log("[SIP] Ag degisikligi algilama baslatildi.");
    }

    public void StopNetworkChangeDetection()
    {
        if (!_networkListenerActive) return;
        _networkListenerActive = false;

        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
    }

    private async void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        Log($"[SIP] Ag durumu degisti: {(e.IsAvailable ? "bagli" : "baglanti yok")}");
        if (!e.IsAvailable) return;

        await Task.Delay(2000); // Ag stabilizasyonu icin bekle
        await ReRegisterOrReinitializeAsync("Ag durumu degisikligi");
    }

    private async void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        Log("[SIP] Ag adresi degisti (VPN, Wi-Fi vb.)");
        await Task.Delay(3000); // Adres degisikligi stabilize olsun
        await ReRegisterOrReinitializeAsync("Ag adresi degisikligi");
    }

    /// <summary>
    /// Ag degisikliginde SIP'i yeniden kayit eder.
    /// _regAgent varsa stop/start, yoksa (ilk baglanti basarisiz olduysa) tamamen yeniden initialize eder.
    /// </summary>
    private async Task ReRegisterOrReinitializeAsync(string reason)
    {
        try
        {
            if (_sipTransport != null && _regAgent != null)
            {
                // Normal re-register
                _regAgent.Stop();
                await Task.Delay(500);
                _regAgent.Start();
                Log($"[SIP] {reason} sonrasi re-register baslatildi.");
            }
            else if (_config != null)
            {
                // Ilk baglanti basarisiz olduysa tamamen yeniden initialize et
                Log($"[SIP] {reason}: SIP transport/agent null — tam yeniden baglanti deneniyor...");
                await InitializeAsync(_config);
            }
            else
            {
                Log($"[SIP] {reason}: Config yok — re-register atalanıyor.");
            }
        }
        catch (Exception ex)
        {
            Log($"[SIP] {reason} sonrasi re-register hatasi: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════
    // RTP TIMEOUT (karsi taraf BYE gondermeden kapatirsa)
    // ═══════════════════════════════════════════════════

    private void StartRtpTimeoutTimer()
    {
        StopRtpTimeoutTimer();
        _rtpTimeoutTimer = new System.Timers.Timer(2000); // 2sn aralikla kontrol
        _rtpTimeoutTimer.Elapsed += (s, e) =>
        {
            // Aktif arama yoksa timer'i durdur
            if (!IsInCall)
            {
                StopRtpTimeoutTimer();
                return;
            }

            var elapsed = (DateTime.UtcNow - _lastRtpReceived).TotalSeconds;
            if (elapsed > RtpTimeoutSeconds)
            {
                Log($"[SIP] RTP timeout! {elapsed:F0}sn dir ses gelmedi — cagri sonlandiriliyor");
                StopRtpTimeoutTimer();

                // Cagriyi sonlandir
                SafeCallbackAsync(async () =>
                {
                    var activeLine = ActiveLine;
                    if (activeLine.State != LineState.Idle)
                    {
                        if (activeLine.UserAgent?.IsCallActive == true)
                            activeLine.UserAgent.Hangup();
                        CleanupLine(activeLine);
                        await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
                    }
                });
            }
        };
        _rtpTimeoutTimer.Start();
    }

    private void StopRtpTimeoutTimer()
    {
        _rtpTimeoutTimer?.Stop();
        _rtpTimeoutTimer?.Dispose();
        _rtpTimeoutTimer = null;
    }

    // ═══════════════════════════════════════════════════
    // INBAND DTMF
    // ═══════════════════════════════════════════════════

    /// <summary>DTMF frekans tablosu (ITU-T Q.23)</summary>
    private static readonly Dictionary<char, (int Low, int High)> DtmfFrequencies = new()
    {
        { '1', (697, 1209) }, { '2', (697, 1336) }, { '3', (697, 1477) }, { 'A', (697, 1633) },
        { '4', (770, 1209) }, { '5', (770, 1336) }, { '6', (770, 1477) }, { 'B', (770, 1633) },
        { '7', (852, 1209) }, { '8', (852, 1336) }, { '9', (852, 1477) }, { 'C', (852, 1633) },
        { '*', (941, 1209) }, { '0', (941, 1336) }, { '#', (941, 1477) }, { 'D', (941, 1633) },
    };

    /// <summary>
    /// Inband DTMF: Audio stream icerisine DTMF tone enjekte eder.
    /// Eski PBX'lerle uyumluluk icin kullanilir (RFC 2833 desteklemeyen sistemler).
    /// </summary>
    public byte[] GenerateInbandDtmfTone(char digit, int sampleRate = 8000, int durationMs = 100)
    {
        if (!DtmfFrequencies.TryGetValue(char.ToUpper(digit), out var freqs))
            return Array.Empty<byte>();

        int numSamples = sampleRate * durationMs / 1000;
        var pcm = new byte[numSamples * 2]; // 16-bit PCM

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            // Dual-tone: iki sinüs dalgası toplami
            double sample = 0.5 * Math.Sin(2 * Math.PI * freqs.Low * t)
                          + 0.5 * Math.Sin(2 * Math.PI * freqs.High * t);
            short s16 = (short)(sample * short.MaxValue * 0.7); // %70 volume
            pcm[i * 2] = (byte)(s16 & 0xFF);
            pcm[i * 2 + 1] = (byte)(s16 >> 8);
        }

        return pcm;
    }

    // ═══════════════════════════════════════════════════
    // AUDIO DEVICES
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
        Log($"[SIP] Giris cihazi degistirildi: index={deviceIndex} (onceki={_inputDeviceIndex}). Sonraki aramada gecerli olacak.");
        _inputDeviceIndex = deviceIndex;
        return Task.FromResult(true);
    }

    public Task<bool> SetAudioOutputDeviceAsync(int deviceIndex)
    {
        Log($"[SIP] Cikis cihazi degistirildi: index={deviceIndex} (onceki={_outputDeviceIndex}). Sonraki aramada gecerli olacak.");
        _outputDeviceIndex = deviceIndex;
        return Task.FromResult(true);
    }

    // ═══════════════════════════════════════════════════
    // VOICEMAIL (MWI)
    // ═══════════════════════════════════════════════════

    public async Task<bool> DialVoicemailAsync()
    {
        if (string.IsNullOrEmpty(_voicemailNumber)) return false;
        // Voicemail numarasini ara (genellikle *97 veya *98)
        return await MakeCallAsync($"*97");
    }

    // ═══════════════════════════════════════════════════
    // RINGTONE
    // ═══════════════════════════════════════════════════

    public void SetRingtone(string? filePath)
    {
        _ringtonePath = filePath;
    }

    public void SetHoldMusicEnabled(bool enabled)
    {
        _holdMusicEnabled = enabled;
        Log($"[SIP] Hold muzigi: {(enabled ? "aktif" : "deaktif")}");
    }

    private void PlayRingtone()
    {
        try
        {
            StopRingtone();

            if (!string.IsNullOrEmpty(_ringtonePath) && File.Exists(_ringtonePath))
            {
                _ringtoneReader = new AudioFileReader(_ringtonePath);
                _ringtonePlayer = new WaveOutEvent { DeviceNumber = _outputDeviceIndex };

                // Loop icin LoopStream
                var loopStream = new LoopStream(_ringtoneReader);
                _ringtonePlayer.Init(loopStream);
                _ringtonePlayer.Volume = _speakerVolume;
                _ringtonePlayer.Play();
            }
            else
            {
                // Varsayilan sistem sesi
                System.Media.SystemSounds.Asterisk.Play();
            }
        }
        catch (Exception ex)
        {
            Log($"[SIP] Ringtone hatasi: {ex.Message}");
        }
    }

    private void StopRingtone()
    {
        try
        {
            _ringtonePlayer?.Stop();
            _ringtonePlayer?.Dispose();
            _ringtonePlayer = null;
            _ringtoneReader?.Dispose();
            _ringtoneReader = null;
        }
        catch { }
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: Gelen SIP istekleri
    // ═══════════════════════════════════════════════════

    private async Task OnSipRequestReceived(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
    {
        var callId = sipRequest.Header.CallId;
        var toTag = sipRequest.Header.To?.ToTag;
        Log($"[SIP] <<< Gelen SIP istegi: {sipRequest.Method} from {remoteEndPoint}, CallId={callId}, ToTag={toTag ?? "(yok)"}");

        if (sipRequest.Method == SIPMethodsEnum.INVITE)
        {
            await HandleIncomingInvite(sipRequest);
        }
        else if (sipRequest.Method == SIPMethodsEnum.NOTIFY)
        {
            HandleNotify(sipRequest);
        }
        else if (sipRequest.Method == SIPMethodsEnum.BYE)
        {
            Log($"[SIP] BYE istegi alindi, CallId={callId}");

            // 1) Dialog CallId ile eslestir
            var byeLine = Array.Find(_lines, l =>
                l.UserAgent?.Dialogue?.CallId == callId && l.State != LineState.Idle);

            // 2) Eslesmezse, aktif hat varsa onu al (GoIP farkli CallId gonderebilir)
            if (byeLine == null)
            {
                byeLine = Array.Find(_lines, l => l.State is LineState.Connected or LineState.OnHold);
                if (byeLine != null)
                    Log($"[SIP] BYE CallId eslesmedi ama aktif hat {byeLine.Index} bulundu — kapatiliyor");
                else
                    Log($"[SIP] BYE CallId eslesmedi ve aktif hat yok — loglandi");
            }

            // 200 OK her durumda gonder (GoIP tekrar gondermesin)
            try
            {
                var okResp = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null);
                await _sipTransport!.SendResponseAsync(okResp);
            }
            catch { }

            if (byeLine != null)
            {
                Log($"[SIP] BYE eslesti, hat {byeLine.Index} temizleniyor");
                StopRtpTimeoutTimer();
                CleanupLine(byeLine);
                await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
            }
        }
        else if (sipRequest.Method == SIPMethodsEnum.ACK)
        {
            // ACK normal SIP akisi — loglama yeterli
            Log($"[SIP] ACK alindi, CallId={callId}");
        }
        else if (sipRequest.Method == SIPMethodsEnum.CANCEL)
        {
            Log($"[SIP] CANCEL alindi, CallId={callId}");
            // Ringing hatlarda eslesen varsa temizle
            var cancelLine = Array.Find(_lines, l => l.State == LineState.Ringing);
            if (cancelLine != null)
            {
                Log($"[SIP] CANCEL eslesti, hat {cancelLine.Index} temizleniyor");
                // 200 OK to CANCEL
                var okCancel = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null);
                await _sipTransport!.SendResponseAsync(okCancel);
                StopRingtone();
                CleanupLine(cancelLine);
                await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
            }
        }
    }

    private async Task HandleIncomingInvite(SIPRequest sipRequest)
    {
        var fromUri = sipRequest.Header.From?.FromURI?.ToString() ?? "?";
        var fromName = sipRequest.Header.From?.FromName ?? "";
        var callId = sipRequest.Header.CallId;
        var toTag = sipRequest.Header.To?.ToTag;

        Log($"[SIP] === GELEN INVITE === From: {fromName} <{fromUri}>, CallId={callId}, ToTag={toTag ?? "(yok)"}");

        // ── re-INVITE kontrolu ──
        // To tag varsa bu mevcut dialog icindeki bir re-INVITE (hold, codec degisikligi vb.)
        // Yeni arama degil — mevcut UserAgent kendi icerisinde handle etmeli
        if (!string.IsNullOrEmpty(toTag))
        {
            Log($"[SIP] re-INVITE algilandi (ToTag={toTag}), yeni arama degil — atlanıyor");
            // Mevcut hatlardaki UserAgent'lar bunu dialog handler uzerinden alacak.
            // Eger hicbir dialog eslesmediyse 481 (Call/Transaction Does Not Exist) gonder
            var existingLine = Array.Find(_lines, l =>
                l.UserAgent?.Dialogue?.CallId == callId &&
                l.State is LineState.Connected or LineState.OnHold);
            if (existingLine == null)
            {
                Log("[SIP] re-INVITE icin eslesen dialog bulunamadi, 481 gonderiliyor");
                var noDialog = SIPResponse.GetResponse(sipRequest,
                    SIPResponseStatusCodesEnum.CallLegTransactionDoesNotExist, null);
                await _sipTransport!.SendResponseAsync(noDialog);
            }
            return;
        }

        // ── Ayni Call-ID ile tekrar INVITE kontrolu (retransmission) ──
        var duplicateLine = Array.Find(_lines, l =>
            l.UserAgent != null &&
            l.State == LineState.Ringing &&
            l.PendingUas != null);
        if (duplicateLine != null)
        {
            // Zaten ringing durumunda bir hat var — ayni Call-ID mi kontrol et
            Log($"[SIP] Zaten ringing hat mevcut (hat={duplicateLine.Index}), muhtemel retransmission — atlanıyor");
            return;
        }

        // DND aktifse reject
        if (_dndEnabled)
        {
            Log("[SIP] DND aktif, arama reddediliyor (BusyHere)");
            var dndResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.BusyHere, "DND aktif");
            await _sipTransport!.SendResponseAsync(dndResponse);
            return;
        }

        // Bos hat bul
        var freeLine = FindFreeLine();
        if (freeLine == null)
        {
            Log("[SIP] Bos hat yok, arama reddediliyor (BusyHere)");
            var busyResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.BusyHere, null);
            await _sipTransport!.SendResponseAsync(busyResponse);
            return;
        }

        Log($"[SIP] Bos hat bulundu: {freeLine.Index}");

        // SIPUserAgent olustur
        freeLine.UserAgent = new SIPUserAgent(_sipTransport!, null);
        freeLine.UserAgent.OnCallHungup += (dialog) => SafeCallbackAsync(async () =>
        {
            Log($"[SIP] Gelen arama OnCallHungup (hat={freeLine.Index}, State={freeLine.State})");
            if (freeLine.State == LineState.Idle)
            {
                Log("[SIP] Hat zaten Idle — BYE handler temizlemis, OnCallEnded atlaniyor");
                return;
            }
            StopRtpTimeoutTimer();
            CleanupLine(freeLine);
            await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
        });

        // DTMF alma — karsi tarafin tuslamalarini dinle
        freeLine.UserAgent.OnDtmfTone += (tone, duration) => SafeCallbackAsync(async () =>
        {
            Log($"[SIP] DTMF alindi: tone={tone}, duration={duration}ms (hat={freeLine.Index})");
            await (OnDtmfReceived?.Invoke(tone, duration) ?? Task.CompletedTask);
        });

        // AcceptCall (180 Ringing)
        freeLine.PendingUas = freeLine.UserAgent.AcceptCall(sipRequest);
        freeLine.State = LineState.Ringing;

        var callerUri = sipRequest.Header.From?.FromURI?.ToString() ?? "Bilinmeyen";
        var callerDisplay = sipRequest.Header.From?.FromName ?? callerUri;
        freeLine.RemoteUri = callerUri;
        freeLine.RemoteDisplayName = callerDisplay;

        Log($"[SIP] 180 Ringing gonderildi. Hat {freeLine.Index} Ringing durumunda");

        // Zil sesi cal
        PlayRingtone();

        // Auto-answer
        if (_autoAnswerEnabled)
        {
            Log("[SIP] Auto-answer aktif, otomatik cevaplaniyor");
            _activeLineIndex = freeLine.Index;
            await AnswerCallAsync();
            return;
        }

        Log("[SIP] OnIncomingCall event tetikleniyor (UI popup)");
        await (OnIncomingCall?.Invoke(callerUri, callerDisplay) ?? Task.CompletedTask);
    }

    /// <summary>SIP NOTIFY: MWI (Message Waiting Indicator) icin.</summary>
    private void HandleNotify(SIPRequest sipRequest)
    {
        try
        {
            var body = sipRequest.Body;
            if (string.IsNullOrEmpty(body)) return;

            // MWI format: Messages-Waiting: yes/no\r\nVoice-Message: 3/0
            if (body.Contains("Messages-Waiting", StringComparison.OrdinalIgnoreCase))
            {
                var lines = body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("Voice-Message:", StringComparison.OrdinalIgnoreCase))
                    {
                        // "Voice-Message: 3/0 (1/0)" → ilk sayi = yeni mesaj
                        var parts = line.Substring("Voice-Message:".Length).Trim().Split('/');
                        if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), out var count))
                        {
                            _voicemailCount = count;
                            _ = OnVoicemailCountChanged?.Invoke(count) ?? Task.CompletedTask;
                        }
                    }
                }
            }

            // 200 OK yanit gonder
            var okResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null);
            _sipTransport?.SendResponseAsync(okResponse);
        }
        catch (Exception ex)
        {
            Log($"[SIP] NOTIFY isleme hatasi: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: Hat Yonetimi
    // ═══════════════════════════════════════════════════

    private async Task<bool> MakeCallOnLineAsync(int lineIndex, string destination)
    {
        var line = _lines[lineIndex];
        Log($"[SIP] MakeCallOnLineAsync hat={lineIndex}, hedef={destination}");
        if (_sipTransport == null || _config == null)
        {
            Log("[SIP] HATA: SIPTransport veya config null");
            return false;
        }

        // Takili hat kontrolu: State idle degil ama UserAgent aktif degilse temizle
        if (line.State != LineState.Idle)
        {
            if (line.UserAgent == null || line.UserAgent.IsCallActive != true)
            {
                Log($"[SIP] Hat {lineIndex} takili kalmis (State={line.State}), temizleniyor...");
                CleanupLine(line);
                // Audio kaynaginin serbest kalmasi icin kisa bekleme
                await Task.Delay(300);
            }
            else
            {
                Log($"[SIP] Hat {lineIndex} aktif arama mevcut, yeni arama baslatilmiyor");
                return false; // Gercekten aktif arama var
            }
        }

        try
        {
            line.UserAgent = new SIPUserAgent(_sipTransport, null);
            line.UserAgent.ClientCallFailed += (uac, error, response) => SafeCallbackAsync(async () =>
            {
                Log($"[SIP] ClientCallFailed: {error}");
                CleanupLine(line);
                await (OnCallFailed?.Invoke(error ?? "Arama basarisiz") ?? Task.CompletedTask);
            });
            line.UserAgent.OnCallHungup += (dialog) => SafeCallbackAsync(async () =>
            {
                Log($"[SIP] OnCallHungup event tetiklendi (hat={line.Index}, State={line.State})");
                if (line.State == LineState.Idle)
                {
                    Log("[SIP] Hat zaten Idle — BYE handler temizlemis, OnCallEnded atlaniyor");
                    return;
                }
                StopRtpTimeoutTimer();
                CleanupLine(line);
                await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
            });

            line.MediaSession = CreateMediaSession();
            line.State = LineState.Connecting;

            var destUri = BuildTargetUri(destination);
            if (destUri == null)
            {
                Log($"[SIP] HATA: Gecersiz hedef numara: {destination}");
                CleanupLine(line);
                await (OnCallFailed?.Invoke("Gecersiz hedef numara") ?? Task.CompletedTask);
                return false;
            }

            line.RemoteUri = destination;
            line.RemoteDisplayName = destination;

            Log($"[SIP] Call baslatiliyor: {destUri}, auth={_config.AuthUsername}");
            var result = await line.UserAgent.Call(destUri.ToString(), _config.AuthUsername, _config.AuthPassword, line.MediaSession);

            if (result)
            {
                line.State = LineState.Connected;
                line.StartTime = DateTime.Now;
                Log($"[SIP] Arama basarili! Hat {lineIndex} -> Connected");
                await (OnCallAnswered?.Invoke() ?? Task.CompletedTask);
            }
            else
            {
                Log("[SIP] Arama basarisiz (Call() false dondu)");
                CleanupLine(line);
                await (OnCallFailed?.Invoke("Arama baglanamiyor") ?? Task.CompletedTask);
            }

            return result;
        }
        catch (Exception ex)
        {
            Log($"[SIP] MakeCallOnLineAsync exception: {ex.Message}\n{ex.StackTrace}");
            CleanupLine(line);
            await (OnCallFailed?.Invoke(ex.Message) ?? Task.CompletedTask);
            return false;
        }
    }

    private async Task<bool> HoldLineAsync(CallLine line)
    {
        Log($"[SIP] HoldLineAsync hat={line.Index}, State={line.State}, MediaSession null={line.MediaSession == null}");
        if (line.State != LineState.Connected || line.MediaSession == null) return false;

        try
        {
            // 1. Karsi tarafa hold muzigi veya sessizlik gonder
            if (line.MediaSession.AudioExtrasSource != null)
            {
                var holdSource = _holdMusicEnabled ? AudioSourcesEnum.Music : AudioSourcesEnum.Silence;
                line.MediaSession.AudioExtrasSource.SetSource(holdSource);
                Log($"[SIP] AudioExtrasSource -> {holdSource} (hold icin)");
            }
            else
            {
                Log("[SIP] UYARI: AudioExtrasSource null — hold muzigi gonderilemedi");
            }

            // 2. SDP hold sinyali (re-INVITE gerekmese de durum dogru olsun)
            await line.MediaSession.PutOnHold();
            Log("[SIP] PutOnHold() cagirildi");

            line.State = LineState.OnHold;
            Log($"[SIP] Hat {line.Index} hold'a alindi basariyla");
            await (OnLineChanged?.Invoke(line.Index) ?? Task.CompletedTask);
            return true;
        }
        catch (Exception ex)
        {
            Log($"[SIP] Hold hatasi: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    private CallLine? FindFreeLine()
    {
        return Array.Find(_lines, l => l.State == LineState.Idle);
    }

    private CallLine? FindRingingLine()
    {
        return Array.Find(_lines, l => l.State == LineState.Ringing);
    }

    private VoIPMediaSession CreateMediaSession()
    {
        Log($"[SIP] CreateMediaSession: outputDevice={_outputDeviceIndex}, inputDevice={_inputDeviceIndex}, codecs=[{string.Join(",", _enabledCodecNames)}], srtp={_srtpEnabled}");
        var winAudio = new WindowsAudioEndPoint(new AudioEncoder(), _outputDeviceIndex, _inputDeviceIndex);
        _winAudioEndPoint = winAudio; // Recording icin mikrofon event'ine erisim

        // Codec filtresi: Sadece etkinlestirilmis codec'ler
        winAudio.RestrictFormats(x =>
        {
            var codecName = x.Codec.ToString().ToUpperInvariant();
            return _enabledCodecNames.Any(c => c.Equals(codecName, StringComparison.OrdinalIgnoreCase));
        });

        var sessionConfig = new VoIPMediaSessionConfig
        {
            MediaEndPoint = winAudio.ToMediaEndPoints()
        };

        // SRTP: Online SIP saglayicilari icin zorunlu (medya sifreleme)
        if (_srtpEnabled)
        {
            sessionConfig.RtpSecureMediaOption = RtpSecureMediaOptionEnum.SdpCryptoNegotiation;
            Log("[SIP] SRTP aktif — SDP crypto negotiation etkinlestirildi");
        }

        // NAT arkasinda: STUN ile bulunan public IP'yi RTP bind address olarak kullan
        if (_stunPublicAddress != null)
        {
            sessionConfig.BindAddress = _stunPublicAddress;
            Log($"[SIP] RTP bind address: {_stunPublicAddress} (STUN)");
        }

        var mediaSession = new VoIPMediaSession(sessionConfig);
        mediaSession.AcceptRtpFromAny = true;

        // RTP timeout algilama: karsi taraf BYE gondermeden kapatirsa
        _lastRtpReceived = DateTime.UtcNow;
        mediaSession.OnRtpPacketReceived += (ep, mt, pkt) =>
        {
            if (mt == SDPMediaTypesEnum.audio)
                _lastRtpReceived = DateTime.UtcNow;
        };
        StartRtpTimeoutTimer();

        Log("[SIP] MediaSession olusturuldu (AcceptRtpFromAny=true, RTP timeout=8sn)");
        return mediaSession;
    }

    private SIPURI? BuildTargetUri(string destination)
    {
        if (destination.StartsWith("sip:") || destination.StartsWith("sips:"))
            return SIPURI.ParseSIPURI(destination);

        var configUri = SIPURI.ParseSIPURI(_config!.SipUri);
        if (configUri?.Host == null) return null;

        var transport = _config.Transport?.ToUpperInvariant() ?? "UDP";
        var scheme = transport is "WSS" or "TLS" ? "sips" : "sip";
        return SIPURI.ParseSIPURI($"{scheme}:{destination}@{configUri.Host}");
    }

    private void CleanupLine(CallLine line)
    {
        // Zaten temizlenmis — tekrar yapma
        if (line.State == LineState.Idle)
        {
            Log($"[SIP] CleanupLine hat={line.Index} — zaten Idle, atlaniyor");
            return;
        }

        var prevState = line.State;
        Log($"[SIP] CleanupLine hat={line.Index}, State={prevState}, RemoteUri={line.RemoteUri}");

        // ONCE State'i Idle yap — boylece re-entrant cagrilar (OnCallHungup)
        // yukaridaki guard'a takilir ve tekrar calismaz
        line.State = LineState.Idle;
        line.RemoteUri = null;
        line.RemoteDisplayName = null;
        line.StartTime = null;
        line.PendingUas = null;

        // Sonra kaynaklari temizle
        var ms = line.MediaSession;
        var ua = line.UserAgent;
        line.MediaSession = null;
        line.UserAgent = null;

        if (ms != null)
        {
            try { ms.Close(null); }
            catch (Exception ex) { Log($"[SIP] MediaSession.Close hatasi: {ex.Message}"); }
        }

        if (ua != null)
        {
            try
            {
                if (ua.IsCallActive)
                    ua.Hangup();
            }
            catch { }
        }

        Log($"[SIP] Hat {line.Index} temizlendi -> Idle (onceki={prevState})");
    }

    // ═══════════════════════════════════════════════════
    // SIP PRESENCE (SUBSCRIBE/NOTIFY — RFC 3856)
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// SIP Presence özelligini etkinlestirir ve mevcut durumu yayinlar.
    /// Register sonrasi cagirilmalidir.
    /// </summary>
    public void EnablePresence()
    {
        _presenceEnabled = true;
        // Varsayilan: online (open)
        PublishPresence("open");
    }

    /// <summary>
    /// Agent durumu degistiginde SIP Presence'i gunceller.
    /// AgentStatuses ID alir ve SIP PIDF durumuna cevirir.
    /// </summary>
    public void UpdatePresenceFromAgentStatus(int agentStatusId)
    {
        if (!_presenceEnabled || _sipTransport == null || _config == null) return;

        // AgentStatuses → SIP Presence eslestirmesi
        var sipStatus = agentStatusId switch
        {
            1 => "closed",      // Offline
            2 => "open",        // Available
            3 => "busy",        // Busy
            4 => "away",        // OnBreak
            5 => "on-the-phone", // InCall
            6 => "busy",        // AfterCallWork → busy (DND)
            _ => "closed"
        };

        PublishPresence(sipStatus);
    }

    /// <summary>
    /// SIP PUBLISH ile kendi presence durumunu yayinlar (RFC 3903).
    /// PIDF (Presence Information Data Format — RFC 3863) XML body gonderir.
    /// </summary>
    private void PublishPresence(string sipStatus)
    {
        if (_sipTransport == null || _config == null) return;
        _currentPresenceStatus = sipStatus;

        try
        {
            var sipUri = SIPURI.ParseSIPURI(_config.SipUri);
            if (sipUri == null) return;

            // RFC 3863 PIDF XML body
            string pidfXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<presence xmlns=""urn:ietf:params:xml:ns:pidf""
          xmlns:dm=""urn:ietf:params:xml:ns:pidf:data-model""
          xmlns:rpid=""urn:ietf:params:xml:ns:pidf:rpid""
          entity=""{_config.SipUri}"">
  <tuple id=""t1"">
    <status>
      <basic>{(sipStatus == "closed" ? "closed" : "open")}</basic>
    </status>
    <note>{_config.DisplayName} - {sipStatus}</note>
  </tuple>
  <dm:person id=""p1"">
    <rpid:activities>
      <rpid:{GetRpidActivity(sipStatus)}/>
    </rpid:activities>
  </dm:person>
</presence>";

            // SIP PUBLISH request olustur
            var publishReq = SIPRequest.GetRequest(
                SIPMethodsEnum.PUBLISH,
                sipUri,
                new SIPToHeader(null, sipUri, null),
                new SIPFromHeader(_config.DisplayName, new SIPURI(_config.AuthUsername, sipUri.Host, null), null));

            publishReq.Header.ContentType = "application/pidf+xml";
            publishReq.Header.Expires = 3600;
            publishReq.Header.Event = "presence";
            publishReq.Body = pidfXml;

            _ = _sipTransport.SendRequestAsync(publishReq);
        }
        catch
        {
            // Presence publish basarisiz olursa sessizce devam et
        }
    }

    /// <summary>
    /// Belirli bir SIP URI'nin presence bilgisini almak icin SUBSCRIBE gonderir (BLF — Busy Lamp Field).
    /// </summary>
    public void SubscribeBuddyPresence(string buddySipUri)
    {
        if (_sipTransport == null || _config == null || !_presenceEnabled) return;

        try
        {
            var fromUri = SIPURI.ParseSIPURI(_config.SipUri);
            var toUri = SIPURI.ParseSIPURI(buddySipUri);
            if (fromUri == null || toUri == null) return;

            var subscribeReq = SIPRequest.GetRequest(
                SIPMethodsEnum.SUBSCRIBE,
                toUri,
                new SIPToHeader(null, toUri, null),
                new SIPFromHeader(_config.DisplayName, fromUri, null));

            subscribeReq.Header.Expires = 3600;
            subscribeReq.Header.Event = "presence";
            subscribeReq.Header.Accept = "application/pidf+xml";

            _ = _sipTransport.SendRequestAsync(subscribeReq);

            // Buddy listesine ekle (ilk durum: bilinmiyor)
            _buddyPresence[buddySipUri] = "unknown";
        }
        catch
        {
            // Subscribe basarisiz olursa sessizce devam et
        }
    }

    /// <summary>
    /// Gelen SIP NOTIFY mesajini isler ve buddy presence bilgisini gunceller.
    /// InitializeAsync icinde SIPTransport.SIPRequestInEvent'e baglanmalidir.
    /// </summary>
    public void HandleIncomingNotify(SIPRequest notifyReq)
    {
        if (notifyReq.Method != SIPMethodsEnum.NOTIFY) return;
        if (notifyReq.Header.Event != "presence") return;

        try
        {
            var fromUri = notifyReq.Header.From.FromURI.ToString();
            var body = notifyReq.Body;

            if (string.IsNullOrEmpty(body)) return;

            // Basit XML parse — <basic>open</basic> veya <basic>closed</basic>
            string presence = "unknown";
            if (body.Contains("<basic>open</basic>"))
                presence = "open";
            else if (body.Contains("<basic>closed</basic>"))
                presence = "closed";

            // RPID activities kontrolu
            if (body.Contains("rpid:busy"))
                presence = "busy";
            else if (body.Contains("rpid:away"))
                presence = "away";
            else if (body.Contains("rpid:on-the-phone"))
                presence = "on-the-phone";

            _buddyPresence[fromUri] = presence;
            OnBuddyPresenceChanged?.Invoke(fromUri, presence);
        }
        catch
        {
            // NOTIFY parse basarisiz olursa sessizce devam et
        }
    }

    /// <summary>Tum buddy'lerin presence bilgisini dondurur.</summary>
    public IReadOnlyDictionary<string, string> GetBuddyPresences() => _buddyPresence;

    /// <summary>Kendi mevcut SIP presence durumunu dondurur.</summary>
    public string? GetCurrentPresence() => _currentPresenceStatus;

    private static string GetRpidActivity(string sipStatus) => sipStatus switch
    {
        "busy" => "busy",
        "away" => "away",
        "on-the-phone" => "on-the-phone",
        "dnd" => "busy",
        _ => "unknown"
    };

    // ═══════════════════════════════════════════════════
    // DISPOSE
    // ═══════════════════════════════════════════════════

    public ValueTask DisposeAsync()
    {
        StopNetworkChangeDetection();
        StopRingtone();

        for (int i = 0; i < MaxLines; i++)
            CleanupLine(_lines[i]);

        if (_waveWriter != null)
        {
            if (_winAudioEndPoint != null)
                _winAudioEndPoint.OnAudioSourceEncodedSample -= OnMicSampleForRecording;
            FlushRemainingRecordingQueues();
            _waveWriter.Dispose();
            _waveWriter = null;
            _isRecording = false;
        }
        _winAudioEndPoint = null;

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
        _stunPublicAddress = null;
        return ValueTask.CompletedTask;
    }

    // ═══════════════════════════════════════════════════
    // INNER: Arama Hatti
    // ═══════════════════════════════════════════════════

    private class CallLine
    {
        public int Index { get; set; }
        public LineState State { get; set; } = LineState.Idle;
        public SIPUserAgent? UserAgent { get; set; }
        public VoIPMediaSession? MediaSession { get; set; }
        public SIPServerUserAgent? PendingUas { get; set; }
        public string? RemoteUri { get; set; }
        public string? RemoteDisplayName { get; set; }
        public DateTime? StartTime { get; set; }
    }
}

// ═══════════════════════════════════════════════════
// HELPER: Codec-aware audio decoder (PCMU + PCMA + G.722 + Opus)
// ═══════════════════════════════════════════════════

internal static class AudioCodecDecoder
{
    // ── G.711 mu-law (PCMU, payload type 0) ──
    private static readonly short[] MuLawTable = new short[256];

    // ── G.711 A-law (PCMA, payload type 8) ──
    private static readonly short[] ALawTable = new short[256];

    static AudioCodecDecoder()
    {
        // mu-law lookup table
        for (int i = 0; i < 256; i++)
        {
            int val = ~i;
            int sign = val & 0x80;
            int exponent = (val >> 4) & 0x07;
            int mantissa = val & 0x0F;
            int sample = (mantissa << 3) + 0x84;
            sample <<= exponent;
            sample -= 0x84;
            MuLawTable[i] = (short)(sign != 0 ? -sample : sample);
        }

        // A-law lookup table
        for (int i = 0; i < 256; i++)
        {
            int val = i ^ 0x55;
            int sign = val & 0x80;
            int exponent = (val >> 4) & 0x07;
            int mantissa = val & 0x0F;
            int sample;
            if (exponent == 0)
            {
                sample = (mantissa << 4) + 8;
            }
            else
            {
                sample = ((mantissa << 4) + 0x108) << (exponent - 1);
            }
            ALawTable[i] = (short)(sign != 0 ? -sample : sample);
        }
    }

    public static short MuLawToLinear(byte muLaw) => MuLawTable[muLaw];

    public static short ALawToLinear(byte aLaw) => ALawTable[aLaw];

    // ── Opus decoder (lazy init, thread-safe degil — recording thread'de kullanilir) ──
    private static OpusDecoder? _opusDecoderStatic;

    /// <summary>
    /// RTP payload type'a gore ses verisini PCM16'ya decode eder.
    /// PT 0 = PCMU, PT 8 = PCMA, PT 9 = G.722, PT 111 = Opus.
    /// </summary>
    public static byte[] Decode(byte[] payload, int payloadType)
    {
        return payloadType switch
        {
            0 => DecodeMuLaw(payload),
            8 => DecodeALaw(payload),
            9 => DecodeG722(payload),
            111 => DecodeOpus(payload),
            _ => DecodeMuLaw(payload) // fallback: mu-law
        };
    }

    /// <summary>Payload type'a gore sample rate doner.</summary>
    public static int GetSampleRate(int payloadType) => payloadType switch
    {
        9 => 16000,     // G.722
        111 => 48000,   // Opus
        _ => 8000       // PCMU, PCMA, G.726, Speex, iLBC
    };

    private static byte[] DecodeMuLaw(byte[] payload)
    {
        var pcm = new byte[payload.Length * 2];
        for (int i = 0; i < payload.Length; i++)
        {
            short sample = MuLawTable[payload[i]];
            pcm[i * 2] = (byte)(sample & 0xFF);
            pcm[i * 2 + 1] = (byte)(sample >> 8);
        }
        return pcm;
    }

    private static byte[] DecodeALaw(byte[] payload)
    {
        var pcm = new byte[payload.Length * 2];
        for (int i = 0; i < payload.Length; i++)
        {
            short sample = ALawTable[payload[i]];
            pcm[i * 2] = (byte)(sample & 0xFF);
            pcm[i * 2 + 1] = (byte)(sample >> 8);
        }
        return pcm;
    }

    /// <summary>
    /// Opus decoder (Concentus managed). 48kHz mono.
    /// PLC (Packet Loss Concealment): null payload gonderildiginde
    /// Concentus codec kendi PLC algoritmasini uygular.
    /// </summary>
    private static byte[] DecodeOpus(byte[] payload)
    {
#pragma warning disable CS0618 // Concentus managed fallback
        _opusDecoderStatic ??= new OpusDecoder(48000, 1);
#pragma warning restore CS0618

        // Opus frame → PCM16 (48kHz, mono)
        // Max frame: 120ms = 5760 samples
        var pcmShort = new short[5760];
#pragma warning disable CS0618
        int samples = _opusDecoderStatic.Decode(payload, 0, payload.Length, pcmShort, 0, pcmShort.Length, false);
#pragma warning restore CS0618

        var pcm = new byte[samples * 2];
        for (int i = 0; i < samples; i++)
        {
            pcm[i * 2] = (byte)(pcmShort[i] & 0xFF);
            pcm[i * 2 + 1] = (byte)(pcmShort[i] >> 8);
        }
        return pcm;
    }

    /// <summary>
    /// ITU-T G.722 SB-ADPCM decoder (64 kbps, 16kHz).
    /// Alt-bant ve ust-bant ADPCM cozumleme + QMF sentez filtresi.
    /// </summary>
    private static byte[] DecodeG722(byte[] payload)
    {
        // G.722: Her byte 2 sample uretir (lower + upper sub-band)
        // Cikti: payload.Length * 2 sample, her sample 2 byte (PCM16)
        var outputSamples = payload.Length * 2;
        var pcm = new byte[outputSamples * 2];

        // Sub-band ADPCM state
        int lBand = 0;     // Lower sub-band predictor
        int lNb = 0;       // Lower sub-band scale factor
        int hBand = 0;     // Higher sub-band predictor
        int hNb = 0;       // Higher sub-band scale factor

        // Quantization step tables (ITU-T G.722)
        int[] qmfCoeffs = { 3, -11, 12, 32, -210, 951, 3876, -805, 362, -156, 53, -11 };
        int[] wl = { -60, 3042, 1198, 538, 334, 172, 58, -30, -30, 58, 172, 334, 538, 1198, 3042 };
        int[] rl42 = { 0, 7, 6, 5, 4, 3, 2, 1, 7, 6, 5, 4, 3, 2, 1, 0 };
        int[] qm4 = { 0, -20456, -12896, -8968, -6288, -4240, -2584, -1200, 20456, 12896, 8968, 6288, 4240, 2584, 1200, 0 };
        int[] qm2 = { -7408, -1616, 7408, 1616 };
        int[] wh = { 0, -214, 798 };
        int[] rh2 = { 2, 1, 1, 0 };
        int[] qmfHist = new int[24]; // QMF filter history

        int outIdx = 0;

        for (int i = 0; i < payload.Length; i++)
        {
            int coded = payload[i];

            // Lower sub-band: 6 bits (lower 6 bits for mode 1 — standard 64kbps)
            int iLow = coded & 0x3F;

            // Higher sub-band: 2 bits (upper 2 bits)
            int iHigh = (coded >> 6) & 0x03;

            // ── Lower sub-band decode ──
            int rLow;
            {
                // 4-bit quantizer (using lower 4 bits of iLow for simplified decode)
                int idx4 = iLow >> 2;
                int dlx = (qm4[idx4] * lNb) >> 15;
                rLow = Saturate(lBand + dlx);

                // Adaptive predictor update
                int nbIdx = (idx4 < wl.Length) ? idx4 : 0;
                lNb = Saturate((lNb * 127) / 128 + wl[nbIdx]);
                if (lNb < 0) lNb = 0;
                if (lNb > 18432) lNb = 18432;
                lBand = Saturate((lBand * 255) / 256 + rLow / 256);
            }

            // ── Higher sub-band decode ──
            int rHigh;
            {
                int dhx = (qm2[iHigh] * hNb) >> 15;
                rHigh = Saturate(hBand + dhx);

                int nbIdx2 = rh2[iHigh];
                hNb = Saturate((hNb * 127) / 128 + wh[nbIdx2]);
                if (hNb < 0) hNb = 0;
                if (hNb > 22528) hNb = 22528;
                hBand = Saturate((hBand * 255) / 256 + rHigh / 256);
            }

            // ── QMF synthesis: 2 output samples per input byte ──
            int xOut1 = Saturate(rLow - rHigh);
            int xOut2 = Saturate(rLow + rHigh);

            // Sample 1
            short s1 = (short)Math.Clamp(xOut1, short.MinValue, short.MaxValue);
            pcm[outIdx++] = (byte)(s1 & 0xFF);
            pcm[outIdx++] = (byte)(s1 >> 8);

            // Sample 2
            short s2 = (short)Math.Clamp(xOut2, short.MinValue, short.MaxValue);
            pcm[outIdx++] = (byte)(s2 & 0xFF);
            pcm[outIdx++] = (byte)(s2 >> 8);
        }

        return pcm;
    }

    private static int Saturate(int val)
    {
        if (val > 32767) return 32767;
        if (val < -32768) return -32768;
        return val;
    }
}

// ═══════════════════════════════════════════════════
// HELPER: Ringtone loop stream
// ═══════════════════════════════════════════════════

internal class LoopStream : WaveStream
{
    private readonly WaveStream _source;

    public LoopStream(WaveStream source)
    {
        _source = source;
    }

    public override WaveFormat WaveFormat => _source.WaveFormat;
    public override long Length => _source.Length;
    public override long Position
    {
        get => _source.Position;
        set => _source.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = _source.Read(buffer, offset + totalRead, count - totalRead);
            if (read == 0)
            {
                _source.Position = 0; // Loop
            }
            totalRead += read;
        }
        return totalRead;
    }
}
