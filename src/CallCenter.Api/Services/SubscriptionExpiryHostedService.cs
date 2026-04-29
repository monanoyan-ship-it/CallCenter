using CallCenter.Data;
using CallCenter.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

/// <summary>
/// Periyodik trial/abonelik suresi dolma kontrolu.
/// Trial: NextBillingDate + TrialSuspensionGraceDays (5) sonrasi askiya.
/// Ucretli: odenmemis en eski tahakkugun PeriodStartDate + UnpaidGraceDays (5) sonrasi askiya.
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

        // 1) Trial: deneme bitiminden sonra ek N gun (Policy), sonra askiya
        var trialGrace = PlatformBillingAccessPolicy.TrialSuspensionGraceDays;
        var expiredTrials = await db.CustomerSubscriptions
            .Where(s => s.StatusId == 1 && s.MonthlyPrice == 0 && s.NextBillingDate.AddDays(trialGrace) < now)
            .ToListAsync(ct);
        foreach (var sub in expiredTrials) sub.StatusId = 2;
        if (expiredTrials.Count > 0)
            _logger.LogInformation("Trial subscription expired (grace {Grace}d): {Count}", trialGrace, expiredTrials.Count);

        // 2) Ucretli: odenmemis tahakkukda PeriodStartDate + Policy gun asildiysa askiya
        var activePaid = await db.CustomerSubscriptions
            .Where(s => s.StatusId == 1 && s.MonthlyPrice > 0)
            .ToListAsync(ct);
        int suspendedPaid = 0;
        foreach (var sub in activePaid)
        {
            var oldestUnpaid = await db.CustomerBillingPeriods
                .Where(p => p.CustomerId == sub.CustomerId && !p.IsPaid && p.StatusId != BillingPeriodStatuses.Ids.Paid
                    && p.Amount + p.ServiceAmount > 0m)
                .OrderBy(p => p.Year).ThenBy(p => p.Month)
                .FirstOrDefaultAsync(ct);
            if (oldestUnpaid == null) continue;
            var overdueLimit = oldestUnpaid.PeriodStartDate.AddDays(PlatformBillingAccessPolicy.UnpaidGraceDaysAfterPeriodStart);
            if (overdueLimit >= now) continue;
            sub.StatusId = 2;
            suspendedPaid++;
        }
        if (suspendedPaid > 0)
            _logger.LogInformation("Paid subscription suspended (grace + unpaid): {Count}", suspendedPaid);

        if (expiredTrials.Count > 0 || suspendedPaid > 0)
            await db.SaveChangesAsync(ct);
    }
}
