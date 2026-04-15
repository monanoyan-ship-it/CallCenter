using CallCenter.Data;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

/// <summary>
/// Periyodik trial/abonelik suresi dolma kontrolu.
/// PAY.5: 5 gun trial (MonthlyPrice=0) suresi gecmis aktif aboneliklerin StatusId=2 (askıda) yapilir.
/// Aktif (ucretli) aboneliklerde NextBillingDate + PaymentGraceDays gecmisse + odenmemis tahakkuk varsa askıya alinir.
/// </summary>
public class SubscriptionExpiryHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<SubscriptionExpiryHostedService> _logger;
    private static readonly TimeSpan Period = TimeSpan.FromHours(1);

    public SubscriptionExpiryHostedService(IServiceProvider sp, ILogger<SubscriptionExpiryHostedService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ilk calistirma 60sn gecikmeli (startup yarisindan kacin)
        try { await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken); } catch { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Subscription expiry loop hatasi"); }

            try { await Task.Delay(Period, stoppingToken); } catch { return; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        // 1) Trial expiry (MonthlyPrice=0, StatusId=1, NextBillingDate < now)
        var expiredTrials = await db.CustomerSubscriptions
            .Where(s => s.StatusId == 1 && s.MonthlyPrice == 0 && s.NextBillingDate < now)
            .ToListAsync(ct);
        foreach (var sub in expiredTrials) sub.StatusId = 2;
        if (expiredTrials.Count > 0)
            _logger.LogInformation("Trial subscription expired: {Count}", expiredTrials.Count);

        // 2) Ucretli aboneliklerde grace period asilmis + odenmemis tahakkuk
        var activePaid = await db.CustomerSubscriptions
            .Where(s => s.StatusId == 1 && s.MonthlyPrice > 0)
            .ToListAsync(ct);
        int suspendedPaid = 0;
        foreach (var sub in activePaid)
        {
            var overdueLimit = sub.NextBillingDate.AddDays(sub.PaymentGraceDays);
            if (overdueLimit >= now) continue;
            // Bekleyen tahakkuk var mi
            var hasUnpaid = await db.CustomerBillingPeriods
                .AnyAsync(p => p.CustomerId == sub.CustomerId && !p.IsPaid && p.StatusId != 3, ct);
            if (hasUnpaid)
            {
                sub.StatusId = 2;
                suspendedPaid++;
            }
        }
        if (suspendedPaid > 0)
            _logger.LogInformation("Paid subscription suspended (grace + unpaid): {Count}", suspendedPaid);

        if (expiredTrials.Count > 0 || suspendedPaid > 0)
            await db.SaveChangesAsync(ct);
    }
}
