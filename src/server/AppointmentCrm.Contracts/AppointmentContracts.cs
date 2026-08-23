using System.ComponentModel.DataAnnotations;

namespace AppointmentCrm.Contracts;

public sealed record CreateAppointmentRequest(
    [NotEmptyGuid] Guid CustomerId,
    [NotEmptyGuid] Guid EmployeeId,
    [NotEmptyGuid] Guid ServiceId,
    DateTimeOffset StartsAtUtc,
    [StringLength(1_000)] string? Notes);

public sealed record RescheduleAppointmentRequest(
    DateTimeOffset StartsAtUtc,
    [Required, Range(1, long.MaxValue)] long? ExpectedRevision);

public sealed record TransitionAppointmentRequest(
    [Required, Range(1, long.MaxValue)] long? ExpectedRevision,
    [StringLength(500)] string? Reason);

public sealed record AppointmentSummaryResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid EmployeeId,
    string EmployeeName,
    Guid ServiceId,
    string ServiceName,
    int ServiceDurationMinutes,
    decimal ServicePrice,
    string ServiceCurrency,
    string Status,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset LocalStart,
    DateTimeOffset LocalEnd,
    string TimeZone,
    string? Notes,
    long Revision,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AppointmentStatusHistoryResponse(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    string ActorName,
    string? Reason,
    DateTimeOffset OccurredAtUtc);

public sealed record AppointmentResponse(
    AppointmentSummaryResponse Appointment,
    IReadOnlyList<AppointmentStatusHistoryResponse> StatusHistory);
