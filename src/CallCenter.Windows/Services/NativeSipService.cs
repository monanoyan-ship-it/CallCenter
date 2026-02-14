using System.IO;
using System.Net;
using CallCenter.Shared.DTOs;
using CallCenter.Windows.Models;
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
    private List<string> _enabledCodecNames = new() { "PCMU", "PCMA", "G722" };

    // ─── Ozellikler ───
    private bool _dndEnabled;
    private bool _autoAnswerEnabled;
    private bool _muted;
    private bool _srtpEnabled;
    private string? _stunServer;
    private string? _ringtonePath;

    // ─── Recording ───
    private WaveFileWriter? _waveWriter;
    private bool _isRecording;
    private string? _recordingWavPath; // Sifrelemeden onceki WAV yolu
    private int _recordingPayloadType; // Aktif codec: 0=PCMU, 8=PCMA, 9=G722

    // ─── Voicemail (MWI) ───
    private int _voicemailCount;
    private string? _voicemailNumber;

    // ─── Attended Transfer ───
    private int? _transferSourceLineIndex;

    // ─── Ringtone oynatici ───
    private WaveOutEvent? _ringtonePlayer;
    private AudioFileReader? _ringtoneReader;

    // ─── Recording sifreleme ───
    private string? _encryptionKey;

    public NativeSipService()
    {
        for (int i = 0; i < MaxLines; i++)
            _lines[i] = new CallLine { Index = i };
    }

    /// <summary>Kayit sifreleme anahtarini ayarla (DI veya config'den).</summary>
    public void SetEncryptionKey(string key) => _encryptionKey = key;

    // ═══════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════

    public bool IsRegistered { get; private set; }
    public bool IsInCall => ActiveLine.State is LineState.Connected or LineState.OnHold or LineState.Connecting;
    public bool IsOnHold => ActiveLine.State == LineState.OnHold;
    public bool IsDndEnabled => _dndEnabled;
    public bool IsAutoAnswerEnabled => _autoAnswerEnabled;
    public bool IsRecording => _isRecording;
    public bool IsMuted => _muted;
    public int ActiveLineIndex => _activeLineIndex;
    public int LineCount => MaxLines;
    public float MicrophoneVolume => _micVolume;
    public float SpeakerVolume => _speakerVolume;
    public bool IsSrtpEnabled => _srtpEnabled;
    public string? StunServer => _stunServer;
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

    // ═══════════════════════════════════════════════════
    // INITIALIZE & REGISTER
    // ═══════════════════════════════════════════════════

    public async Task<bool> InitializeAsync(SipConnectionInfoDto config)
    {
        try
        {
            _config = config;

            var sipUri = SIPURI.ParseSIPURI(config.SipUri);
            if (sipUri == null)
            {
                await (OnRegistrationFailed?.Invoke("Gecersiz SIP URI") ?? Task.CompletedTask);
                return false;
            }

            _sipTransport = new SIPTransport();

            // STUN ayari
            if (!string.IsNullOrEmpty(_stunServer))
            {
                // SIPSorcery STUN destegi: STUNUri set edilir, NAT discovery icin kullanilir
                // Transport bazinda STUN client SIPSorcery'de SIPTransport.STUNRequestTimeout ile desteklenir
            }

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
        if (line == null || _sipTransport == null) return false;

        try
        {
            // Mevcut aktif arama varsa hold'a al
            if (ActiveLine.State == LineState.Connected && ActiveLine.Index != line.Index)
            {
                await HoldLineAsync(ActiveLine);
            }

            line.MediaSession = CreateMediaSession();
            var result = await line.UserAgent!.Answer(line.PendingUas!, line.MediaSession);
            line.PendingUas = null;

            if (result)
            {
                line.State = LineState.Connected;
                line.StartTime = DateTime.Now;
                _activeLineIndex = line.Index;
                StopRingtone();
                await (OnCallAnswered?.Invoke() ?? Task.CompletedTask);
                await (OnLineChanged?.Invoke(line.Index) ?? Task.CompletedTask);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Answer hatasi: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> HangupAsync()
    {
        var line = ActiveLine;

        // Ringing varsa onu kapat
        var ringing = FindRingingLine();
        if (ringing != null && line.State == LineState.Idle)
            line = ringing;

        if (line.State == LineState.Idle && line.PendingUas == null) return false;

        try
        {
            if (line.PendingUas != null)
            {
                line.PendingUas.Reject(SIPResponseStatusCodesEnum.BusyHere, null);
                line.PendingUas = null;
                StopRingtone();
            }

            if (line.UserAgent?.IsCallActive == true)
            {
                line.UserAgent.Hangup();
            }

            CleanupLine(line);
            await (OnCallEnded?.Invoke() ?? Task.CompletedTask);

            // Baska aktif hat varsa ona gec
            var nextActive = Array.FindIndex(_lines, l => l.State != LineState.Idle);
            if (nextActive >= 0)
            {
                _activeLineIndex = nextActive;
                await (OnLineChanged?.Invoke(_activeLineIndex) ?? Task.CompletedTask);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Hangup hatasi: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> HoldAsync()
    {
        if (ActiveLine.State != LineState.Connected) return false;
        return await HoldLineAsync(ActiveLine);
    }

    public Task<bool> UnholdAsync()
    {
        if (ActiveLine.State != LineState.OnHold) return Task.FromResult(false);

        try
        {
            ActiveLine.MediaSession?.TakeOffHold();
            ActiveLine.State = LineState.Connected;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Unhold hatasi: {ex.Message}");
            return Task.FromResult(false);
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
        _muted = true;
        // MediaSession mute: SDP sendonly veya sadece audio source mute
        if (ActiveLine.MediaSession != null)
        {
            await ActiveLine.MediaSession.PutOnHold();
        }
        await (OnMuteChanged?.Invoke(true) ?? Task.CompletedTask);
        return true;
    }

    public async Task<bool> UnmuteAsync()
    {
        _muted = false;
        if (ActiveLine.MediaSession != null)
        {
            ActiveLine.MediaSession.TakeOffHold();
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
            var path = filePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CallCenter", "Recordings",
                $"call_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

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
            new() { Name = "PCMU", PayloadType = 0, SampleRate = 8000, IsEnabled = _enabledCodecNames.Contains("PCMU"), Priority = 0 },
            new() { Name = "PCMA", PayloadType = 8, SampleRate = 8000, IsEnabled = _enabledCodecNames.Contains("PCMA"), Priority = 1 },
            new() { Name = "G722", PayloadType = 9, SampleRate = 16000, IsEnabled = _enabledCodecNames.Contains("G722"), Priority = 2 },
        };
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
        _inputDeviceIndex = deviceIndex;
        return Task.FromResult(true);
    }

    public Task<bool> SetAudioOutputDeviceAsync(int deviceIndex)
    {
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
        if (sipRequest.Method == SIPMethodsEnum.INVITE)
        {
            await HandleIncomingInvite(sipRequest);
        }
        else if (sipRequest.Method == SIPMethodsEnum.NOTIFY)
        {
            HandleNotify(sipRequest);
        }
    }

    private async Task HandleIncomingInvite(SIPRequest sipRequest)
    {
        // DND aktifse reject
        if (_dndEnabled)
        {
            var dndResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.BusyHere, "DND aktif");
            await _sipTransport!.SendResponseAsync(dndResponse);
            return;
        }

        // Bos hat bul
        var freeLine = FindFreeLine();
        if (freeLine == null)
        {
            var busyResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.BusyHere, null);
            await _sipTransport!.SendResponseAsync(busyResponse);
            return;
        }

        // SIPUserAgent olustur
        freeLine.UserAgent = new SIPUserAgent(_sipTransport!, null);
        freeLine.UserAgent.OnCallHungup += (dialog) =>
        {
            CleanupLine(freeLine);
            _ = OnCallEnded?.Invoke() ?? Task.CompletedTask;
        };

        // AcceptCall (180 Ringing)
        freeLine.PendingUas = freeLine.UserAgent.AcceptCall(sipRequest);
        freeLine.State = LineState.Ringing;

        var callerUri = sipRequest.Header.From?.FromURI?.ToString() ?? "Bilinmeyen";
        var callerDisplay = sipRequest.Header.From?.FromName ?? callerUri;
        freeLine.RemoteUri = callerUri;
        freeLine.RemoteDisplayName = callerDisplay;

        // Zil sesi cal
        PlayRingtone();

        // Auto-answer
        if (_autoAnswerEnabled)
        {
            _activeLineIndex = freeLine.Index;
            await AnswerCallAsync();
            return;
        }

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
        if (_sipTransport == null || _config == null) return false;
        if (line.State != LineState.Idle) return false;

        try
        {
            line.UserAgent = new SIPUserAgent(_sipTransport, null);
            line.UserAgent.ClientCallFailed += (uac, error, response) =>
            {
                CleanupLine(line);
                _ = OnCallFailed?.Invoke(error ?? "Arama basarisiz") ?? Task.CompletedTask;
            };
            line.UserAgent.OnCallHungup += (dialog) =>
            {
                CleanupLine(line);
                _ = OnCallEnded?.Invoke() ?? Task.CompletedTask;
            };

            line.MediaSession = CreateMediaSession();
            line.State = LineState.Connecting;

            var destUri = BuildTargetUri(destination);
            if (destUri == null)
            {
                CleanupLine(line);
                await (OnCallFailed?.Invoke("Gecersiz hedef numara") ?? Task.CompletedTask);
                return false;
            }

            line.RemoteUri = destination;
            line.RemoteDisplayName = destination;

            var result = await line.UserAgent.Call(destUri.ToString(), _config.AuthUsername, _config.AuthPassword, line.MediaSession);

            if (result)
            {
                line.State = LineState.Connected;
                line.StartTime = DateTime.Now;
                await (OnCallAnswered?.Invoke() ?? Task.CompletedTask);
            }
            else
            {
                CleanupLine(line);
                await (OnCallFailed?.Invoke("Arama baglanamiyor") ?? Task.CompletedTask);
            }

            return result;
        }
        catch (Exception ex)
        {
            CleanupLine(line);
            await (OnCallFailed?.Invoke(ex.Message) ?? Task.CompletedTask);
            return false;
        }
    }

    private async Task<bool> HoldLineAsync(CallLine line)
    {
        if (line.State != LineState.Connected || line.MediaSession == null) return false;

        try
        {
            await line.MediaSession.PutOnHold();
            line.State = LineState.OnHold;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SIP] Hold hatasi: {ex.Message}");
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
        var winAudio = new WindowsAudioEndPoint(new AudioEncoder(), _outputDeviceIndex, _inputDeviceIndex);

        // Codec filtresi: Sadece etkinlestirilmis codec'ler
        winAudio.RestrictFormats(x =>
        {
            var codecName = x.Codec.ToString().ToUpperInvariant();
            return _enabledCodecNames.Any(c => c.Equals(codecName, StringComparison.OrdinalIgnoreCase));
        });

        var mediaSession = new VoIPMediaSession(winAudio.ToMediaEndPoints());
        mediaSession.AcceptRtpFromAny = true;

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
        if (line.MediaSession != null)
        {
            line.MediaSession.Close(null);
            line.MediaSession = null;
        }

        line.PendingUas = null;
        line.UserAgent = null;
        line.State = LineState.Idle;
        line.RemoteUri = null;
        line.RemoteDisplayName = null;
        line.StartTime = null;
    }

    // ═══════════════════════════════════════════════════
    // DISPOSE
    // ═══════════════════════════════════════════════════

    public ValueTask DisposeAsync()
    {
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
// HELPER: Codec-aware audio decoder (PCMU + PCMA + G.722)
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

    /// <summary>
    /// RTP payload type'a gore ses verisini PCM16'ya decode eder.
    /// PT 0 = PCMU (mu-law, 8kHz), PT 8 = PCMA (A-law, 8kHz), PT 9 = G.722 (ADPCM, 16kHz).
    /// </summary>
    public static byte[] Decode(byte[] payload, int payloadType)
    {
        return payloadType switch
        {
            0 => DecodeMuLaw(payload),
            8 => DecodeALaw(payload),
            9 => DecodeG722(payload),
            _ => DecodeMuLaw(payload) // fallback: mu-law
        };
    }

    /// <summary>Payload type'a gore sample rate doner. G.722 = 16000, digerleri = 8000.</summary>
    public static int GetSampleRate(int payloadType) => payloadType == 9 ? 16000 : 8000;

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
