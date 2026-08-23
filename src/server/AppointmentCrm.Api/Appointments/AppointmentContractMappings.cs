using AppointmentCrm.Application.Appointments;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Contracts;
using AppointmentCrm.Domain.Appointments;

namespace AppointmentCrm.Api.Appointments;

internal static class AppointmentContractMappings
{
    public static CreateAppointmentInput ToInput(this CreateAppointmentRequest request) =>
        new(
            request.CustomerId,
            request.EmployeeId,
            request.ServiceId,
            request.StartsAtUtc,
            request.Notes);

    public static RescheduleAppointmentInput ToInput(this RescheduleAppointmentRequest request) =>
        new(request.StartsAtUtc, request.ExpectedRevision!.Value);

    public static TransitionAppointmentInput ToInput(this TransitionAppointmentRequest request) =>
        new(request.ExpectedRevision!.Value, request.Reason);

    public static PagedResponse<AppointmentSummaryResponse> ToResponse(
        this PagedResult<AppointmentSummary> result) =>
        new(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);

    public static AppointmentResponse ToResponse(this AppointmentDetail detail) =>
        new(
            detail.Appointment.ToResponse(),
            detail.StatusHistory.Select(history => new AppointmentStatusHistoryResponse(
                    history.Id,
                    history.FromStatus.HasValue ? StatusValue(history.FromStatus.Value) : null,
                    StatusValue(history.ToStatus),
                    history.ActorName,
                    history.Reason,
                    history.OccurredAtUtc))
                .ToList());

    private static AppointmentSummaryResponse ToResponse(this AppointmentSummary appointment) =>
        new(
            appointment.Id,
            appointment.CustomerId,
            appointment.CustomerName,
            appointment.EmployeeId,
            appointment.EmployeeName,
            appointment.ServiceId,
            appointment.ServiceName,
            appointment.ServiceDurationMinutes,
            appointment.ServicePrice,
            appointment.ServiceCurrency,
            StatusValue(appointment.Status),
            appointment.StartsAtUtc,
            appointment.EndsAtUtc,
            appointment.LocalStart,
            appointment.LocalEnd,
            appointment.TimeZone,
            appointment.Notes,
            appointment.Revision,
            appointment.CreatedAtUtc,
            appointment.UpdatedAtUtc);

    private static string StatusValue(AppointmentStatus status) =>
        status == AppointmentStatus.NoShow
            ? "no-show"
            : status.ToString().ToLowerInvariant();
}
