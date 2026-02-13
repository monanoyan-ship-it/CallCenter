using CallCenter.Data;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Api.Services;

/// <summary>
/// AuditLogs partition bakım servisi.
/// Uygulama açıkken arka planda çalışır:
/// - Gelecek 3 ayın partition'larını oluşturur
/// - KVKK 3 yıl (36 ay) saklama süresi dolmuş partition'ları siler
/// Kontrol sıklığı: Günde bir (varsayılan).
/// </summary>
public class AuditPartitionMaintenanceService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditPartitionMaintenanceService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public AuditPartitionMaintenanceService(
        IServiceScopeFactory scopeFactory,
        ILogger<AuditPartitionMaintenanceService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İlk çalıştırma: Uygulama başladıktan 30 saniye sonra (diğer servisler ayağa kalksın)
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunMaintenanceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuditLogs partition bakımı sırasında hata oluştu");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task RunMaintenanceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        _logger.LogInformation("AuditLogs partition bakımı başlatılıyor...");

        // maintain_audit_partitions() fonksiyonu:
        // 1. Gelecek 3 ayın partition'larını oluşturur (varsa atlar)
        // 2. 36 aydan eski partition'ları siler (KVKK uyumu)
        await db.Database.ExecuteSqlRawAsync(
            "SELECT maintain_audit_partitions()", ct);

        _logger.LogInformation("AuditLogs partition bakımı tamamlandı");
    }
}
