using AppointmentCrm.Application.Scheduling;

namespace AppointmentCrm.UnitTests.Scheduling;

public sealed class AvailabilityCalculatorTests
{
    [Fact]
    public void Calculate_UsesHalfOpenTimeOffBoundaries()
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var date = new DateOnly(2026, 8, 24);
        DateTimeOffset tenUtc = LocalUtc(date, new TimeOnly(10, 0), timeZone);
        DateTimeOffset tenThirtyUtc = LocalUtc(date, new TimeOnly(10, 30), timeZone);

        IReadOnlyList<AvailabilitySlot> slots = AvailabilityCalculator.Calculate(
            date,
            timeZone,
            30,
            [new AvailabilityPeriod(10 * 60, 11 * 60)],
            [new UnavailableInterval(tenUtc, tenThirtyUtc)]);

        AvailabilitySlot slot = Assert.Single(slots);
        Assert.Equal(new TimeOnly(10, 30), TimeOnly.FromDateTime(slot.LocalStart.DateTime));
    }

    [Fact]
    public void Calculate_StopsAtTheLocalMidnightBoundary()
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var date = new DateOnly(2026, 8, 24);

        IReadOnlyList<AvailabilitySlot> slots = AvailabilityCalculator.Calculate(
            date,
            timeZone,
            30,
            [new AvailabilityPeriod((23 * 60) + 30, 24 * 60)],
            []);

        AvailabilitySlot slot = Assert.Single(slots);
        Assert.Equal(new TimeOnly(23, 30), TimeOnly.FromDateTime(slot.LocalStart.DateTime));
        Assert.Equal(date.AddDays(1), DateOnly.FromDateTime(slot.LocalEnd.DateTime));
    }

    [Fact]
    public void Calculate_SkipsNonexistentSpringForwardLocalTimes()
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        var date = new DateOnly(2026, 3, 29);

        IReadOnlyList<AvailabilitySlot> slots = AvailabilityCalculator.Calculate(
            date,
            timeZone,
            30,
            [new AvailabilityPeriod(60, 4 * 60)],
            []);

        Assert.NotEmpty(slots);
        Assert.DoesNotContain(slots, slot => slot.LocalStart.Hour == 2);
    }

    [Fact]
    public void Calculate_ReturnsBothOffsetsForRepeatedFallBackLocalTime()
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        var date = new DateOnly(2026, 10, 25);

        IReadOnlyList<AvailabilitySlot> slots = AvailabilityCalculator.Calculate(
            date,
            timeZone,
            30,
            [new AvailabilityPeriod(60, 4 * 60)],
            []);
        AvailabilitySlot[] repeated = slots
            .Where(slot => slot.LocalStart.Hour == 2 && slot.LocalStart.Minute == 0)
            .ToArray();

        Assert.Equal(2, repeated.Length);
        Assert.Equal(2, repeated.Select(slot => slot.LocalStart.Offset).Distinct().Count());
        Assert.Equal(2, repeated.Select(slot => slot.StartUtc).Distinct().Count());
    }

    private static DateTimeOffset LocalUtc(
        DateOnly date,
        TimeOnly time,
        TimeZoneInfo timeZone)
    {
        DateTime local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), TimeSpan.Zero);
    }
}
