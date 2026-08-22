using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Scheduling;
using AppointmentCrm.Contracts;

namespace AppointmentCrm.Api.Scheduling;

internal static class SchedulingContractMappings
{
    internal static WeeklyScheduleInput ToInput(
        this PutWeeklyScheduleRequest request) =>
        new(
            request.ExpectedRevision!.Value,
            request.Periods
                .Select(period => new SchedulePeriodInput(
                    period.DayOfWeek,
                    period.StartMinute,
                    period.EndMinute))
                .ToList(),
            request.ChangeNote);

    internal static RestoreWeeklyScheduleVersionInput ToInput(
        this RestoreWeeklyScheduleVersionRequest request) =>
        new(request.ExpectedRevision!.Value, request.ChangeNote);

    internal static DateOverrideInput ToInput(this PutDateOverrideRequest request) =>
        new(
            request.IsClosed,
            request.Periods
                .Select(period => new SchedulePeriodInput(
                    0,
                    period.StartMinute,
                    period.EndMinute))
                .ToList());

    internal static LocalTimeOffInput ToInput(this CreateTimeOffRequest request) =>
        new(
            request.EmployeeId,
            request.StartDate,
            request.StartTime,
            request.EndDate,
            request.EndTime,
            request.TimeZone,
            request.Reason);

    internal static WeeklyScheduleResponse ToResponse(this WeeklyScheduleSummary summary) =>
        new(
            summary.EmployeeId,
            summary.State,
            summary.Source,
            summary.Revision,
            summary.VersionId,
            summary.VersionNumber,
            summary.EffectiveVersionId,
            summary.EffectiveVersionNumber,
            summary.Periods.Select(period => period.ToResponse()).ToList(),
            summary.PublishedAtUtc,
            summary.PublishedBy,
            summary.ChangeNote);

    internal static WeeklyScheduleVersionResponse ToResponse(
        this WeeklyScheduleVersionSummary summary) =>
        new(
            summary.Id,
            summary.VersionNumber,
            summary.Mode,
            summary.Periods.Select(period => period.ToResponse()).ToList(),
            summary.CreatedAtUtc,
            summary.PublishedBy,
            summary.ChangeNote,
            summary.RestoredFromVersionId,
            summary.RestoredFromVersionNumber);

    internal static PagedResponse<WeeklyScheduleVersionResponse> ToResponse(
        this PagedResult<WeeklyScheduleVersionSummary> result) =>
        new(
            result.Items.Select(version => version.ToResponse()).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);

    internal static DateOverrideResponse ToResponse(this DateOverrideSummary summary) =>
        new(
            summary.Id,
            summary.EmployeeId,
            summary.Date,
            summary.IsClosed,
            summary.Periods.Select(period => period.ToResponse()).ToList(),
            summary.UpdatedAtUtc);

    internal static IReadOnlyList<DateOverrideResponse> ToResponse(
        this IReadOnlyList<DateOverrideSummary> summaries) =>
        summaries.Select(summary => summary.ToResponse()).ToList();

    internal static TimeOffResponse ToResponse(this TimeOffSummary summary) =>
        new(
            summary.Id,
            summary.EmployeeId,
            summary.EmployeeName,
            summary.StartUtc,
            summary.EndUtc,
            summary.LocalStartDate,
            summary.LocalStartTime,
            summary.LocalEndDate,
            summary.LocalEndTime,
            summary.TimeZone,
            summary.Reason);

    internal static IReadOnlyList<TimeOffResponse> ToResponse(
        this IReadOnlyList<TimeOffSummary> summaries) =>
        summaries.Select(summary => summary.ToResponse()).ToList();

    internal static AvailabilityResponse ToResponse(this AvailabilityDay day) =>
        new(
            day.Date,
            day.EmployeeId,
            day.ServiceId,
            day.ServiceDurationMinutes,
            day.TimeZone,
            day.Slots.Select(slot => new AvailabilitySlotResponse(
                slot.StartUtc,
                slot.EndUtc,
                slot.LocalStart,
                slot.LocalEnd)).ToList());

    private static SchedulePeriodResponse ToResponse(this SchedulePeriodSummary period) =>
        new(period.DayOfWeek, period.StartMinute, period.EndMinute);
}
