using AppointmentCrm.Api.Security;
using AppointmentCrm.Application.Appointments;
using AppointmentCrm.Application.Common;
using AppointmentCrm.Application.Identity;
using AppointmentCrm.Contracts;
using AppointmentCrm.Domain.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentCrm.Api.Appointments;

[ApiController]
[Route("api/v1/appointments")]
[Tags("Appointments")]
[Authorize]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.AppointmentRead)]
    [ProducesResponseType<PagedResponse<AppointmentSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PagedResponse<AppointmentSummaryResponse>>> ListAsync(
        [FromQuery] AppointmentListQuery query,
        CancellationToken cancellationToken)
    {
        PagedResult<AppointmentSummary> result = await appointmentService.ListAsync(
            query.ToPageRequest(),
            query.ToFilter(),
            AppointmentAccessScope.Tenant,
            cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpGet("{appointmentId:guid}", Name = "GetAppointmentById")]
    [Authorize(Policy = Permissions.AppointmentRead)]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AppointmentResponse>> GetAsync(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        AppointmentDetail appointment = await appointmentService.GetAsync(
            appointmentId,
            AppointmentAccessScope.Tenant,
            cancellationToken);
        return Ok(appointment.ToResponse());
    }

    [HttpPost]
    [Authorize(Policy = Permissions.AppointmentManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AppointmentResponse>> CreateAsync(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        AppointmentDetail appointment = await appointmentService.CreateAsync(
            request.ToInput(),
            cancellationToken);
        return CreatedAtRoute(
            "GetAppointmentById",
            new { appointmentId = appointment.Appointment.Id },
            appointment.ToResponse());
    }

    [HttpPut("{appointmentId:guid}/schedule")]
    [Authorize(Policy = Permissions.AppointmentManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AppointmentResponse>> RescheduleAsync(
        Guid appointmentId,
        RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        AppointmentDetail appointment = await appointmentService.RescheduleAsync(
            appointmentId,
            request.ToInput(),
            cancellationToken);
        return Ok(appointment.ToResponse());
    }

    [HttpPost("{appointmentId:guid}/confirm")]
    [Authorize(Policy = Permissions.AppointmentManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public Task<ActionResult<AppointmentResponse>> ConfirmAsync(
        Guid appointmentId,
        TransitionAppointmentRequest request,
        CancellationToken cancellationToken) =>
        TransitionAsync(appointmentId, AppointmentStatus.Confirmed, request, cancellationToken);

    [HttpPost("{appointmentId:guid}/complete")]
    [Authorize(Policy = Permissions.AppointmentManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public Task<ActionResult<AppointmentResponse>> CompleteAsync(
        Guid appointmentId,
        TransitionAppointmentRequest request,
        CancellationToken cancellationToken) =>
        TransitionAsync(appointmentId, AppointmentStatus.Completed, request, cancellationToken);

    [HttpPost("{appointmentId:guid}/cancel")]
    [Authorize(Policy = Permissions.AppointmentManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public Task<ActionResult<AppointmentResponse>> CancelAsync(
        Guid appointmentId,
        TransitionAppointmentRequest request,
        CancellationToken cancellationToken) =>
        TransitionAsync(appointmentId, AppointmentStatus.Cancelled, request, cancellationToken);

    [HttpPost("{appointmentId:guid}/no-show")]
    [Authorize(Policy = Permissions.AppointmentManage)]
    [ValidateTrustedOrigin]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public Task<ActionResult<AppointmentResponse>> NoShowAsync(
        Guid appointmentId,
        TransitionAppointmentRequest request,
        CancellationToken cancellationToken) =>
        TransitionAsync(appointmentId, AppointmentStatus.NoShow, request, cancellationToken);

    private async Task<ActionResult<AppointmentResponse>> TransitionAsync(
        Guid appointmentId,
        AppointmentStatus targetStatus,
        TransitionAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        AppointmentDetail appointment = await appointmentService.TransitionAsync(
            appointmentId,
            targetStatus,
            request.ToInput(),
            AppointmentAccessScope.Tenant,
            cancellationToken);
        return Ok(appointment.ToResponse());
    }
}
