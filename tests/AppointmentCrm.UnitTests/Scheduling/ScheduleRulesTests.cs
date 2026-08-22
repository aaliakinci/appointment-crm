using AppointmentCrm.Domain.Scheduling;

namespace AppointmentCrm.UnitTests.Scheduling;

public sealed class ScheduleRulesTests
{
    [Fact]
    public void WeeklySchedule_AllowsAdjacentPeriods()
    {
        Guid actorUserId = Guid.NewGuid();
        Guid actorMembershipId = Guid.NewGuid();
        WeeklySchedule schedule = WeeklySchedule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            WeeklyScheduleVersionMode.Custom,
            [
                new SchedulePeriodDefinition(1, 10 * 60, (10 * 60) + 30),
                new SchedulePeriodDefinition(1, (10 * 60) + 30, 11 * 60),
            ],
            actorUserId,
            actorMembershipId,
            null,
            DateTimeOffset.UtcNow);

        WeeklyScheduleVersion version = Assert.Single(schedule.Versions);
        Assert.Equal(2, version.Periods.Count);
        Assert.Equal(1, schedule.Revision);
        Assert.Equal(version.Id, schedule.CurrentVersionId);
    }

    [Fact]
    public void WeeklySchedule_RejectsRealOverlap()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            WeeklySchedule.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                WeeklyScheduleVersionMode.Custom,
                [
                    new SchedulePeriodDefinition(1, 10 * 60, 11 * 60),
                    new SchedulePeriodDefinition(1, (10 * 60) + 30, (11 * 60) + 30),
                ],
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                DateTimeOffset.UtcNow));

        Assert.Contains("cannot overlap", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WeeklySchedule_PublishAppendsSnapshotAndAdvancesCurrentPointer()
    {
        Guid actorUserId = Guid.NewGuid();
        Guid actorMembershipId = Guid.NewGuid();
        WeeklySchedule schedule = WeeklySchedule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            WeeklyScheduleVersionMode.Custom,
            [new SchedulePeriodDefinition(1, 9 * 60, 17 * 60)],
            actorUserId,
            actorMembershipId,
            "Initial",
            DateTimeOffset.UtcNow);
        WeeklyScheduleVersion first = Assert.Single(schedule.Versions);

        WeeklyScheduleVersion second = schedule.Publish(
            Guid.NewGuid(),
            WeeklyScheduleVersionMode.Closed,
            [],
            actorUserId,
            actorMembershipId,
            "Closed for now",
            first.Id,
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(2, schedule.Revision);
        Assert.Equal(second.Id, schedule.CurrentVersionId);
        Assert.Equal(2, schedule.Versions.Count);
        Assert.Equal(WeeklyScheduleVersionMode.Custom, first.Mode);
        Assert.Single(first.Periods);
        Assert.Equal(first.Id, second.RestoredFromVersionId);
    }

    [Fact]
    public void TenantWeeklySchedule_CannotPublishInheritedMode()
    {
        Assert.Throws<ArgumentException>(() => WeeklySchedule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            WeeklyScheduleVersionMode.Inherited,
            [],
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void DateOverride_RequiresPeriodsWhenOpenAndNoneWhenClosed()
    {
        Assert.Throws<ArgumentException>(() => DateScheduleOverride.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            new DateOnly(2026, 8, 22),
            isClosed: false,
            [],
            DateTimeOffset.UtcNow));

        Assert.Throws<ArgumentException>(() => DateScheduleOverride.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            new DateOnly(2026, 8, 22),
            isClosed: true,
            [new SchedulePeriodDefinition(0, 9 * 60, 10 * 60)],
            DateTimeOffset.UtcNow));
    }
}
