using CallCenter.Shared.DTOs;
using CallCenter.Windows.Models;

namespace CallCenter.Windows.Services;

/// <summary>
/// SIP servis interface'i — MicroSIP feature parity.
/// Register, arama, hold, DTMF, blind/attended transfer,
/// multi-line, DND, auto-answer, volume, recording, codec, STUN/ICE, voicemail.
/// </summary>
public interface ISipService : IAsyncDisposable
{
    // ─── State ───
    bool IsRegistered { get; }
    bool IsInCall { get; }
    bool IsOnHold { get; }
    bool IsDndEnabled { get; }
    bool IsAutoAnswerEnabled { get; }
    bool IsRecording { get; }
    bool IsMuted { get; }
    int ActiveLineIndex { get; }
    int LineCount { get; }

    // ─── Events ───
    event Func<Task>? OnRegistered;
    event Func<string, Task>? OnRegistrationFailed;
    event Func<string, string, Task>? OnIncomingCall;
    event Func<Task>? OnCallAnswered;
    event Func<Task>? OnCallEnded;
    event Func<string, Task>? OnCallFailed;
    event Func<int, Task>? OnLineChanged;
    event Func<bool, Task>? OnMuteChanged;
    event Func<int, Task>? OnVoicemailCountChanged;
    event Func<byte, int, Task>? OnDtmfReceived; // (tone 0-15, durationMs)

    // ─── Core Call Methods ───
    Task<bool> InitializeAsync(SipConnectionInfoDto config);
    Task<bool> MakeCallAsync(string destination);
    Task<bool> AnswerCallAsync();
    Task<bool> HangupAsync();
    Task<bool> HoldAsync();
    Task<bool> UnholdAsync();
    Task<bool> SendDtmfAsync(string tone);

    // ─── Transfer ───
    Task<bool> BlindTransferAsync(string target);
    Task<bool> AttendedTransferAsync(string target);
    Task<bool> CompleteAttendedTransferAsync();
    Task<bool> CancelAttendedTransferAsync();

    // ─── Multi-Line ───
    Task<List<LineInfo>> GetLinesAsync();
    Task<bool> SwitchLineAsync(int lineIndex);
    Task<bool> MakeCallOnNewLineAsync(string destination);

    // ─── DND & Auto-Answer ───
    void SetDnd(bool enabled);
    void SetAutoAnswer(bool enabled);

    // ─── Mute ───
    Task<bool> MuteAsync();
    Task<bool> UnmuteAsync();

    // ─── Volume ───
    float MicrophoneVolume { get; }
    float SpeakerVolume { get; }
    void SetMicrophoneVolume(float volume);
    void SetSpeakerVolume(float volume);

    // ─── Recording ───
    string? LastRecordingPath { get; }
    void SetEncryptionKey(string key);
    Task<bool> StartRecordingAsync(string? filePath = null);
    Task<bool> StopRecordingAsync();

    // ─── Codec ───
    List<CodecInfo> GetAvailableCodecs();
    List<CodecInfo> GetEnabledCodecs();
    void SetEnabledCodecs(List<string> codecNames);

    // ─── SRTP ───
    bool IsSrtpEnabled { get; }
    void SetSrtp(bool enabled);

    // ─── STUN/ICE/NAT ───
    string? StunServer { get; }
    string? TurnServer { get; }
    string? TurnUsername { get; }
    void SetStunServer(string? stunServer);
    void SetIceServers(string? stunServer, string? turnServer, string? turnUsername, string? turnPassword);

    // ─── Audio Devices ───
    Task<List<AudioDeviceInfo>> GetAudioInputDevicesAsync();
    Task<List<AudioDeviceInfo>> GetAudioOutputDevicesAsync();
    Task<bool> SetAudioInputDeviceAsync(int deviceIndex);
    Task<bool> SetAudioOutputDeviceAsync(int deviceIndex);

    // ─── Voicemail (MWI) ───
    int VoicemailCount { get; }
    Task<bool> DialVoicemailAsync();

    // ─── Ringtone ───
    string? RingtonePath { get; }
    void SetRingtone(string? filePath);

    // ─── Hold Music ───
    void SetHoldMusicEnabled(bool enabled);

    // ─── Reset ───
    /// <summary>SIP baglantiyi sifirla: tum hatlari temizle, re-register yap, API'deki hat atamasini serbest birak</summary>
    Task ResetConnectionAsync();
}
