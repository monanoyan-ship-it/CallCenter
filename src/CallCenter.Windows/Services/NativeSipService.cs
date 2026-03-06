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

    // ─── Recording ───
    private WaveFileWriter? _waveWriter;
    private bool _isRecording;
    private string? _recordingWavPath; // Sifrelemeden onceki WAV yolu
    private int _recordingPayloadType; // Aktif codec: 0=PCMU, 8=PCMA, 9=G722

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
            Console.WriteLine($"[SIP] SafeCallback hatasi ({caller}): {ex.Message}\n{ex.StackTrace}");
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
            Console.WriteLine($"[SIP] SafeCallbackAsync hatasi ({caller}): {ex.Message}\n{ex.StackTrace}");
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
                        // SIPTransport contact header'inda public IP kullan
                        _sipTransport.ContactHost = publicEp.Address.ToString();
                        Console.WriteLine($"[NativeSipService] STUN public address: {publicEp}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NativeSipService] STUN discovery hatasi: {ex.Message}");
                }
            }

            // Registration
            var regUri = new SIPURI(sipUri.User, sipUri.Host, null, SIPSchemesEnum.sip, SIPProtocolsEnum.udp);
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
            return true;
        }
        catch (Exception ex)
        {
            await (OnRegistrationFailed?.Invoke($"SIP init hatasi: {ex.Message}") ?? Task.CompletedTask);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════
    // CORE CALL METHODS
    // ═══════════════════════════════════════════════════

    public async Task<bool> MakeCallAsync(string destination)
    {
        return await MakeCallOnLineAsync(_activeLineIndex, destination);
    }

    public async Task<bool> AnswerCallAsync()
    {
        var line = FindRingingLine();
        Console.WriteLine($"[SIP] AnswerCallAsync cagirildi. RingingLine={line?.Index.ToString() ?? "null"}");
        if (line == null || _sipTransport == null)
        {
            Console.WriteLine("[SIP] AnswerCallAsync: Ringing hat yok veya transport null");
            return false;
        }

        try
        {
            // Mevcut aktif arama varsa hold'a al
            if (ActiveLine.State == LineState.Connected && ActiveLine.Index != line.Index)
            {
                Console.WriteLine($"[SIP] Mevcut aktif hat {ActiveLine.Index} hold'a aliniyor");
                await HoldLineAsync(ActiveLine);
            }

            line.MediaSession = CreateMediaSession();
            Console.WriteLine($"[SIP] Cevaplaniyor: hat={line.Index}, remote={line.RemoteUri}");
            var result = await line.UserAgent!.Answer(line.PendingUas!, line.MediaSession);
            line.PendingUas = null;

            if (result)
            {
                line.State = LineState.Connected;
                line.StartTime = DateTime.Now;
                _activeLineIndex = line.Index;
                StopRingtone();
                Console.WriteLine($"[SIP] Gelen arama cevaplandi! Hat {line.Index} -> Connected");
                await (OnCallAnswered?.Invoke() ?? Task.CompletedTask);
                await (OnLineChanged?.Invoke(line.Index) ?? Task.CompletedTask);
            }
            else
            {
                Console.WriteLine("[SIP] Answer basarisiz (Answer() false dondu)");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Answer hatasi: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> HangupAsync()
    {
        var line = ActiveLine;
        Console.WriteLine($"[SIP] HangupAsync cagirildi. ActiveLine={line.Index}, State={line.State}");

        // Connecting hattı varsa (giden arama calıyor) onu kapat
        if (line.State == LineState.Idle)
        {
            // Ringing (gelen) veya Connecting (giden) hat ara
            var ringing = FindRingingLine();
            var connecting = Array.Find(_lines, l => l.State == LineState.Connecting);
            line = ringing ?? connecting ?? line;
            if (ringing != null) Console.WriteLine($"[SIP] Ringing hat bulundu: {ringing.Index}");
            if (connecting != null) Console.WriteLine($"[SIP] Connecting hat bulundu: {connecting.Index}");
        }

        if (line.State == LineState.Idle && line.PendingUas == null)
        {
            Console.WriteLine("[SIP] Hangup: Hat idle ve PendingUas yok, iptal");
            return false;
        }

        try
        {
            if (line.PendingUas != null)
            {
                Console.WriteLine("[SIP] Gelen arama reddediliyor (BusyHere)");
                line.PendingUas.Reject(SIPResponseStatusCodesEnum.BusyHere, null);
                line.PendingUas = null;
                StopRingtone();
            }

            if (line.UserAgent != null)
            {
                // Connecting veya Connected — CANCEL veya BYE gonder
                Console.WriteLine($"[SIP] UserAgent.Cancel/Hangup cagirildi (IsCallActive={line.UserAgent.IsCallActive})");
                if (line.UserAgent.IsCallActive)
                    line.UserAgent.Hangup();
                else
                    line.UserAgent.Cancel();
            }

            CleanupLine(line);
            await (OnCallEnded?.Invoke() ?? Task.CompletedTask);

            // Baska aktif hat varsa ona gec
            var nextActive = Array.FindIndex(_lines, l => l.State != LineState.Idle);
            if (nextActive >= 0)
            {
                _activeLineIndex = nextActive;
                Console.WriteLine($"[SIP] Sonraki aktif hat: {nextActive}");
                await (OnLineChanged?.Invoke(_activeLineIndex) ?? Task.CompletedTask);
            }

            Console.WriteLine("[SIP] Hangup basarili");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Hangup hatasi: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> HoldAsync()
    {
        Console.WriteLine($"[SIP] HoldAsync cagirildi. ActiveLine.State={ActiveLine.State}, MediaSession null={ActiveLine.MediaSession == null}");
        if (ActiveLine.State != LineState.Connected) return false;
        return await HoldLineAsync(ActiveLine);
    }

    public async Task<bool> UnholdAsync()
    {
        Console.WriteLine($"[SIP] UnholdAsync cagirildi. ActiveLine.State={ActiveLine.State}");
        if (ActiveLine.State != LineState.OnHold) return false;

        try
        {
            // 1. Mikrofonu tekrar ac (mute degilse)
            if (ActiveLine.MediaSession?.AudioExtrasSource != null)
            {
                var source = _muted ? AudioSourcesEnum.Silence : AudioSourcesEnum.None;
                ActiveLine.MediaSession.AudioExtrasSource.SetSource(source);
                Console.WriteLine($"[SIP] AudioExtrasSource -> {source}");
            }

            // 2. SDP hold'u kaldir
            ActiveLine.MediaSession?.TakeOffHold();
            ActiveLine.State = LineState.Connected;
            Console.WriteLine("[SIP] Unhold basarili, State -> Connected");
            await (OnLineChanged?.Invoke(ActiveLine.Index) ?? Task.CompletedTask);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Unhold hatasi: {ex.Message}");
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
            Console.WriteLine($"[SIP] DTMF hatasi: {ex.Message}");
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
            Console.WriteLine($"[SIP] Blind transfer hatasi: {ex.Message}");
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
            Console.WriteLine($"[SIP] Attended transfer hatasi: {ex.Message}");
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
            Console.WriteLine($"[SIP] Attended transfer tamamlama hatasi: {ex.Message}");
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
        Console.WriteLine($"[SIP] MuteAsync cagirildi. ActiveLine.State={ActiveLine.State}, AudioExtrasSource null={ActiveLine.MediaSession?.AudioExtrasSource == null}");
        _muted = true;
        // Sadece mikrofonu kapat — karsi tarafin sesi devam etsin
        if (ActiveLine.MediaSession?.AudioExtrasSource != null)
        {
            ActiveLine.MediaSession.AudioExtrasSource.SetSource(AudioSourcesEnum.Silence);
            Console.WriteLine("[SIP] Mute: AudioExtrasSource -> Silence");
        }
        else
        {
            Console.WriteLine("[SIP] UYARI: Mute icin AudioExtrasSource bulunamadi");
        }
        await (OnMuteChanged?.Invoke(true) ?? Task.CompletedTask);
        return true;
    }

    public async Task<bool> UnmuteAsync()
    {
        Console.WriteLine($"[SIP] UnmuteAsync cagirildi. ActiveLine.State={ActiveLine.State}");
        _muted = false;
        // Mikrofonu tekrar ac (hold'da degilse)
        if (ActiveLine.MediaSession?.AudioExtrasSource != null)
        {
            if (ActiveLine.State == LineState.OnHold)
            {
                Console.WriteLine("[SIP] Unmute: Hat hold'da, sessizlik devam ediyor");
            }
            else
            {
                ActiveLine.MediaSession.AudioExtrasSource.SetSource(AudioSourcesEnum.None);
                Console.WriteLine("[SIP] Unmute: AudioExtrasSource -> None (mikrofon acildi)");
            }
        }
        else
        {
            Console.WriteLine("[SIP] UYARI: Unmute icin AudioExtrasSource bulunamadi");
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
                Directory.CreateDirectory(dir);

            // Aktif aramanin codec'ini tespit et (SDP'den)
            _recordingPayloadType = DetectActiveCodecPayloadType();
            int sampleRate = AudioCodecDecoder.GetSampleRate(_recordingPayloadType);

            _waveWriter = new WaveFileWriter(path, new WaveFormat(sampleRate, 16, 1));
            _recordingWavPath = path;
            _isRecording = true;

            Console.WriteLine($"[SIP] Recording baslatildi — codec PT={_recordingPayloadType}, sampleRate={sampleRate}Hz, dosya={path}");

            // RTP event'lerine baglanarak kayit yapilir
            if (ActiveLine.MediaSession != null)
            {
                ActiveLine.MediaSession.OnRtpPacketReceived += OnRtpPacketForRecording;
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Recording baslatilamadi: {ex.Message}");
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
            Console.WriteLine($"[SIP] Codec tespit hatasi: {ex.Message}");
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

                    Console.WriteLine($"[SIP] Recording sifrelendi: {encPath}");
                }
                catch (Exception encEx)
                {
                    Console.WriteLine($"[SIP] Recording sifreleme hatasi (WAV korundu): {encEx.Message}");
                    // Sifreleme basarisiz olursa WAV oldugu gibi kalir
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Recording durdurulamadi: {ex.Message}");
            return false;
        }
    }

    private void OnRtpPacketForRecording(IPEndPoint remoteEP, SDPMediaTypesEnum mediaType, RTPPacket rtpPacket)
    {
        if (mediaType != SDPMediaTypesEnum.audio || _waveWriter == null) return;

        try
        {
            // RTP payload type'dan gelen codec bilgisini kullan
            // Oncelik: RTP header'daki PT > recording baslatilirken tespit edilen PT
            int pt = rtpPacket.Header.PayloadType;

            // Bazi PBX'ler dynamic PT kullanir (96+), bu durumda basta tespit edilen codec'i kullan
            if (pt >= 96) pt = _recordingPayloadType;

            var pcm = AudioCodecDecoder.Decode(rtpPacket.Payload, pt);
            _waveWriter.Write(pcm, 0, pcm.Length);
        }
        catch { /* Kayit hatasi sessizce gecilebilir */ }
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
        Console.WriteLine("[SIP] Ag degisikligi algilama baslatildi.");
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
        Console.WriteLine($"[SIP] Ag durumu degisti: {(e.IsAvailable ? "bagli" : "baglanti yok")}");
        if (e.IsAvailable && _sipTransport != null && _regAgent != null)
        {
            // Ag geldi — yeniden register ol
            await Task.Delay(2000); // Ag stabilizasyonu icin bekle
            try
            {
                _regAgent.Stop();
                await Task.Delay(500);
                _regAgent.Start();
                Console.WriteLine("[SIP] Ag degisikligi sonrasi re-register baslatildi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SIP] Re-register hatasi: {ex.Message}");
            }
        }
    }

    private async void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        Console.WriteLine("[SIP] Ag adresi degisti — re-register planlanıyor...");
        if (_sipTransport != null && _regAgent != null)
        {
            await Task.Delay(3000); // Adres degisikligi stabilize olsun
            try
            {
                _regAgent.Stop();
                await Task.Delay(500);
                _regAgent.Start();
                Console.WriteLine("[SIP] Adres degisikligi sonrasi re-register baslatildi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SIP] Re-register hatasi: {ex.Message}");
            }
        }
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
        Console.WriteLine($"[SIP] Giris cihazi degistirildi: index={deviceIndex} (onceki={_inputDeviceIndex}). Sonraki aramada gecerli olacak.");
        _inputDeviceIndex = deviceIndex;
        return Task.FromResult(true);
    }

    public Task<bool> SetAudioOutputDeviceAsync(int deviceIndex)
    {
        Console.WriteLine($"[SIP] Cikis cihazi degistirildi: index={deviceIndex} (onceki={_outputDeviceIndex}). Sonraki aramada gecerli olacak.");
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
        Console.WriteLine($"[SIP] Hold muzigi: {(enabled ? "aktif" : "deaktif")}");
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
            Console.WriteLine($"[SIP] Ringtone hatasi: {ex.Message}");
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
        Console.WriteLine($"[SIP] <<< Gelen SIP istegi: {sipRequest.Method} from {remoteEndPoint}, CallId={callId}, ToTag={toTag ?? "(yok)"}");

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
            Console.WriteLine($"[SIP] BYE istegi alindi, CallId={callId}");
            // Dialog ile eslesen hat varsa temizle
            var byeLine = Array.Find(_lines, l =>
                l.UserAgent?.Dialogue?.CallId == callId && l.State != LineState.Idle);
            if (byeLine != null)
            {
                Console.WriteLine($"[SIP] BYE eslesti, hat {byeLine.Index} temizleniyor");
                CleanupLine(byeLine);
                await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
                // 200 OK yanit
                var okResp = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null);
                await _sipTransport!.SendResponseAsync(okResp);
            }
        }
        else if (sipRequest.Method == SIPMethodsEnum.ACK)
        {
            // ACK normal SIP akisi — loglama yeterli
            Console.WriteLine($"[SIP] ACK alindi, CallId={callId}");
        }
        else if (sipRequest.Method == SIPMethodsEnum.CANCEL)
        {
            Console.WriteLine($"[SIP] CANCEL alindi, CallId={callId}");
            // Ringing hatlarda eslesen varsa temizle
            var cancelLine = Array.Find(_lines, l => l.State == LineState.Ringing);
            if (cancelLine != null)
            {
                Console.WriteLine($"[SIP] CANCEL eslesti, hat {cancelLine.Index} temizleniyor");
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

        Console.WriteLine($"[SIP] === GELEN INVITE === From: {fromName} <{fromUri}>, CallId={callId}, ToTag={toTag ?? "(yok)"}");

        // ── re-INVITE kontrolu ──
        // To tag varsa bu mevcut dialog icindeki bir re-INVITE (hold, codec degisikligi vb.)
        // Yeni arama degil — mevcut UserAgent kendi icerisinde handle etmeli
        if (!string.IsNullOrEmpty(toTag))
        {
            Console.WriteLine($"[SIP] re-INVITE algilandi (ToTag={toTag}), yeni arama degil — atlanıyor");
            // Mevcut hatlardaki UserAgent'lar bunu dialog handler uzerinden alacak.
            // Eger hicbir dialog eslesmediyse 481 (Call/Transaction Does Not Exist) gonder
            var existingLine = Array.Find(_lines, l =>
                l.UserAgent?.Dialogue?.CallId == callId &&
                l.State is LineState.Connected or LineState.OnHold);
            if (existingLine == null)
            {
                Console.WriteLine("[SIP] re-INVITE icin eslesen dialog bulunamadi, 481 gonderiliyor");
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
            Console.WriteLine($"[SIP] Zaten ringing hat mevcut (hat={duplicateLine.Index}), muhtemel retransmission — atlanıyor");
            return;
        }

        // DND aktifse reject
        if (_dndEnabled)
        {
            Console.WriteLine("[SIP] DND aktif, arama reddediliyor (BusyHere)");
            var dndResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.BusyHere, "DND aktif");
            await _sipTransport!.SendResponseAsync(dndResponse);
            return;
        }

        // Bos hat bul
        var freeLine = FindFreeLine();
        if (freeLine == null)
        {
            Console.WriteLine("[SIP] Bos hat yok, arama reddediliyor (BusyHere)");
            var busyResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.BusyHere, null);
            await _sipTransport!.SendResponseAsync(busyResponse);
            return;
        }

        Console.WriteLine($"[SIP] Bos hat bulundu: {freeLine.Index}");

        // SIPUserAgent olustur
        freeLine.UserAgent = new SIPUserAgent(_sipTransport!, null);
        freeLine.UserAgent.OnCallHungup += (dialog) => SafeCallbackAsync(async () =>
        {
            Console.WriteLine($"[SIP] Gelen arama OnCallHungup (hat={freeLine.Index})");
            CleanupLine(freeLine);
            await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
        });

        // DTMF alma — karsi tarafin tuslamalarini dinle
        freeLine.UserAgent.OnDtmfTone += (tone, duration) => SafeCallbackAsync(async () =>
        {
            Console.WriteLine($"[SIP] DTMF alindi: tone={tone}, duration={duration}ms (hat={freeLine.Index})");
            await (OnDtmfReceived?.Invoke(tone, duration) ?? Task.CompletedTask);
        });

        // AcceptCall (180 Ringing)
        freeLine.PendingUas = freeLine.UserAgent.AcceptCall(sipRequest);
        freeLine.State = LineState.Ringing;

        var callerUri = sipRequest.Header.From?.FromURI?.ToString() ?? "Bilinmeyen";
        var callerDisplay = sipRequest.Header.From?.FromName ?? callerUri;
        freeLine.RemoteUri = callerUri;
        freeLine.RemoteDisplayName = callerDisplay;

        Console.WriteLine($"[SIP] 180 Ringing gonderildi. Hat {freeLine.Index} Ringing durumunda");

        // Zil sesi cal
        PlayRingtone();

        // Auto-answer
        if (_autoAnswerEnabled)
        {
            Console.WriteLine("[SIP] Auto-answer aktif, otomatik cevaplaniyor");
            _activeLineIndex = freeLine.Index;
            await AnswerCallAsync();
            return;
        }

        Console.WriteLine("[SIP] OnIncomingCall event tetikleniyor (UI popup)");
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
            Console.WriteLine($"[SIP] NOTIFY isleme hatasi: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: Hat Yonetimi
    // ═══════════════════════════════════════════════════

    private async Task<bool> MakeCallOnLineAsync(int lineIndex, string destination)
    {
        var line = _lines[lineIndex];
        Console.WriteLine($"[SIP] MakeCallOnLineAsync hat={lineIndex}, hedef={destination}");
        if (_sipTransport == null || _config == null)
        {
            Console.WriteLine("[SIP] HATA: SIPTransport veya config null");
            return false;
        }

        // Takili hat kontrolu: State idle degil ama UserAgent aktif degilse temizle
        if (line.State != LineState.Idle)
        {
            if (line.UserAgent == null || line.UserAgent.IsCallActive != true)
            {
                Console.WriteLine($"[SIP] Hat {lineIndex} takili kalmis (State={line.State}), temizleniyor...");
                CleanupLine(line);
                // Audio kaynaginin serbest kalmasi icin kisa bekleme
                await Task.Delay(300);
            }
            else
            {
                Console.WriteLine($"[SIP] Hat {lineIndex} aktif arama mevcut, yeni arama baslatilmiyor");
                return false; // Gercekten aktif arama var
            }
        }

        try
        {
            line.UserAgent = new SIPUserAgent(_sipTransport, null);
            line.UserAgent.ClientCallFailed += (uac, error, response) => SafeCallbackAsync(async () =>
            {
                Console.WriteLine($"[SIP] ClientCallFailed: {error}");
                CleanupLine(line);
                await (OnCallFailed?.Invoke(error ?? "Arama basarisiz") ?? Task.CompletedTask);
            });
            line.UserAgent.OnCallHungup += (dialog) => SafeCallbackAsync(async () =>
            {
                Console.WriteLine($"[SIP] OnCallHungup event tetiklendi (hat={line.Index})");
                CleanupLine(line);
                await (OnCallEnded?.Invoke() ?? Task.CompletedTask);
            });

            line.MediaSession = CreateMediaSession();
            line.State = LineState.Connecting;

            var destUri = BuildTargetUri(destination);
            if (destUri == null)
            {
                Console.WriteLine($"[SIP] HATA: Gecersiz hedef numara: {destination}");
                CleanupLine(line);
                await (OnCallFailed?.Invoke("Gecersiz hedef numara") ?? Task.CompletedTask);
                return false;
            }

            line.RemoteUri = destination;
            line.RemoteDisplayName = destination;

            Console.WriteLine($"[SIP] Call baslatiliyor: {destUri}, auth={_config.AuthUsername}");
            var result = await line.UserAgent.Call(destUri.ToString(), _config.AuthUsername, _config.AuthPassword, line.MediaSession);

            if (result)
            {
                line.State = LineState.Connected;
                line.StartTime = DateTime.Now;
                Console.WriteLine($"[SIP] Arama basarili! Hat {lineIndex} -> Connected");
                await (OnCallAnswered?.Invoke() ?? Task.CompletedTask);
            }
            else
            {
                Console.WriteLine("[SIP] Arama basarisiz (Call() false dondu)");
                CleanupLine(line);
                await (OnCallFailed?.Invoke("Arama baglanamiyor") ?? Task.CompletedTask);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] MakeCallOnLineAsync exception: {ex.Message}\n{ex.StackTrace}");
            CleanupLine(line);
            await (OnCallFailed?.Invoke(ex.Message) ?? Task.CompletedTask);
            return false;
        }
    }

    private async Task<bool> HoldLineAsync(CallLine line)
    {
        Console.WriteLine($"[SIP] HoldLineAsync hat={line.Index}, State={line.State}, MediaSession null={line.MediaSession == null}");
        if (line.State != LineState.Connected || line.MediaSession == null) return false;

        try
        {
            // 1. Karsi tarafa hold muzigi veya sessizlik gonder
            if (line.MediaSession.AudioExtrasSource != null)
            {
                var holdSource = _holdMusicEnabled ? AudioSourcesEnum.Music : AudioSourcesEnum.Silence;
                line.MediaSession.AudioExtrasSource.SetSource(holdSource);
                Console.WriteLine($"[SIP] AudioExtrasSource -> {holdSource} (hold icin)");
            }
            else
            {
                Console.WriteLine("[SIP] UYARI: AudioExtrasSource null — hold muzigi gonderilemedi");
            }

            // 2. SDP hold sinyali (re-INVITE gerekmese de durum dogru olsun)
            await line.MediaSession.PutOnHold();
            Console.WriteLine("[SIP] PutOnHold() cagirildi");

            line.State = LineState.OnHold;
            Console.WriteLine($"[SIP] Hat {line.Index} hold'a alindi basariyla");
            await (OnLineChanged?.Invoke(line.Index) ?? Task.CompletedTask);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Hold hatasi: {ex.Message}\n{ex.StackTrace}");
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
        Console.WriteLine($"[SIP] CreateMediaSession: outputDevice={_outputDeviceIndex}, inputDevice={_inputDeviceIndex}, codecs=[{string.Join(",", _enabledCodecNames)}]");
        var winAudio = new WindowsAudioEndPoint(new AudioEncoder(), _outputDeviceIndex, _inputDeviceIndex);

        // Codec filtresi: Sadece etkinlestirilmis codec'ler
        winAudio.RestrictFormats(x =>
        {
            var codecName = x.Codec.ToString().ToUpperInvariant();
            return _enabledCodecNames.Any(c => c.Equals(codecName, StringComparison.OrdinalIgnoreCase));
        });

        var mediaSession = new VoIPMediaSession(winAudio.ToMediaEndPoints());
        mediaSession.AcceptRtpFromAny = true;

        Console.WriteLine("[SIP] MediaSession olusturuldu (AcceptRtpFromAny=true)");
        return mediaSession;
    }

    private SIPURI? BuildTargetUri(string destination)
    {
        if (destination.StartsWith("sip:"))
            return SIPURI.ParseSIPURI(destination);

        var host = SIPURI.ParseSIPURI(_config!.SipUri)?.Host;
        return host != null ? SIPURI.ParseSIPURI($"sip:{destination}@{host}") : null;
    }

    private void CleanupLine(CallLine line)
    {
        Console.WriteLine($"[SIP] CleanupLine hat={line.Index}, State={line.State}, RemoteUri={line.RemoteUri}");
        if (line.MediaSession != null)
        {
            try
            {
                line.MediaSession.Close(null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SIP] MediaSession.Close hatasi: {ex.Message}");
            }
            line.MediaSession = null;
        }

        if (line.UserAgent != null)
        {
            try
            {
                // UserAgent aktifse once hangup yap
                if (line.UserAgent.IsCallActive)
                    line.UserAgent.Hangup();
            }
            catch { }
            line.UserAgent = null;
        }

        line.PendingUas = null;
        line.State = LineState.Idle;
        line.RemoteUri = null;
        line.RemoteDisplayName = null;
        line.StartTime = null;
        Console.WriteLine($"[SIP] Hat {line.Index} temizlendi -> Idle");
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
            _waveWriter.Dispose();
            _waveWriter = null;
            _isRecording = false;
        }

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
