using System.Text.Json;
using System.Threading.Channels;
using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Services;

/// <summary>
/// Channel-based webhook event publisher (Singleton).
/// Factory'ler event olustugunda Publish() cagirir — non-blocking.
/// WebhookDispatchService bu channel'dan okuyup HTTP POST yapar.
/// </summary>
public class WebhookEventPublisher
{
    private readonly Channel<WebhookDispatchItem> _channel;

    public WebhookEventPublisher()
    {
        // Bounded channel — 10K event kuyrugu, dolu ise en eski drop edilir
        _channel = Channel.CreateBounded<WebhookDispatchItem>(new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        });
    }

    /// <summary>Event kuyruya ekler (non-blocking). Factory'lerden cagirilir.</summary>
    public void Publish(int customerId, Guid customerUid, string eventType, object eventData)
    {
        var payload = new WebhookEventPayload
        {
            Event = eventType,
            EventId = $"evt_{Guid.NewGuid():N}",
            Timestamp = DateTime.UtcNow,
            CustomerUid = customerUid.ToString(),
            Data = eventData
        };

        var item = new WebhookDispatchItem
        {
            CustomerId = customerId,
            EventType = eventType,
            EventId = payload.EventId,
            PayloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false
            })
        };

        // TryWrite non-blocking — channel dolu ise en eski event drop edilir
        _channel.Writer.TryWrite(item);
    }

    /// <summary>Dispatch service tarafindan okunur</summary>
    public ChannelReader<WebhookDispatchItem> Reader => _channel.Reader;
}

/// <summary>Channel'a yazilan item — dispatch service bunu okur</summary>
public class WebhookDispatchItem
{
    public int CustomerId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}
