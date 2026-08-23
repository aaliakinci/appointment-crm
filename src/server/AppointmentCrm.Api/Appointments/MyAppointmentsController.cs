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
[Route("api/v1/my/appointments")]
[Tags("My Appointments")]
[Authorize]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class MyAppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.AppointmentReadOwn)]
    [ProducesResponseType<PagedResponse<AppointmentSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PagedResponse<AppointmentSummaryResponse>>> ListAsync(
        [FromQuery] AppointmentListQuery query,
        CancellationToken cancellationToken)
    {
        PagedResult<AppointmentSummary> result = await appointmentService.ListAsync(
            query.ToPageRequest(),
            query.ToFilter(),
            AppointmentAccessScope.CurrentEmployee,
            cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpGet("{appointmentId:guid}")]
    [Authorize(Policy = Permissions.AppointmentReadOwn)]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AppointmentResponse>> GetAsync(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        AppointmentDetail appointment = await appointmentService.GetAsync(
            appointmentId,
            AppointmentAccessScope.CurrentEmployee,
            cancellationToken);
        return Ok(appointment.ToResponse());
    }

    [HttpPost("{appointmentId:guid}/confirm")]
    [Authorize(Policy = Permissions.AppointmentTransitionOwn)]
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
    [Authorize(Policy = Permissions.AppointmentTransitionOwn)]
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

    [HttpPost("{appointmentId:guid}/no-show")]
    [Authorize(Policy = Permissions.AppointmentTransitionOwn)]
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
            AppointmentAccessScope.CurrentEmployee,
            cancellationToken);
        return Ok(appointment.ToResponse());
    }
}
