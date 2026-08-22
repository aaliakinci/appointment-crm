using System.ComponentModel.DataAnnotations;

namespace AppointmentCrm.Contracts;

public sealed record WeeklySchedulePeriodRequest(
    [Range(1, 7)] int DayOfWeek,
    [Range(0, 1435)] int StartMinute,
    [Range(5, 1440)] int EndMinute);

public sealed record PutWeeklyScheduleRequest(
    [Required, Range(0, long.MaxValue)] long? ExpectedRevision,
    [Required] IReadOnlyList<WeeklySchedulePeriodRequest> Periods,
    [StringLength(500)] string? ChangeNote);

public sealed record RestoreWeeklyScheduleVersionRequest(
    [Required, Range(0, long.MaxValue)] long? ExpectedRevision,
    [StringLength(500)] string? ChangeNote);

public sealed record SchedulePeriodResponse(
    int DayOfWeek,
    int StartMinute,
    int EndMinute);

public sealed record WeeklyScheduleResponse(
    Guid? EmployeeId,
    string State,
    string Source,
    long Revision,
    Guid? VersionId,
    long? VersionNumber,
    Guid? EffectiveVersionId,
    long? EffectiveVersionNumber,
    IReadOnlyList<SchedulePeriodResponse> Periods,
    DateTimeOffset? PublishedAtUtc,
    string? PublishedBy,
    string? ChangeNote);

public sealed record WeeklyScheduleVersionResponse(
    Guid Id,
    long VersionNumber,
    string Mode,
    IReadOnlyList<SchedulePeriodResponse> Periods,
    DateTimeOffset CreatedAtUtc,
    string? PublishedBy,
    string? ChangeNote,
    Guid? RestoredFromVersionId,
    long? RestoredFromVersionNumber);

public sealed record DateSchedulePeriodRequest(
    [Range(0, 1435)] int StartMinute,
    [Range(5, 1440)] int EndMinute);

public sealed record PutDateOverrideRequest(
    bool IsClosed,
    [Required] IReadOnlyList<DateSchedulePeriodRequest> Periods);

public sealed record DateOverrideResponse(
    Guid Id,
    Guid? EmployeeId,
    DateOnly Date,
    bool IsClosed,
    IReadOnlyList<SchedulePeriodResponse> Periods,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateTimeOffRequest(
    [NotEmptyGuid] Guid EmployeeId,
    DateOnly StartDate,
    TimeOnly StartTime,
    DateOnly EndDate,
    TimeOnly EndTime,
    [Required, StringLength(100, MinimumLength = 1)] string TimeZone,
    [StringLength(500)] string? Reason);

public sealed record TimeOffResponse(
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

public sealed record AvailabilitySlotResponse(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    DateTimeOffset LocalStart,
    DateTimeOffset LocalEnd);

public sealed record AvailabilityResponse(
    DateOnly Date,
    Guid EmployeeId,
    Guid ServiceId,
    int ServiceDurationMinutes,
    string TimeZone,
    IReadOnlyList<AvailabilitySlotResponse> Slots);
