using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using CallCenter.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace CallCenter.Windows.Services;

/// <summary>
/// SignalR hub baglantisini yoneten servis.
/// JWT ile yetkilendirilmis baglanti kurar ve event'leri expose eder.
/// </summary>
public class WindowsHubService : IAsyncDisposable
{
    private readonly IConfiguration _config;
    private readonly WindowsAuthService _auth;
    private readonly WindowsSessionEvents _sessionEvents;
    private HubConnection? _connection;

    // ─── Events ───
    public event Action<AgentStatusUpdate>? OnAgentStatusChanged;
    public event Action<CallNotification>? OnIncomingCall;
    public event Action<object>? OnNewCallbackTask;
    public event Action<object>? OnCallbackTaskStarted;
    public event Action<object>? OnCallbackTaskCompleted;
    public event Action<object>? OnCallbackTaskReverted;
    public event Action<int>? OnCallEnded;
    public event Action<HubConnectionState>? OnConnectionStateChanged;

    // ─── Force Logout Event ───
    public event Func<Task>? OnForceLogout;

    public WindowsHubService(IConfiguration config, WindowsAuthService auth, WindowsSessionEvents sessionEvents)
    {
        _config = config;
        _auth = auth;
        _sessionEvents = sessionEvents;
    }

    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync()
    {
        if (_connection != null && _connection.State != HubConnectionState.Disconnected)
            return;

        var token = await _auth.GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return;

        var hubUrl = _config["ApiBaseUrl"]?.TrimEnd('/') + "/hubs/callcenter";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => _auth.GetTokenAsync()!;
                // IPv4 zorla - bazi PC'lerde IPv6 DNS cozumlemesi sorun yaratir
                options.HttpMessageHandlerFactory = _ => new SocketsHttpHandler
                {
                    ConnectCallback = async (context, ct) =>
                    {
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.NoDelay = true;
                        try
                        {
                            await socket.ConnectAsync(context.DnsEndPoint, ct);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                        catch
                        {
                            socket.Dispose();
                            throw;
                        }
                    },
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = (_, _, _, _) => true
                    }
                };
            })
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
            .Build();

        // ─── Listeners ───

        _connection.On<AgentStatusUpdate>("AgentStatusChanged", update =>
        {
            OnAgentStatusChanged?.Invoke(update);
        });

        _connection.On<CallNotification>("IncomingCall", notification =>
        {
            OnIncomingCall?.Invoke(notification);
        });

        _connection.On<object>("NewCallbackTask", data =>
        {
            OnNewCallbackTask?.Invoke(data);
        });

        _connection.On<object>("CallbackTaskStarted", data =>
        {
            OnCallbackTaskStarted?.Invoke(data);
        });

        _connection.On<object>("CallbackTaskCompleted", data =>
        {
            OnCallbackTaskCompleted?.Invoke(data);
        });

        _connection.On<object>("CallbackTaskReverted", data =>
        {
            OnCallbackTaskReverted?.Invoke(data);
        });

        _connection.On<int>("CallEnded", callId =>
        {
            OnCallEnded?.Invoke(callId);
        });

        _connection.On("ForceLogout", async () =>
        {
            await _sessionEvents.InvalidateAsync("Bu kullanici baska bir cihazda oturum acti.");
            if (OnForceLogout != null) await OnForceLogout.Invoke();
        });

        _connection.On<string>("ForceLogout", async reason =>
        {
            await _sessionEvents.InvalidateAsync(string.IsNullOrWhiteSpace(reason) ? "Oturum sonlandirildi." : reason);
            if (OnForceLogout != null) await OnForceLogout.Invoke();
        });

        _connection.Closed += async (error) =>
        {
            OnConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
            if (IsAuthFailure(error))
                await _sessionEvents.InvalidateAsync("SignalR oturumu yetkisiz kaldi. Lutfen tekrar giris yapin.");
        };

        _connection.Reconnecting += (error) =>
        {
            OnConnectionStateChanged?.Invoke(HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        _connection.Reconnected += (connectionId) =>
        {
            OnConnectionStateChanged?.Invoke(HubConnectionState.Connected);
            return Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync();
            OnConnectionStateChanged?.Invoke(HubConnectionState.Connected);
        }
        catch (Exception ex) when (IsAuthFailure(ex))
        {
            await _sessionEvents.InvalidateAsync("SignalR oturumu yetkisiz kaldi. Lutfen tekrar giris yapin.");
            throw;
        }
    }

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

    public async Task SendAsync(string methodName, object? arg1 = null)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            if (arg1 != null) await _connection.SendAsync(methodName, arg1);
            else await _connection.SendAsync(methodName);
        }
    }

    // ─── Helpers (Missing methods restored) ───

    public async Task UpdateStatusAsync(int statusId)
    {
        await SendAsync("UpdateMyStatus", statusId);
    }

    public async Task NotifyCallEndedAsync(int callId)
    {
        await SendAsync("NotifyCallEnded", callId);
    }

    public async Task UpdateGatewayHealthAsync(object update)
    {
        await SendAsync("UpdateGatewayHealth", update);
    }

    public void OnGatewayHealthUpdate(Action<object> action)
    {
        if (_connection != null)
        {
            _connection.On("UpdateGatewayHealth", action);
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

    private static bool IsAuthFailure(Exception? error)
    {
        if (error == null) return false;
        var message = error.Message;
        return message.Contains("401", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("403", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase);
    }
}
