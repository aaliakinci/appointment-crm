namespace AppointmentCrm.Domain.Scheduling;

public readonly record struct SchedulePeriodDefinition(
    int DayOfWeek,
    int StartMinute,
    int EndMinute);

internal static class SchedulePeriodRules
{
    public const int MinutesPerDay = 24 * 60;
    public const int MinuteIncrement = 5;

    public static void Validate(
        IReadOnlyCollection<SchedulePeriodDefinition> periods,
        bool includesDayOfWeek)
    {
        foreach (SchedulePeriodDefinition period in periods)
        {
            if (includesDayOfWeek && period.DayOfWeek is < 1 or > 7)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(periods),
                    "DayOfWeek must use ISO-8601 values between 1 (Monday) and 7 (Sunday).");
            }

            if (period.StartMinute is < 0 or >= MinutesPerDay
                || period.EndMinute is <= 0 or > MinutesPerDay
                || period.StartMinute >= period.EndMinute
                || period.StartMinute % MinuteIncrement != 0
                || period.EndMinute % MinuteIncrement != 0)
            {
                throw new ArgumentException(
                    "Schedule periods must be ordered, remain within one local day, and use five-minute increments.",
                    nameof(periods));
            }
        }

        IEnumerable<IGrouping<int, SchedulePeriodDefinition>> groups = includesDayOfWeek
            ? periods.GroupBy(period => period.DayOfWeek)
            : periods.GroupBy(_ => 0);

        foreach (IGrouping<int, SchedulePeriodDefinition> group in groups)
        {
            SchedulePeriodDefinition[] ordered = group
                .OrderBy(period => period.StartMinute)
                .ThenBy(period => period.EndMinute)
                .ToArray();
            for (int index = 1; index < ordered.Length; index++)
            {
                if (ordered[index].StartMinute < ordered[index - 1].EndMinute)
                {
                    throw new ArgumentException(
                        "Schedule periods cannot overlap. Adjacent periods are allowed.",
                        nameof(periods));
                }
            }
        }
    }
}
