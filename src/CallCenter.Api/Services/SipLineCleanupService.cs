using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Infrastructure;

namespace CallCenter.Api.Services;

/// <summary>
/// Stale SIP hat tahsislerini temizler.
/// SignalR disconnect yakalanamazsa (elektrik kesintisi, network drop)
/// bu servis periyodik olarak eski tahsisleri serbest birakir.
/// </summary>
public class SipLineCleanupService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<SipLineCleanupService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);

    public SipLineCleanupService(IServiceProvider sp, ILogger<SipLineCleanupService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);

            try
            {
                using var scope = _sp.CreateScope();
                var lineEs = scope.ServiceProvider.GetRequiredService<ISipLineEntityService>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                await lineEs.ReleaseStaleAllocationsAsync(MaxAge);
                await uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SIP hat temizligi hatasi");
            }
        }
    }
}
