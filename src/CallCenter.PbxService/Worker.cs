using CallCenter.PbxService.Configuration;
using CallCenter.PbxService.Services;
using Microsoft.Extensions.Options;

namespace CallCenter.PbxService;

public class Worker(
    ILogger<Worker> logger,
    IOptions<PbxConfig> pbxConfig,
    SipTransportService transportService,
    SipRequestHandler requestHandler,
    ICallSessionManager sessionManager) : BackgroundService
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

        // SIP Transport baslat
        await transportService.StartAsync(stoppingToken);

        // Request handler'i transport'a bagla
        requestHandler.Bind();

        logger.LogInformation("PBX Service hazir. Cagri bekleniyor...");

        // Periyodik durum raporu
        using var statusTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        try
        {
            while (await statusTimer.WaitForNextTickAsync(stoppingToken))
            {
                logger.LogInformation("PBX Durum: {ActiveCalls} aktif cagri",
                    sessionManager.ActiveCallCount);
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

        await transportService.StopAsync();
        await base.StopAsync(cancellationToken);
        logger.LogInformation("PBX Service durduruldu.");
    }
}
