using AppointmentCrm.Domain.Common;

namespace AppointmentCrm.Domain.Scheduling;

public enum WeeklyScheduleVersionMode
{
    Custom,
    Closed,
    Inherited,
}

public sealed class WeeklySchedule : ITenantOwnedEntity
{
    private readonly List<WeeklyScheduleVersion> _versions = [];

    private WeeklySchedule()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid? EmployeeId { get; private set; }

    public Guid CurrentVersionId { get; private set; }

    public long Revision { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<WeeklyScheduleVersion> Versions => _versions;

    public static WeeklySchedule Create(
        Guid id,
        Guid tenantId,
        Guid? employeeId,
        WeeklyScheduleVersionMode mode,
        IReadOnlyCollection<SchedulePeriodDefinition> periods,
        Guid actorUserId,
        Guid actorMembershipId,
        string? changeNote,
        DateTimeOffset now)
    {
        var schedule = new WeeklySchedule
        {
            Id = id,
            TenantId = tenantId,
            EmployeeId = employeeId,
            CreatedAtUtc = now,
        };
        _ = schedule.Publish(
            Guid.NewGuid(),
            mode,
            periods,
            actorUserId,
            actorMembershipId,
            changeNote,
            restoredFromVersionId: null,
            now);
        return schedule;
    }

    public WeeklyScheduleVersion Publish(
        Guid versionId,
        WeeklyScheduleVersionMode mode,
        IReadOnlyCollection<SchedulePeriodDefinition> periods,
        Guid actorUserId,
        Guid actorMembershipId,
        string? changeNote,
        Guid? restoredFromVersionId,
        DateTimeOffset now)
    {
        if (mode == WeeklyScheduleVersionMode.Inherited && !EmployeeId.HasValue)
        {
            throw new ArgumentException(
                "A tenant weekly schedule cannot inherit another schedule.",
                nameof(mode));
        }

        long nextVersion = checked(Revision + 1);
        WeeklyScheduleVersion version = WeeklyScheduleVersion.Create(
            versionId,
            TenantId,
            Id,
            nextVersion,
            mode,
            periods,
            actorUserId,
            actorMembershipId,
            changeNote,
            restoredFromVersionId,
            now);
        _versions.Add(version);
        CurrentVersionId = version.Id;
        Revision = nextVersion;
        UpdatedAtUtc = now;
        return version;
    }
}

public sealed class WeeklyScheduleVersion : ITenantOwnedEntity
{
    private readonly List<WeeklyScheduleVersionPeriod> _periods = [];

    private WeeklyScheduleVersion()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ScheduleId { get; private set; }

    public long VersionNumber { get; private set; }

    public WeeklyScheduleVersionMode Mode { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public Guid? ActorMembershipId { get; private set; }

    public string? ChangeNote { get; private set; }

    public Guid? RestoredFromVersionId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public WeeklySchedule Schedule { get; private set; } = null!;

    public IReadOnlyCollection<WeeklyScheduleVersionPeriod> Periods => _periods;

    internal static WeeklyScheduleVersion Create(
        Guid id,
        Guid tenantId,
        Guid scheduleId,
        long versionNumber,
        WeeklyScheduleVersionMode mode,
        IReadOnlyCollection<SchedulePeriodDefinition> periods,
        Guid actorUserId,
        Guid actorMembershipId,
        string? changeNote,
        Guid? restoredFromVersionId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(periods);
        string? normalizedNote = string.IsNullOrWhiteSpace(changeNote)
            ? null
            : changeNote.Trim();
        if (normalizedNote?.Length > 500)
        {
            throw new ArgumentException(
                "The weekly schedule change note cannot exceed 500 characters.",
                nameof(changeNote));
        }

        if (mode == WeeklyScheduleVersionMode.Custom)
        {
            if (periods.Count == 0)
            {
                throw new ArgumentException(
                    "A custom weekly schedule requires at least one period.",
                    nameof(periods));
            }

            SchedulePeriodRules.Validate(periods, includesDayOfWeek: true);
        }
        else if (periods.Count != 0)
        {
            throw new ArgumentException(
                "Closed and inherited weekly schedules cannot contain periods.",
                nameof(periods));
        }

        var version = new WeeklyScheduleVersion
        {
            Id = id,
            TenantId = tenantId,
            ScheduleId = scheduleId,
            VersionNumber = versionNumber,
            Mode = mode,
            ActorUserId = actorUserId,
            ActorMembershipId = actorMembershipId,
            ChangeNote = normalizedNote,
            RestoredFromVersionId = restoredFromVersionId,
            CreatedAtUtc = now,
        };
        foreach (SchedulePeriodDefinition period in periods)
        {
            version._periods.Add(WeeklyScheduleVersionPeriod.Create(
                Guid.NewGuid(),
                tenantId,
                id,
                period.DayOfWeek,
                period.StartMinute,
                period.EndMinute));
        }

        return version;
    }
}

public sealed class WeeklyScheduleVersionPeriod : ITenantOwnedEntity
{
    private WeeklyScheduleVersionPeriod()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid VersionId { get; private set; }

    public int DayOfWeek { get; private set; }

    public int StartMinute { get; private set; }

    public int EndMinute { get; private set; }

    public WeeklyScheduleVersion Version { get; private set; } = null!;

    internal static WeeklyScheduleVersionPeriod Create(
        Guid id,
        Guid tenantId,
        Guid versionId,
        int dayOfWeek,
        int startMinute,
        int endMinute) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            VersionId = versionId,
            DayOfWeek = dayOfWeek,
            StartMinute = startMinute,
            EndMinute = endMinute,
        };
}
