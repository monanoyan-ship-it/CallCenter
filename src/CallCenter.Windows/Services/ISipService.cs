using CallCenter.Shared.DTOs;
using CallCenter.Windows.Models;

namespace CallCenter.Windows.Services;

/// <summary>
/// SIP servis interface'i — platform bagimsiz arama kontrolu.
/// Web'deki SipService (JS interop) ile ayni method/event yapisi.
/// </summary>
public interface ISipService : IAsyncDisposable
{
    // ─── State ───
    bool IsRegistered { get; }
    bool IsInCall { get; }
    bool IsOnHold { get; }

    // ─── Events ───
    event Func<Task>? OnRegistered;
    event Func<string, Task>? OnRegistrationFailed;
    event Func<string, string, Task>? OnIncomingCall;
    event Func<Task>? OnCallAnswered;
    event Func<Task>? OnCallEnded;
    event Func<string, Task>? OnCallFailed;

    // ─── Methods ───
    Task<bool> InitializeAsync(SipConnectionInfoDto config);
    Task<bool> MakeCallAsync(string destination);
    Task<bool> AnswerCallAsync();
    Task<bool> HangupAsync();
    Task<bool> HoldAsync();
    Task<bool> UnholdAsync();
    Task<bool> SendDtmfAsync(string tone);
    Task<bool> TransferAsync(string target);

    // ─── Audio ───
    Task<List<AudioDeviceInfo>> GetAudioInputDevicesAsync();
    Task<List<AudioDeviceInfo>> GetAudioOutputDevicesAsync();
    Task<bool> SetAudioInputDeviceAsync(int deviceIndex);
    Task<bool> SetAudioOutputDeviceAsync(int deviceIndex);
}
