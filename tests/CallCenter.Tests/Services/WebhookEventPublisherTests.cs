using System.Text.Json;
using CallCenter.Api.Services;
using CallCenter.Shared.DTOs;

namespace CallCenter.Tests.Services;

public class WebhookEventPublisherTests
{
    [Fact]
    public void Publish_ShouldWriteToChannel()
    {
        var sut = new WebhookEventPublisher();

        sut.Publish(1, Guid.NewGuid(), "call.started", new { callUid = "test-uid" });

        sut.Reader.TryRead(out var item).Should().BeTrue();
        item.Should().NotBeNull();
        item!.CustomerId.Should().Be(1);
        item.EventType.Should().Be("call.started");
        item.EventId.Should().StartWith("evt_");
        item.PayloadJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Publish_Payload_ShouldContainSnakeCaseJson()
    {
        var sut = new WebhookEventPublisher();
        var customerUid = Guid.NewGuid();

        sut.Publish(1, customerUid, "call.ended", new WebhookCallEventData
        {
            CallUid = "uid-123",
            CallerNumber = "+905551234567",
            Direction = "Inbound",
            DurationSeconds = 120
        });

        sut.Reader.TryRead(out var item);
        item.Should().NotBeNull();

        // snake_case serialization kontrol
        item!.PayloadJson.Should().Contain("\"event\":");
        item.PayloadJson.Should().Contain("\"event_id\":");
        item.PayloadJson.Should().Contain("\"customer_uid\":");
        item.PayloadJson.Should().Contain("call.ended");
    }

    [Fact]
    public void Publish_Multiple_ShouldAllBeReadable()
    {
        var sut = new WebhookEventPublisher();

        for (int i = 0; i < 5; i++)
        {
            sut.Publish(1, Guid.NewGuid(), $"event.{i}", new { index = i });
        }

        var count = 0;
        while (sut.Reader.TryRead(out _))
            count++;

        count.Should().Be(5);
    }

    [Fact]
    public void Publish_UniqueEventIds()
    {
        var sut = new WebhookEventPublisher();

        sut.Publish(1, Guid.NewGuid(), "call.started", new { });
        sut.Publish(1, Guid.NewGuid(), "call.started", new { });

        sut.Reader.TryRead(out var item1);
        sut.Reader.TryRead(out var item2);

        item1!.EventId.Should().NotBe(item2!.EventId);
    }

    [Fact]
    public void Reader_Empty_ShouldReturnFalse()
    {
        var sut = new WebhookEventPublisher();

        sut.Reader.TryRead(out var item).Should().BeFalse();
        item.Should().BeNull();
    }

    [Fact]
    public void Publish_ShouldContainTimestamp()
    {
        var sut = new WebhookEventPublisher();
        var before = DateTime.UtcNow;

        sut.Publish(1, Guid.NewGuid(), "agent.status_changed", new { });

        sut.Reader.TryRead(out var item);
        item.Should().NotBeNull();

        // payload JSON contains timestamp
        item!.PayloadJson.Should().Contain("\"timestamp\":");
    }
}
