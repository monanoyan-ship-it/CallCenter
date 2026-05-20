using CallCenter.Shared.Entities;
using CallCenter.Shared.Enums;

namespace CallCenter.Tests.Enums;

public class SlnWaitlistStatusesTests
{
    [Fact]
    public void All_StatusIds_AreDefinedAndUnique()
    {
        SlnWaitlistStatuses.All.Select(s => s.Id)
            .Should().BeEquivalentTo(new[]
            {
                SlnWaitlistStatuses.Ids.Waiting,
                SlnWaitlistStatuses.Ids.Notified,
                SlnWaitlistStatuses.Ids.AppointmentBooked,
                SlnWaitlistStatuses.Ids.Cancelled,
                SlnWaitlistStatuses.Ids.Completed
            });
        SlnWaitlistStatuses.All.Select(s => s.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void LifecycleHelpers_DefineActiveArchiveAndTerminalSets()
    {
        SlnWaitlistStatuses.IsActive(SlnWaitlistStatuses.Ids.Waiting).Should().BeTrue();
        SlnWaitlistStatuses.IsActive(SlnWaitlistStatuses.Ids.Notified).Should().BeTrue();
        SlnWaitlistStatuses.IsArchived(SlnWaitlistStatuses.Ids.AppointmentBooked).Should().BeTrue();
        SlnWaitlistStatuses.IsArchived(SlnWaitlistStatuses.Ids.Cancelled).Should().BeTrue();
        SlnWaitlistStatuses.IsArchived(SlnWaitlistStatuses.Ids.Completed).Should().BeTrue();
        SlnWaitlistStatuses.IsTerminal(SlnWaitlistStatuses.Ids.Cancelled).Should().BeTrue();
        SlnWaitlistStatuses.IsTerminal(SlnWaitlistStatuses.Ids.Completed).Should().BeTrue();
        SlnWaitlistStatuses.IsDefined(999).Should().BeFalse();
    }

    [Fact]
    public void Entity_DefaultStatus_IsWaiting()
    {
        new SlnWaitlistEntry().StatusId.Should().Be(SlnWaitlistStatuses.Ids.Waiting);
    }
}
