using System.Collections.Concurrent;
using CallCenter.PbxService.Configuration;
using Microsoft.Extensions.Options;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;

namespace CallCenter.PbxService.Services;

public class TrunkManager : ITrunkManager, IDisposable
{
    private readonly ILogger<TrunkManager> _logger;
    private readonly IApiClient _apiClient;
    private readonly ISipTransportService _transport;
    private readonly SipConfig _sipConfig;

    private readonly ConcurrentDictionary<int, TrunkState> _trunkStates = new();
    private readonly ConcurrentDictionary<int, SIPRegistrationUserAgent> _registrations = new();
    private readonly ConcurrentDictionary<int, TrunkInfo> _trunkInfos = new();

    public int RegisteredTrunkCount => _trunkStates.Values.Count(t => t.Status == TrunkStatus.Registered);

    public TrunkManager(
        ILogger<TrunkManager> logger,
        IApiClient apiClient,
        ISipTransportService transport,
        IOptions<SipConfig> sipConfig)
    {
        _logger = logger;
        _apiClient = apiClient;
        _transport = transport;
        _sipConfig = sipConfig.Value;
    }

    public async Task LoadTrunksAsync(string customerUid)
    {
        _logger.LogInformation("Trunk bilgileri API'den yukleniyor...");

        var trunks = await _apiClient.GetSipAccountsAsync(customerUid);

        if (trunks.Count == 0)
        {
            _logger.LogWarning("Hicbir SIP trunk bulunamadi! API endpoint kontrolu gerekli.");
            return;
        }

        foreach (var trunk in trunks.Where(t => t.IsActive))
        {
            _trunkInfos[trunk.Id] = trunk;
            _trunkStates[trunk.Id] = new TrunkState
            {
                SipAccountId = trunk.Id,
                Name = trunk.Name,
                Server = trunk.Server
            };
            _logger.LogInformation("Trunk yuklendi: {Name} ({Server}:{Port})",
                trunk.Name, trunk.Server, trunk.Port);
        }

        _logger.LogInformation("{Count} aktif trunk yuklendi", _trunkStates.Count);
    }

    public async Task RegisterAllAsync()
    {
        foreach (var (id, trunk) in _trunkInfos)
        {
            await RegisterTrunkAsync(id, trunk);
        }
    }

    public async Task UnregisterAllAsync()
    {
        foreach (var (id, regAgent) in _registrations)
        {
            try
            {
                regAgent.Stop();
                if (_trunkStates.TryGetValue(id, out var state))
                {
                    state.Status = TrunkStatus.Disconnected;
                }
                _logger.LogInformation("Trunk unregister: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Trunk unregister hatasi: {Id}", id);
            }
        }
        _registrations.Clear();
        await Task.CompletedTask;
    }

    public TrunkState? GetTrunkState(int sipAccountId)
    {
        _trunkStates.TryGetValue(sipAccountId, out var state);
        return state;
    }

    public IReadOnlyCollection<TrunkState> GetAllTrunkStates()
    {
        return _trunkStates.Values.ToList().AsReadOnly();
    }

    private async Task RegisterTrunkAsync(int id, TrunkInfo trunk)
    {
        if (_trunkStates.TryGetValue(id, out var state))
        {
            state.Status = TrunkStatus.Registering;
        }

        try
        {
            var sipUri = SIPURI.ParseSIPURI($"sip:{trunk.Username}@{trunk.Server}:{trunk.Port}");

            var regAgent = new SIPRegistrationUserAgent(
                _transport.Transport,
                trunk.Username,
                trunk.Password,
                trunk.Domain ?? trunk.Server,
                expiry: 3600);

            regAgent.RegistrationSuccessful += (uri, resp) =>
            {
                if (_trunkStates.TryGetValue(id, out var s))
                {
                    s.Status = TrunkStatus.Registered;
                    s.LastRegisteredAt = DateTime.UtcNow;
                    s.FailCount = 0;
                    s.LastError = null;
                }
                _logger.LogInformation("Trunk REGISTERED: {Name} ({Server})", trunk.Name, trunk.Server);
            };

            regAgent.RegistrationFailed += (uri, resp, err) =>
            {
                if (_trunkStates.TryGetValue(id, out var s))
                {
                    s.Status = TrunkStatus.Failed;
                    s.LastFailedAt = DateTime.UtcNow;
                    s.FailCount++;
                    s.LastError = err ?? resp?.ReasonPhrase ?? "Bilinmeyen hata";
                }
                _logger.LogError("Trunk REGISTER FAILED: {Name} - {Error}",
                    trunk.Name, err ?? resp?.ReasonPhrase);
            };

            _registrations[id] = regAgent;
            regAgent.Start();

            _logger.LogInformation("Trunk register baslatildi: {Name} ({User}@{Server}:{Port})",
                trunk.Name, trunk.Username, trunk.Server, trunk.Port);
        }
        catch (Exception ex)
        {
            if (_trunkStates.TryGetValue(id, out var s))
            {
                s.Status = TrunkStatus.Failed;
                s.LastFailedAt = DateTime.UtcNow;
                s.FailCount++;
                s.LastError = ex.Message;
            }
            _logger.LogError(ex, "Trunk register hatasi: {Name}", trunk.Name);
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var reg in _registrations.Values)
        {
            try { reg.Stop(); } catch { }
        }
        _registrations.Clear();
    }
}
