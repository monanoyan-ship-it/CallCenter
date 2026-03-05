using CallCenter.PbxService.Configuration;
using CallCenter.PbxService.Services;
using Microsoft.Extensions.Options;

namespace CallCenter.PbxService;

public class Worker(
    ILogger<Worker> logger,
    IOptions<PbxConfig> pbxConfig,
    SipTransportService transportService,
    SipRequestHandler requestHandler,
    ICallSessionManager sessionManager,
    ITrunkManager trunkManager) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = pbxConfig.Value;

        if (string.IsNullOrEmpty(config.CustomerUid))
        {
            logger.LogError("CustomerUid yapilandirilmamis! appsettings.json kontrol edin.");
            return;
        }

        logger.LogInformation("PBX Service baslatiliyor - Musteri: {CustomerUid}", config.CustomerUid);

        // 1. SIP Transport baslat
        await transportService.StartAsync(stoppingToken);

        // 2. Request handler'i transport'a bagla
        requestHandler.Bind();

        // 3. API'den trunk bilgilerini yukle ve register ol
        await trunkManager.LoadTrunksAsync(config.CustomerUid);
        await trunkManager.RegisterAllAsync();

        logger.LogInformation("PBX Service hazir. {TrunkCount} trunk, cagri bekleniyor...",
            trunkManager.RegisteredTrunkCount);

        // Periyodik durum raporu
        using var statusTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        try
        {
            while (await statusTimer.WaitForNextTickAsync(stoppingToken))
            {
                logger.LogInformation(
                    "PBX Durum: {ActiveCalls} aktif cagri, {RegisteredTrunks}/{TotalTrunks} trunk registered",
                    sessionManager.ActiveCallCount,
                    trunkManager.RegisteredTrunkCount,
                    trunkManager.GetAllTrunkStates().Count);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("PBX Service durduruluyor...");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        var activeCalls = sessionManager.ActiveCallCount;
        if (activeCalls > 0)
        {
            logger.LogWarning("{ActiveCalls} aktif cagri var, kapatiliyor...", activeCalls);
        }

        await trunkManager.UnregisterAllAsync();
        await transportService.StopAsync();
        await base.StopAsync(cancellationToken);
        logger.LogInformation("PBX Service durduruldu.");
    }
}
