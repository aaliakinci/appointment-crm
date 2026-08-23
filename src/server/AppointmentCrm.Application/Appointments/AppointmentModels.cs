using AppointmentCrm.Application.Common;
using AppointmentCrm.Domain.Appointments;

namespace AppointmentCrm.Application.Appointments;

public enum AppointmentAccessScope
{
    Tenant,
    CurrentEmployee,
}

public sealed record AppointmentListFilter(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? EmployeeId,
    Guid? CustomerId,
    AppointmentStatus? Status);

public sealed record CreateAppointmentInput(
    Guid CustomerId,
    Guid EmployeeId,
    Guid ServiceId,
    DateTimeOffset StartsAtUtc,
    string? Notes);

public sealed record RescheduleAppointmentInput(
    DateTimeOffset StartsAtUtc,
    long ExpectedRevision);

public sealed record TransitionAppointmentInput(
    long ExpectedRevision,
    string? Reason);

public sealed record AppointmentSummary(
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
    AppointmentStatus Status,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset LocalStart,
    DateTimeOffset LocalEnd,
    string TimeZone,
    string? Notes,
    long Revision,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AppointmentStatusHistorySummary(
    Guid Id,
    AppointmentStatus? FromStatus,
    AppointmentStatus ToStatus,
    string ActorName,
    string? Reason,
    DateTimeOffset OccurredAtUtc);

public sealed record AppointmentDetail(
    AppointmentSummary Appointment,
    IReadOnlyList<AppointmentStatusHistorySummary> StatusHistory);

public interface IAppointmentService
{
    Task<PagedResult<AppointmentSummary>> ListAsync(
        PageRequest request,
        AppointmentListFilter filter,
        AppointmentAccessScope accessScope,
        CancellationToken cancellationToken);

    Task<AppointmentDetail> GetAsync(
        Guid appointmentId,
        AppointmentAccessScope accessScope,
        CancellationToken cancellationToken);

    Task<PagedResult<AppointmentSummary>> ListCustomerHistoryAsync(
        Guid customerId,
        PageRequest request,
        CancellationToken cancellationToken);

    Task<AppointmentDetail> CreateAsync(
        CreateAppointmentInput input,
        CancellationToken cancellationToken);

    Task<AppointmentDetail> RescheduleAsync(
        Guid appointmentId,
        RescheduleAppointmentInput input,
        CancellationToken cancellationToken);

    Task<AppointmentDetail> TransitionAsync(
        Guid appointmentId,
        AppointmentStatus targetStatus,
        TransitionAppointmentInput input,
        AppointmentAccessScope accessScope,
        CancellationToken cancellationToken);
}
