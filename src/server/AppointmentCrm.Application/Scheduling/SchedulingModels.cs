using AppointmentCrm.Application.Common;

namespace AppointmentCrm.Application.Scheduling;

public sealed record SchedulePeriodInput(
    int DayOfWeek,
    int StartMinute,
    int EndMinute);

public sealed record SchedulePeriodSummary(
    int DayOfWeek,
    int StartMinute,
    int EndMinute);

public sealed record WeeklyScheduleInput(
    long ExpectedRevision,
    IReadOnlyCollection<SchedulePeriodInput> Periods,
    string? ChangeNote);

public sealed record RestoreWeeklyScheduleVersionInput(
    long ExpectedRevision,
    string? ChangeNote);

public sealed record WeeklyScheduleSummary(
    Guid? EmployeeId,
    string State,
    string Source,
    long Revision,
    Guid? VersionId,
    long? VersionNumber,
    Guid? EffectiveVersionId,
    long? EffectiveVersionNumber,
    IReadOnlyList<SchedulePeriodSummary> Periods,
    DateTimeOffset? PublishedAtUtc,
    string? PublishedBy,
    string? ChangeNote);

public sealed record WeeklyScheduleVersionSummary(
    Guid Id,
    long VersionNumber,
    string Mode,
    IReadOnlyList<SchedulePeriodSummary> Periods,
    DateTimeOffset CreatedAtUtc,
    string? PublishedBy,
    string? ChangeNote,
    Guid? RestoredFromVersionId,
    long? RestoredFromVersionNumber);

public sealed record DateOverrideInput(
    bool IsClosed,
    IReadOnlyCollection<SchedulePeriodInput> Periods);

public sealed record DateOverrideSummary(
    Guid Id,
    Guid? EmployeeId,
    DateOnly Date,
    bool IsClosed,
    IReadOnlyList<SchedulePeriodSummary> Periods,
    DateTimeOffset UpdatedAtUtc);

public sealed record LocalTimeOffInput(
    Guid EmployeeId,
    DateOnly StartDate,
    TimeOnly StartTime,
    DateOnly EndDate,
    TimeOnly EndTime,
    string TimeZone,
    string? Reason);

public sealed record TimeOffSummary(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    DateOnly LocalStartDate,
    TimeOnly LocalStartTime,
    DateOnly LocalEndDate,
    TimeOnly LocalEndTime,
    string TimeZone,
    string? Reason);

public sealed record AvailabilityQuery(
    DateOnly Date,
    Guid EmployeeId,
    Guid ServiceId);

public sealed record AvailabilitySlot(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    DateTimeOffset LocalStart,
    DateTimeOffset LocalEnd);

public sealed record AvailabilityDay(
    DateOnly Date,
    Guid EmployeeId,
    Guid ServiceId,
    int ServiceDurationMinutes,
    string TimeZone,
    IReadOnlyList<AvailabilitySlot> Slots);

public interface ISchedulingService
{
    Task<WeeklyScheduleSummary> GetWeeklyScheduleAsync(
        Guid? employeeId,
        CancellationToken cancellationToken);

    Task<WeeklyScheduleSummary> PutWeeklyScheduleAsync(
        Guid? employeeId,
        WeeklyScheduleInput input,
        CancellationToken cancellationToken);

    Task DeleteEmployeeWeeklyScheduleAsync(
        Guid employeeId,
        long expectedRevision,
        string? changeNote,
        CancellationToken cancellationToken);

    Task<PagedResult<WeeklyScheduleVersionSummary>> ListWeeklyScheduleVersionsAsync(
        Guid? employeeId,
        PageRequest request,
        CancellationToken cancellationToken);

    Task<WeeklyScheduleVersionSummary> GetWeeklyScheduleVersionAsync(
        Guid? employeeId,
        Guid versionId,
        CancellationToken cancellationToken);

    Task<WeeklyScheduleSummary> RestoreWeeklyScheduleVersionAsync(
        Guid? employeeId,
        Guid versionId,
        RestoreWeeklyScheduleVersionInput input,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DateOverrideSummary>> ListDateOverridesAsync(
        Guid? employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken);

    Task<DateOverrideSummary> PutDateOverrideAsync(
        Guid? employeeId,
        DateOnly date,
        DateOverrideInput input,
        CancellationToken cancellationToken);

    Task DeleteDateOverrideAsync(
        Guid? employeeId,
        DateOnly date,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TimeOffSummary>> ListTimeOffAsync(
        Guid? employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken);

    Task<TimeOffSummary> CreateTimeOffAsync(
        LocalTimeOffInput input,
        CancellationToken cancellationToken);

    Task DeleteTimeOffAsync(Guid timeOffId, CancellationToken cancellationToken);

    Task<AvailabilityDay> GetAvailabilityAsync(
        AvailabilityQuery query,
        CancellationToken cancellationToken);
}
