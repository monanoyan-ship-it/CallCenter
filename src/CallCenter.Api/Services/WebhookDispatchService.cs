using System.Security.Cryptography;
using System.Text;
using CallCenter.Api.EntityServices.Interfaces;
using CallCenter.Api.Infrastructure;
using CallCenter.Shared.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace CallCenter.Api.Services;

/// <summary>
/// BackgroundService: WebhookEventPublisher channel'indan event okur,
/// aktif aboneliklere HTTP POST yapar, basarisiz gonderimleri retry eder.
/// Retry stratejisi: 1dk, 5dk, 30dk, 2saat, 24saat (5 deneme, exponential backoff).
/// HMAC-SHA256 imzalama ile guvenlik saglanir.
/// </summary>
public class WebhookDispatchService : BackgroundService
{
    private readonly WebhookEventPublisher _publisher;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookDispatchService> _logger;
    private readonly HttpClient _httpClient;

    // Retry araliklari (dakika cinsinden)
    private static readonly int[] RetryDelays = { 1, 5, 30, 120, 1440 }; // 1dk, 5dk, 30dk, 2saat, 24saat
    private const int MaxAttempts = 5;

    public WebhookDispatchService(
        WebhookEventPublisher publisher,
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookDispatchService> logger)
    {
        _publisher = publisher;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebhookDispatchService baslatildi.");

        // Iki gorev paralel calisir: yeni eventler + retry
        var dispatchTask = DispatchNewEventsAsync(stoppingToken);
        var retryTask = RetryFailedDeliveriesAsync(stoppingToken);

        await Task.WhenAll(dispatchTask, retryTask);
    }

    /// <summary>Channel'dan yeni eventleri okuyup gonderir</summary>
    private async Task DispatchNewEventsAsync(CancellationToken ct)
    {
        await foreach (var item in _publisher.Reader.ReadAllAsync(ct))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var webhookEs = scope.ServiceProvider.GetRequiredService<IWebhookSubscriptionEntityService>();
                var deliveryEs = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryEntityService>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Bu musteri + event tipi icin aktif abonelikleri bul
                var subscriptions = await webhookEs.GetActiveByCustomerAndEventAsync(item.CustomerId, item.EventType);
                if (subscriptions.Count == 0) continue;

                foreach (var sub in subscriptions)
                {
                    // Idempotency kontrolu
                    if (await deliveryEs.IsEventDeliveredAsync(sub.Id, item.EventId))
                        continue;

                    var delivery = new WebhookDelivery
                    {
                        WebhookSubscriptionId = sub.Id,
                        EventType = item.EventType,
                        EventId = item.EventId,
                        Payload = item.PayloadJson,
                        Attempt = 1
                    };

                    await SendWebhookAsync(delivery, sub);
                    deliveryEs.Add(delivery);
                    await uow.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook dispatch hatasi: {EventType} {EventId}", item.EventType, item.EventId);
            }
        }
    }

    /// <summary>Basarisiz gonderimleri periyodik retry eder (60sn aralik)</summary>
    private async Task RetryFailedDeliveriesAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var deliveryEs = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryEntityService>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var pending = await deliveryEs.GetPendingRetriesAsync(50);
                if (pending.Count == 0) continue;

                foreach (var delivery in pending)
                {
                    if (delivery.WebhookSubscription == null) continue;
                    await SendWebhookAsync(delivery, delivery.WebhookSubscription);
                    deliveryEs.Update(delivery);
                }

                await uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook retry hatasi.");
            }
        }
    }

    /// <summary>Tek bir webhook'u HTTP POST ile gonderir</summary>
    private async Task SendWebhookAsync(WebhookDelivery delivery, WebhookSubscription subscription)
    {
        try
        {
            var content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json");

            // HMAC-SHA256 imza ekle
            if (!string.IsNullOrEmpty(subscription.Secret))
            {
                var signature = ComputeHmacSha256(delivery.Payload, subscription.Secret);
                content.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
            }

            content.Headers.Add("X-Webhook-Event", delivery.EventType);
            content.Headers.Add("X-Webhook-Id", delivery.EventId);

            var response = await _httpClient.PostAsync(subscription.TargetUrl, content);
            delivery.StatusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                delivery.DeliveredAt = DateTime.UtcNow;
                delivery.NextRetryAt = null;
                _logger.LogInformation("Webhook gonderildi: {EventType} -> {Url} ({StatusCode})",
                    delivery.EventType, subscription.TargetUrl, delivery.StatusCode);
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                delivery.ResponseBody = responseBody.Length > 4000 ? responseBody[..4000] : responseBody;
                delivery.ErrorMessage = $"HTTP {delivery.StatusCode}";
                ScheduleRetry(delivery);
            }
        }
        catch (TaskCanceledException)
        {
            delivery.ErrorMessage = "Timeout (10s)";
            ScheduleRetry(delivery);
        }
        catch (HttpRequestException ex)
        {
            delivery.ErrorMessage = $"Baglanti hatasi: {ex.Message}";
            ScheduleRetry(delivery);
        }
        catch (Exception ex)
        {
            delivery.ErrorMessage = $"Beklenmeyen hata: {ex.Message}";
            ScheduleRetry(delivery);
        }
    }

    /// <summary>Sonraki retry zamanini hesapla</summary>
    private static void ScheduleRetry(WebhookDelivery delivery)
    {
        if (delivery.Attempt >= MaxAttempts)
        {
            // Maks deneme asildi — birak
            delivery.NextRetryAt = null;
            delivery.ErrorMessage += " (max deneme asildi)";
            return;
        }

        var delayMinutes = RetryDelays[Math.Min(delivery.Attempt - 1, RetryDelays.Length - 1)];
        delivery.Attempt++;
        delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
    }

    /// <summary>HMAC-SHA256 imzalama</summary>
    private static string ComputeHmacSha256(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToHexStringLower(hash);
    }
}
