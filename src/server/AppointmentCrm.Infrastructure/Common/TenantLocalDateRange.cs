namespace AppointmentCrm.Infrastructure.Common;

internal static class TenantLocalDateRange
{
    internal static (DateTimeOffset StartUtc, DateTimeOffset EndUtcExclusive) Resolve(
        DateOnly fromDate,
        DateOnly toDate,
        TimeZoneInfo timeZone)
    {
        if (fromDate > toDate)
        {
            throw new ArgumentOutOfRangeException(nameof(toDate));
        }

        return (
            ResolveStart(fromDate, timeZone),
            ResolveStart(toDate.AddDays(1), timeZone));
    }

    private static DateTimeOffset ResolveStart(DateOnly date, TimeZoneInfo timeZone)
    {
        DateTime local = DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Unspecified);
        while (timeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(1);
        }

        if (timeZone.IsAmbiguousTime(local))
        {
            TimeSpan offset = timeZone.GetAmbiguousTimeOffsets(local).Max();
            return new DateTimeOffset(local, offset).ToUniversalTime();
        }

        return new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(local, timeZone),
            TimeSpan.Zero);
    }
}
