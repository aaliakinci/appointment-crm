using AppointmentCrm.Domain.Common;

namespace AppointmentCrm.Domain.Scheduling;

public sealed class DateScheduleOverride : ITenantOwnedEntity
{
    private DateScheduleOverride()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid? EmployeeId { get; private set; }

    public DateOnly Date { get; private set; }

    public bool IsClosed { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<DateScheduleOverridePeriod> Periods { get; } = [];

    public static DateScheduleOverride Create(
        Guid id,
        Guid tenantId,
        Guid? employeeId,
        DateOnly date,
        bool isClosed,
        IReadOnlyCollection<SchedulePeriodDefinition> periods,
        DateTimeOffset now)
    {
        var scheduleOverride = new DateScheduleOverride
        {
            Id = id,
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            CreatedAtUtc = now,
        };
        scheduleOverride.Replace(isClosed, periods, now);
        return scheduleOverride;
    }

    public void Replace(
        bool isClosed,
        IReadOnlyCollection<SchedulePeriodDefinition> periods,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(periods);
        if (isClosed && periods.Count > 0)
        {
            throw new ArgumentException(
                "A closed-date override cannot contain working periods.",
                nameof(periods));
        }

        if (!isClosed && periods.Count == 0)
        {
            throw new ArgumentException(
                "An open-date override must contain at least one working period.",
                nameof(periods));
        }

        SchedulePeriodRules.Validate(periods, includesDayOfWeek: false);
        Periods.Clear();
        foreach (SchedulePeriodDefinition period in periods)
        {
            Periods.Add(DateScheduleOverridePeriod.Create(
                Guid.NewGuid(),
                TenantId,
                Id,
                period.StartMinute,
                period.EndMinute));
        }

        IsClosed = isClosed;
        UpdatedAtUtc = now;
    }
}

public sealed class DateScheduleOverridePeriod : ITenantOwnedEntity
{
    private DateScheduleOverridePeriod()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid OverrideId { get; private set; }

    public int StartMinute { get; private set; }

    public int EndMinute { get; private set; }

    public DateScheduleOverride Override { get; private set; } = null!;

    internal static DateScheduleOverridePeriod Create(
        Guid id,
        Guid tenantId,
        Guid overrideId,
        int startMinute,
        int endMinute) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            OverrideId = overrideId,
            StartMinute = startMinute,
            EndMinute = endMinute,
        };
}
