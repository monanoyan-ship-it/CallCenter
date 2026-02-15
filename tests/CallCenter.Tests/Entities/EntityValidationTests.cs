using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;

namespace CallCenter.Tests.Entities;

/// <summary>
/// Entity varsayilan degerleri ve davranislarini test eder.
/// </summary>
public class EntityValidationTests
{
    [Fact]
    public void InstantMessage_DefaultValues_AreCorrect()
    {
        var msg = new InstantMessage();

        msg.Uid.Should().NotBeEmpty();
        msg.MessageTypeId.Should().Be(1); // Text
        msg.IsRead.Should().BeFalse();
        msg.ReadAt.Should().BeNull();
        msg.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        msg.Content.Should().BeEmpty();
    }

    [Fact]
    public void Contact_DefaultValues_AreCorrect()
    {
        var contact = new Contact();

        contact.Uid.Should().NotBeEmpty();
        contact.SourceId.Should().Be(ContactSources.Ids.Manual);
        contact.IsFavorite.Should().BeFalse();
        contact.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        contact.FullName.Should().BeEmpty();
        contact.PhoneNumber.Should().BeEmpty();
    }

    [Fact]
    public void CallForwardingRule_DefaultValues_AreCorrect()
    {
        var rule = new CallForwardingRule();

        rule.Uid.Should().NotBeEmpty();
        rule.IsActive.Should().BeTrue();
        rule.TimeoutSeconds.Should().Be(20);
        rule.Priority.Should().Be(0);
        rule.Destination.Should().BeEmpty();
    }

    [Fact]
    public void User_DefaultValues_AreCorrect()
    {
        var user = new User();

        user.Uid.Should().NotBeEmpty();
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
