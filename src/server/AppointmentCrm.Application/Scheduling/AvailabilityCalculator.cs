namespace AppointmentCrm.Application.Scheduling;

public readonly record struct AvailabilityPeriod(int StartMinute, int EndMinute);

public readonly record struct UnavailableInterval(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc);

public static class AvailabilityCalculator
{
    private static readonly TimeSpan CandidateIncrement = TimeSpan.FromMinutes(5);

    public static IReadOnlyList<AvailabilitySlot> Calculate(
        DateOnly localDate,
        TimeZoneInfo timeZone,
        int durationMinutes,
        IReadOnlyCollection<AvailabilityPeriod> workingPeriods,
        IReadOnlyCollection<UnavailableInterval> unavailableIntervals)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(workingPeriods);
        ArgumentNullException.ThrowIfNull(unavailableIntervals);
        if (durationMinutes is < 5 or > 480 || durationMinutes % 5 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        }

        if (workingPeriods.Count == 0)
        {
            return [];
        }

        DateTime utcAnchor = DateTime.SpecifyKind(
            localDate.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
        var searchStart = new DateTimeOffset(utcAnchor).AddHours(-18);
        var searchEnd = new DateTimeOffset(utcAnchor).AddDays(1).AddHours(18);
        TimeSpan duration = TimeSpan.FromMinutes(durationMinutes);
        var slots = new List<AvailabilitySlot>();

        for (DateTimeOffset candidate = searchStart;
             candidate < searchEnd;
             candidate = candidate.Add(CandidateIncrement))
        {
            DateTimeOffset localStart = TimeZoneInfo.ConvertTime(candidate, timeZone);
            if (DateOnly.FromDateTime(localStart.DateTime) != localDate
                || localStart.Second != 0
                || localStart.Millisecond != 0
                || localStart.Minute % 5 != 0)
            {
                continue;
            }

            DateTimeOffset candidateEnd = candidate.Add(duration);
            if (unavailableIntervals.Any(interval =>
                    candidate < interval.EndUtc && candidateEnd > interval.StartUtc)
                || !FitsWorkingPeriods(
                    candidate,
                    candidateEnd,
                    localDate,
                    timeZone,
                    workingPeriods))
            {
                continue;
            }

            slots.Add(new AvailabilitySlot(
                candidate,
                candidateEnd,
                localStart,
                TimeZoneInfo.ConvertTime(candidateEnd, timeZone)));
        }

        return slots;
    }

    private static bool FitsWorkingPeriods(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        DateOnly localDate,
        TimeZoneInfo timeZone,
        IReadOnlyCollection<AvailabilityPeriod> workingPeriods)
    {
        for (DateTimeOffset instant = startUtc;
             instant < endUtc;
             instant = instant.Add(CandidateIncrement))
        {
            DateTimeOffset local = TimeZoneInfo.ConvertTime(instant, timeZone);
            if (DateOnly.FromDateTime(local.DateTime) != localDate)
            {
                return false;
            }

            int minute = (local.Hour * 60) + local.Minute;
            if (!workingPeriods.Any(period =>
                    minute >= period.StartMinute && minute < period.EndMinute))
            {
                return false;
            }
        }

        return true;
    }
}
