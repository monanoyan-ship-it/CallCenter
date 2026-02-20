using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace CallCenter.Windows.Services;

/// <summary>
/// SignalR hub baglantisini yoneten servis.
/// JWT ile authenticate olur, otomatik reconnect yapar.
/// </summary>
public class WindowsHubService : IAsyncDisposable
{
    private readonly WindowsAuthService _authService;
    private readonly string _hubUrl;
    private HubConnection? _connection;

    // ─── Events ───
    public event Action<AgentStatusUpdate>? OnAgentStatusChanged;
    public event Action<CallNotification>? OnIncomingCall;
    public event Action<int>? OnCallEnded;
    public event Action<HubConnectionState>? OnConnectionStateChanged;

    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public WindowsHubService(WindowsAuthService authService, IConfiguration config)
    {
        _authService = authService;
        var apiBase = config["ApiBaseUrl"] ?? "https://localhost:7147";
        _hubUrl = $"{apiBase}/hubs/callcenter";
    }

    /// <summary>Hub'a baglanir. Token yoksa veya gecersizse baglanmaz.</summary>
    public async Task ConnectAsync()
    {
        if (_connection is { State: HubConnectionState.Connected or HubConnectionState.Connecting })
            return;

        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.AccessTokenProvider = () => _authService.GetTokenAsync()!;
            })
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
            .Build();

        // Event handler'lari kaydet
        _connection.On<AgentStatusUpdate>("AgentStatusChanged", update =>
        {
            OnAgentStatusChanged?.Invoke(update);
        });

        _connection.On<CallNotification>("IncomingCall", notification =>
        {
            OnIncomingCall?.Invoke(notification);
        });

        _connection.On<int>("CallEnded", callId =>
        {
            OnCallEnded?.Invoke(callId);
        });

        _connection.Reconnecting += _ =>
        {
            OnConnectionStateChanged?.Invoke(HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            OnConnectionStateChanged?.Invoke(HubConnectionState.Connected);
            return Task.CompletedTask;
        };

        _connection.Closed += _ =>
        {
            OnConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync();
            OnConnectionStateChanged?.Invoke(HubConnectionState.Connected);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HubService] Baglanti hatasi: {ex.Message}");
            OnConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
        }
    }

    /// <summary>Hub baglantisini kapatir.</summary>
    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
            OnConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
        }
    }

    /// <summary>Agent durumunu gunceller (SignalR uzerinden).</summary>
    public async Task UpdateStatusAsync(int statusId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("UpdateMyStatus", statusId);
        }
    }

    /// <summary>Gelen arama bildirimi gonderir.</summary>
    public async Task NotifyIncomingCallAsync(CallNotification notification)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("NotifyIncomingCall", notification);
        }
    }

    /// <summary>Arama bitti bildirimi gonderir.</summary>
    public async Task NotifyCallEndedAsync(int callId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("NotifyCallEnded", callId);
        }
    }

    /// <summary>Gateway (SIP) saglik durumunu hub'a bildirir.</summary>
    public async Task UpdateGatewayHealthAsync(GatewayHealthUpdate update)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("UpdateGatewayHealth", update);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
