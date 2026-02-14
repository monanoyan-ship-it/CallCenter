using Microsoft.JSInterop;

namespace CallCenter.Web.Services;

/// <summary>
/// SIP.js JavaScript modülünü Blazor tarafından yöneten servis.
/// IJSRuntime üzerinden sipClient.js fonksiyonlarını çağırır.
/// [JSInvokable] metodlarla JS → C# callback köprüsü kurar.
/// </summary>
public class SipService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private DotNetObjectReference<SipService>? _dotNetRef;

    // ─── Events (Component'lar dinler) ───
    public event Func<Task>? OnRegistered;
    public event Func<string, Task>? OnRegistrationFailed;
    public event Func<string, string, Task>? OnIncomingCall; // callerUri, callerDisplay
    public event Func<Task>? OnCallAnswered;
    public event Func<Task>? OnCallEnded;
    public event Func<string, Task>? OnCallFailed;

    // ─── State ───
    public bool IsRegistered { get; private set; }
    public bool IsInCall { get; private set; }
    public bool IsOnHold { get; private set; }

    public SipService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>SIP client'ı başlatır ve register olur. TURN/ICE parametreleri opsiyonel.</summary>
    public async Task<bool> InitializeAsync(string wsUri, string sipUri, string authUser, string authPass, string displayName,
        string? stunServer = null, string? turnServer = null, string? turnUsername = null, string? turnPassword = null)
    {
        _dotNetRef ??= DotNetObjectReference.Create(this);

        try
        {
            var result = await _js.InvokeAsync<bool>(
                "sipClient.initialize",
                wsUri, sipUri, authUser, authPass, displayName, _dotNetRef,
                stunServer, turnServer, turnUsername, turnPassword);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SipService] Initialize hatası: {ex.Message}");
            return false;
        }
    }

    /// <summary>Dış arama yapar.</summary>
    public async Task<bool> MakeCallAsync(string destination)
    {
        try
        {
            var result = await _js.InvokeAsync<bool>("sipClient.makeCall", destination);
            if (result)
            {
                IsInCall = true;
                IsOnHold = false;
            }
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SipService] MakeCall hatası: {ex.Message}");
            return false;
        }
    }

    /// <summary>Gelen aramayı kabul eder.</summary>
    public async Task<bool> AnswerCallAsync()
    {
        try
        {
            var result = await _js.InvokeAsync<bool>("sipClient.answerCall");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SipService] AnswerCall hatası: {ex.Message}");
            return false;
        }
    }

    /// <summary>Aktif aramayı kapatır.</summary>
    public async Task<bool> HangupAsync()
    {
        try
        {
            var result = await _js.InvokeAsync<bool>("sipClient.hangup");
            if (result)
            {
                IsInCall = false;
                IsOnHold = false;
            }
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SipService] Hangup hatası: {ex.Message}");
            IsInCall = false;
            IsOnHold = false;
            return false;
        }
    }

    /// <summary>Aramayı bekletir.</summary>
    public async Task<bool> HoldAsync()
    {
        try
        {
            var result = await _js.InvokeAsync<bool>("sipClient.holdCall");
            if (result) IsOnHold = true;
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SipService] Hold hatası: {ex.Message}");
            return false;
        }
    }

    /// <summary>Beklemedeki aramayı devam ettirir.</summary>
    public async Task<bool> UnholdAsync()
    {
        try
        {
            var result = await _js.InvokeAsync<bool>("sipClient.unholdCall");
            if (result) IsOnHold = false;
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SipService] Unhold hatası: {ex.Message}");
            return false;
        }
    }

    /// <summary>DTMF tonu gönderir.</summary>
    public async Task<bool> SendDtmfAsync(string tone)
    {
        try
        {
            var result = await _js.InvokeAsync<bool>("sipClient.sendDtmf", tone);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SipService] DTMF hatası: {ex.Message}");
            return false;
        }
    }

    /// <summary>Blind transfer yapar.</summary>
    public async Task<bool> TransferAsync(string target)
    {
        try
        {
            var result = await _js.InvokeAsync<bool>("sipClient.transferCall", target);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SipService] Transfer hatası: {ex.Message}");
            return false;
        }
    }

    /// <summary>Ses cihazlarını listeler.</summary>
    public async Task<List<AudioDeviceInfo>> GetAudioDevicesAsync()
    {
        try
        {
            var devices = await _js.InvokeAsync<List<AudioDeviceInfo>>("sipClient.getAudioDevices");
            return devices ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SipService] GetAudioDevices hatası: {ex.Message}");
            return [];
        }
    }

    /// <summary>Ses çıkış cihazını değiştirir.</summary>
    public async Task<bool> SetAudioDeviceAsync(string deviceId)
    {
        try
        {
            var result = await _js.InvokeAsync<bool>("sipClient.setAudioDevice", deviceId);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SipService] SetAudioDevice hatası: {ex.Message}");
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // JS → C# CALLBACK'LER ([JSInvokable])
    // ═══════════════════════════════════════════════════════════════

    [JSInvokable("OnRegistered")]
    public async Task JsOnRegistered(string _)
    {
        IsRegistered = true;
        if (OnRegistered != null)
            await OnRegistered.Invoke();
    }

    [JSInvokable("OnRegistrationFailed")]
    public async Task JsOnRegistrationFailed(string error)
    {
        IsRegistered = false;
        if (OnRegistrationFailed != null)
            await OnRegistrationFailed.Invoke(error);
    }

    [JSInvokable("OnIncomingCall")]
    public async Task JsOnIncomingCall(string data)
    {
        // data formatı: "callerUri|callerDisplay"
        var parts = data.Split('|', 2);
        var callerUri = parts[0];
        var callerDisplay = parts.Length > 1 ? parts[1] : "";

        IsInCall = true;
        IsOnHold = false;

        if (OnIncomingCall != null)
            await OnIncomingCall.Invoke(callerUri, callerDisplay);
    }

    [JSInvokable("OnCallAnswered")]
    public async Task JsOnCallAnswered(string _)
    {
        IsInCall = true;
        if (OnCallAnswered != null)
            await OnCallAnswered.Invoke();
    }

    [JSInvokable("OnCallEnded")]
    public async Task JsOnCallEnded(string _)
    {
        IsInCall = false;
        IsOnHold = false;
        if (OnCallEnded != null)
            await OnCallEnded.Invoke();
    }

    [JSInvokable("OnCallFailed")]
    public async Task JsOnCallFailed(string error)
    {
        IsInCall = false;
        IsOnHold = false;
        if (OnCallFailed != null)
            await OnCallFailed.Invoke(error);
    }

    // ═══════════════════════════════════════════════════════════════

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("sipClient.dispose");
        }
        catch { }

        _dotNetRef?.Dispose();
        _dotNetRef = null;

        IsRegistered = false;
        IsInCall = false;
        IsOnHold = false;
    }
}

/// <summary>Ses cihazı bilgisi (JS'den gelen).</summary>
public class AudioDeviceInfo
{
    public string DeviceId { get; set; } = "";
    public string Label { get; set; } = "";
    public string Kind { get; set; } = ""; // "audioinput" veya "audiooutput"
}
